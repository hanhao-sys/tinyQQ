using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using tinyQQ.Web.Data;
using tinyQQ.Web.Models;

namespace tinyQQ.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 用户连接后加入自己的组（用于私信推送）
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // 更新在线状态
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Flag = 1;
            await _db.SaveChangesAsync();
        }

        // 通知好友上线
        await NotifyFriendsOnlineStatus(userId, 1);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 断开连接时设置离线
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Flag = 0;
            await _db.SaveChangesAsync();
        }

        // 通知好友离线
        await NotifyFriendsOnlineStatus(userId, 0);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 发送聊天消息 — 存数据库 + 实时推送给接收方
    /// </summary>
    public async Task SendMessage(int toUserId, string messageContent)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(messageContent))
            return;

        // 获取发送者昵称
        var sender = await _db.Users.FindAsync(userId);
        var senderName = sender?.NickName ?? userId.ToString();

        // 存入数据库
        var msg = new Message
        {
            FromUserID = userId,
            ToUserID = toUserId,
            MessageContent = messageContent,
            MessageTypeID = 1,   // 聊天消息
            MessageState = 0,     // 未读
            MessageTime = DateTime.Now
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        // 推送给接收方
        await Clients.Group($"user_{toUserId}").SendAsync("ReceiveMessage", new
        {
            id = msg.Id,
            fromUserId = userId,
            sender = senderName,
            toUserId = toUserId,
            content = messageContent,
            messageTypeId = 1,
            messageState = 0,
            messageTime = msg.MessageTime
        });

        // 同时推送给发送方（确认消息已送达）
        await Clients.Group($"user_{userId}").SendAsync("MessageSent", new
        {
            id = msg.Id,
            toUserId = toUserId,
            content = messageContent,
            messageTime = msg.MessageTime
        });

        // 推送给接收方：未读消息数更新
        var unreadCount = await _db.Messages
            .Where(m => m.ToUserID == toUserId && m.MessageState == 0)
            .CountAsync();
        await Clients.Group($"user_{toUserId}").SendAsync("UnreadUpdate", unreadCount);
    }

    /// <summary>
    /// 发送好友请求
    /// </summary>
    public async Task SendFriendRequest(int toUserId)
    {
        var userId = GetUserId();
        var sender = await _db.Users.FindAsync(userId);
        var senderName = sender?.NickName ?? userId.ToString();

        // 检查是否已是好友
        var already = await _db.Friends.AnyAsync(f => f.HostID == userId && f.FriendID == toUserId);
        if (already) return;

        var msg = new Message
        {
            FromUserID = userId,
            ToUserID = toUserId,
            MessageContent = "",
            MessageTypeID = 2,
            MessageState = 0,
            MessageTime = DateTime.Now
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        // 推送给接收方
        await Clients.Group($"user_{toUserId}").SendAsync("FriendRequest", new
        {
            messageId = msg.Id,
            fromUserId = userId,
            fromNickName = senderName,
            messageTime = msg.MessageTime
        });
    }

    /// <summary>
    /// 正在输入状态
    /// </summary>
    public async Task Typing(int toUserId)
    {
        var userId = GetUserId();
        var sender = await _db.Users.FindAsync(userId);
        await Clients.Group($"user_{toUserId}").SendAsync("UserTyping", new
        {
            userId = userId,
            nickName = sender?.NickName
        });
    }

    // ===== 辅助方法 =====

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }

    /// <summary>
    /// 通知所有好友在线/离线状态变化
    /// </summary>
    private async Task NotifyFriendsOnlineStatus(int userId, int flag)
    {
        var friendIds = await _db.Friends
            .Where(f => f.HostID == userId)
            .Select(f => f.FriendID)
            .ToListAsync();

        foreach (var fid in friendIds)
        {
            await Clients.Group($"user_{fid}").SendAsync("FriendStatusChanged", new
            {
                userId = userId,
                flag = flag
            });
        }
    }
}
