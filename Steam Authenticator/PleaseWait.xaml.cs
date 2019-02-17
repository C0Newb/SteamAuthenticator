using System.Windows;
using System.Windows.Input;
using System.Threading;

namespace SteamAuthenticator
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class PleaseWait : Window
    {
        public Thread thread;

        public PleaseWait(string text = "Please wait. . .", Thread userThread = null)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            InitializeComponent();

            thread = userThread;

            txtInfo.Text = text;
            progress.IsIndeterminate = true;
            progress.Maximum = 100;
            progress.Minimum = 0;
            progress.Value = 0;
            progress.ClearValue(BorderBrushProperty);
            progress.ClearValue(BackgroundProperty);
            progress.ClearValue(ForegroundProperty);
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (thread != null)
            {
                thread.Abort();
                Hide();
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

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
