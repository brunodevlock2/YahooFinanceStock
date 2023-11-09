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
        public async Task<ActionResult<IEnumerable<StockPriceDto>>> GetPriceVariation(string symbol)
        {
          
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol cannot be empty.");
            }

            try
            {
                string accessToken = await _oauthService.GetAccessTokenAsync();
                JObject yahooFinanceData = await _financeService.GetStockDataAsync(symbol, accessToken);

                var pricesToSave = new List<StockPrice>();

                JObject? results = yahooFinanceData?["chart"]["result"]?.FirstOrDefault() as JObject;
                if (results != null)
                {
                    var timestamps = results["timestamp"]?.ToObject<long[]>();
                    var quotes = results["indicators"]["quote"]?.FirstOrDefault() as JObject;

                    if (quotes != null)
                    {
                        var opens = quotes["open"]?.ToObject<decimal?[]>();
                        for (int i = 0; i < timestamps?.Length; i++)
                        {
                            if (opens[i].HasValue)
                            {
                                var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime;
                                pricesToSave.Add(new StockPrice
                                {
                                    Date = date,
                                    Symbol = symbol,
                                    OpenPrice = opens[i].Value
                                });
                            }
                        }
                    }
                }
               
                await SaveStockDataAsync(pricesToSave);
               
                var priceDtos = await CalculatePriceVariation(symbol);

                return Ok(priceDtos);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
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
