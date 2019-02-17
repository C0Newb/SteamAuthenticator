using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using SteamAuthenticator.BackEnd;
using System.Windows.Input;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for PhoneExtract.xaml
    /// </summary>
    public partial class PhoneExtract : Window
    {
        private PhoneBridge bridge;
        private SteamAuth.SteamGuardAccount steamAccount;
        private string SelectedSteamID = "*";
        public SteamAuth.SteamGuardAccount Result;

        private System.Timers.Timer tCheckDevice = new System.Timers.Timer();


        public PhoneExtract()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            tCheckDevice.Interval = 5000;
            tCheckDevice.Enabled = true;
            tCheckDevice.Elapsed += TCheckDevice_Elapsed;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void TCheckDevice_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckDevice();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private void Init()
        {
            bridge = new PhoneBridge();
            bridge.DeviceWaited += Bridge_DeviceWaited;
            bridge.PhoneBridgeError += Bridge_PhoneBridgeError;
            bridge.OutputLog += Bridge_OutputLog;
            bridge.MoreThanOneAccount += Bridge_MoreThanOneAccount;
        }

        private void Bridge_PhoneBridgeError(string msg)
        {
            Log(msg);
            if (msg != "Device not detected")
                ResetAll();
        }
        private void Bridge_DeviceWaited(object sender, EventArgs e)
        {
            Log("Starting");
        }
        private void Bridge_MoreThanOneAccount(List<string> accounts)
        {
            Log("More than one account found");
            tCheckDevice.Stop();

            ListSelection frm = new ListSelection(accounts);
            frm.ShowDialog();
            SelectedSteamID = accounts[frm.SelectedIndex];
            CheckDevice();
        }

        private void Bridge_OutputLog(string msg)
        {
            Log(msg);
        }


        private void WaitForDevice()
        {
            Log("Waiting for device...");
            bridge.WaitForDeviceAsync();
        }
        private void ResetAll()
        {
            bridge.Close();
            Init();
            tCheckDevice.Start();
            txtBlock.Text = "";
        }
        private void Log(string l)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                txtBlock.Text = txtBlock.Text + Environment.NewLine + l;
                txtBlockScroller.ScrollToBottom();
            }));

        }
        private void Extract()
        {
            steamAccount = bridge.ExtractSteamGuardAccount(SelectedSteamID, SelectedSteamID != "*");

            if (steamAccount != null)
            {
                Result = steamAccount;
                Log("Account extracted successfully!");
                LoginAccount();
            }
        }

        private void LoginAccount()
        {
            MessageBox.Show("Account extracted successfully! Please login to it.");
            Login login = new Login
            {
                androidAccount = steamAccount,
                loginFromAndroid = true
            };
            login.SetUsername(steamAccount.AccountName);
            login.ShowDialog();
            Close();
        }
        private void CheckDevice()
        {
            string state = bridge.GetState();
            if (state == "device")
            {
                tCheckDevice.Stop();
                Log("Starting");
                Extract();
            }
            else if (state == "noadb")
            {
                Log("ADB not found");
                tCheckDevice.Stop();
            }
            else
                Log("Device not connected");
        }

        private void BtnConnectOverWifi_Click(object sender, RoutedEventArgs e)
        {
            InputForm input = new InputForm("Enter the IP of the device");
            input.ShowDialog();
            if (!input.Canceled)
                bridge.ConnectWiFi(input.GetText());
        }

        private void BtnCheckDevice_Click(object sender, RoutedEventArgs e)
        {
            CheckDevice();
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
