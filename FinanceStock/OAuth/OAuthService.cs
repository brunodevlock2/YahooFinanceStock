using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinanceStock.Models;
using Microsoft.Extensions.Options;

public class OAuthService
{
    private readonly HttpClient _httpClient;
    private readonly OAuthConfig _config;

    public OAuthService(HttpClient httpClient, IOptions<OAuthConfig> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> GetAccessTokenAsync()
{
    string tokenUrl = "https://api.login.yahoo.com/oauth2/get_token";

    var clientCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

    var requestData = new Dictionary<string, string>
    {
        { "client_id", _config.ClientId },
        { "client_secret", _config.ClientSecret },
        { "redirect_uri", _config.RedirectUri },
        { "grant_type", "authorization_code" }
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
