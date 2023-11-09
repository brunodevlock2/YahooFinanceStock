using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceStock.Models;
using FinanceStock.DTO;
using Newtonsoft.Json.Linq;

namespace FinanceStock.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockController : ControllerBase
    {
        private readonly StockContext _context;
        private readonly OAuthService _oauthService;
        private readonly YahooFinanceService _financeService;

        public StockController(StockContext context, OAuthService oauthService, YahooFinanceService financeService)
        {
            _context = context;
            _oauthService = oauthService;
            _financeService = financeService;
        }

        [HttpGet("{symbol}")]
        public IActionResult GetPriceVariation(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(OAuthService.AccessToken))
            {
                _oauthService.SetRequestedSymbol(symbol);

                return Authorize(symbol);
            }

            return RedirectToAction("FetchPriceVariation");
        }


        [HttpGet("authorize")]
        public IActionResult Authorize(string symbol)
        {
            _oauthService.SetRequestedSymbol(symbol);
            var authorizationUrl = _oauthService.GetAuthorizationUrl();
           
            return Redirect(authorizationUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            var code = HttpContext.Request.Query["code"];
            if (!string.IsNullOrEmpty(code))
            {
                try
                {
                    string accessToken = await _oauthService.GetAccessTokenAsync(code);
                    OAuthService.AccessToken = accessToken;

                    var symbol = OAuthService.Symbol;
                    if (string.IsNullOrWhiteSpace(symbol))
                    {
                        return BadRequest("Symbol not provided.");
                    }
                   
                    return RedirectToAction("FetchPriceVariation", new { symbol });
                }       
                catch (Exception ex)
                {
                    return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
                }
            }
            else
            {
                return BadRequest("Authorization code is missing in the request.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockPriceDto>>> FetchPriceVariation(string symbol)
        {
            try
            {
                // Utilize o accessToken para buscar dados da API do Yahoo
                JObject yahooFinanceData = await _financeService.GetStockDataAsync(symbol, OAuthService.AccessToken);
                var pricesToSave = ParseYahooFinanceData(yahooFinanceData, symbol);

                await SaveStockDataAsync(pricesToSave);
                var priceDtos = await CalculatePriceVariation(symbol);

                return Ok(priceDtos);
            }        
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
            }
        }

        private List<StockPrice> ParseYahooFinanceData(JObject yahooFinanceData, string symbol)
        {
            var results = yahooFinanceData["chart"]["result"]?.First;
            var timestamps = results?["timestamp"]?.ToObject<long[]>() ?? Array.Empty<long>();
            var quotes = results?["indicators"]["quote"]?.First;

            var opens = quotes?["open"]?.ToObject<decimal?[]>() ?? Array.Empty<decimal?>();

            return timestamps.Zip(opens, (timestamp, open) => new { timestamp, open })
                            .Where(tp => tp.open.HasValue)
                            .Select(tp => new StockPrice
                            {
                                Date = DateTimeOffset.FromUnixTimeSeconds(tp.timestamp).UtcDateTime,
                                Symbol = symbol,
                                OpenPrice = tp.open.Value
                            })
                            .ToList();
        }

        private async Task SaveStockDataAsync(List<StockPrice> prices)
        {
            await _context.StockPrices.AddRangeAsync(prices);
            await _context.SaveChangesAsync();
        }

        private async Task<List<StockPriceDto>> CalculatePriceVariation(string symbol)
        {
            var prices = await _context.StockPrices
                .Where(sp => sp.Symbol == symbol)
                .OrderByDescending(sp => sp.Date)
                .Take(30)
                .ToListAsync();

            var priceDtos = prices
                .Select((price, index) => new StockPriceDto
                {
                    Date = price.Date,
                    OpenPrice = price.OpenPrice,
                    VariationFromFirst = index == 0 ? 0 : (price.OpenPrice - prices.Last().OpenPrice) / prices.Last().OpenPrice * 100,
                    VariationFromPreviousDay = index == 0 ? 0 : (price.OpenPrice - prices[index + 1].OpenPrice) / prices[index + 1].OpenPrice * 100
                })
                .ToList();

            return priceDtos;
        }
    }
}
