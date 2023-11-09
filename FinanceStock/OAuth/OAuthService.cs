using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinanceStock.Models;
using Microsoft.Extensions.Options;
using System.Net;

public class OAuthService
{
    private readonly HttpClient _httpClient;
    private readonly OAuthConfig _config;
    public static string AccessToken { get; set; }
    public static string RequestedSymbol { get; set; }
    public static string Symbol { get; private set; }

    public OAuthService(HttpClient httpClient, IOptions<OAuthConfig> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public void SetRequestedSymbol(string symbol)
    {
        RequestedSymbol = symbol;
    }

    public string GetAuthorizationUrl()
    {
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", _config.ClientId },
            { "redirect_uri", _config.RedirectUri },
            { "response_type", "code" },
            { "language", "en-us" } 
        };

        var queryString = string.Join("&", queryParams.Select(
            kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

        return $"https://api.login.yahoo.com/oauth2/request_auth?{queryString}";
    }

    public async Task<string> GetAccessTokenAsync(string authorizationCode)
    {
        string tokenUrl = "https://api.login.yahoo.com/oauth2/get_token";
       
        string clientCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
       
        var requestData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" }, 
            { "code", authorizationCode },         
            { "redirect_uri", _config.RedirectUri } 
        };

        var requestContent = new FormUrlEncodedContent(requestData);
      
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);
       
        var response = await _httpClient.PostAsync(tokenUrl, requestContent);
      
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error while requesting token: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
     
        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("No access token found in the response.");
        }
       
        return tokenResponse.AccessToken;
    }

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
    }

    public class OAuthConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
    }
}
