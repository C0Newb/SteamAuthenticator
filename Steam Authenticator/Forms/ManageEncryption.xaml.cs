using Steam_Authenticator.Backend;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Steam_Authenticator.Forms
{
    /// <summary>
    /// Interaction logic for ManageEncryption.xaml
    /// </summary>
    public partial class ManageEncryption : Window
    {
        private Manifest manifest = new Manifest();

        private Brush norm;
        private Brush red = new SolidColorBrush(Color.FromRgb(255, 0, 0));

        private bool _unlocked = false;
        private bool unlocked
        {
            get { return _unlocked; }
            set {
                _unlocked = value;
                if (value == true) {
                    pass.BorderBrush = norm; Height = 270; gridMain.IsEnabled = true;
                    pass.IsEnabled = !manifest.CredentialLocker; btnUnlock.Content = "Lock";
                }
                else {
                    gridMain.IsEnabled = false; Height = 78; pass.IsEnabled = true; btnUnlock.Content = "Unlock";
                }
            }
        }

        private void checkPassword()
        {
            if ((newPass1.Password == pass.Password && newPass2.Password == pass.Password) || (newPass1.Password == "" && newPass2.Password == "") || (newPass1.Password != newPass2.Password))
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

        public ManageEncryption()
        {
            InitializeComponent();
            norm = pass.BorderBrush;
        }

        private void btnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (!manifest.Encrypted) { unlocked = true; }
            else if (unlocked) { pass.Password = ""; unlocked = false; }
            else if (string.IsNullOrEmpty(pass.Password))
            { pass.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0)); MessageBox.Show("Password incorrect.", "", MessageBoxButton.OK, MessageBoxImage.Error); }
            else if (manifest.VerifyPasskey(pass.Password)) { unlocked = true; }
            else { pass.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0)); MessageBox.Show("Password incorrect.", "", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            pass.Password = ""; unlocked = false; pass.BorderBrush = norm;
        }

        private void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            checkPassword();
            if (newPass1.Password != newPass2.Password)
            {
                newPass1.BorderBrush = red; newPass2.BorderBrush = red;
                MessageBox.Show("Passwords do not match.", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                newPass1.BorderBrush = norm; newPass2.BorderBrush = norm;
                btnUpdatePassword.Content = "Update password";
                // Update the password
                manifest.CredentialLocker = (bool)chkCredentialLocker.IsChecked;
                manifest.Save();



                string newPassKey = newPass1.Password;
                if (newPassKey.Length == 0) newPassKey = null;
                string action = newPassKey == null ? "remove" : "change";
                string action1 = newPassKey == null ? "Removing" : "Changing";
                PleaseWait wait = new PleaseWait(action1 + " password, please wait . . ."); wait.Show(this); wait.Owner = this;

                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.CurrentThread.IsBackground = true;
                    try
                    {
                        if (!manifest.ChangeEncryptionKey(pass.Password, newPassKey))
                            Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                wait.txtInfo.Text = "Unable to " + action + " passkey.";
                                wait.progress.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                wait.progress.Background = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                                wait.progress.Foreground = new SolidColorBrush(Color.FromRgb(200, 21, 21));
                            });
                        else Dispatcher.BeginInvoke((Action)delegate () { wait.Hide(); });
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke((Action)delegate ()
                        {
                            wait.txtInfo.Text = "Unable to " + action + " passkey.";
                            wait.progress.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                            wait.progress.Background = new SolidColorBrush(Color.FromRgb(203, 128, 128));
                            wait.progress.Foreground = new SolidColorBrush(Color.FromRgb(200, 21, 21));
                        });
                    }
                }).Start();

                pass.Password = newPassKey;
                newPass1.Password = newPass2.Password = "";

                checkPassword();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            manifest = Manifest.GetManifest();

            pass.Password = "";

            if (manifest.CredentialLocker || !manifest.UseMaFiles) chkCredentialLocker.IsChecked = true; else chkCredentialLocker.IsChecked = false;
            chkAutounlock.IsChecked = manifest.AutoUnlockEncryptionManager;

            if (manifest.Encrypted) { unlocked = false; btnUpdatePassword.Content = "Update password"; }
            else { unlocked = true; btnUpdatePassword.Content = "Set password"; }

            if (manifest.CredentialLocker)
            {
                CredManifest credMan = new CredManifest();
                credMan = CredManifest.GetManifest();
                if (!string.IsNullOrEmpty(credMan.key))
                    if (manifest.AutoUnlockEncryptionManager == true)
                        if (manifest.VerifyPasskey(credMan.key)) { pass.Password = credMan.key; unlocked = true; }
                        else unlocked = false;
                    else unlocked = false;
                else unlocked = false;
            }

            checkPassword();
        }

        private void chkCredentialLocker_Click(object sender, RoutedEventArgs e)
        { if (manifest.UseMaFiles) { manifest.CredentialLocker = (bool)chkCredentialLocker.IsChecked; manifest.Save(); } else chkCredentialLocker.IsChecked = true; }
        private void chkAutounlock_Checked(object sender, RoutedEventArgs e) { manifest.AutoUnlockEncryptionManager = (bool)chkAutounlock.IsChecked; manifest.Save(); }

        private void btnRemovePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!manifest.ChangeEncryptionKey(pass.Password, null)) MessageBox.Show("Unable to remove passkey.");
                else
                {
                    MessageBox.Show("Passkey successfull removed.");
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Unable to remove passkey.\nReason: " + ex.Message); }

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

        private void newPass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            checkPassword();
        }
    }
}
