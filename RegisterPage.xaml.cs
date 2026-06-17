using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using VitalGuard;

namespace VitalGuard // GANTI INI
{
    public partial class RegisterPage : ContentPage
    {
        private byte[] _faceData;

        public RegisterPage()
        {
            InitializeComponent();
            SetupRolePicker();
        }

        private void SetupRolePicker()
        {
            bool isDoctorTaken = Preferences.Get("has_doctor", false);

            PickerRole.Items.Clear();
            PickerRole.Items.Add("Pasien");

            if (!isDoctorTaken)
            {
                PickerRole.Items.Add("Dokter");
            }
        }

        private async void OnCaptureFaceClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return;

                // Menggunakan disposable pattern yang aman untuk mengolah stream foto
                using (var stream = await photo.OpenReadAsync())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    _faceData = memoryStream.ToArray();
                }

                // Menampilkan preview gambar dari byte array yang baru diambil
                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(_faceData));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Gagal mengambil foto: {ex.Message}", "OK");
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = EntryUsername.Text?.Trim();
            string password = EntryPassword.Text;

            // 1. Validasi Kelengkapan Input
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                PickerRole.SelectedIndex == -1 ||
                _faceData == null)
            {
                await DisplayAlert("Gagal", "Semua data (termasuk foto wajah) wajib diisi!", "OK");
                return;
            }

            // 2. Cek Apakah Username Sudah Terdaftar (Pencegahan Duplikasi)
            if (Preferences.ContainsKey($"user_pass_{username}"))
            {
                await DisplayAlert("Gagal", "Username sudah terdaftar! Gunakan nama lain.", "OK");
                return;
            }

            string selectedRole = PickerRole.SelectedItem.ToString();

            // 3. Simpan Kredensial & Role ke Preferences
            Preferences.Set($"user_pass_{username}", password);
            Preferences.Set($"user_role_{username}", selectedRole);

            // Kunci status jika doctor sudah dibuat
            if (selectedRole == "Dokter")
            {
                Preferences.Set("has_doctor", true);
            }

            // 4. Simpan File Foto Wajah ke Storage Lokal
            try
            {
                string imagePath = Path.Combine(FileSystem.AppDataDirectory, $"{username}_face.png");
                await File.WriteAllBytesAsync(imagePath, _faceData);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Peringatan", $"Gagal menyimpan file gambar: {ex.Message}", "OK");
            }

            // 5. Notifikasi Sukses & Pindah ke Halaman Login
            await DisplayAlert("Sukses", $"User '{username}' berhasil didaftarkan!", "OK");
            Application.Current.MainPage = new MainPage();
        }

        private void OnBackToLoginClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new MainPage();
        }
    }
}