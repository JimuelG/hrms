using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public class HttpTenantIsolationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<string> LoginAsync(HttpClient client, string email, string password, string? tenantSlug = null)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password, tenantSlug });
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken),
            $"Login did not return an access token (tenantSelectionRequired={body?.TenantSelectionRequired}). " +
            "Check that the seeded user is a member of the requested tenant.");
        return body!.AccessToken!;
    }

    private static void Authorize(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Globex_User_Cannot_Read_Acmes_Branch_By_Id()
    {
        using var acmeClient = factory.CreateClient();
        var acmeToken = await LoginAsync(acmeClient, "admin@acme.test", "Password123!");
        Authorize(acmeClient, acmeToken);

        // Create a real Branch inside Acme, the exact "known, real ID" an attacker would have.
        var createResp = await acmeClient.PostAsJsonAsync("/api/v1/branches", new
        {
            name = "Isolation Test Branch",
            code = $"ISO-{Guid.NewGuid():N}"[..10]
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<BranchDto>();
        Assert.NotNull(created);

        // Authenticate as a DIFFERENT tenant (Globex), then attempt to read Acme's branch.
        using var globexClient = factory.CreateClient();
        var globexToken = await LoginAsync(globexClient, "multi@hrms.test", "Password123!", tenantSlug: "globex");
        Authorize(globexClient, globexToken);

        var attackResp = await globexClient.GetAsync($"/api/v1/branches/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, attackResp.StatusCode);
    }

    [Fact]
    public async Task Globex_User_Branch_List_Never_Contains_Acmes_Branch()
    {
        using var acmeClient = factory.CreateClient();
        var acmeToken = await LoginAsync(acmeClient, "admin@acme.test", "Password123!");
        Authorize(acmeClient, acmeToken);

        var createResp = await acmeClient.PostAsJsonAsync("/api/v1/branches", new
        {
            name = "Isolation List Test Branch",
            code = $"ISL-{Guid.NewGuid():N}"[..10]
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<BranchDto>();

        using var globexClient = factory.CreateClient();
        var globexToken = await LoginAsync(globexClient, "multi@hrms.test", "Password123!", tenantSlug: "globex");
        Authorize(globexClient, globexToken);

        var listResp = await globexClient.GetAsync("/api/v1/branches");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<List<BranchDto>>();

        Assert.DoesNotContain(list!, b => b.Id == created!.Id);
    }

    private record LoginResponseDto(string? AccessToken, bool TenantSelectionRequired);
    private record BranchDto(Guid Id, string Name, string Code);
}