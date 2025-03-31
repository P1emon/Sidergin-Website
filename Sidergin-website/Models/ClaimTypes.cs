
using System.Security.Claims;

namespace Sidergin_website.Utilities
{
    public static class ClaimUtility
    {
        // Phương thức tạo Claim cho tên người dùng
        public static Claim CreateNameClaim(string name)
        {
            return new Claim(ClaimTypes.Name, name);
        }

        // Phương thức tạo Claim cho email người dùng
        public static Claim CreateEmailClaim(string email)
        {
            return new Claim(ClaimTypes.Email, email);
        }

        // Phương thức tạo Claim tùy chỉnh cho UserId
        public static Claim CreateUserIdClaim(string userId)
        {
            return new Claim("UserId", userId);
        }

        // Phương thức tạo danh sách Claim từ thông tin người dùng
        public static List<Claim> CreateUserClaims(string name, string email, string userId)
        {
            return new List<Claim>
            {
                CreateNameClaim(name),
                CreateEmailClaim(email),
                CreateUserIdClaim(userId)
            };
        }

        internal static IEnumerable<Claim>? CreateUserClaims(ClaimsIdentity? name, string email, string? v)
        {
            throw new NotImplementedException();
        }
    }
}

