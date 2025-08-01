# SecureMicroservices


## Adding Identituservice UI
* Refer this Link : https://github.com/DuendeArchive/IdentityServer.Quickstart.UI
  1. Run the following command in Identityserver project terminal
     <pre> iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/DuendeSoftware/IdentityServer.Quickstart.UI/main/getmain.ps1'))</pre>
     This will add some new folders with code with html pages
  2. Duende  Identity server uses Razer pages instead of mvc template
     <pre>
      builder.Services.AddRazorPages();
      builder.Services.AddAuthentication();

       app.UseStaticFiles();
      app.UseHttpsRedirection();
      app.UseRouting();
      app.UseIdentityServer();
      app.UseAuthorization();
      app.MapRazorPages()
          .RequireAuthorization();
     </pre>  in to the program.cs file
  
     
## Connecting Client Application to Identity Server

### Api Application
  Api application foloows Client Credential so we don't need to install Identityserver related Nuget packages
  <pre>
    builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5005";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });

    builder.Services.AddAuthorization(options=>
    {
        options.AddPolicy("ClientIdPolicy", policy => policy.RequireClaim("client_id", "movieClient"));
    });
  </pre>
  By hitting token end-point we can get the access token and pass it to the Api

  ### MVC Application
  We need to install Nuget packages to connect the application to talk to IdentityServer
    * Microsoft.AspNetCore.Authentication.OpenIdConnect
    and then configure Identityserver inside MVC project
    
    <pre>
      builder.Services.AddAuthentication(options =>
      {
          options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
      .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
      {
          options.Authority = "https://localhost:5005"; // IdentityServer URL
          options.ClientId = "movies_mvc_clinet"; // Client ID registered in IdentityServer
          options.ClientSecret= "secret"; // Client Secret registered in IdentityServer
          options.ResponseType = "code"; // Use Authorization Code flow
          options.Scope.Add("openid"); // OpenID Connect scope
          options.Scope.Add("profile"); // Profile scope for user information
          options.SaveTokens = true; // Save tokens in the authentication properties
          options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint
      });

      app.UseAuthentication();
      app.UseAuthorization();
    </pre>
    
