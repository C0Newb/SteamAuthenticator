using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using SteamAuthenticator.Notification;
using SteamAuthenticator.BackEnd;
using SteamAuthenticator.Forms;
using SteamAuthenticator.Trading;
using SteamAuth;
using System.Security;

using MaterialDesignThemes.Wpf;
using System.Windows.Media;

namespace SteamAuthenticator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    [ComVisible(true)]
    [Guid("ddad094a-bb27-4630-a930-5a474381d246")] // This is the application's GUID, used for notification things
    [ClassInterface(ClassInterfaceType.None)]

    public partial class MainWindow : Window
    {
        #region
        private PleaseWait wait = new PleaseWait(Properties.strings.MainWindowSetupFirstTime);
        private DispatcherTimer TimerSteamGuardForeground;
        private DispatcherTimer TimerSteamGuardBackground;
        private DispatcherTimer TimerTradeChecking;

        private System.Windows.Forms.NotifyIcon trayIcon = new System.Windows.Forms.NotifyIcon();
        private System.Windows.Forms.MenuItem trayIconMenuRestore = new System.Windows.Forms.MenuItem();

        private static TradeHandler tradeHandler = new TradeHandler();
        private Trades tradeWindow = new Trades(tradeHandler);

        private SteamGuardAccount[] allAccounts;
        private Manifest manifest;

        private long steamTime = 0;
        public long steamTimeP = 0;
        private long currentSteamChunk = 0;
        private SecureString passKey = null;

        private AutoEntry autoEnter = new AutoEntry();

        private UpdateMan UpdateManager;

        private string[] StartupArguments; // we process these AFTER setting up the window (unless '-q' is applied, '-q'/'-quiet')
        private bool QuietStartup = false; // see above
        #endregion

        #region Notification stuff
        public void ToastActivated(string appUserModelId, string invokedArgs)
        {
            Dispatcher.Invoke(() => // So this is what runs whenever someone 'activates' the toast notification (clicks on it)
            {
                Activate();

                string[] args = invokedArgs.Split(',');
                string action = "";
                for (int i = 0; i < args.Length; i++)
                {
                    string current = args[i];
                    if (current.StartsWith("action"))
                    {
                        action = current.Substring(7);
                        break;
                    }
                }
                switch (action)
                {
                    case "showTrades":
                        Trades tradeWindow = new Trades(tradeHandler);
                        tradeWindow.Show(this);
                        break;

                    case "debugActive":
                        MessageBox.Show("Activated");
                        break;

                    case "update":
                        UpdateManager.UpdateLatest();
                        break;

                    default:
                        break;
                }
            });
        }
        #endregion

        #region Window functions
        public MainWindow(string[] args)
        {
            InitializeComponent();

            if (args == null)
                args = new string[0];
            StartupArguments = args;
            for (int i = 0; i != args.Length; ++i)
            {
                if (args[i] == "q")
                    QuietStartup = true;
                    
            }

            UpdateManager = new UpdateMan(this); // So it knows which window is currently open
            manifest = Manifest.GetManifest(); // Load the manifest

            Notifications.RegisterAppForNotificationSupport(); // Setup notification support
            NotificationActivator.Initialize(); // Initialize
            NotificationActivator.mainWindow = this;

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            TimerSteamGuardForeground = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = new TimeSpan(0, 0, 1) // Every one second, tick. 1000 mili's;
            };  // This just updates the progress bar and time every one second.
            TimerSteamGuardForeground.Tick += TimerSteamGuardForeground_Tick;
            TimerSteamGuardForeground.Start();

            TimerSteamGuardBackground = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = new TimeSpan(0, 0, 5) // Tick every 5 seconds
            }; // Background tick. This runs ONLY when SA is in the background and is used to refresh auth codes and initate autoEntry. (Runs every 5s).
            TimerSteamGuardBackground.Tick += TimerSteamGuardBackground_Tick;
            //TimerSteamGuardBackground.Start();

            TimerTradeChecking = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = new TimeSpan(0, 0, manifest.PeriodicCheckingInterval)
            };
            TimerTradeChecking.Tick += TimerTradeChecking_Tick;

            // Localize
            menuFileAddAccount.Header = Properties.strings.MainWindowUIAddAccount;
            menuFileNew.Header = Properties.strings.MainWindowUILogin;
            menuFileImportMaFile.Header = Properties.strings.MainWindowUIImportFile;
            //menuFileAndroid.Header = Properties.strings.MainWindowUIImportAndroid;
            menuFileSettings.Header = Properties.strings.MainWindowUISettings;
            menuFileSettingsMain.Header = Properties.strings.MainWindowUIAuthenticatorSettings;
            menuFileSettingsEncryption.Header = Properties.strings.MainWindowUIEncryptionSettings;
            menuFileSettingsUpdate.Header = Properties.strings.MainWindowUIBrowseUpdates;
