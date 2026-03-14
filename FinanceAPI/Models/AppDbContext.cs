using Microsoft.EntityFrameworkCore;

namespace FinanceAPI.Models
{
    public class AppDbContext : DbContext
    {
        // Veritabanı bağlantı ayarlarını (şifre, sunucu adresi vb.) almamızı sağlayan yapıcı metot
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanındaki tablolarımızı temsil eden DbSet'ler
        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}