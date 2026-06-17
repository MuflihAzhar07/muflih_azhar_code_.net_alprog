using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Ditambahkan untuk Preferences jika belum ter-import otomatis
using System;

namespace VitalGuard
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = EntryUsername.Text?.Trim();
            string password = EntryPassword.Text;

            // 1. Validasi Input Kosong
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Gagal", "Username dan password tidak boleh kosong!", "OK");
                return;
            }

            // 2. Validasi Akun Terdaftar
            string userKey = $"user_pass_{username}";
            if (!Preferences.ContainsKey(userKey))
            {
                await DisplayAlert("Ditolak", "Username tidak ditemukan!", "OK");
                return;
            }

            // 3. Validasi Password
            string storedPassword = Preferences.Get(userKey, string.Empty);
            if (password != storedPassword)
            {
                await DisplayAlert("Ditolak", "Password salah!", "OK");
                return;
            }

            // 4. Set Session & Pindah Halaman
            string role = Preferences.Get($"user_role_{username}", "User");

            Preferences.Set("session_username", username);
            Preferences.Set("session_role", role);

            // Ganti halaman utama ke FaceVerificationPage
            Application.Current.MainPage = new FaceVerificationPage(username, role);
        }

        private void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new RegisterPage();
        }
    }
}