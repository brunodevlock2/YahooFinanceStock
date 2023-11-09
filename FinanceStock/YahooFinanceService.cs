using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;  

public class YahooFinanceService
{
    private readonly HttpClient _httpClient;

    public YahooFinanceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JObject> GetStockDataAsync(string symbol, string accessToken)
    {
        string url = $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}";
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        return JObject.Parse(content);
    }

}
