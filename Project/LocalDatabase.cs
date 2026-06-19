using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace VitalGuard
{
    public class LocalDatabase
    {
        private SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database is not null)
                return;

            // Membuat file database di penyimpanan internal perangkat
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "VitalGuard.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Membuat tabel berdasarkan struktur AnomalyLog
            await _database.CreateTableAsync<AnomalyLog>();
        }

        public async Task<List<AnomalyLog>> GetLogsAsync()
        {
            await Init();
            // Mengambil semua data dan mengurutkannya dari yang paling baru
            return await _database.Table<AnomalyLog>().OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<int> SaveLogAsync(AnomalyLog log)
        {
            await Init();
            // Menyimpan data anomali baru
            return await _database.InsertAsync(log);
        }
    }
}