#if DEBUG
            menuFileDevMode.Header = Properties.strings.MainWindowUIDEV;
            devShowToastI.Header = Properties.strings.MainWindowUIDEVToastI;
            devShowToastE.Header = Properties.strings.MainWindowUIDEVToastE;
            devForceAE.Header = Properties.strings.MainWindowUIDEVFAE;
            devCrash.Header = Properties.strings.MainWindowUIDEVCrash;
            devRefreshManifest.Header = Properties.strings.MainWindowUIDEVRefreshManifest;
#endif
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!manifest.DeveloperMode)
            {
                menuFileDevMode.IsEnabled = false;
                menuFileDevMode.Visibility = Visibility.Collapsed;
            }
            else
            {
                menuFileDevMode.IsEnabled = true;
                menuFileDevMode.Visibility = Visibility.Visible;
            }

            if (!QuietStartup)
            {
                wait.Show(this);
                wait.Focus();
            }
            else
                Hide(); // see above, quiet startup
            wait.txtInfo.Text = Properties.strings.MainWindowSetupFirstTime;

            // Setup (icon)
            wait.txtInfo.Text = Properties.strings.MainWindowSetupTrayIcon;
            Tuple<System.Windows.Forms.NotifyIcon, System.Windows.Forms.MenuItem> tray = TrayIcon.Setup(TrayIconMenuRestore_Click, TrayIconMenuTrades_Click, TrayIconMenuCopyCode_Click, TrayIconMenuQuit_Click, TrayIcon_DoubleClick);
            trayIcon = tray.Item1;
            trayIconMenuRestore = tray.Item2;
            
            if (!manifest.DisplaySearch)
            {
                lblSearch.IsEnabled = txtSearch.IsEnabled = false;
                lblSearch.Visibility = txtSearch.Visibility = Visibility.Hidden;
                MinHeight = 160;
                accounts.Margin = new Thickness(0, 62, 0, 10);
            }
            else
                MinHeight = 190;
            /*if (manifest.SortAlpha)
                accounts.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
            else
                accounts.Items.SortDescriptions.Clear();*/

            // Tick first time manually to sync time
            wait.txtInfo.Text = Properties.strings.MainWindowSetupSyncTime;
            TimerSteamGuardForeground_Tick(new object(), EventArgs.Empty);

            // Check for updates
            if (manifest.CheckForUpdates)
            {
                wait.txtInfo.Text = Properties.strings.MainWindowSetupCheckForUpdate;
                if (UpdateManager.CheckForUpdate())
                    menuFileUpdate.Header = Properties.strings.MainWindowUIUpdateButton;
            }
            // Check if we updated
            try
            {
                if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + @"\updated"))
                {
                    System.IO.Directory.Delete(System.IO.Directory.GetCurrentDirectory() + @"\updatefiles", true);
                    System.IO.File.Delete(System.IO.Directory.GetCurrentDirectory() + @"\updated");
                }
            }
            catch (Exception) { }

            wait.txtInfo.Text = Properties.strings.MainWindowSetupDecrypt;

            if (manifest.Encrypted)
            {
                passKey = manifest.PromptForPasskey();
                if (passKey == null) Close();
            }

            wait.txtInfo.Text = Properties.strings.MainWindowPopulateAccounts;
            LoadAccountsList();

            if (manifest.PeriodicChecking)
                TimerTradeChecking.Start(); // Start checking for trades, if enabled...

            TimerTradeChecking_Tick(new object(), EventArgs.Empty); // Force a tick

            wait.txtInfo.Text = Properties.strings.MainWindowApplyTheme;

            // Theme
            // Okay. So I don't have a formal way of chaning this, BUT there is a way to change this via the manifest itself.
            // nevermind you can totally change the base theme color, but for the primary and secondary check the manifest
            Color primaryColor = manifest.PrimaryColor;
            Color secondaryColor = manifest.AccentColor;
            IBaseTheme baseTheme = manifest.BaseTheme;

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);
            PaletteHelper pHelper = new PaletteHelper();
            pHelper.SetTheme(theme);

            wait.txtInfo.Text = Properties.strings.MainWindowProcessArgs;

            // Approved arguments:
            /*
                -t, -trades, -viewtrades <accountName>: Open the trades window and focus on <accountName> (view the trades for <accountName>)
                -q: Quiet startup
                -RefreshSession, -rs <accountName>: Refresh <accountName>'s session
                -RefreshLogin, -relogin, -rl <accountName>: Open the 'login again' window for <accountName>
                -RemoveAccount, -rm <accountName>: Remove <accountName> from the manifest (only if 'ArgAllowRemove' is true)
                -copycode, -cc <accountName>: If allowed via the manifest, use this to copy the current authcode for <accountName> into the clipboard. MUST ENABLE 'ArgAllowAuthCopying'
                
                -exit, -e: Exit Steam Authenticator after processing all arguments
            */
            bool exitOnComplete = false;
            for (int i = 0; i != StartupArguments.Length; ++i)
            {
                if (StartupArguments[i].ToLower() == "-q" || StartupArguments[i].ToLower() == "-quiet")
                { } // to skip this arg
#if DEBUG
                else if (StartupArguments[i].ToLower() == "-printarguments")
                {
                    string args = "";
                    foreach (string arg in StartupArguments)
                        args = args + Environment.NewLine + arg;
                    MessageBox.Show(args);
                }
                else if (StartupArguments[i].ToLower() == "-debug")
                    manifest.DeveloperMode = true;
#endif
                else if (StartupArguments[i].ToLower() == "-e")
                    exitOnComplete = true;
                else
                {
                    // Okay, so it's a account based arg
                    bool accountFound = false;
                    if (StartupArguments.Length > i + 1)
                    {
                        foreach (SteamGuardAccount account in allAccounts)
                        {
                            if (StartupArguments[i + 1] == account.AccountName)
                            {
                                // this be the account
                                accountFound = true;
                                string arg = StartupArguments[i].ToLower(); // it'll be nicer below
                                i++;

                                // Now process the arg itself
                                if (arg == "-refreshsession" || arg == "-rs")
                                    account.RefreshSession();
                                else if (arg == "-refreshlogin" || arg == "-relogin" || arg == "-rl")
                                {
                                    wait.Hide();
                                    PromptRefreshLogin(account);
                                    wait.Show();
                                }
                                else if (arg == "-removeaccount" || arg == "-rm")
                                {
                                    if (!manifest.ArgAllowRemove)
                                    {
                                        try
                                        {
                                            if (manifest.Encrypted)
                                                MessageBox.Show(Properties.strings.MainWindowRemoveAccountManifestEncrypted, Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OK, MessageBoxImage.Error);
                                            else
                                            {
                                                MessageBoxResult res = MessageBox.Show(String.Format(Properties.strings.MainWindowRemoveAccountManifestConfirm, account.AccountName), Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OKCancel, MessageBoxImage.Information);
                                                if (res == MessageBoxResult.OK)
                                                {
                                                    manifest.RemoveAccount(account, false);
                                                    MessageBox.Show(String.Format(Properties.strings.MainWindowRemoveAccountManifestComplete, account.AccountName), Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OK, MessageBoxImage.Information);
                                                    LoadAccountsList();
                                                }
                                            }
                                        }
                                        catch (Exception) { }
                                    }

                                }
                                else if (arg == "-copycode" || arg == "-cc")
                                {
                                    if (!manifest.ArgAllowAuthCopy)
                                        Clipboard.SetText(account.GenerateSteamGuardCode());
                                }
                                else if (arg == "-viewtrades" || arg == "-trades" || arg == "-t")
                                {
                                    try
                                    {
                                        wait.Hide();
                                        tradeWindow.ShowDialog(account.AccountName, this);
                                        wait.Show();
                                    }
                                    catch (SteamGuardAccount.WGTokenInvalidException)
                                    { account.RefreshSession(); }
                                    catch (SteamGuardAccount.WGTokenExpiredException)
                                    { PromptRefreshLogin(account); }
                                    catch (WebException) { }
                                }
                            }
                        }
                        if (!accountFound)
                            MessageBox.Show("Sorry, but that account, \"" + StartupArguments[i + 1] + "\" was not found.");
                    }
                }
            }

            // Done
            wait.Hide();

            if (exitOnComplete)
                this.Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                TimerSteamGuardForeground.Stop();
                TimerSteamGuardBackground.Start();
                Hide();
            }
            else
            {
                TimerSteamGuardForeground.Start();
                TimerSteamGuardBackground.Stop();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            trayIcon.Visible = false;
            Environment.Exit(0);
        }
