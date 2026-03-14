using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceAPI; 
using FinanceAPI.Models;
using FinanceAPI.Services;

namespace FinanceAPI.Controllers
{
    public class TransactionRequest
    {
        public string Symbol { get; set; }
        public string TransactionType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime Date { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StockApiService _stockApiService;

        public TransactionsController(AppDbContext context, StockApiService stockApiService)
        {
            _context = context;
            _stockApiService = stockApiService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTransactions()
        {
            var transactions = await _context.Transactions
                .Include(t => t.Asset)
                .OrderByDescending(t => t.Date)
                .Select(t => new {
                    t.Id,
                    t.TransactionType,
                    t.Quantity,
                    t.UnitPrice,
                    t.Date,
                    Symbol = t.Asset.Symbol,
                    Currency = t.Asset.Currency
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> PostTransaction([FromBody] TransactionRequest req)
        {
            try
            {
                string symbol = req.Symbol.ToUpper().Trim();
                //if (!symbol.Contains(".") && symbol.Length >= 4 && !symbol.EndsWith(".IS"))
                //{
                //    symbol += ".IS";
                //}

                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Symbol == symbol);
                if (asset == null)
                {
                    // ŞİMDİ BURASI UYUMLU: 3 değişken geliyor
                    var (name, type, currency) = await _stockApiService.GetAssetDetailsAsync(symbol);
                    asset = new Asset { Symbol = symbol, Name = name, Type = type, Currency = currency };
                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();
                }

                var transaction = new Transaction
                {
                    AssetId = asset.Id,
                    TransactionType = req.TransactionType,
                    Quantity = req.Quantity,
                    UnitPrice = req.UnitPrice,
                    Date = req.Date == default ? DateTime.UtcNow : req.Date,
                    UserId = 1 
                };

                if (transaction.UnitPrice <= 0)
                {
                    transaction.UnitPrice = transaction.Date.Date < DateTime.UtcNow.Date 
                        ? await _stockApiService.GetHistoricalPriceAsync(asset.Symbol, transaction.Date)
                        : await _stockApiService.GetCurrentPriceAsync(asset.Symbol);

                    if (transaction.UnitPrice <= 0) return BadRequest("Fiyat bulunamadı.");
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Başarılı", AssetName = asset.Name });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}