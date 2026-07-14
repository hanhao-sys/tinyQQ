using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tinyQQ.Web.Data;
using tinyQQ.Web.Models;

namespace tinyQQ.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    // ===== 用户管理 =====

    /// <summary>
    /// 查询所有用户
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? id,
        [FromQuery] string? nickName,
        [FromQuery] string? valid)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(id))
            query = query.Where(u => u.Id.ToString().Contains(id));
        if (!string.IsNullOrWhiteSpace(nickName))
            query = query.Where(u => u.NickName != null && u.NickName.Contains(nickName));
        if (!string.IsNullOrWhiteSpace(valid))
            query = query.Where(u => u.Valid.ToString() == valid);

        var users = await query
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.NickName,
                u.Sex,
                u.Age,
                u.Name,
                u.HeadID,
                u.Flag,
                u.Valid,
                u.FriendLimitID,
                u.Sign
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// 封禁用户
    /// </summary>
    [HttpPut("users/ban")]
    public async Task<IActionResult> BanUsers([FromBody] BatchRequest req)
    {
        if (req.Ids.Count == 0)
            return BadRequest(new { error = "请先选中要封禁的账号！" });

        var users = await _db.Users.Where(u => req.Ids.Contains(u.Id)).ToListAsync();
        foreach (var u in users)
            u.Valid = 0;

        await _db.SaveChangesAsync();
        return Ok(new { message = $"已封禁 {users.Count} 个账号" });
    }

    /// <summary>
    /// 解封用户
    /// </summary>
    [HttpPut("users/unban")]
    public async Task<IActionResult> UnbanUsers([FromBody] BatchRequest req)
    {
        if (req.Ids.Count == 0)
            return BadRequest(new { error = "请先选中要解封的账号！" });

        var users = await _db.Users.Where(u => req.Ids.Contains(u.Id)).ToListAsync();
        foreach (var u in users)
            u.Valid = 1;

        await _db.SaveChangesAsync();
        return Ok(new { message = $"已解封 {users.Count} 个账号" });
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpPost("users/delete")]
    public async Task<IActionResult> DeleteUsers([FromBody] BatchRequest req)
    {
        if (req.Ids.Count == 0)
            return BadRequest(new { error = "请先选中要删除的项目！" });

        // 删除用户及其关联的好友关系
        var friends = await _db.Friends
            .Where(f => req.Ids.Contains(f.HostID) || req.Ids.Contains(f.FriendID))
            .ToListAsync();
        _db.Friends.RemoveRange(friends);

        var users = await _db.Users.Where(u => req.Ids.Contains(u.Id)).ToListAsync();
        _db.Users.RemoveRange(users);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"已删除 {users.Count} 个用户" });
    }

    // ===== 消息管理 =====

    /// <summary>
    /// 查询所有消息（视图）
    /// </summary>
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        [FromQuery] string? id,
        [FromQuery] string? fromUserId,
        [FromQuery] string? sender,
        [FromQuery] string? toUserId,
        [FromQuery] string? receiver,
        [FromQuery] string? message)
    {
        var query = _db.MessageView.AsQueryable();

        if (!string.IsNullOrWhiteSpace(id))
            query = query.Where(m => m.Id.ToString().Contains(id));
        if (!string.IsNullOrWhiteSpace(fromUserId))
            query = query.Where(m => m.FromUserID.ToString().Contains(fromUserId));
        if (!string.IsNullOrWhiteSpace(sender))
            query = query.Where(m => m.Sender != null && m.Sender.Contains(sender));
        if (!string.IsNullOrWhiteSpace(toUserId))
            query = query.Where(m => m.ToUserID.ToString().Contains(toUserId));
        if (!string.IsNullOrWhiteSpace(receiver))
            query = query.Where(m => m.Receiver != null && m.Receiver.Contains(receiver));
        if (!string.IsNullOrWhiteSpace(message))
            query = query.Where(m => m.MessageContent != null && m.MessageContent.Contains(message));

        var messages = await query
            .OrderByDescending(m => m.MessageTime)
            .Take(500)
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

        return Ok(messages);
    }

    /// <summary>
    /// 批量删除消息
    /// </summary>
    [HttpPost("messages/delete")]
    public async Task<IActionResult> DeleteMessages([FromBody] BatchRequest req)
    {
        if (req.Ids.Count == 0)
            return BadRequest(new { error = "请先选中要删除的消息！" });

        var messages = await _db.Messages.Where(m => req.Ids.Contains(m.Id)).ToListAsync();
        _db.Messages.RemoveRange(messages);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"已删除 {messages.Count} 条消息" });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }
}
