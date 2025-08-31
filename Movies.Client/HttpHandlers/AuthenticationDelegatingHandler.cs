using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Data.SqlTypes;

namespace Movies.Client.HttpHandlers;

public class AuthenticationDelegatingHandler:DelegatingHandler
{

    /// <summary>
    /// Since we use Hybrid flow, Access token will part of the user login process. 
    /// So we don;t need to call Identityprovider end-point for access token.
    /// Instead we use HttpContextAccessor to access the token
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>

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
