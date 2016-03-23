﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NxtWallet.ViewModel;

namespace NxtWallet.Views
{
    public sealed partial class TransactionListControl
    {
        public TransactionListViewModel ViewModel { get; } = new Ioc().TransactionListViewModel;

        public delegate void SelectedTransactionChangedHandler(object source, SelectionChangedEventArgs e);
        public event SelectedTransactionChangedHandler SelectedTransactionChanged;

        public TransactionListControl()
        {
            InitializeComponent();
        }

        private async void TransactionListControl_OnLoading(FrameworkElement sender, object args)
        {
            ViewModel.LoadFromRepository();
            Bindings.Update();
            await ViewModel.LoadFromNxtServerAsync();
            Bindings.Update();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedTransactionChanged?.Invoke(this, e);
        }
    }
}
