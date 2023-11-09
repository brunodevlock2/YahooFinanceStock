using Microsoft.EntityFrameworkCore;
using FinanceStock.Models;

public class StockContext : DbContext
{
    public StockContext(DbContextOptions<StockContext> options) : base(options)
    {
    }
    public DbSet<StockPrice> StockPrices { get; set; }   

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=finance.db; Cache=Shared");
    }
    
}

    

    
