using System.ComponentModel.DataAnnotations.Schema;

namespace DTC.Api.Models
{
    public partial class RefreshToken
    {
        [NotMapped]
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
        [NotMapped]
        public bool IsRevoked => RevokedAt != null;
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
