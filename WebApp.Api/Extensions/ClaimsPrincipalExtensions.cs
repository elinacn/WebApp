using System.Security.Claims;

namespace WebApp.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // ID of the signed-in user (from the login cookie), or null for anonymous requests.
    // In controllers: User.GetUserId()
    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
