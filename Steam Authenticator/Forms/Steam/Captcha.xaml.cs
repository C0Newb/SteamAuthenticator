using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for Captcha.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Captcha")]
    public partial class Captcha : Window
    {
        public bool Canceled = true;
        public string GID = "";
        public string Url = "";
        public string Code { get { return txtBox.Text; } }

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

        public Captcha(string gid)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            GID = gid;
            Url = "https://steamcommunity.com/public/captcha.php?gid=" + GID;
            var image = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("https://steamcommunity.com/public/captcha.php?gid=" + GID, UriKind.Absolute);
            bitmap.EndInit();

            image.Source = bitmap;
            panel.Children.Add(image);

            InitializeComponent();

            label.Content = Properties.strings.CaptchaEnter;
            btnSubmit.Content = Properties.strings.btnSubmit;
            btnSubmit.Content = Properties.strings.btnCancel;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            Canceled = false; Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Canceled = true; Close();
        }
    }
}
