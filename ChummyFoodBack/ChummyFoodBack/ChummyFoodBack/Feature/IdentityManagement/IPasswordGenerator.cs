namespace ChummyFoodBack.Feature.IdentityManagement;

public interface IPasswordGenerator
{
    public string GeneratePassword(int minPasswordLength, int maxPasswordLength);
}
