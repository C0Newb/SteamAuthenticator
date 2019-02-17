using System.Security;
using System.Windows;
using System.Windows.Input;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for InputForm.xaml
    /// </summary>
    public partial class InputForm : Window
    {
        private bool pass = false;

        public bool Canceled = false;
        private bool userClosed = true;

        public InputForm(string label, bool password = false)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            InitializeComponent();

            Title = label;
            txtInfo.Text = label;
            if (password)
            {
                txtBox.IsEnabled = false;
                txtBox.Visibility = Visibility.Hidden;

                pass = true;
                txtPass.IsEnabled = true;
                txtPass.Visibility = Visibility.Visible;
                txtPass.ToolTip = label;
            }
            else
            {
                txtBox.ToolTip = label;
                txtBox.IsEnabled = true;
                txtBox.Visibility = Visibility.Visible;

                txtPass.Visibility = Visibility.Hidden;
                txtPass.IsEnabled = false;
            }
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public string GetText()
        {
            if (pass)
                return txtPass.Password;
            else
                return txtBox.Text;
        }

        public SecureString GetPassword()
        {
            return txtPass.SecurePassword;
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            string text = GetText();
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
            {
                Canceled = true;
                userClosed = false;
                Close();
            }
            else
            {
                Canceled = userClosed = false;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Canceled = true; userClosed = false; Close();
        }

        private void TxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string text = GetText();
                if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                {
                    Canceled = true;
                    userClosed = false;
                    Close();
                }
                else
                {
                    Canceled = userClosed = false;
                    Close();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (pass)
                txtPass.Focus();
            else
                txtBox.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (userClosed)
                Canceled = true;
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
