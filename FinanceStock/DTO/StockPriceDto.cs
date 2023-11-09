using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceStock.DTO
{
    public class StockPriceDto
    {
        public DateTime Date { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal VariationFromPreviousDay { get; set; }
        public decimal VariationFromFirst { get; set; }
    }
}