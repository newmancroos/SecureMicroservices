using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Movies.Client.Models;
using Newtonsoft.Json;
using System.Text;
namespace Movies.Client.ApiServices;

public class MovieApiService : IMovieApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public MovieApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserInfoViewModel> GetUserInfo()
    {
        var idpClient = _httpClientFactory.CreateClient("IDPClient");

        var metaDataResponse = await idpClient.GetDiscoveryDocumentAsync();

        if(metaDataResponse.IsError)
        {
            throw new Exception($"Something went wrong while getting the discovery document: {metaDataResponse.Error}");
        }

        var accessToken = await _httpContextAccessor!.HttpContext!.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

        var userInfoResponse = await idpClient.GetUserInfoAsync(new UserInfoRequest
        {
            Address = metaDataResponse.UserInfoEndpoint,
            Token = accessToken
        });

        if (userInfoResponse.IsError)
        {
            throw new Exception($"Something went wrong while getting the user info: {userInfoResponse.Error}");
        }

        var userInfoDictionary = new Dictionary<string, string>();

        foreach (var claim in userInfoResponse.Claims)
        {
            userInfoDictionary.Add(claim.Type, claim.Value);
        }

        return new UserInfoViewModel(userInfoDictionary);

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

    public Task<Movie> CreateMovies(Movie movie)
    {
        throw new NotImplementedException();
    }

    public Task DeleteMovie(int id)
    {
        throw new NotImplementedException();
    }

    public async  Task<Movie> GetMovie(int id)
    {
        var httpClient = _httpClientFactory.CreateClient("MovieAPIClient");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/movies/{id}");
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var movieContent = await response.Content.ReadAsStringAsync();

        //deserialize the response string to movie object
        Movie movie = JsonConvert.DeserializeObject<Movie>(movieContent);
        return movie;
    }



    public async Task<Movie> UpdateMovie(Movie movie)
    {
        var httpClient = _httpClientFactory.CreateClient("MovieAPIClient");

        var movieId = movie.Id;

        var uri = "/api/movies/" + movieId;
        var content = new StringContent(
                    JsonConvert.SerializeObject(movie),
                    Encoding.UTF8,
                    "application/json"
        );

        var response = await httpClient.PutAsync(uri, content);

        response.EnsureSuccessStatusCode();

        return movie;
    }
    public Task<API.Models.Movie> CreateMovies(API.Models.Movie movie)
    {
        throw new NotImplementedException();
    }

    public Task<API.Models.Movie> UpdateMovie(API.Models.Movie movie)
    {
        throw new NotImplementedException();
    }


}
