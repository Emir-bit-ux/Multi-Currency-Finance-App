using System.Text;
using System.Text.Json;

namespace FinanceAPI.Services
{
    public class AiAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "AIzaSyAxmCCGlZVvcHTD8knII1jFkXdCHjHnCqE";

        public AiAssistantService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> AnalyzePortfolioAsync(string portfolioDetails)
        {
            // Google Gemini API'sinin adresi
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            // Yapay zekaya vereceğimiz gizli komut (Prompt)
            var prompt = $"Sen profesyonel ama samimi bir finansal asistansın. " +
                         $"Kullanıcının güncel portföyü şu şekilde: {portfolioDetails}. " +
                         $"Lütfen bu yatırımların sektörel dağılımı (özellikle teknoloji ve kuantum ağırlığı) ve genel risk durumu hakkında 2-3 cümlelik, Türkçe, kısa ve net bir yorum yap. Uygulama arayüzünde gösterileceği için gereksiz uzatma.";

            // API'nin anladığı JSON formatını hazırlıyoruz
            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(responseString);
                    
                    var aiText = document.RootElement.GetProperty("candidates")[0]
                                     .GetProperty("content")
                                     .GetProperty("parts")[0]
                                     .GetProperty("text").GetString();

                    return aiText ?? "Analiz yapılamadı.";
                }
                
                // --- DEĞİŞEN KISIM BURASI ---
                // Eğer hata alırsak, Google'ın gönderdiği gerçek hata mesajını yakalıyoruz
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Google API Hatası ({response.StatusCode}): {errorContent}";
                // ----------------------------
            }
            catch (Exception ex)
            {
                return $"Yapay zeka kod hatası: {ex.Message}";
            }
        }
    }
}