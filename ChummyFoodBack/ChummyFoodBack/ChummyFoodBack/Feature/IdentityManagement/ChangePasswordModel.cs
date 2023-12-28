namespace ChummyFoodBack.Feature.IdentityManagement;

public class ChangePasswordModel
{
    public string Email { get; set; }

    public string Password { get; set; }

    public string RestoreCode { get; set; }
}
