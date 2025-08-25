using UCDASearches_Blazor.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UCDASearches_Blazor.Security;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using UCDASearches_Blazor.Data;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage; // ← add this
using UCDASearches_Blazor.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// our ADO.NET repo
builder.Services.AddScoped<RequestsRepository>();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")));

builder.Services.AddScoped<PreviousSearchService>();
// store auth info in the browser (encrypted)



// 🔐 Minimal auth for Blazor
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();
builder.Services.AddScoped<ISimpleAuthService, SimpleAuthService>();


builder.Services.AddDataProtection();                    // for encryption under the hood
builder.Services.AddScoped<ProtectedLocalStorage>();     // encrypted localStorage
builder.Services.AddScoped<ProtectedSessionStorage>();   // encrypted sessionStorage

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
