# SecureMicroservices

## Note:
  Three banches. Main contain **Authorization Code** flow. hybridFlow on **Hybrid flow**  and then Ocelet branch has **Ocelet Gateway** implementation


  
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

<b>What is DelegatingHandler?</b><br/>
A DelegatingHandler in ASP.NET Core is a specialized type of HTTP message handler that allows for the interception and manipulation of outgoing HTTP requests and incoming HTTP responses when using HttpClient. It functions similarly to middleware in the ASP.NET Core request pipeline, but specifically for client-side HTTP operations.<br/>
<pre>
  using Duende.IdentityModel.Client;

namespace Movies.Client.HttpHandlers;

public class AuthenticationDelegatingHandler:DelegatingHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClientCredentialsTokenRequest _tokenRequest;

    public AuthenticationDelegatingHandler(IHttpClientFactory httpClientFactory, ClientCredentialsTokenRequest tokenRequest)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _tokenRequest = tokenRequest ?? throw new ArgumentNullException(nameof(tokenRequest));

    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("IDPClient");
        var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(_tokenRequest);

        if(tokenResponse.IsError)
        {
            throw new Exception($"Something went wrong while requesting token: {tokenResponse.Error}");
        }

       
        request.SetBearerToken(tokenResponse!.AccessToken!);

        return await base.SendAsync(request, cancellationToken);
    }
}
</pre>

In program.cs

<pre>
  builder.Services.AddHttpClient("MovieAPIClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001");
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

builder.Services.AddSingleton(new ClientCredentialsTokenRequest
{
    Address = "https://localhost:5005/connect/token",
    ClientId = "movieClient",
    ClientSecret = "secret",
    Scope = "movieAPI"
});
</pre>

<pre>
  public class MovieApiService : IMovieApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public MovieApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }
    public async Task<IEnumerable<Movie>> GetMovies()
    {
        //Refactor code using HttpClientFactory

        var httpClient = _httpClientFactory.CreateClient("MovieAPIClient");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/movies/");
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var movieContent = await response.Content.ReadAsStringAsync();

        //deserialize the response string to movie object
        List<Movie> movieList = JsonConvert.DeserializeObject<List<Movie>>(movieContent);
        return movieList;

        #region Old Code
        /////Old Code
        ////1. Get token from Identityserver, Need to provide url, clientId and Client-Secret

        //var client = new HttpClient();
        ////Check if we can reach to discover document

        //var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5005");

        //if(disco.IsError)
        //{
        //    throw new Exception($"Something went wrong while connecting to IdentityServer: {disco.Error}");
        //}

        //var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        //{
        //    Address = "https://localhost:5005/connect/token",
        //    ClientId = "movieClient",
        //    ClientSecret = "secret",
        //    Scope = "movieAPI"
        //});

        //if(response.IsError)
        //{
        //    throw new Exception($"Something went wrong while requesting token: {response.Error}");
        //}

        ////2. Call the API with the token

        //var apiClient = new HttpClient();
        //apiClient.SetBearerToken(response.AccessToken);

        ////3. Get the response and deserialize it to a list of movies
        //var apiResponse = await apiClient.GetAsync("https://localhost:5001/api/movies");
        //if (!apiResponse.IsSuccessStatusCode)
        //{
        //    throw new Exception($"Something went wrong while calling the API: {apiResponse.ReasonPhrase}");
        //}
        //apiResponse.EnsureSuccessStatusCode();

        //var movieContent= await apiResponse.Content.ReadAsStringAsync();

        ////deserialize the response string to movie object

        //List<Movie> movieList = JsonConvert.DeserializeObject<List<Movie>>(movieContent);
        //return movieList;

        #endregion

   }

</pre>


DelegateHandler intercept the request and add token.<br/>


## Hybrid Flow

