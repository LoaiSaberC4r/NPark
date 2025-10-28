using BuildingBlock.Application.Abstraction.Encryption;
using Microsoft.AspNetCore.Identity;

namespace BuildingBlock.Infrastracture.Service
{
    public sealed class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            // PasswordHasher يقوم بإضافة Salt داخلياً ويخزن الإصدار داخل الـHash.
            return _hasher.HashPassword(user: null!, password);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            var result = _hasher.VerifyHashedPassword(user: null!, hashedPassword: passwordHash, providedPassword: password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}