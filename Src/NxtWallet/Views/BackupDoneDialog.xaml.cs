﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NxtWallet.Views
{
    public sealed partial class BackupDoneDialog
    {
        public BackupDoneDialog()
        {
            InitializeComponent();
        }

        private void OnFinishButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var appShell = (AppShell) Window.Current.Content;
            appShell.Navigate(NavigationPage.ReceiveMoneyPage, null);
        }
    }
}
