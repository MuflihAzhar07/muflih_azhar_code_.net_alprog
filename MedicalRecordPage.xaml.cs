using Microsoft.Maui.Controls;
using System;

namespace VitalGuard
{
    public partial class MedicalRecordPage : ContentPage
    {
        public MedicalRecordPage()
        {
            InitializeComponent();
        }

        // Fungsi ini otomatis dipanggil MAUI setiap kali halaman muncul di layar
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Mengambil data dari database dan memasukkannya ke ListRiwayat di XAML
            ListRiwayat.ItemsSource = await App.Database.GetLogsAsync();
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new MainDashboardPage();
        }
    }
}