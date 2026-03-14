using FinanceAPI.Models;
using FinanceAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// React uygulamasının (5173 portu) bu API'ye erişmesine izin veriyoruz
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React'in çalıştığı adres
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- BİZİM EKLEDİĞİMİZ VERİTABANI BAĞLANTISI ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// -----------------------------------------------

// Döngüleri engellemek için JSON ayarlarına bu eklemeyi yapıyoruz
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
// İnternet istekleri atacak olan borsa servisimizi sisteme kaydediyoruz
builder.Services.AddHttpClient<StockApiService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Yapay zeka asistan servisimizi sisteme kaydediyoruz
builder.Services.AddHttpClient<AiAssistantService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Eğer hiç kullanıcı yoksa, "Emir" adında ilk kullanıcıyı oluştur
if (!context.Users.Any())
{
    context.Users.Add(new User { 
        Id = 1, 
        Username = "Emir", 
        Email = "emir@example.com" 
    });
    context.SaveChanges();
}
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();
app.Run();