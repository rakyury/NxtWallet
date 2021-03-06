﻿using AutoMapper;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using NxtWallet.Core;
using NxtWallet.Core.Fakes;
using NxtWallet.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;

namespace NxtWallet.IntegrationTest
{
    [TestClass]
    public class AccountLedgerRunnerTest
    {
        private List<LedgerEntry> _addedLedgerEntries = new List<LedgerEntry>();
        private const string _testnetFile = "known_testnet_addresses";
        private const string _mainnetFile = "known_mainnet_addresses";
        private readonly HashSet<string> ignoreAccounts = new HashSet<string> { "NXT-6MVF-XTDR-MDTK-DPHEU" };

        [TestMethod]
        public async Task TryCheckAllTransactionsForAllTestnetAccountTest()
        {
            await TryCheckAllTransactionsTest("NXT-6GT3-FZH7-4MG9-BF8RZ");
        }

        [TestMethod]
        public async Task TryCheckAllTransactionsForAllTestnetAccountsTest()
        {
            using (var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream($"NxtWallet.IntegrationTest.{_mainnetFile}.txt"))
            using (var reader = new System.IO.StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var accountRs = reader.ReadLine();
                    if (ignoreAccounts.Contains(accountRs))
                    {
                        continue;
                    }
                    Debug.WriteLine($"Checking account: {accountRs}");
                    await TryCheckAllTransactionsTest(accountRs);
                }
            }
        }

        public async Task TryCheckAllTransactionsTest(string accountRs)
        {
            var walletRepository = new FakeWalletRepository();
            walletRepository.NxtAccount = accountRs;
            walletRepository.IsReadOnlyAccount = true;
            walletRepository.NqtBalance = 0;
            walletRepository.SecretPhrase = string.Empty;
            walletRepository.LastLedgerEntryBlockId = 2680262203532249785UL;
            walletRepository.NxtServer = "http://localhost:7876/nxt";
            walletRepository.SleepTime = 10000;
            walletRepository.BackupCompleted = true;
            walletRepository.NotificationsEnabled = false;

            var mapper = MapperConfig.Setup(walletRepository).CreateMapper();
            var serviceFactory = new NxtLib.ServiceFactory(walletRepository.NxtServer);
            var nxtServer = new NxtServer(walletRepository, mapper, serviceFactory);
            var accountLedgerRepository = new FakeAccountLedgerRepository();
            var contactRepository = new FakeContactRepository();

            var runner = new AccountLedgerRunner(walletRepository, nxtServer, accountLedgerRepository);

            _addedLedgerEntries.Clear();
            Messenger.Default.Register<LedgerEntryMessage>(this, (message) => 
            {
                if (message.Action == LedgerEntryMessageAction.Added)
                {
                    _addedLedgerEntries.Add(message.LedgerEntry);
                }
            });
            var cancellationSource = new CancellationTokenSource();

            try
            {
                await runner.TryCheckAllLedgerEntries(cancellationSource.Token);
            }
            catch (Exception e)
            {
                throw new Exception($"Exception with account: {accountRs}", e);
            }

            var previousBalance = 0L;
            for (int i = _addedLedgerEntries.Count - 1; i >= 0; i--)
            {
                var addedLedgerEntry = _addedLedgerEntries[i];
                var calculatedBalance = previousBalance;
                calculatedBalance += addedLedgerEntry.NqtAmount;
                calculatedBalance += (addedLedgerEntry.UserIsSender) ? addedLedgerEntry.NqtFee : 0;

                if (addedLedgerEntry.NqtBalance != calculatedBalance)
                {
                    throw new Exception($"Wrong balance for account: {accountRs}, expected: {calculatedBalance} but got: {addedLedgerEntry.NqtBalance} on height: {addedLedgerEntry.Height}");
                }
                previousBalance = calculatedBalance;
            }
        }
    }
}
