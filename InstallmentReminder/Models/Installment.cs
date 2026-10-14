using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace InstallmentReminder.Models
{
    public class Installment
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime ReminderAt { get; set; }

        public string? ImagePath { get; set; }

        public string? Notes { get; set; }

        public bool IsPaid { get; set; }
    }
}
