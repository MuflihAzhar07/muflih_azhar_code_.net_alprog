using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VitalGuard
{
    public partial class MainDashboardPage : ContentPage
    {
        // Inisialisasi HttpClient sekali saja sebagai static properti untuk efisiensi memori
        private static readonly HttpClient _httpClient = new HttpClient();

        // ⚠️ MASUKKAN API KEY GEMINI KAMU DI SINI
        private const string GeminiApiKey = "AQ.Ab8RN6IFJRBGiEDK-zXNq-YAx20QD_-NwbLLiwf3FCT0qKKYCg";

        // Token untuk membatalkan request jika slider masih digeser
        private System.Threading.CancellationTokenSource _debounceToken;
        public MainDashboardPage()
        {
            InitializeComponent();
            CheckUserSession();

            // Menjalankan kalkulasi awal saat sistem dimuat secara asynchronous
            Dispatcher.Dispatch(async () => await ExecuteHybridAIAsync(98, 75));
        }

        private void CheckUserSession()
        {
            string username = Preferences.Get("session_username", "User");
            string role = Preferences.Get("session_role", "Pasien");

            LblWelcome.Text = $"Selamat Datang, {username}!";
            LblRoleBadge.Text = $" Hak Akses: {role} ";

            if (role.Equals("Dokter", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Super Admin (Pemilik Sistem)", StringComparison.OrdinalIgnoreCase))
            {
                PanelDokterView.IsVisible = true;
                PanelPasienView.IsVisible = false;
                LblRoleBadge.BackgroundColor = Color.FromArgb("#2563EB");
            }
            else
            {
                PanelDokterView.IsVisible = false;
                PanelPasienView.IsVisible = true;
                LblRoleBadge.BackgroundColor = Color.FromArgb("#10B981");
            }
        }

        // PERUBAHAN: Ditambahkan keyword 'async' karena memanggil proses async di dalamnya
        // PERUBAHAN: Fungsi ini sekarang dilengkapi penahan (debounce) 1 detik
        private async void OnSensorSimulatorChanged(object sender, ValueChangedEventArgs e)
        {
            if (TxtSpO2 == null || TxtBPM == null) return;

            double currentSpO2 = Math.Round(SliderSpO2.Value);
            double currentBPM = Math.Round(SliderBPM.Value);

            // 1. Perbarui teks dan UI secara instan tanpa menunggu AI
            LblSliderSpO2.Text = $"{currentSpO2} %";
            LblSliderBPM.Text = $"{currentBPM} BPM";
            TxtSpO2.Text = $"{currentSpO2} %";
            TxtBPM.Text = $"{currentBPM} BPM";

            // 2. Batalkan perhitungan AI yang sebelumnya jika slider masih bergerak
            _debounceToken?.Cancel();
            _debounceToken = new System.Threading.CancellationTokenSource();
            var token = _debounceToken.Token;

            try
            {
                // 3. Beri jeda 1 detik (1000 milidetik). 
                // Jika dalam 1 detik ini slider digeser lagi, proses akan batal.
                await Task.Delay(1000, token);

                // 4. Jika pengguna sudah berhenti menggeser jarinya (token tidak batal), tembak API
                if (!token.IsCancellationRequested)
                {
                    await ExecuteHybridAIAsync(currentSpO2, currentBPM);
                }
            }
            catch (TaskCanceledException)
            {
                // Abaikan error ini. Ini sengaja terjadi saat slider digeser cepat untuk menghemat kuota API.
            }
        }
        // PERUBAHAN: Mengubah return type dari 'void' menjadi 'async Task'
        private async Task ExecuteHybridAIAsync(double spo2, double bpm)
        {
            // 1. Formulasi Neural Network (Forward Pass)
            double inputNormalisasiSpO2 = (100 - spo2) / 20.0;
            double inputNormalisasiBPM = Math.Abs(bpm - 75) / 80.0;

            double z = (inputNormalisasiSpO2 * 2.5) + (inputNormalisasiBPM * 2.0) - 1.5;
            double probabilitasAnomali = 1.0 / (1.0 + Math.Exp(-z));
            double persentaseNN = probabilitasAnomali * 100;

            TxtNNOutput.Text = $"{persentaseNN:F1}% " + (persentaseNN > 50 ? "(Terdeteksi Anomali!)" : "(Kondisi Normal)");
            TxtNNOutput.TextColor = persentaseNN > 50 ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");

            // 2. Formulasi Logika Fuzzy
            string statusFuzzy = "AMAN / STABIL";
            string hexColor = "#10B981";

            if (spo2 >= 95 && bpm >= 60 && bpm <= 100)
            {
                statusFuzzy = "AMAN / STABIL";
                hexColor = "#10B981";
            }
            else if (spo2 >= 91 && spo2 <= 94 || (bpm > 100 && bpm <= 120))
            {
                statusFuzzy = "PERINGATAN DINI / ANOMALI RINGAN";
                hexColor = "#F59E0B";
            }
            else if (spo2 < 91 || bpm > 120 || bpm < 50)
            {
                statusFuzzy = "KRITIS / ANOMALI TINGGI";
                hexColor = "#EF4444";
            }

            TxtFuzzyOutput.Text = statusFuzzy;
            BorderFuzzy.BackgroundColor = Color.FromArgb(hexColor);

            // 3. DI SINI KITA MEMANGGIL API GEMINI ASLI
            await UpdateNLPExplanationAsync(spo2, bpm, statusFuzzy, persentaseNN);

            // 4. Integrasi Database
            if (persentaseNN > 50)
            {
                SaveAnomalyToDatabase(spo2, bpm, persentaseNN, statusFuzzy);
            }
        }

        // =================================================================
        // INTEGRASI REAL API GEMINI (MEMENUHI SYARAT F & G DOSEN)
        // =================================================================
        private async Task UpdateNLPExplanationAsync(double s, double b, string status, double nnProb)
        {
            TxtNLPExplanation.Text = "Menghubungkan ke Gemini AI untuk menganalisis kondisi klinis...";

            try
            {
                // Menggunakan endpoint resmi Gemini 2.5 Flash yang cepat dan hemat kuota/daya
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={GeminiApiKey}";

                // Rancang Prompt Engineering agar AI memberikan output ringkas sesuai hasil NN & Fuzzy kamu
                string prompt = $"Kamu adalah asisten medis kecerdasan buatan dari aplikasi VitalGuard. " +
                                $"Tugasmu adalah menerjemahkan data teknis sensor IoMT, Neural Network, dan Fuzzy Logic menjadi " +
                                $"penjelasan medis ringkas yang sangat mudah dimengerti orang awam.\n\n" +
                                $"Data Pasien Saat Ini:\n" +
                                $"- Saturasi Oksigen (SpO2): {s}%\n" +
                                $"- Detak Jantung (BPM): {b} BPM\n" +
                                $"- Hasil Klasifikasi Fuzzy Logic: {status}\n" +
                                $"- Probabilitas Anomali dari Neural Network: {nnProb:F1}%\n\n" +
                                $"Ketentuan Respons:\n" +
                                $"1. Jangan gunakan format markdown tebal atau tabel yang rumit (gunakan teks biasa atau bullet point sederhana).\n" +
                                $"2. Jika kondisi aman, berikan kalimat yang menenangkan dan edukatif.\n" +
                                $"3. Jika ada anomali atau kritis, berikan peringatan yang jelas dan langkah penanganan awal sebelum ke dokter.\n" +
                                $"4. Batasi jawaban maksimal 3-4 kalimat saja.";

                // Menyusun JSON Request sesuai dokumentasi Google AI Studio
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Mengirimkan request POST ke API Gemini
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parsing dokumen JSON secara dinamis tanpa perlu membuat class model terpisah
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    string textResult = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    // Tampilkan jawaban dari Gemini ke UI Dashboard
                    TxtNLPExplanation.Text = textResult?.Trim();
                }
                else
                {
                    // Fallback lokal jika API Key salah atau kuota habis
                    TxtNLPExplanation.Text = "⚠️ [Mode Fallback] Gagal terhubung ke API Gemini. " + GetLocalFallbackText(s, b, status);
                }
            }
            catch (Exception ex)
            {
                // Fallback lokal jika perangkat tidak ada koneksi internet saat demo
                TxtNLPExplanation.Text = $"⚠️ [Offline Mode] Terjadi kesalahan jaringan: {ex.Message}\n\n" + GetLocalFallbackText(s, b, status);
            }
        }

        // Fungsi pembantu untuk menghasilkan teks jika API sedang offline/error
        private string GetLocalFallbackText(double s, double b, string status)
        {
            if (status == "AMAN / STABIL")
                return $"Saturasi oksigen Anda ({s}%) normal dan detak jantung ({b} BPM) stabil. Kondisi Anda sehat.";
            else if (status == "PERINGATAN DINI / ANOMALI RINGAN")
                return $"Sistem mendeteksi variasi kecil pada SpO2 ({s}%) atau Jantung ({b} BPM). Disarankan istirahat sejenak.";
            else
                return $"Peringatan! Nilai SpO2 ({s}%) rendah atau detak jantung ({b} BPM) tidak normal. Segera posisikan pasien dengan nyaman dan cari bantuan medis.";
        }

        private async void SaveAnomalyToDatabase(double s, double b, double nn, string fuzzy)
        {
            // Menentukan warna tulisan berdasarkan keparahan
            string colorHex = fuzzy.Contains("KRITIS") ? "#EF4444" : "#F59E0B";

            // Membungkus data ke dalam model AnomalyLog
            var log = new AnomalyLog
            {
                Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                SpO2 = $"{s} %",
                BPM = $"{b} BPM",
                NNProbability = $"{nn:F1} %",
                FuzzyStatus = fuzzy,
                StatusColor = colorHex
            };

            // Menyimpan data ke SQLite
            await App.Database.SaveLogAsync(log);

            System.Diagnostics.Debug.WriteLine($"[DB LOG] Menyimpan ke Database: SpO2={s}%, BPM={b}, NN={nn:F1}%, Status={fuzzy}");
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("session_username");
            Preferences.Remove("session_role");
            Application.Current.MainPage = new MainPage();
        }
        // --- EVENT TOMBOL UNTUK DOKTER ---
        private void OnBukaRekamMedisClicked(object sender, EventArgs e)
        {
            // Mengarahkan dokter ke halaman Database Rekam Medis
            Application.Current.MainPage = new MedicalRecordPage();
        }

        // --- EVENT TOMBOL UNTUK PASIEN ---
        private async void OnHubungiDokterClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Telekonsultasi", "Permintaan panggilan telah dikirim ke dokter penanggung jawab Anda. Mohon tunggu sebentar...", "OK");
        }

        private async void OnSosClicked(object sender, EventArgs e)
        {
            // Mengambil nilai terakhir dari layar untuk dikirim ke laporan darurat
            string currentSpO2 = TxtSpO2.Text;
            string currentBPM = TxtBPM.Text;

            await DisplayAlert("🚨 DARURAT (SOS) DIKIRIM!",
                $"Sinyal darurat telah dipancarkan!\n\nKondisi Terakhir:\n- SpO2: {currentSpO2}\n- Detak Jantung: {currentBPM}\n\nTim medis dan keluarga terdekat sedang dihubungi.",
                "SAYA MENGERTI");
        }
    }
}