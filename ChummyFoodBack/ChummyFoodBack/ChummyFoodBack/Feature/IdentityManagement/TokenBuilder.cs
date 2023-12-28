using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChummyFoodBack.Persistance.DAO;
using Microsoft.IdentityModel.Tokens;

namespace ChummyFoodBack.Feature.IdentityManagement;

public class TokenBuilder
{
    public static string FromIdentity(IdentityDAO dao, string secretKey)
    {
        // Create a security key from the provided secret key
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        // Create signing credentials using the security key
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Create a new JWT token handler
        var tokenHandler = new JwtSecurityTokenHandler();

        // Create a new JWT token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, dao.Email),
                new Claim(ClaimTypes.Role, dao.RoleDao.Name)
            }),
            Issuer = TokenOptions.Issuer,
            Audience = TokenOptions.Audience,
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = signingCredentials
        };

        // Generate the JWT token
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Serialize the JWT token to a string
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
    }
}
