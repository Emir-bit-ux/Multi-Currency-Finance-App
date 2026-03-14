namespace FinanceAPI.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Buy" veya "Sell"
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        // İlişkiler
        public int UserId { get; set; }
        public User? User { get; set; } // DİKKAT: Buraya soru işareti (?) eklendi

        public int AssetId { get; set; }
        public Asset? Asset { get; set; } // DİKKAT: Buraya soru işareti (?) eklendi
    }
}