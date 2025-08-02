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


    ## For Logout, create a end-point method in any controller and content should be
    <pre>
            public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        }
    </pre>
   To redirect automatically to our web page we need to set
   <pre>
     AutomaticRedirectAfterSignOut=true;
   </pre>
   in LogoutOptions class


<img width="671" height="377" alt="image" src="https://github.com/user-attachments/assets/03c8d447-538d-4433-8f6e-03cb9360b413" />


<hr/>
To communicate from MVC application to MovieApi we need to install <b>Duende.IdentityModel and Duende.AccessTokenManagement</b> Nuget package in MVC application.
and service class we can write the logic to fetch token from Identity server.

<pre>
  
    //1. Get token from Identityserver, Need to provide url, clientId and Client-Secret
    
    var client = new HttpClient();
    //Check if we can reach to discover document
    
    var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5005");
    
    if(disco.IsError)
    {
        throw new Exception($"Something went wrong while connecting to IdentityServer: {disco.Error}");
    }
    
    var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = "https://localhost:5005/connect/token",
        ClientId = "movieApiClient",
        ClientSecret = "secret",
        Scope = "movieApi"
    });
    
    if(response.IsError)
    {
        throw new Exception($"Something went wrong while requesting token: {response.Error}");
    }
    
    //2. Call the API with the token
    
    var apiClient = new HttpClient();
    apiClient.SetBearerToken(response.AccessToken);
    
    //3. Get the response and deserialize it to a list of movies
    var apiResponse = await apiClient.GetAsync("https://localhost:5001/api/movies");
    if (!apiResponse.IsSuccessStatusCode)
    {
        throw new Exception($"Something went wrong while calling the API: {apiResponse.ReasonPhrase}");
    }
    apiResponse.EnsureSuccessStatusCode();
    
    var movieContent= await apiResponse.Content.ReadAsStringAsync();
    
    //deserialize the response string to movie object
    
    List<Movie> movieList = JsonConvert.DeserializeObject<List<Movie>>(movieContent);
    return movieList;
</pre>

### We can refactor the comue operation using HttpClientFactor: <br/>
<u>What is DelegatingHandler?</u>
A DelegatingHandler in ASP.NET Core is a specialized type of HTTP message handler that allows for the interception and manipulation of outgoing HTTP requests and incoming HTTP responses when using HttpClient. It functions similarly to middleware in the ASP.NET Core request pipeline, but specifically for client-side HTTP operations.<br/>

