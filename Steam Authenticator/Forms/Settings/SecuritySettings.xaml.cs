using SteamAuthenticator.BackEnd;
using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for SecuritySettings.xaml
    /// </summary>
    public partial class SecuritySettings : Window
    {
        private Manifest manifest = new Manifest();

        private Brush norm;
        private Brush red = new SolidColorBrush(Color.FromRgb(255, 0, 0));

        private bool Loading = true;

        private bool _unlocked = false;
        private bool Unlocked
        {
            get { return _unlocked; }
            set
            {
                _unlocked = value;
                if (value == true)
                {
                    pass.BorderBrush = norm;
                    Height = 315;
                    gridMain.IsEnabled = true;
                    pass.IsEnabled = !manifest.CredentialLocker;
                    btnUnlock.Content = "Lock";
                }
                else
                {
                    gridMain.IsEnabled = false;
                    Height = 78;
                    pass.IsEnabled = true;
                    btnUnlock.Content = "Unlock";
                }
            }
        }

        private void CheckPassword()
        {
            if ((newPass1.Password == pass.Password && newPass2.Password == pass.Password) || (String.IsNullOrEmpty(newPass1.Password) && String.IsNullOrEmpty(newPass2.Password)) || (newPass1.Password != newPass2.Password))
            {
                btnUpdatePassword.IsEnabled = false;
                if (newPass1.Password != newPass2.Password && newPass1.Password.Length >= 1 && newPass2.Password.Length >= 1)
                    newPass1.BorderBrush = newPass2.BorderBrush = red;
                else
                    newPass1.BorderBrush = newPass2.BorderBrush = norm;
            }
            else
            {
                btnUpdatePassword.IsEnabled = true;
                newPass1.BorderBrush = norm; newPass2.BorderBrush = norm;
            }
        }

        public SecuritySettings()
        {
            Loading = true;
            InitializeComponent();
            norm = pass.BorderBrush;

            chkUseMaFiles.Content = Properties.strings.SettingsStoreMaFiles;
            chkUseMaFiles.ToolTip = Properties.strings.SettingsStoreMaFilesHelp;
            chkUseDPAPI.Content = Properties.strings.SettingsUseDPAPI;
            chkUseDPAPI.ToolTip = Properties.strings.SettingsUseDPAPIH;
            chkUseWindowsFileEncryption.Content = Properties.strings.SecuritySettingsUseWFE;
            chkUseWindowsFileEncryption.ToolTip = Properties.strings.SecuritySettingsUseWFEH;

            btnRemovePassword.Content = Properties.strings.SecuritySettingsRemoveBTN;
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (!manifest.Encrypted)
            {
                Unlocked = true;
            }
            else if (Unlocked)
            {
                pass.Password = "";
                Unlocked = false;
            }
            else if (string.IsNullOrEmpty(pass.Password))
            {
                pass.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                MessageBox.Show(Properties.strings.SecuritySettingsPassIncorrect, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (manifest.VerifyPasskey(pass.SecurePassword))
            {
                Unlocked = true;
            }
            else
            {
                pass.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                MessageBox.Show(Properties.strings.SecuritySettingsPassIncorrect, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            pass.Password = null;
            Unlocked = false;
            pass.BorderBrush = norm;
            newPass1.Password = null;
            newPass2.Password = null;
            GC.Collect();
        }

        private void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            CheckPassword();
            if (newPass1.Password != newPass2.Password)
            {
                newPass1.BorderBrush = red;
                newPass2.BorderBrush = red;
                MessageBox.Show(Properties.strings.SecuritySettingsPasskeyDoNotMatch, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                newPass1.BorderBrush = norm;
                newPass2.BorderBrush = norm;
                btnUpdatePassword.Content = Properties.strings.SecuritySettingsPasskeyButtonUpdate;


                SecureString newPassKey = newPass1.SecurePassword;
                if (newPassKey.Length == 0) newPassKey = null;
                string action = newPassKey == null ? Properties.strings.SecuritySettingsRemove : Properties.strings.SecuritySettingsChange;
                string action1 = newPassKey == null ? Properties.strings.SecuritySettingsRemoving : Properties.strings.SecuritySettingsChanging;
                PleaseWait wait = new PleaseWait(String.Format(Properties.strings.SecuritySettingsProcessingPasskey, action1));
                wait.Show(this);
                wait.Owner = this;

                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.CurrentThread.IsBackground = true;
                    try
                    {
                        CredManifest credMan = new CredManifest();
                        credMan = CredManifest.GetManifest();
                        if (manifest.CredentialLocker)
                        {
                            if (!manifest.ChangeEncryptionKey(credMan.Key, newPassKey))
                            {
                                Dispatcher.BeginInvoke((Action)delegate ()
                                {
                                    wait.txtInfo.Text = String.Format(Properties.strings.SecuritySettingsProcessedFailedPasskey, action);
                                    wait.progress.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                    wait.progress.Background = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                    wait.progress.Foreground = new SolidColorBrush(Color.FromRgb(200, 21, 21));
                                });
                            }
                            else
                            {
                                credMan.Key = newPassKey;
                                credMan.Save();
                                Dispatcher.BeginInvoke((Action)delegate () { wait.Hide(); });
                            }
                        }
                        else
                        {
                            if (!manifest.ChangeEncryptionKey(pass.SecurePassword, newPassKey))
                            {
                                Dispatcher.BeginInvoke((Action)delegate ()
                                {
                                    wait.txtInfo.Text = String.Format(Properties.strings.SecuritySettingsProcessedFailedPasskey, action);
                                    wait.progress.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                    wait.progress.Background = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                    wait.progress.Foreground = new SolidColorBrush(Color.FromRgb(200, 21, 21));
                                });
                            }
                            else
                            {
                                credMan.Key = newPassKey;
                                credMan.Save();
                                Dispatcher.BeginInvoke((Action)delegate () { wait.Hide(); });
                            }
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke((Action)delegate ()
                        {
                            wait.txtInfo.Text = String.Format(Properties.strings.SecuritySettingsProcessedFailedPasskey, action);
                            wait.progress.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                            wait.progress.Background = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                            wait.progress.Foreground = new SolidColorBrush(Color.FromRgb(200, 21, 21));
                        });
                    }
                }).Start();

                pass.Password = new String(char.Parse(" "), newPassKey.Length);
                newPass1.Password = newPass2.Password = "";

                CheckPassword();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            manifest = Manifest.GetManifest();

            // CLEAR (should have already been done!)
            pass.Password = "";

            // Load settings
            chkRememberPasskey.IsChecked = Manifest.RememberPasskey;
            chkAutounlock.IsChecked = manifest.AutoUnlockEncryptionManager;
            chkUseMaFiles.IsChecked = manifest.UseMaFiles;
            chkUseDPAPI.IsChecked = Manifest.UseDPAPI;
            chkUseWindowsFileEncryption.IsChecked = Manifest.UseWindowsFileEncryption;
            if (manifest.AutoUnlockEncryptionManager)
                chkRememberPasskey.IsEnabled = false;

            if (manifest.Encrypted)
            {
                Unlocked = false;
                btnUpdatePassword.Content = Properties.strings.SecuritySettingsUpdate;

                if (Manifest.RememberPasskey)
                {
                    CredManifest credMan = new CredManifest();
                    credMan = CredManifest.GetManifest();
                    if (credMan.Key.Length >= 1)
                        if (manifest.AutoUnlockEncryptionManager == true)
                            if (manifest.VerifyPasskey(credMan.Key))
                            {
                                pass.Password = new String(char.Parse(" "), credMan.Key.Length);
                                Unlocked = true;
                            }
                            else Unlocked = false;
                        else Unlocked = false;
                    else Unlocked = false;
                }
            }
            else
            {
                Unlocked = true;
                btnUpdatePassword.Content = Properties.strings.SecuritySettingsSet;
            }

            CheckPassword();
            Loading = false;
        }

        private void Check(CheckBox sender)
        {
            if (!Loading)
            {
                Manifest.RememberPasskey = (bool)chkRememberPasskey.IsChecked;
                manifest.AutoUnlockEncryptionManager = (bool)chkAutounlock.IsChecked;
                manifest.UseMaFiles = (bool)chkUseMaFiles.IsChecked;
                Manifest.UseDPAPI = (bool)chkUseDPAPI.IsChecked;
                Manifest.UseWindowsFileEncryption = (bool)chkUseWindowsFileEncryption.IsChecked;

                manifest.CredentialLocker = ((bool)chkRememberPasskey.IsChecked || !(bool)chkUseMaFiles.IsChecked);

                if (manifest.AutoUnlockEncryptionManager)
                {
                    Manifest.RememberPasskey = true;
                    chkRememberPasskey.IsChecked = true;
                    chkRememberPasskey.IsEnabled = false;
                }
                else
                {
                    chkRememberPasskey.IsChecked = Manifest.RememberPasskey;
                    chkRememberPasskey.IsEnabled = true;
                }

                manifest.Save();
                if (sender == chkUseDPAPI || sender == chkUseWindowsFileEncryption)
                    manifest.UpdateDPAPI(); // Only update it IF necessary
            }
        }

        private void Chk_Checked(object sender, RoutedEventArgs e)
        {
            Check((CheckBox)e.Source);
        }
        private void Chk_Unchecked(object sender, RoutedEventArgs e)
        {
            Check((CheckBox)e.Source);
        }

        private void BtnRemovePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CredManifest credMan = new CredManifest();
                credMan = CredManifest.GetManifest();
                if (manifest.CredentialLocker)
                {
                    if (!manifest.ChangeEncryptionKey(credMan.Key, null))
                    {
                        MessageBox.Show(String.Format(Properties.strings.SecuritySettingsUnableToRemove, ""));
                    }
                    else
                    {
                        MessageBox.Show(Properties.strings.SecuritySettingsRemoved);
                        this.Close();
                    }
                }
                else
                {
                    if (!manifest.ChangeEncryptionKey(pass.SecurePassword, null))
                    {
                        MessageBox.Show(String.Format(Properties.strings.SecuritySettingsUnableToRemove, ""));
                    }
                    else
                    {
                        MessageBox.Show(Properties.strings.SecuritySettingsRemoved);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(Properties.strings.SecuritySettingsUnableToRemove,"\n" + ex.Message));
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

        private void NewPass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CheckPassword();
        }
    }
}
