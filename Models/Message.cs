using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace tinyQQ.Web.Models;

/// <summary>
/// 映射到现有表 tb_Message（不改表结构）
/// </summary>
[Table("tb_Message")]
public class Message
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("FromUserID")]
    public int FromUserID { get; set; }

    [Column("ToUserID")]
    public int ToUserID { get; set; }

    [Column("Message")]
    public string? MessageContent { get; set; }

    /// <summary>
    /// 1=聊天消息  2=好友请求
    /// </summary>
    [Column("MessageTypeID")]
    public int MessageTypeID { get; set; } = 1;

    /// <summary>
    /// 0=未读  1=已读
    /// </summary>
    [Column("MessageState")]
    public int MessageState { get; set; }

    [Column("MessageTime")]
    public DateTime MessageTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 映射到现有视图 v_Message（只读）
/// </summary>
[Table("v_Message")]
[Keyless]
public class MessageView
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("FromUserID")]
    public int FromUserID { get; set; }

    [Column("Sender")]
    public string? Sender { get; set; }

    [Column("ToUserID")]
    public int ToUserID { get; set; }

    [Column("Receiver")]
    public string? Receiver { get; set; }

    [Column("Message")]
    public string? MessageContent { get; set; }

    [Column("MessageTypeID")]
    public int MessageTypeID { get; set; }

    [Column("MessageState")]
    public int MessageState { get; set; }

    [Column("MessageTime")]
    public DateTime MessageTime { get; set; }
}
