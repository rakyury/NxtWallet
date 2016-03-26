﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using NxtLib;
using NxtLib.Accounts;
using NxtLib.Local;

namespace NxtWallet.Model
{
    public class WalletRepository : IWalletRepository
    {
        private const string SecretPhraseKey = "secretPhrase";
        private const string NxtServerKey = "nxtServer";
        private const string BalanceKey = "balance";

        public AccountWithPublicKey NxtAccount { get; private set; }
        public string NxtServer { get; private set; }
        public string SecretPhrase { get; private set; }
        public string Balance { get; private set; }

        public async Task LoadAsync()
        {
            using (var context = new WalletContext())
            {
                CreateAndMigrateDb(context);

                var dbSettings = await context.Settings.ToListAsync();

                ReadOrGenerateSecretPhrase(dbSettings, context);
                ReadOrGenerateNxtServer(dbSettings, context);
                ReadOrGenerateBalance(dbSettings, context);

                NxtAccount = new LocalAccountService().GetAccount(AccountIdLocator.BySecretPhrase(SecretPhrase));
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ITransaction>> GetAllTransactionsAsync()
        {
            using (var context = new WalletContext())
            {
                var transactions = await context.Transactions
                    .OrderByDescending(t => t.Timestamp)
                    .ToListAsync();

                return transactions;
            }
        }

        public async Task SaveTransactionAsync(ITransaction transaction)
        {
            using (var context = new WalletContext())
            {
                var existingTransaction = await context.Transactions.SingleOrDefaultAsync(t => t.NxtId == transaction.NxtId);
                if (existingTransaction == null)
                {
                    context.Transactions.Add((Transaction)transaction);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task UpdateTransactionAsync(ITransaction transaction)
        {
            using (var context = new WalletContext())
            {
                context.Transactions.Attach((Transaction)transaction);
                context.Entry(transaction).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
        }

        public async Task SaveTransactionsAsync(IEnumerable<ITransaction> transactions)
        {
            using (var context = new WalletContext())
            {
                var existingTransactions = (await GetAllTransactionsAsync()).ToList();
                foreach (var transaction in transactions.Except(existingTransactions))
                {
                    context.Transactions.Add((Transaction)transaction);
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task SaveBalanceAsync(string balance)
        {
            using (var context = new WalletContext())
            {
                var dbBalance = await context.Settings.SingleOrDefaultAsync(s => s.Key.Equals(BalanceKey));
                if (dbBalance == null)
                {
                    context.Settings.Add(new Setting {Key = BalanceKey, Value = balance});
                }
                else
                {
                    dbBalance.Value = balance;
                }
                await context.SaveChangesAsync();
                Balance = balance;
            }
        }

        private void ReadOrGenerateBalance(IEnumerable<Setting> dbSettings, WalletContext context)
        {
            Balance = dbSettings.SingleOrDefault(s => s.Key.Equals(BalanceKey))?.Value;
            if (Balance == null)
            {
                Balance = "0.0";
                context.Settings.Add(new Setting {Key = BalanceKey, Value = Balance});
            }
        }

        private void ReadOrGenerateNxtServer(IEnumerable<Setting> dbSettings, WalletContext context)
        {
            NxtServer = dbSettings.SingleOrDefault(s => s.Key.Equals(NxtServerKey))?.Value;
            if (NxtServer == null)
            {
                //NxtServer = Constants.DefaultNxtUrl;
                NxtServer = Constants.TestnetNxtUrl;
                context.Settings.Add(new Setting {Key = NxtServerKey, Value = NxtServer});
            }
        }

        private void ReadOrGenerateSecretPhrase(IEnumerable<Setting> dbSettings, WalletContext context)
        {
            SecretPhrase = dbSettings.SingleOrDefault(s => s.Key.Equals(SecretPhraseKey))?.Value;
            if (SecretPhrase == null)
            {
                var generator = new LocalPasswordGenerator();
                SecretPhrase = generator.GeneratePassword();
                context.Settings.Add(new Setting {Key = SecretPhraseKey, Value = SecretPhrase});
            }
        }

        private static void CreateAndMigrateDb(WalletContext context)
        {
            context.Database.Migrate();
        }
    }
}