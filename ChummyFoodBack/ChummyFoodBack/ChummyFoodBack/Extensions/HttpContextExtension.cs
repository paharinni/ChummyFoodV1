using System.Security.Claims;

namespace ChummyFoodBack.Extensions;

public static class HttpContextExtensions
{
    public static string? GetEmail(this HttpContext context)
    {
        return context
            .User
            .Claims.FirstOrDefault(claim => claim.Type is ClaimTypes.Email)
            ?.Value;
    }
}
