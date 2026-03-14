using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceAPI.Models;
using FinanceAPI.Services; // StockApiService'i kullanmak için ekledik

namespace FinanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StockApiService _stockApiService; // Canlı kur için servisi çağırıyoruz

        public PortfolioController(AppDbContext context, StockApiService stockApiService)
        {
            _context = context;
            _stockApiService = stockApiService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPortfolioSummary(int userId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Asset)
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (!transactions.Any()) return NotFound("Bu kullanıcıya ait işlem bulunamadı.");

            // YAHOO'DAN CANLI DOLAR KURUNU ÇEKİYORUZ
            decimal usdTryRate = await _stockApiService.GetCurrentPriceAsync("USDTRY=X");
            
            // Eğer API anlık bir hata verirse (0 dönerse), grafiğin patlamaması için mantıklı bir varsayılan (fallback) kur giriyoruz
            if (usdTryRate == 0) usdTryRate = 32.50m; 

            var portfolioSummary = transactions.GroupBy(t => new { t.AssetId, t.Asset.Symbol, t.Asset.Name, t.Asset.Currency })
                .Select(group => 
                {
                    var netQuantity = group.Where(t => t.TransactionType == "Buy").Sum(t => t.Quantity) - group.Where(t => t.TransactionType == "Sell").Sum(t => t.Quantity);
                    var netInvestment = group.Where(t => t.TransactionType == "Buy").Sum(t => t.Quantity * t.UnitPrice) - group.Where(t => t.TransactionType == "Sell").Sum(t => t.Quantity * t.UnitPrice);

                    // GRAFİK İÇİN MATEMATİKSEL EŞİTLEME (HER ŞEYİ TL'YE ÇEVİR)
                    decimal totalValueTry = netInvestment;
                    if (group.Key.Currency == "USD") 
                    {
                        totalValueTry = netInvestment * usdTryRate;
                    }

                    return new {
                        AssetId = group.Key.AssetId, 
                        Symbol = group.Key.Symbol, 
                        Name = group.Key.Name,
                        Currency = group.Key.Currency,
                        NetQuantity = netQuantity, 
                        TotalInvestment = netInvestment, // Orijinal yatırım (Kartlarda göstermek için)
                        TotalValueTRY = totalValueTry,   // Kur çarpımlı yatırım (Grafik için)
                        AverageCost = netQuantity > 0 ? Math.Round(netInvestment / netQuantity, 2) : 0
                    };
                }).Where(p => p.NetQuantity > 0).ToList();

            return Ok(portfolioSummary);
        }
    }
}
