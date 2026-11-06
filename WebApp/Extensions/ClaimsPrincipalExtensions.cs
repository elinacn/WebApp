using System.Security.Claims;

namespace WebApp.Extensions;

public static class ClaimsPrincipalExtensions
{
    // ID of the signed-in user (from the login cookie), or null for anonymous visitors.
    // In controllers and views, call it on User: User.GetUserId()
    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
