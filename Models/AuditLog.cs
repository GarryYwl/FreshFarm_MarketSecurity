using System;

namespace FreshFarmMarketSecurity.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty; // e.g. LOGIN_SUCCESS, LOGOUT, LOGIN_FAIL

        public DateTimeOffset TimeUtc { get; set; } = DateTimeOffset.UtcNow;

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}
