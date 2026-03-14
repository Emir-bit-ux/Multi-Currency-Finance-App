namespace FinanceAPI.Models;

public class Asset
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty; 
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; 
    public string Currency { get; set; } = "USD";
}