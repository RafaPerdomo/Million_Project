using System;
using Properties.Domain.Common;

namespace Properties.Domain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public DateTime? Revoked { get; set; }
    public bool IsActive => Revoked == null && !IsExpired;
    
    public int UserId { get; set; }
    
    public User User { get; set; }
}
