using System.ComponentModel.DataAnnotations;

namespace ChummyFoodBack.Feature.IdentityManagement;

public class IdentityModel
{
    [EmailAddress]
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
