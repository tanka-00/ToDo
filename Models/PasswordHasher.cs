using System;
using System.Security.Cryptography;

namespace toDo.Models
{
    /// <summary>
    /// PBKDF2 password hashing that matches the existing console generator:
    /// 16-byte salt, 100,000 iterations, 32-byte hash, SHA-1 (the Rfc2898DeriveBytes default).
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static void CreateHash(string password, out string hash, out string salt)
        {
            byte[] saltBytes = new byte[SaltSize];

            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(saltBytes);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
            {
                hash = Convert.ToBase64String(pbkdf2.GetBytes(HashSize));
                salt = Convert.ToBase64String(saltBytes);
            }
        }

        public static bool Verify(string password, string hash, string salt)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(hash) ||
                string.IsNullOrWhiteSpace(salt))
            {
                return false;
            }

            byte[] saltBytes;
            byte[] expectedHash;

            try
            {
                saltBytes = Convert.FromBase64String(salt);
                expectedHash = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
            {
                byte[] actualHash = pbkdf2.GetBytes(HashSize);
                return FixedTimeEquals(actualHash, expectedHash);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;

            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }
}
