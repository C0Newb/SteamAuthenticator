using SteamAuthenticator.BackEnd;
using SteamAuth;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Security;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login
    {
        public UserLogin userLogin;
        public SteamGuardAccount androidAccount;
        public bool refreshLogin = false;
        public bool loginFromAndroid = false;
        public LoginType LoginReason;

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

        public Login(LoginType loginReason = LoginType.Initial, SteamGuardAccount account = null)
        {
            LoginReason = loginReason;
            androidAccount = account;

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            InitializeComponent();

            if (loginReason == LoginType.Refresh)
                txt.Text = Properties.strings.LoginRefreshInfo;
            else
                txt.Text = Properties.strings.LoginInfo;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginUser();
        }

        public void SetUsername(string userName)
        {
            txtUsername.Text = userName;
        }

        public static string FilterPhoneNumber(string phoneNumber)
        {
            if (String.IsNullOrEmpty(phoneNumber))
                return "";
            return phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "");
        }

        public static bool PhoneNumberOkay(string phoneNumber)
        {
            if (phoneNumber == null || phoneNumber.Length == 0)
                return false;
            if (phoneNumber[0] != '+')
                return false;
            return true;
        }

        #region login
        private void LoginUser()
        {
            string username = txtUsername.Text;
            SecureString password = txtPassword.SecurePassword;

            if (LoginReason == LoginType.Android)
            {
                FinishExtract(username, password);
                return;
            }
            else if (LoginReason == LoginType.Refresh)
            {
                RefreshLogin(username, password);
                return;
            }

            var userLogin = new UserLogin(username, password);
            LoginResult response = LoginResult.BadCredentials;

            while ((response = userLogin.DoLogin()) != LoginResult.LoginOkay)
            {
                switch (response)
                {
                    case LoginResult.NeedEmail:
                        InputForm emailForm = new InputForm(Properties.strings.LoginEnterEmailCode);
                        emailForm.ShowDialog(this);
                        if (emailForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        userLogin.EmailCode = emailForm.txtBox.Text;
                        break;


                    case LoginResult.NeedCaptcha:
                        Captcha captchaForm = new Captcha(userLogin.CaptchaGID);
                        captchaForm.ShowDialog();
                        if (captchaForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        userLogin.CaptchaText = captchaForm.Code;
                        break;

                    case LoginResult.Need2FA:
                        MessageBox.Show(Properties.strings.LoginAleadLinked, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.BadRSA:
                        MessageBox.Show(Properties.strings.LoginBadRSA, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.BadCredentials:
                        MessageBox.Show(Properties.strings.LoginBadCreds, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.TooManyFailedLogins:
                        MessageBox.Show(Properties.strings.LoginTooManyFailedAttempts, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.GeneralFailure:
                        MessageBox.Show(Properties.strings.LoginGeneralFailure, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                }
            }

            //Login succeeded

            SessionData session = userLogin.Session;
            AuthenticatorLinker linker = new AuthenticatorLinker(session);

            AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;

            while ((linkResponse = linker.AddAuthenticator()) != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                switch (linkResponse)
                {
                    case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                        string phoneNumber = "";
                        while (!PhoneNumberOkay(phoneNumber))
                        {
                            InputForm phoneNumberForm = new InputForm(Properties.strings.LoginEnterPhoneNumber);
                            phoneNumberForm.txtBox.Text = "+1 ";
                            phoneNumberForm.ShowDialog(this);
                            if (phoneNumberForm.Canceled)
                            {
                                Close();
                                return;
                            }

                            phoneNumber = FilterPhoneNumber(phoneNumberForm.txtBox.Text);
                        }
                        linker.PhoneNumber = phoneNumber;
                        break;

                    case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                        linker.PhoneNumber = null;
                        break;

                    case AuthenticatorLinker.LinkResult.GeneralFailure:
                        MessageBox.Show(Properties.strings.LoginErrorAddingNumber);
                        Close();
                        return;
                }
            }

            Manifest manifest = Manifest.GetManifest();
            SecureString passKey = null;
            if (manifest.Entries.Count == 0)
            {
                passKey = manifest.PromptSetupPasskey(Properties.strings.LoginEnterPasskey);
            }
            else if (manifest.Entries.Count > 0 && manifest.Encrypted)
            {
                bool passKeyValid = false;
                while (!passKeyValid)
                {
                    InputForm passKeyForm = new InputForm(Properties.strings.ManifestEnterKey,true);
                    passKeyForm.ShowDialog(this);
                    if (!passKeyForm.Canceled)
                    {
                        passKey = passKeyForm.GetPassword();
                        passKeyValid = manifest.VerifyPasskey(passKey);
                        if (!passKeyValid)
                            MessageBox.Show(Properties.strings.LoginInvaildPasskey);
                    }
                    else
                    {
                        Close();
                        return;
                    }
                }
            }

            //Save the file immediately; losing this would be bad.
            if (!manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey))
            {
                manifest.RemoveAccount(linker.LinkedAccount);
                MessageBox.Show(Properties.strings.LoginUnableToSaveFile);
                Close();
                return;
            }

            MessageBox.Show(String.Format(Properties.strings.LoginWriteRevocation, linker.LinkedAccount.RevocationCode));

            AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
            while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
            {
                InputForm smsCodeForm = new InputForm(Properties.strings.LoginEnterSMS);
                smsCodeForm.ShowDialog(this);
                if (smsCodeForm.Canceled)
                {
                    manifest.RemoveAccount(linker.LinkedAccount);
                    Close();
                    return;
                }

                InputForm confirmRevocationCode = new InputForm(Properties.strings.LoginEnterRevocation);
                confirmRevocationCode.ShowDialog(this);
                if (confirmRevocationCode.txtBox.Text.ToUpper() != linker.LinkedAccount.RevocationCode)
                {
                    MessageBox.Show(Properties.strings.LoginRevocationIncorrect);
                    manifest.RemoveAccount(linker.LinkedAccount);
                    Close();
                    return;
                }

                string smsCode = smsCodeForm.txtBox.Text;
                finalizeResponse = linker.FinalizeAddAuthenticator(smsCode);

                switch (finalizeResponse)
                {
                    case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                        continue;

                    case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                        MessageBox.Show(String.Format(Properties.strings.LoginUnableToGenerateCodes, linker.LinkedAccount.RevocationCode));
                        manifest.RemoveAccount(linker.LinkedAccount);
                        Close();
                        return;

                    case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                        MessageBox.Show(String.Format(Properties.strings.LoginUnableToFinalize, linker.LinkedAccount.RevocationCode));
                        manifest.RemoveAccount(linker.LinkedAccount);
                        Close();
                        return;
                }
            }

            //Linked, finally. Re-save with FullyEnrolled property.
            manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey);
            MessageBox.Show(String.Format(Properties.strings.LoginSuccessfulAdd, linker.LinkedAccount.RevocationCode));
            Close();
        }
        /// <summary>
        /// Handles logging in to refresh session data. i.e. changing steam password.
        /// </summary>
        /// <param name="username">Steam username</param>
        /// <param name="password">Steam password</param>
        private async void RefreshLogin(string username, SecureString password)
        {
            long steamTime = await TimeAligner.GetSteamTimeAsync();
            Manifest man = Manifest.GetManifest();

            androidAccount.FullyEnrolled = true;

            UserLogin mUserLogin = new UserLogin(username, password);
            LoginResult response = LoginResult.BadCredentials;

            while ((response = mUserLogin.DoLogin()) != LoginResult.LoginOkay)
            {
                switch (response)
                {
                    case LoginResult.NeedEmail:
                        InputForm emailForm = new InputForm(Properties.strings.LoginEnterEmailCode);
                        emailForm.ShowDialog(this);
                        if (emailForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        mUserLogin.EmailCode = emailForm.txtBox.Text;
                        break;

                    case LoginResult.NeedCaptcha:
                        Captcha captchaForm = new Captcha(mUserLogin.CaptchaGID);
                        captchaForm.ShowDialog();
                        if (captchaForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        mUserLogin.CaptchaText = captchaForm.Code;
                        break;

                    case LoginResult.Need2FA:
                        mUserLogin.TwoFactorCode = androidAccount.GenerateSteamGuardCodeForTime(steamTime);
                        break;

                    case LoginResult.BadRSA:
                        MessageBox.Show(Properties.strings.LoginBadRSA, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.BadCredentials:
                        MessageBox.Show(Properties.strings.LoginBadCreds, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.TooManyFailedLogins:
                        MessageBox.Show(Properties.strings.LoginTooManyFailedAttempts, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.GeneralFailure:
                        MessageBox.Show(Properties.strings.LoginGeneralFailure, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                }
            }

            androidAccount.Session = mUserLogin.Session;

            HandleManifest(man, true);
        }

        /// <summary>
        /// Handles logging in after data has been extracted from Android phone
        /// </summary>
        /// <param name="username">Steam username</param>
        /// <param name="password">Steam password</param>
        private async void FinishExtract(string username, SecureString password)
        {
            long steamTime = await TimeAligner.GetSteamTimeAsync();
            Manifest man = Manifest.GetManifest();

            androidAccount.FullyEnrolled = true;

            UserLogin mUserLogin = new UserLogin(username, password);
            LoginResult response = LoginResult.BadCredentials;

            while ((response = mUserLogin.DoLogin()) != LoginResult.LoginOkay)
            {
                switch (response)
                {
                    case LoginResult.NeedEmail:
                        InputForm emailForm = new InputForm(Properties.strings.LoginEnterEmailCode);
                        emailForm.ShowDialog(this);
                        if (emailForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        mUserLogin.EmailCode = emailForm.txtBox.Text;
                        break;

                    case LoginResult.NeedCaptcha:
                        Captcha captchaForm = new Captcha(mUserLogin.CaptchaGID);
                        captchaForm.ShowDialog();
                        if (captchaForm.Canceled)
                        {
                            Close();
                            return;
                        }

                        mUserLogin.CaptchaText = captchaForm.Code;
                        break;

                    case LoginResult.Need2FA:
                        mUserLogin.TwoFactorCode = androidAccount.GenerateSteamGuardCodeForTime(steamTime);
                        break;

                    case LoginResult.BadRSA:
                        MessageBox.Show(Properties.strings.LoginBadRSA, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.BadCredentials:
                        MessageBox.Show(Properties.strings.LoginBadCreds, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.TooManyFailedLogins:
                        MessageBox.Show(Properties.strings.LoginTooManyFailedAttempts, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;

                    case LoginResult.GeneralFailure:
                        MessageBox.Show(Properties.strings.LoginGeneralFailure, Properties.strings.LoginErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                }
            }

            androidAccount.Session = mUserLogin.Session;

            HandleManifest(man);
        }

        private void HandleManifest(Manifest man, bool IsRefreshing = false)
        {
            SecureString passKey = null;
            if (man.Entries.Count == 0)
                passKey = man.PromptSetupPasskey(Properties.strings.LoginEnterPasskey);
            else if (man.Entries.Count > 0 && man.Encrypted)
            {
                bool passKeyValid = false;
                while (!passKeyValid)
                {
                    InputForm passKeyForm = new InputForm(Properties.strings.ManifestEnterKey,true);
                    passKeyForm.ShowDialog(this);
                    if (!passKeyForm.Canceled)
                    {
                        passKey = passKeyForm.GetPassword();
                        passKeyValid = man.VerifyPasskey(passKey);
                        if (!passKeyValid)
                            MessageBox.Show(Properties.strings.LoginInvaildPasskey);
                    }
                    else
                    {
                        Close();
                        return;
                    }
                }
            }

            man.SaveAccount(androidAccount, passKey != null, passKey);
            if (IsRefreshing)
                MessageBox.Show(Properties.strings.LoginRefreshedSession);
            else
                MessageBox.Show(String.Format(Properties.strings.LoginSuccessfulAdd, androidAccount.RevocationCode));
            Close();
        }
        #endregion

        public enum LoginType
        {
            Initial,
            Android,
            Refresh
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoginUser();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (androidAccount != null && androidAccount.AccountName != null)
                txtUsername.Text = androidAccount.AccountName;
        }

        private void TxtUsername_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) txtPassword.Focus();
        }
    }

    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, Properties.strings.LoginFieldRequired)
                : ValidationResult.ValidResult;
        }
    }
}
