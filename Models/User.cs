using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tinyQQ.Web.Models;

/// <summary>
/// 映射到现有表 tb_User（不改表结构）
/// </summary>
[Table("tb_User")]
public class User
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("Pwd")]
    public string? Pwd { get; set; }

    [Column("NickName")]
    public string? NickName { get; set; }

    [Column("Sex")]
    public string? Sex { get; set; }

    [Column("Age")]
    public int? Age { get; set; }

    [Column("Name")]
    public string? Name { get; set; }

    [Column("Star")]
    public string? Star { get; set; }

    [Column("BloodType")]
    public string? BloodType { get; set; }

    [Column("HeadID")]
    public int HeadID { get; set; }

    [Column("Sign")]
    public string? Sign { get; set; }

    [Column("FriendLimitID")]
    public int FriendLimitID { get; set; }

    [Column("Flag")]
    public int Flag { get; set; }

    [Column("Valid")]
    public int Valid { get; set; } = 1;

    [Column("Remember")]
    public int Remember { get; set; }

    [Column("AutoLogin")]
    public int AutoLogin { get; set; }
}
