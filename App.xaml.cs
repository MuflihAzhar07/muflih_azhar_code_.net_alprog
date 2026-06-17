using Microsoft.Extensions.DependencyInjection;

namespace VitalGuard
{
    public partial class App : Application
    {
        // Variabel statis untuk mengakses database secara global
        static LocalDatabase database;
        public static LocalDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new LocalDatabase();
                }
                return database;
            }
        }
        public App()
        {
            // Preferences.Clear();
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}