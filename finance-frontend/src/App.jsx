import { useState, useEffect } from 'react';
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import './App.css';

function App() {
  const [portfolio, setPortfolio] = useState([]);
  const [transactions, setTransactions] = useState([]); 
  const [loading, setLoading] = useState(true);
  
  const [aiComment, setAiComment] = useState("");
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  const [formData, setFormData] = useState({ symbol: '', quantity: '', price: '', date: '', type: 'Buy' });
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  // Grafikteki her bir hisse için kullanılacak renk paleti
  const COLORS = ['#3498db', '#2ecc71', '#f1c40f', '#e74c3c', '#9b59b6', '#34495e', '#e67e22'];
  // --- DARK MODE STATE VE KONTROLÜ ---
  const [isDarkMode, setIsDarkMode] = useState(false);

  useEffect(() => {
    if (isDarkMode) {
      document.body.classList.add('dark-mode');
    } else {
      document.body.classList.remove('dark-mode');
    }
  }, [isDarkMode]);
  // -----------------------------------

  useEffect(() => {
    fetchPortfolio();
    fetchTransactions();
  }, []);

  const fetchPortfolio = () => {
    fetch('http://localhost:5124/api/portfolio/1')
      .then(response => response.json())
      .then(data => {
        setPortfolio(data);
        setLoading(false);
      })
      .catch(error => console.error("Portföy hatası:", error));
  };

  const fetchTransactions = () => {
    fetch('http://localhost:5124/api/transactions')
      .then(response => response.json())
      .then(data => setTransactions(data))
      .catch(error => console.error("İşlem geçmişi hatası:", error));
  };

  const handleAnalyze = () => {
    setIsAnalyzing(true);
    setAiComment(""); 
    fetch('http://localhost:5124/api/portfolio/1/analyze')
      .then(response => response.json())
      .then(data => {
        setAiComment(data.aiAssistant);
        setIsAnalyzing(false);
      })
      .catch(() => {
        setAiComment("Yapay zeka bağlantı kuramıyor.");
        setIsAnalyzing(false);
      });
  };

  const handleSubmitTransaction = (e) => {
    e.preventDefault();
    if (!formData.symbol || !formData.quantity) return alert("Lütfen sembol ve adet giriniz.");

    setIsSubmitting(true);

    const payload = {
      symbol: formData.symbol.trim().toUpperCase(),
      transactionType: formData.type,
      quantity: parseFloat(formData.quantity),
      unitPrice: formData.price ? parseFloat(formData.price) : 0, 
      date: formData.date ? new Date(formData.date).toISOString() : new Date().toISOString()
    };

    fetch('http://localhost:5124/api/transactions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    .then(async response => {
      if (response.ok) {
        fetchPortfolio(); 
        fetchTransactions(); 
        setFormData({ ...formData, symbol: '', quantity: '', price: '', date: '' }); 
        alert("İşlem başarıyla kaydedildi!");
      } else {
        const errorText = await response.text();
        alert("Hata: " + errorText);
      }
    })
    .catch(error => console.error("Kayıt hatası:", error))
    .finally(() => setIsSubmitting(false));
  };

  const handleDeleteTransaction = (id) => {
    if (!window.confirm("Bu işlemi silmek istediğinize emin misiniz?")) return;
    fetch(`http://localhost:5124/api/transactions/${id}`, { method: 'DELETE' })
      .then(res => res.ok && (fetchPortfolio() || fetchTransactions()))
      .catch(err => console.error(err));
  };

  return (
    <div className="container">
      <div className="header-section">
        <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
          <h1>Finansal Portföyüm</h1>
          {/* DARK MODE BUTONU */}
          <button 
            onClick={() => setIsDarkMode(!isDarkMode)} 
            style={{ background: 'none', border: 'none', fontSize: '1.5rem', cursor: 'pointer' }}
            title="Temayı Değiştir"
          >
            {isDarkMode ? '☀️' : '🌙'}
          </button>
        </div>

        <button className={`ai-button ${isAnalyzing ? 'loading' : ''}`} onClick={handleAnalyze} disabled={isAnalyzing || portfolio.length === 0}>
          {isAnalyzing ? "Analiz Ediliyor..." : "✨ Portföyümü Yorumla"}
        </button>
      </div>

      {aiComment && (
        <div className="ai-comment-box">
          <h3>🤖 Finansal Asistanın Diyor ki:</h3>
          <p>{aiComment}</p>
        </div>
      )}
      
      <div className="transaction-form-card">
        <h3>Hızlı İşlem Ekle</h3>
        <form onSubmit={handleSubmitTransaction} className="transaction-form">
          <div className="form-group">
            <label>İşlem Tipi</label>
            <select value={formData.type} onChange={e => setFormData({...formData, type: e.target.value})}>
              <option value="Buy">Alış Yap</option>
              <option value="Sell">Satış Yap</option>
            </select>
          </div>
          <div className="form-group">
            <label>Hisse Kodu (Sembol)</label>
            <input 
              type="text" 
              placeholder="Örn: NVDA, KCHOL, AAPL" 
              value={formData.symbol} 
              onChange={e => setFormData({...formData, symbol: e.target.value})}
              required
            />
          </div>
          <div className="form-group">
            <label>Adet</label>
            <input type="number" min="0.0001" step="any" placeholder="Örn: 10" value={formData.quantity} onChange={e => setFormData({...formData, quantity: e.target.value})} required/>
          </div>
          <div className="form-group">
            <label>Birim Fiyat</label>
            <input type="number" min="0" step="any" placeholder="Boş = Otomatik" value={formData.price} onChange={e => setFormData({...formData, price: e.target.value})}/>
          </div>
          <div className="form-group">
            <label>Tarih</label>
            <input type="date" value={formData.date} onChange={e => setFormData({...formData, date: e.target.value})}/>
          </div>
          <button type="submit" className="submit-button" disabled={isSubmitting}>
            {isSubmitting ? "Kaydediliyor..." : "İşlemi Kaydet"}
          </button>
        </form>
      </div>

      {loading ? (
        <p className="loading-text">Cüzdan bilgileri yükleniyor...</p>
      ) : (
        <>
          {/* SADECE BU KISMI GÜNCELLE */}
          {portfolio.length > 0 && (
            <div className="chart-section" style={{ width: '100%', height: 350, backgroundColor: 'white', padding: '20px', borderRadius: '10px', marginBottom: '20px', boxShadow: '0 4px 6px rgba(0,0,0,0.05)' }}>
              <h2 style={{ textAlign: 'center', marginBottom: '20px', color: '#2c3e50', fontSize: '1.5rem' }}>Portföy Dağılımı (TL Bazında)</h2>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={portfolio}
                    dataKey="totalValueTRY" /* DİKKAT: Artık düz rakama değil, TL karşılığına bakıyor! */
                    nameKey="symbol"
                    cx="50%"
                    cy="50%"
                    outerRadius={100}
                    fill="#8884d8"
                    label={({ symbol, percent }) => `${symbol} ${(percent * 100).toFixed(0)}%`}
                  >
                    {portfolio.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip 
                    formatter={(value, name, props) => {
                      // Grafikte her şeyin TL karşılığını gösteriyoruz
                      return [`₺${value.toFixed(2)}`, 'Güncel Kur ile TL Karşılığı'];
                    }}
                  />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </div>
          )}

          <div className="card-grid">
            {portfolio.map((item) => (
              <div key={item.assetId} className="card">
                <div className="card-header">
                  <h2>{item.symbol}</h2>
                  <span className="asset-type">{item.type}</span>
                </div>
                <p className="name">{item.name}</p>
                <hr />
                <div className="card-details">
                  <p><span>Maliyet:</span> {item.currency === 'TRY' ? '₺' : '$'}{item.averageCost}</p>
                  <p><span>Toplam:</span> {item.currency === 'TRY' ? '₺' : '$'}{item.totalInvestment}</p>
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      <div className="transactions-section">
        <h2>İşlem Geçmişi</h2>
        <div className="table-container">
          <table className="transactions-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Hisse</th>
                <th>Tip</th>
                <th>Adet</th>
                <th>Birim Fiyat</th>
                <th>Toplam</th>
                <th>Aksiyon</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map(t => (
                <tr key={t.id}>
                  <td>{new Date(t.date).toLocaleDateString('tr-TR')}</td>
                  <td><strong>{t.symbol}</strong></td>
                  <td>
                    <span className={`badge ${t.transactionType === 'Buy' ? 'badge-buy' : 'badge-sell'}`}>
                      {t.transactionType === 'Buy' ? 'ALIŞ' : 'SATIŞ'}
                    </span>
                  </td>
                  <td>{t.quantity}</td>
                  <td>{t.currency === 'TRY' ? '₺' : '$'}{t.unitPrice}</td>
                  <td><strong>{t.currency === 'TRY' ? '₺' : '$'}{(t.quantity * t.unitPrice).toFixed(2)}</strong></td>
                  <td>
                    <button className="delete-button" onClick={() => handleDeleteTransaction(t.id)}>🗑️</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

export default App;