using ChummyFoodBack.Utilis;

namespace ChummyFoodBack.Feature.IdentityManagement;

public record CharCode(int StartCode, int EndCode);
public class PasswordGenerator: IPasswordGenerator
{
    private readonly CharCode alphaLoverCase = new CharCode((int)'a', (int)'z');
    private readonly CharCode alphaUpperCase = new CharCode((int)'A', (int)'Z');
    private readonly CharCode numberCodes = new CharCode((int)'0', (int)'9');
    private readonly CharCode[] targetCharCodes;

    public PasswordGenerator()
    {
        targetCharCodes = new[] { alphaLoverCase, alphaUpperCase, numberCodes };
    }
    public string GeneratePassword(int minPasswordLength, int maxPasswordLength)
    {
        int boundaryRange = maxPasswordLength - minPasswordLength + 1;
        int countOfCharactersInPassword = RandomGenerationUtils.GenerateRandomInt() % boundaryRange + minPasswordLength;
        var passwordArray = new char[countOfCharactersInPassword];
        for (var i = 0; i < countOfCharactersInPassword; i++)
        {
            passwordArray[i] = CreateRandomChar();
        }

        string password = new string(passwordArray);
        return password;
    }

    private char CreateRandomChar()
    {
        var charCodeChooseIndex = RandomGenerationUtils.GenerateRandomInt() % 3;
        var targetCharCode = targetCharCodes[charCodeChooseIndex];
        var charCodeToGenerate = (RandomGenerationUtils.GenerateRandomInt() % 
                                  (targetCharCode.EndCode - targetCharCode.StartCode + 1)) + targetCharCode.StartCode;
        char targetChar = Convert.ToChar(charCodeToGenerate);
        return targetChar;
    }
}
