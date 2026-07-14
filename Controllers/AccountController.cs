using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using tinyQQ.Web.Data;
using tinyQQ.Web.Models;

namespace tinyQQ.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AccountController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 登录 — 管理员0123硬编码 / 普通用户查数据库
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // ===== 管理员登录（与桌面端 Frm_Login.cs:75 逻辑一致）=====
        if (req.Id == 123 && req.Password == "0123")
        {
            var adminDto = new UserDto
            {
                Id = 123,
                NickName = "管理员",
                HeadID = 0,
                Flag = 1
            };
            var adminToken = GenerateJwt(123);
            return Ok(new LoginResponse { Token = adminToken, User = adminDto });
        }

        // ===== 普通用户登录 =====
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.Id && u.Pwd == req.Password);

        if (user == null)
            return Unauthorized(new { error = "账号或密码错误" });

        if (user.Valid == 0)
            return Unauthorized(new { error = "账号目前处于封禁状态，请联系管理员！" });

        // 更新在线状态（Flag=1 在线）
        user.Flag = 1;
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.Id);
        return Ok(new LoginResponse { Token = token, User = UserDto.FromUser(user) });
    }

    /// <summary>
    /// 注册 — 自增ID作为QQ号（与桌面端 Frm_Register.cs 逻辑一致）
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NickName) || req.NickName.Length > 20)
            return BadRequest(new { error = "昵称输入有误！" });
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "请输入密码！" });
        if (string.IsNullOrWhiteSpace(req.Sex))
            return BadRequest(new { error = "请选择性别！" });
        if (req.Age < 0)
            return BadRequest(new { error = "请输入有效的年龄！" });

        var user = new User
        {
            Pwd = req.Password,
            NickName = req.NickName,
            Sex = req.Sex,
            Age = req.Age,
            Name = req.Name,
            Star = req.Star,
            BloodType = req.BloodType,
            HeadID = 0,
            FriendLimitID = 2,    // 默认需要身份验证
            Flag = 0,
            Valid = 1,
            Remember = 0,
            AutoLogin = 0
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // EF Core 会在 SaveChanges 后自动填充自增 ID
        return Ok(new { id = user.Id, message = $"注册成功！您的tinyQQ号码是{user.Id}，注册后请先联系管理员获得登录权限" });
    }

    /// <summary>
    /// 获取当前登录用户的个人信息
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { error = "用户不存在" });

        return Ok(UserDto.FromUser(user));
    }

    /// <summary>
    /// 更新个人信息（与桌面端 Frm_EditInfo.cs 逻辑一致）
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { error = "用户不存在" });

        if (req.NickName != null)
        {
            if (string.IsNullOrWhiteSpace(req.NickName) || req.NickName.Length > 20)
                return BadRequest(new { error = "昵称输入有误！" });
            user.NickName = req.NickName;
        }
        if (req.Sex != null) user.Sex = req.Sex;
        if (req.Age != null) user.Age = req.Age;
        if (req.Name != null) user.Name = req.Name;
        if (req.Star != null) user.Star = req.Star;
        if (req.BloodType != null) user.BloodType = req.BloodType;
        if (req.HeadID != null) user.HeadID = req.HeadID.Value;
        if (req.Sign != null) user.Sign = req.Sign;
        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            user.Pwd = req.NewPassword;
            user.Remember = 0;
            user.AutoLogin = 0;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "数据保存成功！", user = UserDto.FromUser(user) });
    }

    /// <summary>
    /// 退出登录 — 设置离线状态
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Flag = 0;  // 离线
            await _db.SaveChangesAsync();
        }
        return Ok(new { message = "已退出" });
    }

    // ===== 辅助方法 =====

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }

    private string GenerateJwt(int userId)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, userId == 123 ? "Admin" : "User"),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(double.Parse(jwtSection["ExpireMinutes"]!)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
