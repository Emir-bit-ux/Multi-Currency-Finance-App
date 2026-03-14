using FinanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlantımızı bu Controller'a enjekte ediyoruz
        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/assets (Sistemdeki tüm varlıkları listeler)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets()
        {
            return await _context.Assets.ToListAsync();
        }

        // POST: api/assets (Sisteme yeni bir hisse/fon ekler)
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset(Asset asset)
        {
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
            
            return Ok(asset);
        }
    }
}