#endregion

        #region Tray icon context menu
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            if (IsVisible)
                Hide();
            else
            {
                Show();
                Focus();
            }
        }

        private void TrayIconMenuQuit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TrayIconMenuCopyCode_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(((Button)((DockPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[1]).Children[0]).Content.ToString());
            } // dear god why
            catch (Exception) { }
        }

        private void TrayIconMenuTrades_Click(object sender, EventArgs e)
        {
            tradeWindow.Show(this);
        }

        private void TrayIconMenuRestore_Click(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                trayIconMenuRestore.Text = Properties.strings.TrayIconRestore;
                Hide();
            }
            else
            {
                trayIconMenuRestore.Text = Properties.strings.TrayIconHide;
                Show();
                Focus();
            }
        }
#endregion

        #region Listbox stuff
        private void CopyAuthCodeItemBox_Click(object sender, RoutedEventArgs e) { Clipboard.SetText(((Button)sender).Content.ToString()); }

        /// <summary>
        /// View the pending trade/market request for the current account
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ViewTrades_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ((Label)((StackPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[0]).Children[0]).Content.ToString(); // Gets the current account name
                foreach (SteamGuardAccount account in allAccounts)
                    if (account.AccountName == name)
                        try
                        {
                            tradeWindow.ShowDialog(name, this);
                        }
                        catch (SteamGuardAccount.WGTokenInvalidException)
                        { await RefreshAccountSession(account); }
                        catch (SteamGuardAccount.WGTokenExpiredException)
                        { PromptRefreshLogin(account); }
                        catch (WebException) { }
            }
            catch (Exception) { }
        }
        /// <summary>
        /// Refresh the current account session
        /// </summary>
        private async void RefreshSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ((Label)((StackPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[0]).Children[0]).Content.ToString(); // Gets the current account name
                foreach (SteamGuardAccount account in allAccounts)
                    if (account.AccountName == name)
                        if (await RefreshAccountSession(account))
                        {
                            MessageBox.Show(Properties.strings.MainWindowSessionRefreshed, Properties.strings.MainWindowSessionRefresh, MessageBoxButton.OK, MessageBoxImage.Information);
                            manifest.SaveAccount(account, manifest.Encrypted, passKey);
                        }
                        else
                            MessageBox.Show(Properties.strings.MainWindowSessionRefreshFailed, Properties.strings.MainWindowSessionRefresh, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// Prompt a login refresh for the current account
        /// </summary>
        private void LoginAgain_Click(object sender, RoutedEventArgs e)
        {
            try {
            string name = ((Label)((StackPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[0]).Children[0]).Content.ToString(); // Gets the current account name
            foreach (SteamGuardAccount account in allAccounts)
                if (account.AccountName == name)
                    PromptRefreshLogin(account);
            }
            catch (Exception)
            {
                if (accounts.SelectedItem == null)
                {
                }
            }
        }
        /// <summary>
        /// Removes the current account file from the manifest
        /// </summary>
        private void RemoveFromManifest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (manifest.Encrypted)
                    MessageBox.Show(Properties.strings.MainWindowRemoveAccountManifestEncrypted, Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    string name = ((Label)((StackPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[0]).Children[0]).Content.ToString(); // Gets the current account name
                    foreach (SteamGuardAccount account in allAccounts)
                        if (account.AccountName == name)
                        {
                            MessageBoxResult res = MessageBox.Show(String.Format(Properties.strings.MainWindowRemoveAccountManifestConfirm, name), Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OKCancel, MessageBoxImage.Information);
                            if (res == MessageBoxResult.OK)
                            {
                                manifest.RemoveAccount(account, false);
                                MessageBox.Show(String.Format(Properties.strings.MainWindowRemoveAccountManifestComplete, name), Properties.strings.MainWindowRemoveAccountManifest, MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadAccountsList();
                            }
                        }
                }
            }
            catch (Exception) { }
        }
        /// <summary>
        /// Deactivate the authenticator for the account selected
        /// </summary>
        private void DeactiveAuthenticator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ((Label)((StackPanel)((Grid)((ListBoxItem)accounts.SelectedItem).Content).Children[0]).Children[0]).Content.ToString(); // Gets the current account name
                foreach (SteamGuardAccount account in allAccounts)
                    if (account.AccountName == name)
                    {
                        MessageBoxResult res = MessageBox.Show(Properties.strings.MainWindowDeactivateAccountConfirm, Properties.strings.MainWindowDeactivateAccount, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        int scheme = 0;
                        if (res == MessageBoxResult.Yes)
                        {
                            scheme = 2;
                        }
                        else if (res == MessageBoxResult.No)
                        {
                            scheme = 1;
                        }
                        else if (res == MessageBoxResult.Cancel)
                        {
                            scheme = 0;
                        }

                        if (scheme != 0)
                        {
                            string confCode = account.GenerateSteamGuardCode();
                            InputForm confirmationDialog = new InputForm(String.Format(Properties.strings.MainWindowDeactivateAccountRemoveConfirm, account.AccountName, confCode));
                            confirmationDialog.ShowDialog();

                            if (confirmationDialog.Canceled)
                            {
                                return;
                            }

                            string enteredCode = confirmationDialog.txtBox.Text.ToUpper();
                            if (enteredCode != confCode)
                            {
                                MessageBox.Show(Properties.strings.MainWindowDeactivateAccountRemoveConfirmFailed, Properties.strings.MainWindowDeactivateAccount, MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            bool success = account.DeactivateAuthenticator(scheme);
                            if (success)
                            {
                                MessageBox.Show((scheme == 2 ? Properties.strings.MainWindowDeactivateAccountSuccessRemoved : Properties.strings.MainWindowDeactivateAccountSuccessEmail), Properties.strings.MainWindowDeactivateAccount, MessageBoxButton.OK, MessageBoxImage.Asterisk);
                                this.manifest.RemoveAccount(account);
                                this.LoadAccountsList();
                            }
                            else
                            {
                                MessageBox.Show(Properties.strings.MainWindowDeactivateAccountFailed, Properties.strings.MainWindowDeactivateAccount, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(Properties.strings.MainWindowDeactivateAccountCanceled, Properties.strings.MainWindowDeactivateAccount, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
            }
            catch (Exception) { }
        }
#endregion

        #region Search
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            accounts.Items.Clear();
            foreach (SteamGuardAccount acc in allAccounts)
                if (IsFilter(acc.AccountName))
                    AddAccountToList(acc);
        }
        private bool IsFilter(string f)
        {
            return f.Contains(txtSearch.Text);
        }
        private void Accounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.Up || e.Key == Key.Down)
                {
                    int to = accounts.SelectedIndex - (e.Key == Key.Up ? 1 : -1);
                    manifest.MoveEntry(accounts.SelectedIndex, to);
                    LoadAccountsList();
                }
                return;
            }

            if (!IsKeyAChar(e.Key) && !IsKeyADigit(e.Key))
                return;
            txtSearch.Focus();
        }
        private static bool IsKeyAChar(Key key)
        {
            return key >= Key.A && key <= Key.Z;
        }
        private static bool IsKeyADigit(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9);
        }
#endregion

        #region Timers
        private async void TimerSteamGuardForeground_Tick(object sender, EventArgs e)
        {
            try
            {
                steamTime = await TimeAligner.GetSteamTimeAsync();

                currentSteamChunk = steamTime / 30L;
                double secondsUntilChange = (steamTime - (currentSteamChunk * 30L));

                if (secondsUntilChange <= 0) // Refresh the auth code when it needs to be
                    LoadAccountInfo();

                await Dispatcher.BeginInvoke((Action)delegate ()
                {
                    progressAuthLifespan.Value = 30 - secondsUntilChange; progressAuthLifespan.IsIndeterminate = false;
                    TaskbarItemInfo.ProgressValue = (100 - (secondsUntilChange * (100 / 30))) / 100;
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                });
                if (manifest.AutoEntry)
                    autoEnter.AutoEnter();
            }
            catch (Exception)
            {
            }
        }

        // Core tick
        private async void TimerSteamGuardBackground_Tick(object sender, EventArgs e)
        {
            try
            {
                steamTime = await TimeAligner.GetSteamTimeAsync();

                currentSteamChunk = steamTime / 30L;
                double secondsUntilChange = (steamTime - (currentSteamChunk * 30L));

                if (secondsUntilChange <= 0) // Refresh the auth code when it needs to be
                    LoadAccountInfo();

                if (manifest.AutoEntry)
                    autoEnter.AutoEnter();
            }
            catch (Exception)
            {
            }
        }

        // trading (timer)
        private void TimerTradeChecking_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - tradeHandler.CachedUpdate).TotalSeconds < 10) return; // Force a 10 second cool-down, if you spam refreshes Steam locks you out for a while

            TimerTradeChecking.Stop();

            if (!manifest.PeriodicChecking) return;

            new Thread(() =>
            {
                Thread.CurrentThread.Name = "Background Trade Checker";
                Thread.CurrentThread.Priority = ThreadPriority.Lowest; // We're not too important...
                List<Confirmation> confs = new List<Confirmation>();
                Confirmation[] priorConfirmations = tradeHandler.Confirmations; // We've already seen these confirmations. We'll copy them to another array and compare it with the refreshed confirmations.
                tradeHandler.RefreshConfirmations(true);
                int acceptedCount = 0;
                if (tradeWindow.IsVisible) return;
                if (allAccounts != null)
                {
                    if (allAccounts.Length <= 0 || accounts.Items.Count <= 0)
                    {
                        TimerTradeChecking.Start();
                        return;
                    }
                }
                else
                {
                    TimerTradeChecking.Start();
                    return;
                }
                if (tradeHandler.TradeAccounts == null)
                {
                    TimerTradeChecking.Start();
                    return;
                }
                bool canGoOn = true;
                try
                {
                        foreach (TradeAccount acc in tradeHandler.TradeAccounts)
                        {
                            try
                            {
                                if (acc.Confirmations == null) continue;
                                foreach (var conf in acc.Confirmations) // Runs through each request, auto accepts it if allowed or adds it to the awaiting requests
                                {
                                    // Trade types
                                    if (conf.ConfType == Confirmation.ConfirmationType.MarketSellTransaction && manifest.AutoConfirmMarketTransactions)
                                    {
                                        acc.Account.AcceptConfirmation(conf);
                                        acceptedCount++;
                                    }
                                    else if (conf.ConfType == Confirmation.ConfirmationType.Trade && manifest.AutoConfirmTrades)
                                    {
                                        acc.Account.AcceptConfirmation(conf);
                                        acceptedCount++;
                                    }
                                    else
                                        confs.Add(conf);
                                }
                            }
                            catch (Exception)
                            {
                                canGoOn = false;
                            }
                        }

                    if (confs.Count > 0)
                    {
#region Notifications about the new trades
                        if (priorConfirmations.Length < tradeHandler.Confirmations.Length)
                        {
                            if (confs.Count == 1)
                            {
                                Notifications.Show(Properties.strings.MainWindowTradingNotificationPendingTSingle,
                                    Properties.strings.MainWindowTradingNotificationPendingSingle,
                                    NotificationIcon.Icon,
                                    "action=showTrades");
                            }
                            else if (confs.Count > 1)
                            {
                                Notifications.Show(Properties.strings.MainWindowTradingNotificationPendingTMany,
                                    String.Format(Properties.strings.MainWindowTradingNotificationPendingMany, confs.Count.ToString()),
                                    NotificationIcon.Icon,
                                    "action=showTrades");
                            }
                        }
#endregion
                    }
                    if (acceptedCount == 1)
                    {
                        Notifications.Show(Properties.strings.MainWindowTradingNotificationAcceptedTSingle,
                            Properties.strings.MainWindowTradingNotificationAcceptedSingle,
                            NotificationIcon.Icon);
                    }
                    else if (acceptedCount > 1)
                    {
                        Notifications.Show(Properties.strings.MainWindowTradingNotificationAcceptedTMany,
                            String.Format(Properties.strings.MainWindowTradingNotificationAcceptedMany, acceptedCount.ToString()),
                            NotificationIcon.Icon);
                    }
                }
                catch (SteamGuardAccount.WGTokenInvalidException) { }
                finally
                {
                    if (canGoOn)
                        TimerTradeChecking.Start();
                }
            }).Start();
        }
#endregion

        #region Account stuff
        /// <summary>
        /// Refresh this account's session data using their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        /// <param name="attemptRefreshLogin">Whether or not to prompt the user to re-login if their OAuth token is expired.</param>
        /// <returns>Whether or not the account refreshed</returns>
        private async Task<bool> RefreshAccountSession(SteamGuardAccount account, bool attemptRefreshLogin = true)
        {
            if (account == null) return false;

            try
            {
                bool refreshed = await account.RefreshSessionAsync();
                return refreshed; //No exception thrown means that we either successfully refreshed the session or there was a different issue preventing us from doing so.
            }
            catch (SteamGuardAccount.WGTokenExpiredException)
            {
                if (!attemptRefreshLogin) return false;

                PromptRefreshLogin(account);

                return await RefreshAccountSession(account, false);
            }
        }

        /// <summary>
        /// Display a login form to the user to refresh their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        private void PromptRefreshLogin(SteamGuardAccount account)
        {
            var loginForm = new Login(Login.LoginType.Refresh, account);
            loginForm.ShowDialog(this);
        }

        /// <summary>
        /// Refresh the auth codes
        /// </summary>
        private void LoadAccountInfo()
        {
            try
            {
                if (accounts.Items.Count > 0 && steamTime != 0)
                {
                    // Check if the account is right
                    for (int a = 0; a < allAccounts.Length; a++)
                    {
                        SteamGuardAccount account = allAccounts[a];
                        for (int b = 0; b < accounts.Items.Count; b++)
                        {
                            if (account.AccountName == (string)((Label)((StackPanel)((Grid)((ListBoxItem)accounts.Items[b]).Content).Children[0]).Children[0]).Content)
                            {
                                // this is right
                                ((Button)((DockPanel)((Grid)((ListBoxItem)accounts.Items[b]).Content).Children[1]).Children[0]).Content = account.GenerateSteamGuardCodeForTime(steamTime);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// Adds an account to the accounts listbox
        /// </summary>
        /// <param name="account">The account you're adding</param>
        private void AddAccountToList(SteamGuardAccount account)
        {
            // Adds the account into the listbox
            ListBoxItem mainItem = new ListBoxItem // This is the whole account entry in the accounts listbox
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = new Grid()
            };             // Grid columns
            ColumnDefinition def1Star = new ColumnDefinition
            {
                Width = new GridLength(2, GridUnitType.Star)
            };
            ColumnDefinition defAuto = new ColumnDefinition
            {
                Width = new GridLength(0, GridUnitType.Auto)
            };
            ((Grid)mainItem.Content).ColumnDefinitions.Add(def1Star); // Our auth code button is in this one
            ((Grid)mainItem.Content).ColumnDefinitions.Add(defAuto); // The name is in this one

            // Make the stuff inside the account item
            // The stack panel (for just the account name)
            StackPanel accountInfo = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };
            accountInfo.SetValue(Grid.ColumnProperty, 0);
            // Account name (label)
            Label accountNameLabel = new Label
            {
                Content = account.AccountName
            };
            accountInfo.Children.Add(accountNameLabel);

            // The dock panel (for the auth code button)
            DockPanel authCodeSection = new DockPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            authCodeSection.SetValue(Grid.ColumnProperty, 1); // Dock panel
            // Auth code (button)
            Button codeButton = new Button
            {
                Style = Resources["MaterialDesignFlatButton"] as Style, // The button style, I like the 'flat' one
                Content = account.GenerateSteamGuardCodeForTime(steamTime), // Initialize the text with the current code
            };
            codeButton.Click += CopyAuthCodeItemBox_Click; // When the button is clicked, have the code copied to the clipboard
            authCodeSection.Children.Add(codeButton); // Adds the button to the dock panel

            ((Grid)mainItem.Content).Children.Add(accountInfo); // Add the label stack panel to the main account entry
            ((Grid)mainItem.Content).Children.Add(authCodeSection); // Add the button dock panel to the main account entry

            accounts.Items.Add(mainItem); // Adds our account entry into our accounts listbox
        }

        /// <summary>
        /// Populates the account list. This will clear the current list and repopulate it using an up-to-date manifest
        /// </summary>
        private void LoadAccountsList(bool EncryptionChanged = false) => new Thread(start: () => // I did this so 1) The UI doesn't freeze, 2) so it can be canceled
        {
            manifest = Manifest.GetManifest(true); // Something may have changed

            if (EncryptionChanged)
            {
                if (manifest.Encrypted)
                {
                    passKey= manifest.PromptForPasskey();
                    if (passKey == null) Close();
                }
            }

            Thread.CurrentThread.IsBackground = true;
            Thread.CurrentThread.Name = "Account Loader";
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal; // We'll surrender some resources to other processes.
            bool weOpened = !wait.IsVisible;
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (weOpened)
                    wait.Show(this);
                accounts.Items.Clear();
                accounts.SelectedIndex = -1;
                wait.txtInfo.Text = Properties.strings.MainWindowLoadAccountsPopulating + Environment.NewLine + String.Format(Properties.strings.MainWindowLoadAccountsPopulatingAccount0, manifest.GetAccountCount());
            });
            if (weOpened)
                wait.thread = Thread.CurrentThread;

            List<SteamGuardAccount> accs = new List<SteamGuardAccount>();
            for (int i = 0; i < manifest.GetAccountCount(); i++)
            {
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    wait.txtInfo.Text = Properties.strings.MainWindowLoadAccountsPopulating + Environment.NewLine + String.Format(Properties.strings.MainWindowLoadAccountsPopulatingAccountX, i, manifest.GetAccountCount());
                    wait.progress.IsIndeterminate = false;
                    wait.progress.Maximum = manifest.GetAccountCount();
                    wait.progress.Value = i;
                });
                var acc = manifest.GetAccount(passKey, i);
                accs.Add(acc);
                try {
                    if (manifest.AutoRefreshSession)
                        acc.RefreshSession();
                } catch (Exception) { } // Refresh the session automatically (so it'll never expire?)
            }
            allAccounts = accs.ToArray();

            if (allAccounts.Length > 0)
            {
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    wait.txtInfo.Text = Properties.strings.MainWindowLoadAccountsPopulating + Environment.NewLine + String.Format(Properties.strings.MainWindowLoadAccountsPopulatingAccount0, manifest.GetAccountCount());
                    wait.progress.IsIndeterminate = false; wait.progress.Maximum = allAccounts.Length; wait.progress.Value = 0;
                    accounts.Items.Clear();
                    accounts.SelectedIndex = -1;
                });
                for (int i = 0; i < allAccounts.Length; i++)
                {
                    SteamGuardAccount account = allAccounts[i];

                    Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        wait.txtInfo.Text = Properties.strings.MainWindowLoadAccountsPopulating + Environment.NewLine + String.Format(Properties.strings.MainWindowLoadAccountsPopulatingAccountX, i, manifest.GetAccountCount());
                        AddAccountToList(account);
                    });
                }
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    accounts.SelectedIndex = 0;
                    tradeHandler.SetAccounts(allAccounts);
                });
            }
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                wait.progress.IsIndeterminate = true;
                if (weOpened)
                    wait.Hide();
                autoEnter.Accounts = allAccounts;
            });
        }).Start();


