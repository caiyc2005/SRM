using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }

    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<string> _passwordHasher = new();

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
                return result == PasswordVerificationResult.Success;
            }
            catch (FormatException)
            {
                // 数据库存的不是哈希（旧明文密码），回退到明文比对
                return hashedPassword == providedPassword;
            }
        }
    }
}
