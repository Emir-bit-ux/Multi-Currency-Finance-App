using System.Text.Json;
using System.Net.Http.Json;

namespace FinanceAPI.Services
{
    public class StockApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public StockApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            // DİKKAT: Artık v7/quote değil, v8/chart sunucusunu kullanıyoruz (Arka kapı)
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}";
            Console.WriteLine($"[V8 DENEMESİ] İstek atılıyor: {url}");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    // JSON yapısı farklı olduğu için fiyatı grafik verisinin içinden (meta) cımbızlıyoruz
                    var meta = doc.RootElement.GetProperty("chart").GetProperty("result")[0].GetProperty("meta");

                    if (meta.TryGetProperty("regularMarketPrice", out var price))
                    {
                        decimal val = price.GetDecimal();
                        Console.WriteLine($"[BAŞARI] {symbol} anlık fiyatı bulundu: {val}");
                        return val;
                    }
                }
                else
                {
                    Console.WriteLine($"[HATA] V8 sunucusu da hata verdi. Kod: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KRİTİK HATA] İstek sırasında hata: {ex.Message}");
            }

            return 0;
        }

        public async Task<(string Name, string Type, string Currency)> GetAssetDetailsAsync(string symbol)
        {
            // Varlık detaylarını da v8'den çekiyoruz ki 401 hatası almayalım
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var meta = doc.RootElement.GetProperty("chart").GetProperty("result")[0].GetProperty("meta");
                    
                    string currency = meta.TryGetProperty("currency", out var c) ? c.GetString() : "USD";
                    string type = meta.TryGetProperty("instrumentType", out var t) ? t.GetString() : "Stock";
                    
                    // v8 sunucusu şirketin tam adını (örn: Apple Inc.) vermez. 
                    // Bu yüzden sistem çökmesin diye şimdilik sembolün kendisini isim olarak kaydediyoruz.
                    string name = symbol; 
                    
                    return (name, type, currency);
                }
            } catch { }
            return (symbol, "Stock", "USD");
        }

        public async Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date)
{
    try
    {
        // O günün başlangıç ve bitiş zamanlarını Unix Timestamp'e çeviriyoruz
        long startTimestamp = ((DateTimeOffset)date.Date).ToUnixTimeSeconds();
        long endTimestamp = startTimestamp + 86400; // 1 gün sonrası

        // Yahoo v8 Chart API'sine geçmiş tarih (period1 ve period2) ile sorgu atıyoruz
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?period1={startTimestamp}&period2={endTimestamp}&interval=1d";
        Console.WriteLine($"[GEÇMİŞ FİYAT] İstek atılıyor: {url}");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0");
        
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            
            // Yahoo grafik verisinde fiyatlar 'indicators' -> 'quote' içindeki 'close' (kapanış) dizisindedir
            if (result.TryGetProperty("indicators", out var indicators) &&
                indicators.TryGetProperty("quote", out var quoteArray) &&
                quoteArray.GetArrayLength() > 0)
            {
                var closePrices = quoteArray[0].GetProperty("close");
                if (closePrices.ValueKind == JsonValueKind.Array && closePrices.GetArrayLength() > 0)
                {
                    var priceElement = closePrices[0];
                    if (priceElement.ValueKind == JsonValueKind.Number)
                    {
                        decimal price = priceElement.GetDecimal();
                        Console.WriteLine($"[BAŞARI] {symbol} {date.ToShortDateString()} kapanış fiyatı: {price}");
                        return price;
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"[HATA] Geçmiş fiyat API Hatası: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[KRİTİK HATA] Geçmiş fiyat çekilemedi ({symbol}): {ex.Message}");
    }

    return 0; // Fiyat bulamazsa 0 döner
}
    }
}