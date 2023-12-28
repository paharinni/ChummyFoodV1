using System.Security.Cryptography;

namespace ChummyFoodBack.Utilis;

public class RandomGenerationUtils
{
    public static int GenerateRandomInt()
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        var byteData = new byte[4];
        randomNumberGenerator.GetBytes(byteData);
        var randomInt = BitConverter.ToInt32(byteData);
        return Math.Abs(randomInt);
    }
}