#endregion

        #region Menu clicks
        //Import
        private void MenuFileImportMaFile_Click(object sender, RoutedEventArgs e)
        {
            new ImportAccount(passKey).ShowDialog(this);
            manifest = Manifest.GetManifest();
            LoadAccountsList();
        }
        private void MenuFileNew_Click(object sender, RoutedEventArgs e)
        {
            new Login().ShowDialog(this);
            LoadAccountsList();
        }
        private void MenuFileAndroid_Click(object sender, RoutedEventArgs e)
        {
            new PhoneExtract().ShowDialog(this);
            LoadAccountsList();
        }
        //Settings
        private void MenuFileSettingsEncryption_Click(object sender, RoutedEventArgs e)
        {
            new SecuritySettings().ShowDialog(this);
            manifest = Manifest.GetManifest(true);
            LoadAccountInfo();
        }
        private void MenuFileSettingsMain_Click(object sender, RoutedEventArgs e)
        {
            new Settings().ShowDialog(this);
            manifest = Manifest.GetManifest(true);
            if (manifest.PeriodicChecking)
                TimerTradeChecking.Start();
            TimerTradeChecking.Interval = new TimeSpan(0,0, manifest.PeriodicCheckingInterval);
            if (!manifest.DisplaySearch)
            {
                lblSearch.IsEnabled = txtSearch.IsEnabled = false;
                lblSearch.Visibility = txtSearch.Visibility = Visibility.Hidden;
                MinHeight = 160;
                accounts.Margin = new Thickness(0, 62, 0, 10);
            }
            else
            {
                lblSearch.IsEnabled = txtSearch.IsEnabled = true;
                lblSearch.Visibility = txtSearch.Visibility = Visibility.Visible;
                MinHeight = 190;
                accounts.Margin = new Thickness(0, 62, 0, 39);
            }
            if (!manifest.DeveloperMode)
            {
                menuFileDevMode.IsEnabled = false;
                menuFileDevMode.Visibility = Visibility.Collapsed;
            }
            else
            {
                menuFileDevMode.IsEnabled = true;
                menuFileDevMode.Visibility = Visibility.Visible;
            }
            /*if (manifest.SortAlpha)
                accounts.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
            else
                accounts.Items.SortDescriptions.Clear();*/
        }
        private void MenuFileSettingsUpdate_Click(object sender, RoutedEventArgs e)
        {
            new BrowseUpdates().ShowDialog(this);
        }
        //Other
        private void MenuFileLoadAccounts_Click(object sender, RoutedEventArgs e)
        {
            LoadAccountsList(true); // Force everything to refresh
        }
        private void MenuFileQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Update
        private void MenuUpdate_Click(object sender, RoutedEventArgs e)
        {
            if ((string)menuFileUpdate.Header == Properties.strings.MainWindowUIUpdateButton)
                UpdateManager.UpdateLatest();
            else
            {
                if (UpdateManager.NewUpdate())
                {
                    menuFileUpdate.Header = Properties.strings.MainWindowUIUpdateButton;
                    Notifications.Show(Properties.strings.MainWindowUpdateFoundT, Properties.strings.MainWindowUpdateFound, NotificationIcon.Update, "action=update");
                }
                else
                    Notifications.Show(Properties.strings.MainWindowUpdateNotFoundT, Properties.strings.MainWindowUpdateNotFound, NotificationIcon.Info);
            }
        }

        // Trade
        private void MenuOpenTrades_Click(object sender, RoutedEventArgs e)
        {
            if (accounts.Items.Count > 0)
                tradeWindow.ShowDialog(this);
        }

        // Developer
