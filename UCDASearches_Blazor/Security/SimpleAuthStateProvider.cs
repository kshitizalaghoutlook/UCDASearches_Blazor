using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

public class SimpleAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _store;
    private const string Key = "auth_user";

    public SimpleAuthStateProvider(ProtectedSessionStorage store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var stored = await _store.GetAsync<AuthUser>(Key);
        if (stored.Success && stored.Value is not null)
            return new AuthenticationState(CreatePrincipal(stored.Value));

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task SignInAsync(AuthUser user)
    {
        await _store.SetAsync(Key, user);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(user))));
    }

    public async Task SignOutAsync()
    {
        await _store.DeleteAsync(Key);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static ClaimsPrincipal CreatePrincipal(AuthUser u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.AccountId),
            new(ClaimTypes.Name, u.DisplayName ?? u.AccountId),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Session"));
    }
}

public record AuthUser(string AccountId, string? DisplayName);
