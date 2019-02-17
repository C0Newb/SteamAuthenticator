using SteamAuth;
using System;
using System.Windows;
using System.Windows.Input;

namespace SteamAuthenticator.Trading
{
    /// <summary>
    /// Interaction logic for Trades_MoreInfo.xaml
    /// </summary>
    public partial class Trades_MoreInfo : Window
    {
        public Trades_MoreInfo(Confirmation confirm, SteamGuardAccount account = null)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            InitializeComponent();

            btnExit.Content = Properties.strings.btnExit;

            if (confirm == null)
                Close();
            else if (account != null)
                txtMore.Text = String.Format(Properties.strings.TradeMoreInfo,account.AccountName,confirm.ConfType,confirm.Creator,confirm.Description,confirm.Receiving,confirm.Time,confirm.ID,confirm.Key,confirm.IntType,confirm.GetHashCode());
            else
                txtMore.Text = String.Format(Properties.strings.TradeMoreInfoNA, "", confirm.ConfType, confirm.Creator, confirm.Description, confirm.Receiving, confirm.Time, confirm.ID, confirm.Key, confirm.IntType, confirm.GetHashCode());
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void ShowDialog(Window owner)
        {
            if (owner != null)
            {
                Owner = owner;
                Left = owner.Left + (owner.ActualWidth - ActualWidth) / 2;
                Top = owner.Top + (owner.ActualHeight - ActualHeight) / 2;
            }
            ShowDialog();
        }

        public void Show(Window owner)
        {
            if (owner != null)
            {
                Owner = owner;
                Left = owner.Left + (owner.ActualWidth - ActualWidth) / 2;
                Top = owner.Top + (owner.ActualHeight - ActualHeight) / 2;
            }
            Show();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception) { }
        }
    }
}