#if DEBUG
        private void DevShowToast_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Show("Testing!", "This is a test.\nThis is a new line", NotificationIcon.Icon, "action=debugActive");
        }
        private void DevShowToastError_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Show("Error!", "This is a test.\nThis is a new line", NotificationIcon.Error, "action=debugActive");
        }
        private void DevForceAutoLogin_Click(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(1000);
            ErrorAutoEnter err = autoEnter.AutoEnter();
            if (!err.Successful)
            {
                if (!string.IsNullOrEmpty(err.Reason))
                    Notifications.Show("Auto login error", err.Reason.ToString(), NotificationIcon.Info);
            }
            else Notifications.Show("Auto login success", err.Successful.ToString());
        }
        private void DevForceCrash_Click(object sender, RoutedEventArgs e) // Force crash
        {
            throw new Exception("Developer forced crash");
        }
        private void DevRefreshManifest_Click(object sender, RoutedEventArgs e)
        {
            manifest = Manifest.GetManifest(true);
        }
#else
        private void DevShowToast_Click(object sender, RoutedEventArgs e) {}
        private void DevShowToastError_Click(object sender, RoutedEventArgs e) {}
        private void DevForceAutoLogin_Click(object sender, RoutedEventArgs e) {}
        private void DevForceCrash_Click(object sender, RoutedEventArgs e) {}
        private void DevRefreshManifest_Click(object sender, RoutedEventArgs e) {}
