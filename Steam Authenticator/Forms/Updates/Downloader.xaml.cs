using SteamAuthenticator.BackEnd;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for Downloader.xaml
    /// </summary>
    public partial class Downloader : Window
    {
        public DownloaderItem downloadItem;

        private string currentPath = AppDomain.CurrentDomain.BaseDirectory.ToString();

        private Stopwatch sw = new Stopwatch();
        WebClient client = new WebClient();

        private UpdateMan UpdateManager;

        public Downloader(DownloaderItem dlItem)
        {
            UpdateManager = new UpdateMan(this);

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            downloadItem = dlItem;

            if (Directory.Exists(currentPath + UpdateManager.updateFolder))
                Directory.Delete(currentPath + UpdateManager.updateFolder, true);

            Directory.CreateDirectory(currentPath + UpdateManager.updateFolder);


            InitializeComponent();
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtAboutUpdate.Text = downloadItem.UpdateInfo;
            StartDownload();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            client.CancelAsync();
            client.Dispose();
            downloadItem.Ran = true;
        }


        private void StartDownload()
        {
            sw.Start();
            pbStatus.IsIndeterminate = false;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadFileAsync(new Uri(downloadItem.DownloadUrl), currentPath + UpdateManager.updateFolder + UpdateManager.updateFile);
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            sw.Reset();
            downloadItem.Complete = true;
            Close();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesDownloaded = double.Parse(e.BytesReceived.ToString());
            double bytesToDownload = double.Parse(e.TotalBytesToReceive.ToString());
            pbStatus.Maximum = bytesToDownload;
            pbStatus.Value = bytesDownloaded;
            TaskbarItemInfo.ProgressValue = Math.Round(bytesDownloaded / bytesToDownload, 2);
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            lblStatus.Content = String.Format(Properties.strings.DownloaderDownloadInfo, (e.BytesReceived / 1048576), (e.TotalBytesToReceive / 1048576), e.ProgressPercentage, (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

    public class DownloaderItem
    {
        public bool Complete { get; set; } = false;
        public string Error { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string UpdateInfo { get; set; } = "";
        public bool Ran { get; set; } = false;
    }
}
