using InstallmentReminder.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstallmentReminder.Data
{
    public class InstallmentDatabase
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initLock = new(1, 1);


        private async Task InitializeAsync()
        {
            await _initLock.WaitAsync();

            try
            {
                if (_database != null)
                    return;

                string databasePath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "installments.db3");

                var connection = new SQLiteAsyncConnection(databasePath);

                await connection.CreateTableAsync<Installment>();

                _database = connection;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<int> SaveAsync(Installment installment)
        {
            await InitializeAsync();

            if (installment.Id == 0)
                return await _database!.InsertAsync(installment);

            return await _database!.UpdateAsync(installment);
        }
        public async Task<List<Installment>> GetAllAsync()
        {
            await InitializeAsync();

            return await _database!
                .Table<Installment>()
                .OrderBy(x => x.DueDate)
                .ToListAsync();
        }

    }
}
