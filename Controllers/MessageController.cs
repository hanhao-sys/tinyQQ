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
public class MessageController : ControllerBase
{
    private readonly AppDbContext _db;

    public MessageController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取与某好友的聊天记录
    /// </summary>
    [HttpGet("{friendId}")]
    public async Task<IActionResult> GetMessages(int friendId,
        [FromQuery] bool all = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();

        var query = _db.MessageView
            .Where(m => m.MessageTypeID == 1)  // 聊天消息
            .Where(m =>
                (m.FromUserID == userId && m.ToUserID == friendId) ||
                (m.FromUserID == friendId && m.ToUserID == userId));

        if (!all)
            query = query.OrderByDescending(m => m.MessageTime);

        var total = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.MessageTime)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                FromUserID = m.FromUserID,
                Sender = m.Sender,
                ToUserID = m.ToUserID,
                Content = m.MessageContent,
                MessageTypeID = m.MessageTypeID,
                MessageState = m.MessageState,
                MessageTime = m.MessageTime
            })
            .ToListAsync();

        return Ok(new { total, messages });
    }

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();

        var count = await _db.Messages
            .Where(m => m.ToUserID == userId && m.MessageState == 0)
            .CountAsync();

        // 按类型分组
        var chatCount = await _db.Messages
            .Where(m => m.ToUserID == userId && m.MessageTypeID == 1 && m.MessageState == 0)
            .CountAsync();

        var requestCount = await _db.Messages
            .Where(m => m.ToUserID == userId && m.MessageTypeID == 2 && m.MessageState == 0)
            .CountAsync();

        return Ok(new { total = count, chat = chatCount, request = requestCount });
    }

    /// <summary>
    /// HTTP 发送消息（降级方案，主力是 SignalR）
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "消息不能为空" });

        var msg = new Message
        {
            FromUserID = userId,
            ToUserID = req.ToUserId,
            MessageContent = req.Message,
            MessageTypeID = 1,
            MessageState = 0,
            MessageTime = DateTime.Now
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        return Ok(new { id = msg.Id, messageTime = msg.MessageTime });
    }

    /// <summary>
    /// 标记某好友的所有消息为已读
    /// </summary>
    [HttpPut("{friendId}/read")]
    public async Task<IActionResult> MarkRead(int friendId)
    {
        var userId = GetUserId();

        var unread = await _db.Messages
            .Where(m => m.FromUserID == friendId && m.ToUserID == userId && m.MessageState == 0)
            .ToListAsync();

        foreach (var m in unread)
            m.MessageState = 1;

        await _db.SaveChangesAsync();
        return Ok(new { count = unread.Count });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }
}