**What is hybrid flow?**
<p>
  hybrid flow is an OpenID Connect (OIDC) authorization flow combining authorization code flow and implicit flow to provide a balance of security and immediate user identity access. It delivers an ID token (and an authorization code) over the front-channel (browser) for quick access to user identity, then uses the authorization code in a back-channel (direct HTTP request) to securely exchange for access and refresh tokens. This flow is recommended for server-side web applications and native mobile/desktop applications. 
</p>

<p>
 ** How it Works**
**1. Request an ID Token and Authorization Code:**
.
The client redirects the user to the authorization endpoint to request an ID token and an authorization code. 
  
**2. Receive Tokens/Code (Front-Channel):**
.
The authorization server returns the ID token and the authorization code to the browser via a redirect. 

**3. Secure Token Exchange (Back-Channel):**
.
The client uses the received authorization code to make a direct, back-channel call to the authorization server's token endpoint. 

**4. Receive Tokens:**
.
The authorization server validates the code and returns the access token and refresh token to the client. 


### Why Use Hybrid Flow?

<img width="671" height="333" alt="image" src="https://github.com/user-attachments/assets/2736e4f0-9d07-4f7a-b979-abc815421811" />


**Immediate User Identity:**
.
Provides quick access to user identity information via the ID token, which can be used by server-side web applications. 

**Secure Access Tokens:**
.
Obtains long-lived access tokens and refresh tokens using a secure back-channel, preventing token leakage. 

**Recommended for Specific Applications:**
.
It is the recommended flow for server-side web applications and native applications that need to access protected resources using access tokens. 
### Key Characteristics

**Uses Both Front and Back Channels:**
Combines browser-based front-channel communication with direct, secure server-to-server back-channel communication. <br/> <br/>
**Balance of Security and Responsiveness:** <br/>
Offers a good balance by delivering identity information quickly and handling sensitive tokens more securely.  <br/> <br/>
**Combination of Grant Types:** <br/>
Typically uses a code id_token or code token response type, combining aspects of the authorization code and implicit flows. 

</p>


<img width="679" height="387" alt="image" src="https://github.com/user-attachments/assets/fc42b17a-5b7b-483b-a510-85eb322e38f5" />


## What does RequirePkce (Proff Key do?

In IdentityServer, "Require PKCE" forces applications to use Proof Key for Code Exchange (PKCE) when using the Authorization Code Grant flow, adding a crucial layer of security for public clients (like mobile and single-page apps) by preventing authorization code interception attacks. The client generates a unique secret (code verifier) and a derived challenge, which the server verifies during token exchange, ensuring the same client that initiated the flow receives the access token. 
How it Works 
**1. Code Verifier & Challenge Creation:**
Before starting the authorization request, the client creates a random string called a "code verifier". It then generates a base64-encoded hash of this verifier, called the "code challenge," which is sent to the authorization server.

**2. Code Challenge Transmission:**
The client sends the code challenge to the IdentityServer's authorization endpoint to obtain an authorization code.

**3. PKCE Enforcement:**
When the "Require PKCE" setting is enabled, the IdentityServer stores the received code challenge.

**4. Token Exchange & Verification:**
When the client exchanges the authorization code for an access token, it must also send the original, un-hashed "code verifier".

**5. Security Check:**
The IdentityServer verifies that the code verifier, when hashed and transformed, matches the code challenge it previously stored for that authorization code.

**6. Token Issuance:**
Only if the verifier and challenge match will the IdentityServer issue the access token; otherwise, the request is rejected.

**Why It's Important**

**Secures Public Clients:**
.PKCE is vital for public clients (like single-page applications and native mobile apps) that cannot securely store client secrets. 

**Prevents Code Interception:**
It protects against authorization code interception attacks, where a malicious application might capture an authorization code and use it to obtain an access token. 

**Ensures Client Authenticity:**
PKCE verifies that the client requesting the token is the same one that initiated the authorization request, preventing impersonation


## Changes on the existing code due to Hybrid flow:

### <ins> In Identity Project </ins>
  
