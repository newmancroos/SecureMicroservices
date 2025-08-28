using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

namespace IdentityServer;

public class Config
{
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
                AllowedGrantTypes=GrantTypes.Code,
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
                    IdentityServerConstants.StandardScopes.Profile
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
            new IdentityResources.Profile()
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
