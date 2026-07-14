using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace tinyQQ.Web.Models;

/// <summary>
/// 映射到现有表 tb_Friend（不改表结构）
/// </summary>
[Table("tb_Friend")]
[PrimaryKey(nameof(HostID), nameof(FriendID))]
public class Friend
{
    [Column("HostID")]
    public int HostID { get; set; }

    [Column("FriendID")]
    public int FriendID { get; set; }
}
