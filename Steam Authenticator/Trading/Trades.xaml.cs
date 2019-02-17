using SteamAuth;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SteamAuthenticator.Trading
{
    /// <summary>
    /// Interaction logic for Trades.xaml
    /// </summary>
    public partial class Trades : Window
    {
        private TradeHandler tradeHandler;

        private int accountIndex = -1;

        private TradeAccount currentAccount;

        private bool acpt = false;
        private bool deny = false;

        public Trades(object mainHandle)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            tradeHandler = (TradeHandler)mainHandle;
            InitializeComponent();

            btnAccept.Content = Properties.strings.TradeAccept;
            btnDeny.Content = Properties.strings.TradeDeny;
            btnMoreInfo.Content = Properties.strings.TradeMoreInfoBTN;
            refreshTrades.Content = Properties.strings.btnRefresh;

        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Pos(Window owner)
        {
            Left = owner.Left + (owner.ActualWidth - ActualWidth) / 2;
            Top = owner.Top + (owner.ActualHeight - ActualHeight) / 2;
        }
        public void ShowDialog(Window owner)
        {
            Pos(owner);
            ShowDialog();
        }
        public void ShowDialog(string accountName, Window owner)
        {
            for (var i = 0; i < tradeHandler.TradeAccounts.Length; i++)
                if (tradeHandler.TradeAccounts[i].Account.AccountName == accountName)
                    accountIndex = i;
            if (accountIndex >= 0 && accountIndex < accountSelection.Items.Count)
                accountSelection.SelectedIndex = accountIndex;

            Pos(owner);
            ShowDialog();
        }
        public void Show(Window owner)
        {
            Pos(owner);
            Show();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            accountSelection.Items.Clear();
            tradeHandler.CleanUp();
            for (var i = 0; i < tradeHandler.TradeAccounts.Length; i++)
            {
                Label lbl = new Label
                {
                    Content = tradeHandler.TradeAccounts[i].Account.AccountName
                };
                accountSelection.Items.Add(lbl);
            }
            accountSelection.SelectedIndex = 0;
            if (accountIndex >= 0 && accountIndex < accountSelection.Items.Count)
                accountSelection.SelectedIndex = accountIndex;

            Reset();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }




        public void Reset()
        {
            btnAccept.Content = Properties.strings.TradeAccept;
            btnDeny.Content = Properties.strings.TradeDeny;
            lblConfirm.Content = "";
            //if (confirms.Count() == 0) Close();
            listBox.Items.Clear();

            if (currentAccount.Confirmations != null)
                foreach (Confirmation conf in currentAccount.Confirmations)
                {
                    ListBoxItem item = new ListBoxItem();
                    Label label = new Label();
                    if (conf.ConfType == Confirmation.ConfirmationType.MarketSellTransaction)
                        label.Content = String.Format(Properties.strings.TradeMarket, conf.Creator);
                    else if (conf.ConfType == Confirmation.ConfirmationType.GenericConfirmation)
                        label.Content = String.Format(Properties.strings.TradeGeneric, conf.Creator);
                    else
                        label.Content = conf.Description;
                    item.Content = label;
                    listBox.Items.Add(item);
                }

            listBox.SelectedIndex = 0;
            if (currentAccount.Confirmations != null && listBox.SelectedIndex != -1)
            {
                lblTradeInfo.Text = String.Format(Properties.strings.TradeDetails, tradeHandler.CahcedUpdateString, currentAccount.Confirmations[listBox.SelectedIndex].Description, currentAccount.Confirmations[listBox.SelectedIndex].Time);
                btnAccept.IsEnabled = btnDeny.IsEnabled = btnMoreInfo.IsEnabled = true;
            }
            else
            {
                lblTradeInfo.Text = String.Format(Properties.strings.TradeCachedTime, tradeHandler.CahcedUpdateString);
                btnAccept.IsEnabled = btnDeny.IsEnabled = btnMoreInfo.IsEnabled = false;
            }

        }

        private void RefreshTrades_Click(object sender, RoutedEventArgs e)
        {
            tradeHandler.RefreshConfirmations();
            Reset();
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            deny = false;
            if (acpt)
            {
                lblConfirm.Content = Properties.strings.TradePleaseWait;
                currentAccount.Account.AcceptConfirmation(currentAccount.Confirmations[listBox.SelectedIndex]);
                currentAccount = tradeHandler.RemoveTradeID(currentAccount.Account.AccountName, currentAccount.Confirmations[listBox.SelectedIndex].ID.ToString());
                listBox.Items.RemoveAt(listBox.SelectedIndex);
                Reset();
            }
            else
            {
                acpt = true;
                lblConfirm.Content = Properties.strings.TradeDoubleAccept;
            }
        }

        private void BtnDeny_Click(object sender, RoutedEventArgs e)
        {
            acpt = false;
            if (deny)
            {
                lblConfirm.Content = Properties.strings.TradePleaseWait;
                currentAccount.Account.DenyConfirmation(currentAccount.Confirmations[listBox.SelectedIndex]);
                currentAccount = tradeHandler.RemoveTradeID(currentAccount.Account.AccountName, currentAccount.Confirmations[listBox.SelectedIndex].ID.ToString());
                listBox.Items.RemoveAt(listBox.SelectedIndex);
                Reset();
            }
            else
            {
                deny = true;
                lblConfirm.Content = Properties.strings.TradeDoubleDeny;
            }
        }

        private void BtnMoreInfo_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedIndex != -1)
                new Trades_MoreInfo(currentAccount.Confirmations[listBox.SelectedIndex]).Show(this);
        }

        private void AccountSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!accountSelection.Items.IsEmpty)
                for (var i = 0; i < tradeHandler.TradeAccounts.Length; i++)
                    if (tradeHandler.TradeAccounts[i].Account.AccountName == (string)((Label)accountSelection.Items[accountSelection.SelectedIndex]).Content)
                        currentAccount = tradeHandler.TradeAccounts[i];

            Reset();
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (currentAccount.Confirmations[listBox.SelectedIndex] != null)
                    lblTradeInfo.Text = String.Format(Properties.strings.TradeDetails, tradeHandler.CahcedUpdateString, currentAccount.Confirmations[listBox.SelectedIndex].Description, currentAccount.Confirmations[listBox.SelectedIndex].Time);
            }
            catch (Exception) { }
        }


    }
}