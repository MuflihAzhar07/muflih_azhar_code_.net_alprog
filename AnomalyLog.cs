using SQLite;
using System;

namespace VitalGuard
{
    public class AnomalyLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Timestamp { get; set; }
        public string SpO2 { get; set; }
        public string BPM { get; set; }
        public string NNProbability { get; set; }
        public string FuzzyStatus { get; set; }
        public string StatusColor { get; set; } // Menyimpan warna teks indikator
    }
}