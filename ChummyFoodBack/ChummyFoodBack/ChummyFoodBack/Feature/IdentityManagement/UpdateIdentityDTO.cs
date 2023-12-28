namespace ChummyFoodBack.Feature.IdentityManagement;

public class UpdateIdentityDTO
{
    public string Email { get; set; }
    
    public string? Name { get; set; }
    
    public string? Surname { get; set; }
    
    public int? Age { get; set; }
    
    public string? City { get; set; }
    
    public string? Country { get; set; }
}