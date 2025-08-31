using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

namespace IdentityServer;

public class Config
{
    /// <summary>
    /// Code flow and Client credential flow defined in the main branch so 
    /// user access MVC and in side MVC we make a code flow call
    /// to get the token and call API end-point.here we had two clients
    /// In this Hybrid flow we use only one client that hold authentication 
    /// and access token to call Api end-point.
    /// Hybrid flow for interactive client, meaning Client application interactive with API authorization
    /// </summary>
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
                    IdentityServerConstants.StandardScopes.Email,
                    "roles"
                }
            }
        };

    public static IEnumerable<ApiResource> ApiResources=>
        new ApiResource[]
        {
        };

    public static IEnumerable<ApiScope> ApiScopes=>
        new ApiScope[]
        {
            new ApiScope("movieAPI","Movie API")
        };

    public static IEnumerable<IdentityResource> IdentityResources=>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Address(),
            new IdentityResources.Email(),
            new IdentityResource("roles","Your role(s)", new List<string>(){ "role"})
        };

    public static List<TestUser> TestUsers=>
        new List<TestUser>
        {
            new TestUser{ 
                SubjectId="DA513259-A736-4FA6-A333-25576BE3CEC0",
                Username="newmancroos",
                Password= "password",
                Claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.GivenName,"newmancroos"),
                    new Claim(JwtClaimTypes.FamilyName,"Croos")
                }
            }
        };

}
