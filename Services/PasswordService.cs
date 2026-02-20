using Microsoft.AspNetCore.Identity;

namespace InventoryManagementApp.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string Hash(string password)
            => _hasher.HashPassword(null!, password);

        public bool Verify(string hash, string password)
        {
            var result = _hasher.VerifyHashedPassword(null!, hash, password);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}