#endif
#endregion

        #region Keyboard shortcuts
        private static bool CtrlPressed() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        //private static bool ShiftPressed() => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        private static bool AltPressed() => Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        /*
            ctrl+<arrowUp>: move account up
            ctrl+<arrowDown>: move account down

            ctrl+,: open settings
            ctrl+e: open encryption settings
            ctrl+u: open updates manager
            ctrl+a: toggle auto-entry

            ctrl+t: open trade window
            ctrl+c: copy the code

            ctrl+alt+i: import account file
            ctrl+alt+a: import account from android
            ctrl+alt+l: login to new account

            F5: Reloads accounts
         */

        private new void KeyUp(object sender, KeyEventArgs e)
        {
            RoutedEventArgs empty = null;
            if (e.Key == Key.I && CtrlPressed() && AltPressed())
                MenuFileImportMaFile_Click(null, empty);
            else if (e.Key == Key.A && CtrlPressed() && AltPressed())
                MenuFileAndroid_Click(null, empty);
            else if (e.Key == Key.L && CtrlPressed() && AltPressed())
                MenuFileNew_Click(null, empty);

            else if (e.Key == Key.OemComma && CtrlPressed())
                MenuFileSettingsMain_Click(null, empty);
            else if (e.Key == Key.E && CtrlPressed())
                MenuFileSettingsEncryption_Click(null, empty);
            else if (e.Key == Key.U && CtrlPressed())
                MenuFileSettingsUpdate_Click(null, empty);
            else if (e.Key == Key.A && CtrlPressed())
            {
                manifest.AutoEntry = !manifest.AutoEntry;
                Notifications.Show(Properties.strings.MainWindowAutoEntry, (manifest.AutoEntry == true ? Properties.strings.MainWindowAutoEntryEnabled : Properties.strings.MainWindowAutoEntryDisabled), NotificationIcon.Icon);
            }

            else if (e.Key == Key.T && CtrlPressed())
                MenuOpenTrades_Click(null, empty);
            else if (e.Key == Key.C && CtrlPressed())
                Clipboard.SetText(allAccounts[accounts.SelectedIndex].GenerateSteamGuardCode());

            else if (e.Key == Key.F5)
                LoadAccountsList(true);
        }
#endregion
    }
}
