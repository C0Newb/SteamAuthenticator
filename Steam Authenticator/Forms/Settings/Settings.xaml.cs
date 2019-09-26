using MaterialDesignThemes.Wpf;
using SteamAuthenticator.BackEnd;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SteamAuthenticator.Forms
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

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            txtIntervalNum.Text = _value.ToString();
            LoadSettings();

#if DEBUG
            chkDevMode.Visibility = Visibility.Visible;
            chkDevMode.IsEnabled = true;
#endif

            // Locals
            chkPeriodicChecking.Content = Properties.strings.SettingsPeriodicallyCheck;
            label.Content = Properties.strings.SettingsCheckingInterval;
            chkAutoConfirm_Market.Content = Properties.strings.SettingsConfirmMarket;
            chkAutoConfirm_Trades.Content = Properties.strings.SettingsConfirmTrade;
            chkAutoEntry.Content = Properties.strings.SettingsAutoEntry;
            chkAutoEntry.ToolTip = Properties.strings.SettingsAutoEntryHelp;
            chkAutoCheckForUpdates.Content = Properties.strings.SettingsCheckForUpdates;
            chkAllowBetaUpdates.Content = Properties.strings.SettingsAllowBeta;
            chkAllowBetaUpdates.ToolTip = Properties.strings.SettingsAllowBetaHelp;
            chkDisplaySearch.Content = Properties.strings.SettingsSearch;
            //chkSortAlpha.Content = Properties.strings.SettingsSortAlpha;
            radDarkTheme.Content = Properties.strings.SettingsDarkTheme;
            radLightTheme.Content = Properties.strings.SettingsLightTheme;
            chkAutoRefreshSession.Content = Properties.strings.SettingsAutoRefreshSession;
            chkAutoRefreshSession.ToolTip = Properties.strings.SettingsAutoRefreshSessionH;

            btnSave.Content = Properties.strings.SettingsSaveBtn;
            btnExit.Content = Properties.strings.SettingsExitBtn;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void LoadSettings()
        {
            manifest = Manifest.GetManifest();
            chkPeriodicChecking.IsChecked = manifest.PeriodicChecking;
            if (manifest.PeriodicChecking)
                gridInterval.IsEnabled = true;
            else
                gridInterval.IsEnabled = false;
            InteravalValue = manifest.PeriodicCheckingInterval;

            chkAutoConfirm_Market.IsChecked = manifest.AutoConfirmMarketTransactions;
            chkAutoConfirm_Trades.IsChecked = manifest.AutoConfirmTrades;

            chkAutoEntry.IsChecked = manifest.AutoEntry;

            chkAutoRefreshSession.IsChecked = manifest.AutoRefreshSession;

            chkAutoCheckForUpdates.IsChecked = manifest.CheckForUpdates;
            chkAllowBetaUpdates.IsChecked = manifest.AllowBetaUpdates;

            chkDevMode.IsChecked = manifest.DeveloperMode;

            chkDisplaySearch.IsChecked = manifest.DisplaySearch;
            //chkSortAlpha.IsChecked = manifest.SortAlpha;

            if (manifest.BaseTheme == Theme.Light)
                radLightTheme.IsChecked = true;
            else
                radDarkTheme.IsChecked = true;

            Check();
        }

        private void SaveSettings()
        {
            manifest = Manifest.GetManifest();

            manifest.PeriodicChecking = (bool)chkPeriodicChecking.IsChecked;
            manifest.PeriodicCheckingInterval = InteravalValue;

            manifest.AutoConfirmMarketTransactions = (bool)chkAutoConfirm_Market.IsChecked;
            manifest.AutoConfirmTrades = (bool)chkAutoConfirm_Trades.IsChecked;

            manifest.AutoEntry = (bool)chkAutoEntry.IsChecked;

            manifest.AutoRefreshSession = (bool)chkAutoRefreshSession.IsChecked;

            manifest.CheckForUpdates = (bool)chkAutoCheckForUpdates.IsChecked;
            manifest.AllowBetaUpdates = (bool)chkAllowBetaUpdates.IsChecked;

            manifest.DeveloperMode = (bool)chkDevMode.IsChecked;

            manifest.DisplaySearch = (bool)chkDisplaySearch.IsChecked;
            //manifest.SortAlpha = (bool)chkSortAlpha.IsChecked;

            IBaseTheme themeBase = Theme.Dark;
            if (radLightTheme.IsChecked == true)
                themeBase = Theme.Light;
            manifest.BaseTheme = themeBase;

            // apply the new theme
            Color primaryColor = manifest.PrimaryColor;
            Color secondaryColor = manifest.AccentColor;
            IBaseTheme baseTheme = manifest.BaseTheme;

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            PaletteHelper pHelper = new PaletteHelper();
            pHelper.SetTheme(theme);
            // okay we good

            manifest.Save();

            LoadSettings();
        }


        #region For the checking interval
        private int _value = 0;

        public int InteravalValue
        {
            get { return _value; }
            set
            {
                _value = value;
                txtIntervalNum.Text = value.ToString();
                Check();
            }
        }

        private void CmdUp_Click(object sender, RoutedEventArgs e)
        {
            InteravalValue++;
        }

        private void CmdDown_Click(object sender, RoutedEventArgs e)
        {
            InteravalValue--;
        }

        private void TxtIntervalNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtIntervalNum == null)
                return;
            if (!int.TryParse(txtIntervalNum.Text, out _value))
                txtIntervalNum.Text = _value.ToString();
        }
        #endregion

        private void Check()
        {
            IBaseTheme themeBase = Theme.Dark;
            if (radLightTheme.IsChecked == true)
                themeBase = Theme.Light;

            if (manifest.PeriodicChecking == chkPeriodicChecking.IsChecked && //
                manifest.PeriodicCheckingInterval == InteravalValue && //
                manifest.AutoConfirmMarketTransactions == chkAutoConfirm_Market.IsChecked && //
                manifest.AutoConfirmTrades == chkAutoConfirm_Trades.IsChecked && //
                manifest.AutoEntry == chkAutoEntry.IsChecked && //
                manifest.AutoRefreshSession == chkAutoRefreshSession.IsChecked && //
                manifest.CheckForUpdates == chkAutoCheckForUpdates.IsChecked && //
                manifest.AllowBetaUpdates == chkAllowBetaUpdates.IsChecked && //
                manifest.DeveloperMode == chkDevMode.IsChecked && //
                manifest.DisplaySearch == chkDisplaySearch.IsChecked && //
                //manifest.SortAlpha == chkSortAlpha.IsChecked
                manifest.BaseTheme == themeBase)
                btnSave.IsEnabled = false;
            else
                btnSave.IsEnabled = true;

            gridInterval.IsEnabled = (bool)chkPeriodicChecking.IsChecked;
        }

        private void Chk_Checked(object sender, RoutedEventArgs e)
        {
            Check();
        }
        private void Chk_Unchecked(object sender, RoutedEventArgs e)
        {
            Check();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (btnSave.IsEnabled == true)
            {
                MessageBoxResult res = MessageBox.Show(Properties.strings.SettingsUnsaved, Properties.strings.SettingsUnsavedT, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (res == MessageBoxResult.No)
                    SaveSettings();
                else if (res == MessageBoxResult.Cancel)
                    e.Cancel = true;
            }
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
    }
}