1. In IdentityProvider project, We don;t need two separate clients for Api and MVC.
2. Chnage AllowedGrantTypes from GrantType.Code to GrantTypes.Hybrid
3. Add RequirePkce = false as we don;t strckly check the challenge
4. Add **movieApi** scops to AllowesScope to MVC client
   
<pre>
  new Client
{
    ClientId="movies_mvc_clinet",
    ClientName="Movies MVC Web App",
    //AllowedGrantTypes=GrantTypes.Code,
    AllowedGrantTypes=GrantTypes.Hybrid,  //becomes interactive client
    RequirePkce=false,  // It need only for Code flow. It rquire authorization code base token request 
    AllowRememberConsent=false,
    RedirectUris= new List<string>()
    {
        "https://localhost:5002/signin-oidc"
    },
    PostLogoutRedirectUris= new List<string>()
    {
        "https://localhost:5002/signout-callback-oidc"
    },
    ClientSecrets= new List<Secret>()
    {
        new Secret("secret".Sha256())
    },
    AllowedScopes= new List<string>()
    {
        IdentityServerConstants.StandardScopes.OpenId,
        IdentityServerConstants.StandardScopes.Profile,
        "movieAPI"
    }
}
  
</pre>

### <ins> In Movie.Client MVC Project </ins>

1. In program.cs, in AddAuthentication block, **add id_token** on top of **code**
2. In program.cs, in AddAuthentication block, add **Scope** "**movieAPI**" on top of **openid** and **profile**

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
    options.ResponseType = "code id_token"; // Use Authorization Code flow/ *********id_token added on top of code flow for user authentication while implementing ***Hybrid flow
    options.Scope.Add("openid"); // OpenID Connect scope
    options.Scope.Add("profile"); // Profile scope for user information
    options.Scope.Add("movieAPI"); // ***************API scope to access the Movie API as part of ***Hybrid flow*************
    options.SaveTokens = true; // Save tokens in the authentication properties
    options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint
});
</pre>

 
3.  Remove AddHttpClient("IDPClient") block and remove registering **ClientCredentialsTokenRequest** as we don't need call to Identity end-point
<pre>
//Configuraing HttpClient to access IDP

//builder.Services.AddHttpClient("IDPClient", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:5005");
//    client.DefaultRequestHeaders.Clear();
//    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
//});


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
</pre>

5. In program.cs register HttpClient as the access token we can get it from here
  <pre>
    builder.Services.AddHttpContextAccessor();
  </pre>
  
4. In **AuthenticationDelegatingHandler**  remove calls to IdentityServer end-point related codes and inject IHttpContextAccessor mand get the token and add it to the request
<pre>
public class AuthenticationDelegatingHandler:DelegatingHandler
{
    //private readonly IHttpClientFactory _httpClientFactory;
    //private readonly ClientCredentialsTokenRequest _tokenRequest;

    //public AuthenticationDelegatingHandler(IHttpClientFactory httpClientFactory, ClientCredentialsTokenRequest tokenRequest)
    //{
    //    _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    //    _tokenRequest = tokenRequest ?? throw new ArgumentNullException(nameof(tokenRequest));
    //}

    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        /// Due to ***Hybrid flow we don;t need to call IDP for token
        
        //var httpClient = _httpClientFactory.CreateClient("IDPClient");    
        //var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(_tokenRequest);

        //if (tokenResponse.IsError)
        //{
        //    throw new Exception($"Something went wrong while requesting token: {tokenResponse.Error}");
        //}
        //request.SetBearerToken(tokenResponse!.AccessToken!);

