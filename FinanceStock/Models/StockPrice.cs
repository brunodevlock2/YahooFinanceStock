using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceStock.Models
{
    public class StockPrice
    {
        public int Id { get; set; }
        public string? Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal OpenPrice { get; set; }
    }
}