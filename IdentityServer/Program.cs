using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using IdentityServer;
using IdentityServerHost;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add services to the container.
builder.Services.AddIdentityServer()
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    //.AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    //.AddTestUsers(Config.TestUsers)     //We created this user
    .AddTestUsers(TestUsers.Users)   // Identity server automatically has these users
    .AddDeveloperSigningCredential();

builder.Services.AddAuthentication();
        //.AddCookie("Cookies", options =>
        //{
        //    options.AccessDeniedPath = "/Account/AccessDenied"; // Set your custom path here
        //});

var app = builder.Build();

app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages()
    .RequireAuthorization();
app.Run();

