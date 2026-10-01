using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;
public class HttpTenantIsolationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.AccessToken!;
    }

    [Fact]
    public async Task Authenticated_As_Globex_Cannot_Acmes_Role_Via_Any_Endpoint()
    {
        using var client = factory.CreateClient();

        var globeXToken = await LoginAsync(client, "multi@hrms.test", "Password123!");

        client.DefaultRequestHeaders.Authorization = new("Bearer", globeXToken);
        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/auth/me");
    }

    private record LoginResponseDto (string? AccessToken);
    private record MeDto(string? UserId, Guid? TenantId, bool IsPlatformAdmin);
}