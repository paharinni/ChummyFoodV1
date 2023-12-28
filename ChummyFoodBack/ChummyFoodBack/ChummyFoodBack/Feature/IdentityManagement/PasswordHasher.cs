using System.Security.Cryptography;

namespace ChummyFoodBack.Feature.IdentityManagement;

public class HashResult
{
    public string PasswordSalt { get; set; }

    public string PasswordHash { get; set; }
}
public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 20;
    private const int Iterations = 10000;

    public static HashResult HashPassword(string password)
    {
        // Generate a random salt
        byte[] salt = new byte[SaltSize];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        // Hash the password and combine it with the salt
        byte[] hash = GenerateHash(password, salt, Iterations, HashSize);

        // Combine the salt and hash together
        byte[] hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        // Convert the combined bytes to a string and return it
        string hashedPassword = Convert.ToBase64String(hashBytes);
        return new HashResult
        {
             PasswordSalt = Convert.ToBase64String(salt),
             PasswordHash = hashedPassword
        };
    }

    public static bool VerifyPassword(string password, string hashedPassword, string saltString)
    {
        // Convert the hashed password string back to bytes
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);
        byte[] salt = Convert.FromBase64String(saltString);
        // Generate the hash of the provided password using the extracted salt
        byte[] computedHash = GenerateHash(password, salt, Iterations, HashSize);

        byte[] resultHashBytes = new byte[computedHash.Length + salt.Length];
        Array.Copy(salt, 0, resultHashBytes, 0, salt.Length);
        Array.Copy(computedHash, 0, resultHashBytes, salt.Length, computedHash.Length );
        // Compare the computed hash with the stored hash
        return AreByteArraysEqual(hashBytes, resultHashBytes);
    }

    private static byte[] GenerateHash(string password, byte[] salt, int iterations, int outputBytes)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
        {
            return pbkdf2.GetBytes(outputBytes);
        }
    }

    private static bool AreByteArraysEqual(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length)
        {
            return false;
        }

        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i])
            {
                return false;
            }
        }

        return true;
    }
}
