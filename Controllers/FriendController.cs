using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tinyQQ.Web.Data;
using tinyQQ.Web.Models;

namespace tinyQQ.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendController : ControllerBase
{
    private readonly AppDbContext _db;

    public FriendController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取我的好友列表（含在线状态）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();

        var friends = await _db.Friends
            .Where(f => f.HostID == userId)
            .Join(_db.Users,
                f => f.FriendID,
                u => u.Id,
                (f, u) => FriendDto.FromUser(u))
            .ToListAsync();

        return Ok(friends);
    }

    /// <summary>
    /// 搜索用户（排除自己）
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? keyword,
        [FromQuery] string? sex,
        [FromQuery] string? ageRange)
    {
        var userId = GetUserId();

        var query = _db.Users.Where(u => u.Id != userId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // 按 ID 或昵称搜索
            if (int.TryParse(keyword, out var id))
                query = query.Where(u => u.Id == id || (u.NickName != null && u.NickName.Contains(keyword)));
            else
                query = query.Where(u => u.NickName != null && u.NickName.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(sex))
            query = query.Where(u => u.Sex == sex);
        if (!string.IsNullOrWhiteSpace(ageRange))
        {
            // 格式: "10-19" 或 "50+"
            var parts = ageRange.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                query = query.Where(u => u.Age >= min && u.Age <= max);
            else if (ageRange.EndsWith("+") && int.TryParse(ageRange.TrimEnd('+'), out var minPlus))
                query = query.Where(u => u.Age >= minPlus);
        }

        var users = await query
            .Select(u => FriendDto.FromUser(u))
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// 发送好友请求 — 在 tb_Message 插入 MessageTypeID=2 的消息
    /// </summary>
    [HttpPost("request/{friendId}")]
    public async Task<IActionResult> SendFriendRequest(int friendId)
    {
        var userId = GetUserId();
        if (friendId == userId)
            return BadRequest(new { error = "不能添加自己为好友" });

        // 检查是否已是好友
        var alreadyFriend = await _db.Friends.AnyAsync(f =>
            f.HostID == userId && f.FriendID == friendId);
        if (alreadyFriend)
            return BadRequest(new { error = "对方已经是您的好友" });

        // 插入好友请求消息
        var msg = new Message
        {
            FromUserID = userId,
            ToUserID = friendId,
            MessageContent = "",
            MessageTypeID = 2,   // 好友请求
            MessageState = 0,     // 未读
            MessageTime = DateTime.Now
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        return Ok(new { message = "已发出好友请求！" });
    }

    /// <summary>
    /// 获取待处理的好友请求
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = GetUserId();

        var requests = await _db.Messages
            .Where(m => m.ToUserID == userId && m.MessageTypeID == 2 && m.MessageState == 0)
            .Join(_db.Users,
                m => m.FromUserID,
                u => u.Id,
                (m, u) => new FriendRequestDto
                {
                    MessageId = m.Id,
                    FromUserId = m.FromUserID,
                    FromNickName = u.NickName,
                    MessageTime = m.MessageTime
                })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// 同意好友请求 — 标记消息已读 + 双向插入 tb_Friend
    /// </summary>
    [HttpPost("approve/{messageId}")]
    public async Task<IActionResult> ApproveRequest(int messageId)
    {
        var userId = GetUserId();

        var msg = await _db.Messages.FindAsync(messageId);
        if (msg == null || msg.ToUserID != userId)
            return NotFound(new { error = "请求不存在" });

        // 标记已读
        msg.MessageState = 1;

        // 双向添加好友
        var existing1 = await _db.Friends.FindAsync(msg.FromUserID, userId);
        var existing2 = await _db.Friends.FindAsync(userId, msg.FromUserID);

        if (existing1 == null)
            _db.Friends.Add(new Friend { HostID = msg.FromUserID, FriendID = userId });
        if (existing2 == null)
            _db.Friends.Add(new Friend { HostID = userId, FriendID = msg.FromUserID });

        await _db.SaveChangesAsync();
        return Ok(new { message = "已添加为好友" });
    }

    /// <summary>
    /// 拒绝好友请求
    /// </summary>
    [HttpPost("reject/{messageId}")]
    public async Task<IActionResult> RejectRequest(int messageId)
    {
        var userId = GetUserId();
        var msg = await _db.Messages.FindAsync(messageId);
        if (msg == null || msg.ToUserID != userId)
            return NotFound(new { error = "请求不存在" });

        msg.MessageState = 1;
        await _db.SaveChangesAsync();
        return Ok(new { message = "已拒绝" });
    }

    /// <summary>
    /// 删除好友 — 双向删除
    /// </summary>
    [HttpDelete("{friendId}")]
    public async Task<IActionResult> DeleteFriend(int friendId)
    {
        var userId = GetUserId();

        var f1 = await _db.Friends.FindAsync(userId, friendId);
        var f2 = await _db.Friends.FindAsync(friendId, userId);

        if (f1 != null) _db.Friends.Remove(f1);
        if (f2 != null) _db.Friends.Remove(f2);

        await _db.SaveChangesAsync();
        return Ok(new { message = "好友已删除" });
    }

    // ===== 辅助 =====

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }
}