        var accessToeken  = await _httpContextAccessor!.HttpContext!.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
        if(!string.IsNullOrEmpty(accessToeken))
        {
            request.SetBearerToken(accessToeken);
        }
        else
        {
            throw new Exception("Access token is null or empty");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
</pre>

5. FInally we need to change Claims based authorization in MovieAPI as we are using only one client and removed MovieApi Client
   
   <pre>
     builder.Services.AddAuthorization(options=>
      {
          //options.AddPolicy("ClientIdPolicy", policy => policy.RequireClaim("client_id", "movieClient")); // Due to hybrid flow and we are using one client for APi and MVC
          options.AddPolicy("ClientIdPolicy", policy => policy.RequireClaim("client_id", "movies_mvc_client"));
      });
   </pre>


## Claim Based Authorization:

<img width="1121" height="655" alt="image" src="https://github.com/user-attachments/assets/66fa8569-d9aa-4215-a6b4-4a3f467d0758" />


- We may have multiple Claims for a user like below

  <pre>
    public static class TestUsers
{
    public static List<TestUser> Users
    {
        get
        {
            var address = new
            {
                street_address = "One Hacker Way",
                locality = "Heidelberg",
                postal_code = "69118",
                country = "Germany"
            };
                
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "a1",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "b1",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            };
        }
    }
}
  </pre>
but we need to change the Identity configuration for include these claim as part of the token.

<pre>
public static IEnumerable<Client> Clients =>
    new Client[]
    {
        new Client
        { 
            ClientId="movieClient",
            AllowedGrantTypes=GrantTypes.ClientCredentials,
            ClientSecrets=
            { 
                new Secret("secret".Sha256())
            },
            AllowedScopes={"movieAPI"}
        },
          new Client
          {
              ClientId="movies_mvc_clinet",
              ClientName="Movies MVC Web App",
              //AllowedGrantTypes=GrantTypes.Code,
              AllowedGrantTypes=GrantTypes.Hybrid,  //becomes interactive client
              RequirePkce=false,  // It need only for Code flow. It rquire authorization code base token request 
              AllowRememberConsent=false,
              RedirectUris= new List<string>()
              {
                  "https://localhost:5002/signin-oidc"
              },
              PostLogoutRedirectUris= new List<string>()
              {
                  "https://localhost:5002/signout-callback-oidc"
              },
              ClientSecrets= new List<Secret>()
              {
                  new Secret("secret".Sha256())
              },
              AllowedScopes= new List<string>()
              {
                  IdentityServerConstants.StandardScopes.OpenId,
                  IdentityServerConstants.StandardScopes.Profile,
                  "movieAPI",
                  IdentityServerConstants.StandardScopes.Address,
                  IdentityServerConstants.StandardScopes.Email
              }
          }
      };
      public static IEnumerable<IdentityResource> IdentityResources=>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Address(),
            new IdentityResources.Email()
        };
</pre>

Finally we need to map the Identity Server user to the new User class in the Identity project, and alosin the MVC Client project we need to specify in the program.cs

- Identity Project
<pre>
  // Add services to the container.
builder.Services.AddIdentityServer()
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    //.AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    //.AddTestUsers(Config.TestUsers)     //We created this user
    .AddTestUsers(TestUsers.Users)   // Identity server automatically has these users
    .AddDeveloperSigningCredential();  

</pre>

- MVC Project
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
    options.ResponseType = "code id_token"; // Use Authorization Code flow/ *********id_token added on top of code flow for user authentication while implementing ***Hybrid flow
    //options.Scope.Add("openid"); // OpenID Connect scope                      Thease claims automatically added by Identity server so we don;t want to specify here
    //options.Scope.Add("profile"); // Profile scope for user information       Thease claims automatically added by Identity server so we don;t want to specify here
    options.Scope.Add("movieAPI"); // ***************API scope to access the Movie API as part of ***Hybrid flow*************

    options.Scope.Add("address"); // Address scope
    options.Scope.Add("email"); // Email scope

    options.SaveTokens = true; // Save tokens in the authentication properties
    options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint
});
</pre>



## Rolebased Authorization

