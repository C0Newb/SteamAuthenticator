using Steam_Authenticator.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Steam_Authenticator.Forms
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private Manifest manifest = new Manifest();
        

        public Settings()
        {
            InitializeComponent();

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(keyUp), true);

            txtIntervalNum.Text = _numValue.ToString();
            loadSettings();
        }
        private void keyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void loadSettings()
        {
            manifest = Manifest.GetManifest();
            chkPeriodicChecking.IsChecked = manifest.PeriodicChecking;
            if (manifest.PeriodicChecking) gridInterval.IsEnabled = true; else gridInterval.IsEnabled = false;
            NumValue = manifest.PeriodicCheckingInterval;

            chkAutoConfirm_Market.IsChecked = manifest.AutoConfirmMarketTransactions;
            chkAutoConfirm_Trades.IsChecked = manifest.AutoConfirmTrades;

            chkAutoEntry.IsChecked = manifest.AutoEntry;

            chkUseMaFiles.IsChecked = manifest.UseMaFiles;

            chkAutoCheckForUpdates.IsChecked= manifest.CheckForUpdates;
            chkAllowBetaUpdates.IsChecked = manifest.AllowBetaUpdates;

            chkDevMode.IsChecked = manifest.DeveloperMode;

            chkDisplaySearch.IsChecked = manifest.DisplaySearch;
            check();
        }

        private void saveSettings()
        {
            PleaseWait wait = new PleaseWait("Saving settings, please wait.");
            manifest = Manifest.GetManifest();



            manifest.PeriodicChecking = (bool)chkPeriodicChecking.IsChecked;
            manifest.PeriodicCheckingInterval = NumValue;

            manifest.AutoConfirmMarketTransactions = (bool)chkAutoConfirm_Market.IsChecked;
            manifest.AutoConfirmTrades = (bool)chkAutoConfirm_Trades.IsChecked;

            manifest.AutoEntry = (bool)chkAutoEntry.IsChecked;

            manifest.UseMaFiles = (bool)chkUseMaFiles.IsChecked;

            manifest.CheckForUpdates = (bool)chkAutoCheckForUpdates.IsChecked;
            manifest.AllowBetaUpdates = (bool)chkAllowBetaUpdates.IsChecked;

            manifest.DeveloperMode = (bool)chkDevMode.IsChecked;

            manifest.DisplaySearch = (bool)chkDisplaySearch.IsChecked;

            manifest.Save();
            loadSettings();

        }


        #region For the checking interval
        private int _numValue = 0;

        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                txtIntervalNum.Text = value.ToString();
            }
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e) { NumValue++; }

        private void cmdDown_Click(object sender, RoutedEventArgs e) { NumValue--; }

        private void txtIntervalNum_TextChanged(object sender, TextChangedEventArgs e) { if (txtIntervalNum == null) return; if (!int.TryParse(txtIntervalNum.Text, out _numValue)) txtIntervalNum.Text = _numValue.ToString(); }
        #endregion

        private void check()
        {
            if (manifest.PeriodicChecking == chkPeriodicChecking.IsChecked &&
                manifest.PeriodicCheckingInterval == NumValue && //
                manifest.AutoConfirmMarketTransactions == chkAutoConfirm_Market.IsChecked &&
                manifest.AutoConfirmTrades == chkAutoConfirm_Trades.IsChecked && //
                manifest.AutoEntry == chkAutoEntry.IsChecked && //
                manifest.UseMaFiles == chkUseMaFiles.IsChecked && //
                manifest.CheckForUpdates == chkAutoCheckForUpdates.IsChecked &&
                manifest.AllowBetaUpdates == chkAllowBetaUpdates.IsChecked && //
                manifest.DeveloperMode == chkDevMode.IsChecked &&//
                manifest.DisplaySearch == chkDisplaySearch.IsChecked)
                btnSave.IsEnabled = false;
            else
                btnSave.IsEnabled = true;

            gridInterval.IsEnabled = (bool)chkPeriodicChecking.IsChecked;
        }

        private void chk_Checked(object sender, RoutedEventArgs e) { check(); }
        private void chk_Unchecked(object sender, RoutedEventArgs e) { check(); }

        private void btnSave_Click(object sender, RoutedEventArgs e) { saveSettings(); }
        private void btnExit_Click(object sender, RoutedEventArgs e) { Close(); }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (btnSave.IsEnabled==true)
            {
                MessageBoxResult res = MessageBox.Show("Wait wait, there are unsaved changes! Are you sure you want to leave and discard these changes?\nYes - Leave and discard\nNo - Save and leave\nCancel - dismiss this and go back", "Discard unsaved changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (res == MessageBoxResult.No)
                    saveSettings();
                else if (res == MessageBoxResult.Cancel)
                    e.Cancel = true;
            }
        }

        public void ShowDialog(Window owner)
        {
            Owner = owner;
            Left = owner.Left + (owner.ActualWidth - ActualWidth) / 2;
            Top = owner.Top + (owner.ActualHeight - ActualHeight) / 2;
            ShowDialog();
        }

        public void Show(Window owner)
        {
            Owner = owner;
            Left = owner.Left + (owner.ActualWidth - ActualWidth) / 2;
            Top = owner.Top + (owner.ActualHeight - ActualHeight) / 2;
            Show();
        }
    }
}
