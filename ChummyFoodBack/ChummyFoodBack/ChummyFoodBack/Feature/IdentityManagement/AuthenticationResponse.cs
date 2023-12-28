namespace ChummyFoodBack.Feature.IdentityManagement;

//Id would be baked in token claims
public class AuthenticationResponse
{
    public string Email { get; set; }
    public string Token { get; set; }
}
