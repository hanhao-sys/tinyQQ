namespace tinyQQ.Web.Models;

// ===== 认证相关 DTO =====

public class LoginRequest
{
    public int Id { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string NickName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Name { get; set; }
    public string? Star { get; set; }
    public string? BloodType { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string? NickName { get; set; }
    public string? Sex { get; set; }
    public int? Age { get; set; }
    public string? Name { get; set; }
    public string? Star { get; set; }
    public string? BloodType { get; set; }
    public int HeadID { get; set; }
    public string? Sign { get; set; }
    public int Flag { get; set; }

    public static UserDto FromUser(User u) => new()
    {
        Id = u.Id,
        NickName = u.NickName,
        Sex = u.Sex,
        Age = u.Age,
        Name = u.Name,
        Star = u.Star,
        BloodType = u.BloodType,
        HeadID = u.HeadID,
        Sign = u.Sign,
        Flag = u.Flag,
    };
}

public class UpdateProfileRequest
{
    public string? NickName { get; set; }
    public string? Sex { get; set; }
    public int? Age { get; set; }
    public string? Name { get; set; }
    public string? Star { get; set; }
    public string? BloodType { get; set; }
    public int? HeadID { get; set; }
    public string? Sign { get; set; }
    public string? NewPassword { get; set; }
}

// ===== 好友相关 DTO =====

public class FriendDto
{
    public int Id { get; set; }
    public string? NickName { get; set; }
    public int HeadID { get; set; }
    public string? Sign { get; set; }
    public int Flag { get; set; }

    public static FriendDto FromUser(User u) => new()
    {
        Id = u.Id,
        NickName = u.NickName,
        HeadID = u.HeadID,
        Sign = u.Sign,
        Flag = u.Flag,
    };
}

public class SearchUserRequest
{
    public string? Keyword { get; set; }
    public string? Sex { get; set; }
    public string? AgeRange { get; set; }  // 0-9,10-19,20-29,30-39,40-49,50+
}

public class FriendRequestDto
{
    public int MessageId { get; set; }
    public int FromUserId { get; set; }
    public string? FromNickName { get; set; }
    public DateTime MessageTime { get; set; }
}

// ===== 消息相关 DTO =====

public class MessageDto
{
    public int Id { get; set; }
    public int FromUserID { get; set; }
    public string? Sender { get; set; }
    public int ToUserID { get; set; }
    public string? Content { get; set; }
    public int MessageTypeID { get; set; }
    public int MessageState { get; set; }
    public DateTime MessageTime { get; set; }
}

public class SendMessageRequest
{
    public int ToUserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ===== 管理员 DTO =====

public class AdminUserSearchRequest
{
    public string? Id { get; set; }
    public string? NickName { get; set; }
    public string? Valid { get; set; }
}

public class AdminMsgSearchRequest
{
    public string? Id { get; set; }
    public string? FromUserID { get; set; }
    public string? Sender { get; set; }
    public string? ToUserID { get; set; }
    public string? Receiver { get; set; }
    public string? Message { get; set; }
}

public class BatchRequest
{
    public List<int> Ids { get; set; } = new();
}