To allow rolebased authorization
1. Need to add roles to the users (IdentityServier project)
  <pre>
    new TestUser
    {
        SubjectId = "1",
        Username = "alice",
        Password = "a1",
        Claims =
        {
            new Claim(JwtClaimTypes.Name, "Alice Smith"),
            new Claim(JwtClaimTypes.GivenName, "Alice"),
            new Claim(JwtClaimTypes.FamilyName, "Smith"),
            new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
            new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json),
            <b>new Claim(JwtClaimTypes.Role, "user") </b>
        }
    },
         new TestUser
     {
         SubjectId = "2",
         Username = "bob",
         Password = "b1",
         Claims =
         {
             new Claim(JwtClaimTypes.Name, "Bob Smith"),
             new Claim(JwtClaimTypes.GivenName, "Bob"),
             new Claim(JwtClaimTypes.FamilyName, "Smith"),
             new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
             new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
             new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
             new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json),
             <b>new Claim(JwtClaimTypes.Role, "admin") </b>
         }
     }
  </pre>
  
2. Need to add **roles**  in Identity Resources   (IdentityServier project)
  <pre>
     public static IEnumerable<IdentityResource> IdentityResources=>
     new IdentityResource[]
     {
         new IdentityResources.OpenId(),
         new IdentityResources.Profile(),
         new IdentityResources.Address(),
         new IdentityResources.Email(),
         <b>new IdentityResource("roles","Your role(s)", new List<string>(){ "role"}) </b>
     };
  </pre>

3. Add **roles** in Client's Allowed scopes   (IdentityServier project)

   <pre>
      AllowedScopes= new List<string>()
       {
           IdentityServerConstants.StandardScopes.OpenId,
           IdentityServerConstants.StandardScopes.Profile,
           "movieAPI",
           IdentityServerConstants.StandardScopes.Address,
           IdentityServerConstants.StandardScopes.Email,
           <b>"roles" </b>
       }
   </pre>
4. Configure Role scope in the MVC Application

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
        options.ResponseType = "code id_token"; // Use Authorization Code flow/ *********id_token added on top of code flow for user authentication while implementing ***Hybrid flow
        //options.Scope.Add("openid"); // OpenID Connect scope                      Thease claims automatically added by Identity server so we don;t want to specify here
        //options.Scope.Add("profile"); // Profile scope for user information       Thease claims automatically added by Identity server so we don;t want to specify here
        options.Scope.Add("movieAPI"); // ***************API scope to access the Movie API as part of ***Hybrid flow*************
    
        options.Scope.Add("address"); // Address scope
        options.Scope.Add("email"); // Email scope
        <b>options.Scope.Add("roles");</b> // Adding roles scope**
        <b>options.ClaimActions.MapUniqueJsonKey("role", "role");</b>
        options.SaveTokens = true; // Save tokens in the authentication properties
        options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint
    });
</pre>

5.  Add Token validation option in the MVC application
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
    options.ResponseType = "code id_token"; // Use Authorization Code flow/ *********id_token added on top of code flow for user authentication while implementing ***Hybrid flow
    //options.Scope.Add("openid"); // OpenID Connect scope                      Thease claims automatically added by Identity server so we don;t want to specify here
    //options.Scope.Add("profile"); // Profile scope for user information       Thease claims automatically added by Identity server so we don;t want to specify here
    options.Scope.Add("movieAPI"); // ***************API scope to access the Movie API as part of ***Hybrid flow*************

    options.Scope.Add("address"); // Address scope
    options.Scope.Add("email"); // Email scope
    options.Scope.Add("roles"); // Adding roles scope
    <b>options.ClaimActions.MapUniqueJsonKey("role", "role");</b> // Map the "role" claim
    options.SaveTokens = true; // Save tokens in the authentication properties
    options.GetClaimsFromUserInfoEndpoint = true; // Retrieve claims from UserInfo endpoint

    //Rolebased Authorization
    <b>options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = JwtClaimTypes.Name,
        RoleClaimType = JwtClaimTypes.Role
    };</b>
});
   </pre>


### To Implement Entity Framework instead of InMemory database follow the link
https://docs.duendesoftware.com/identityserver/quickstarts/4-entity-framework/
