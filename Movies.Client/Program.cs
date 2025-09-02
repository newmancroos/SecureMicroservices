using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Movies.Client.ApiServices;
using Movies.Client.HttpHandlers;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMovieApiService, MovieApiService>();

//Configure OpenIdConnect Here
// Code updated for Hybrid flow:
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = "https://localhost:5005"; // IdentityServer URL
    //options.AccessDeniedPath = "/Account/AccessDenied"; // Access Denied path   // Not working
    options.ClientId = "movies_mvc_clinet"; // Client ID registered in IdentityServer
    options.ClientSecret= "secret"; // Client Secret registered in IdentityServer
    options.ResponseType = "code id_token"; // Use Authorization Code flow/ *********id_token added on top of code flow for user authentication while implementing ***Hybrid flow
    //options.Scope.Add("openid"); // OpenID Connect scope                      Thease claims automatically added by Identity server so we don;t want to specify here
    //options.Scope.Add("profile"); // Profile scope for user information       Thease claims automatically added by Identity server so we don;t want to specify here
    options.Scope.Add("movieAPI"); // ***************API scope to access the Movie API as part of ***Hybrid flow*************

    options.Scope.Add("address"); // Address scope
    options.Scope.Add("email"); // Email scope
    options.Scope.Add("roles"); // Adding roles scope
    options.ClaimActions.MapUniqueJsonKey("role", "role"); // Map the "role" claim
    options.SaveTokens = true; // Save tokens in the authentication properties
    options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint

    //Rolebased Authorization
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = JwtClaimTypes.Name,
        RoleClaimType = JwtClaimTypes.Role
    };
});

//HttpClient Configurations
//1. Create an HttpClient used for accessing the MovieApi
builder.Services.AddTransient<AuthenticationDelegatingHandler>();


//Configuring HttpClient to access Api
//Removing this because now we are using Ocelot API gateway
//builder.Services.AddHttpClient("MovieAPIClient", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:5001");
//    client.DefaultRequestHeaders.Clear();
//    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
//}).AddHttpMessageHandler<AuthenticationDelegatingHandler>();


builder.Services.AddHttpClient("MovieAPIClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:5010");
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
}).AddHttpMessageHandler<AuthenticationDelegatingHandler>();

//Configuraing HttpClient to access IDP

builder.Services.AddHttpClient("IDPClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:5005");
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
});





/// As we use hybrid flow we don;t need to call the IdentityServer end-point for token so we can remove the below code
/// Instead we will use service.AddHttpContextAccessor() method to access the token
/// 
/*
builder.Services.AddSingleton(new ClientCredentialsTokenRequest
{
    Address = "https://localhost:5005/connect/token",
    ClientId = "movieClient",
    ClientSecret = "secret",
    Scope = "movieAPI"
});
*/

builder.Services.AddHttpContextAccessor();  //*** Hybrid flow


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
