using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace UCDASearches_Blazor.Security
{
    public interface ISimpleAuthService
    {
        Task<bool> SignInAsync(string email, string password);
        Task<bool> SignInAsync(string email, string password, string accountNumber);
        Task SignOutAsync();

        Task RestoreAsync();            // ← restore from browser storage

        Task SetAccountNumberAsync(string acct);
        string? Email { get; }
        string? AccountNumber { get; }
        bool IsAuthenticated { get; }
    }

    public class SimpleAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = new(new ClaimsIdentity());
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
        internal void SetUser(ClaimsPrincipal user)
        {
            _user = user;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    public class SimpleAuthService : ISimpleAuthService
    {
        private readonly SimpleAuthStateProvider _stateProvider;
        private readonly ProtectedLocalStorage _storage;
        public string? Email { get; private set; }
        public string? AccountNumber { get; private set; }
        public bool IsAuthenticated { get; private set; }

        private static readonly Dictionary<string, string> _users = new(StringComparer.OrdinalIgnoreCase)
        {
            ["k.alagh@ucda.org"] = "kshitiz123",
            ["user@example.com"] = "password"
        };

        // optional default mapping if account isn’t typed on the form
        private static readonly Dictionary<string, string> _userAccounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["k.alagh@ucda.org"] = "008035-0001",
            ["user@example.com"] = "U0000000001"
        };

        public SimpleAuthService(AuthenticationStateProvider provider, ProtectedLocalStorage storage)
        {
            _stateProvider = (SimpleAuthStateProvider)provider;
            _storage = storage;
        }

      
        public Task<bool> SignInAsync(string email, string password)
        {
            _ = _userAccounts.TryGetValue(email, out var acct);
            return SignInAsync(email, password, acct ?? "");
        }

        public async Task<bool> SignInAsync(string email, string password, string accountNumber)
        {
            var ok = _users.TryGetValue(email, out var stored) && stored == password;
            if (!ok)
            {
                await ClearStorageAsync();
                SignOutCore();
                return false;
            }

            Email = email;
            AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();
            IsAuthenticated = true;

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, Email),
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.Role, "User")
            }, authenticationType: "Simple");
            _stateProvider.SetUser(new ClaimsPrincipal(identity));

            // persist to encrypted localStorage
            await _storage.SetAsync("auth_email", Email);
            await _storage.SetAsync("auth_account", AccountNumber ?? "");
            await _storage.SetAsync("auth_isAuth", true);

            return true;
        }

        public async Task RestoreAsync()
        {
            try
            {
                var isAuth = await _storage.GetAsync<bool>("auth_isAuth");
                if (isAuth.Success && isAuth.Value)
                {
                    var email = await _storage.GetAsync<string>("auth_email");
                    var acct = await _storage.GetAsync<string>("auth_account");

                    Email = email.Success ? email.Value : null;
                    AccountNumber = acct.Success ? acct.Value : null;
                    IsAuthenticated = !string.IsNullOrEmpty(Email);

                    if (IsAuthenticated)
                    {
                        var id = new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, Email!),
                            new Claim(ClaimTypes.Email, Email!),
                            new Claim(ClaimTypes.Role, "User")
                        }, "Simple");
                        _stateProvider.SetUser(new ClaimsPrincipal(id));
                    }
                }
            }
            catch { /* ignore restore errors */ }
        }

        public async Task SignOutAsync()
        {
            SignOutCore();
            await ClearStorageAsync();
        }

        private void SignOutCore()
        {
            Email = null;
            AccountNumber = null;
            IsAuthenticated = false;
            _stateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        private Task ClearStorageAsync() => Task.WhenAll(
            _storage.DeleteAsync("auth_email").AsTask(),
            _storage.DeleteAsync("auth_account").AsTask(),
            _storage.DeleteAsync("auth_isAuth").AsTask()
        );

        // handy fallback so pages can set the account without re-login
        public async Task SetAccountNumberAsync(string acct)
        {
            AccountNumber = string.IsNullOrWhiteSpace(acct) ? null : acct.Trim();
            await _storage.SetAsync("auth_acct", AccountNumber ?? "");
        }
    }
}
