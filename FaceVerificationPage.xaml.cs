using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SkiaSharp;
using System;
using System.IO;
using VitalGuard;

namespace VitalGuard // GANTI INI
{
    public partial class FaceVerificationPage : ContentPage
    {
        private readonly string _username;
        private readonly string _dbPhotoPath;

        public FaceVerificationPage(string username, string role)
        {
            InitializeComponent();
            _username = username;
            LabelRole.Text = $"User: {username} | Role: {role}";
            _dbPhotoPath = Path.Combine(FileSystem.AppDataDirectory, $"{username}_face.png");
        }

        private async void OnVerifyClicked(object sender, EventArgs e)
        {
            if (!File.Exists(_dbPhotoPath))
            {
                await DisplayAlert("Error Database", "Foto wajah untuk user ini tidak ditemukan!", "OK");
                return;
            }

            try
            {
                // 1. Ambil Foto Menggunakan Kamera
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return;

                // Salin stream ke MemoryStream agar bisa dibaca dua kali (untuk preview dan analisis)
                byte[] capturedBytes;
                using (var liveStream = await photo.OpenReadAsync())
                using (var ms = new MemoryStream())
                {
                    await liveStream.CopyToAsync(ms);
                    capturedBytes = ms.ToArray();
                }

                // Tampilkan preview foto yang baru saja diambil ke UI
                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(capturedBytes));

                // 2. Load Gambar Kamera Baru ke SkiaSharp
                using var newMemoryStream = new MemoryStream(capturedBytes);
                using var newSkStream = new SKManagedStream(newMemoryStream);
                using var newBitmap = SKBitmap.Decode(newSkStream);

                // 3. Load Gambar Pembanding dari Database Lokal
                using var dbStream = File.OpenRead(_dbPhotoPath);
                using var dbSkStream = new SKManagedStream(dbStream);
                using var dbBitmap = SKBitmap.Decode(dbSkStream);

                if (newBitmap == null || dbBitmap == null)
                {
                    await DisplayAlert("Error", "Gagal memproses salah satu gambar wajah.", "OK");
                    return;
                }

                // 4. Proses Resizing Menjadi Grid 16x16 (Total 256 Pixel)
                var targetInfo = new SKImageInfo(16, 16);
                using var resizedNew = newBitmap.Resize(targetInfo, SKFilterQuality.Medium);
                using var resizedDb = dbBitmap.Resize(targetInfo, SKFilterQuality.Medium);

                int totalPixels = 256;
                int matchedPixels = 0;

                // 5. Algoritma Perbandingan Pixel Kedekatan Warna Keabuan (Grayscale Comparison)
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        var c1 = resizedNew.GetPixel(x, y);
                        var c2 = resizedDb.GetPixel(x, y);

                        // Konversi RGB ke Nilai Grayscale Luminance
                        int gray1 = (int)(0.3 * c1.Red + 0.59 * c1.Green + 0.11 * c1.Blue);
                        int gray2 = (int)(0.3 * c2.Red + 0.59 * c2.Green + 0.11 * c2.Blue);

                        // Jika selisih ambang batas warna abu di bawah 50, dianggap mirip
                        if (Math.Abs(gray1 - gray2) < 50)
                        {
                            matchedPixels++;
                        }
                    }
                }

                // 6. Hitung Persentase Akurasi
                double similarity = (matchedPixels / (double)totalPixels) * 100;

                if (similarity >= 65.0)
                {
                    BtnLanjut.IsVisible = true;
                    await DisplayAlert("AKSES DITERIMA", $"Identitas Valid. Kemiripan: {similarity:F1}%", "OK");
                }
                else
                {
                    BtnLanjut.IsVisible = false;
                    await DisplayAlert("AKSES DITOLAK", $"Wajah Tidak Cocok! Kemiripan hanya: {similarity:F1}%", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Terjadi kesalahan: {ex.Message}", "OK");
            }
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            // Pastikan halaman MainDashboardPage sudah dibuat di project-mu
            Application.Current.MainPage = new MainDashboardPage();
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new MainPage();
        }
    }
}