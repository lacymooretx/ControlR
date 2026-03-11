using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Startup;

/// <summary>
/// Local-only management endpoints for administrative tasks that require
/// direct server access (e.g., via docker exec). These endpoints are
/// restricted to loopback connections and require no authentication.
/// </summary>
public static class ManagementEndpoints
{
  public static void MapManagementEndpoints(this WebApplication app)
  {
    var group = app.MapGroup("/management")
      .RequireHost("localhost", "127.0.0.1");

    group.MapPost("/create-pat", async (
      [FromBody] CreatePatRequest request,
      [FromServices] UserManager<AppUser> userManager,
      [FromServices] IPersonalAccessTokenManager patManager) =>
    {
      var user = await userManager.FindByEmailAsync(request.Email);
      if (user is null)
      {
        return Results.NotFound($"User with email '{request.Email}' not found.");
      }

      var createRequest = new CreatePersonalAccessTokenRequestDto(
        request.Name ?? $"API Token ({DateTime.UtcNow:yyyy-MM-dd})");

      var result = await patManager.CreateToken(createRequest, user.Id);
      if (!result.IsSuccess)
      {
        return Results.BadRequest(result.Reason);
      }

      return Results.Ok(new
      {
        Token = result.Value.PlainTextToken,
        result.Value.PersonalAccessToken.Id,
        result.Value.PersonalAccessToken.Name,
        result.Value.PersonalAccessToken.ExpiresAt,
        UserId = user.Id,
        UserEmail = user.Email
      });
    })
    .AllowAnonymous();
  }

  public record CreatePatRequest(string Email, string? Name = null);
}
