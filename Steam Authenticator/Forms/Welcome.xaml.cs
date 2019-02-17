using Microsoft.WindowsAPICodePack.Dialogs;
using SteamAuthenticator.BackEnd;
using System;
using System.IO;
using System.Windows;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : Window
    {
        private Manifest man;

        public Welcome()
        {
            InitializeComponent();

            btnImportAccounts.Content = Properties.strings.WelcomeImportFile;
            btnImportFromDevice.Content = Properties.strings.WelcomeImportPhone;
            btnSkip.Content = Properties.strings.btnSkip;
            btnSteamSignIn.Content = Properties.strings.WelcomeSignIn;
            lblMain.Content = Properties.strings.WelcomeLabel;
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnImportAccounts_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new CommonOpenFileDialog();
            folderPicker.IsFolderPicker = folderPicker.ShowHiddenItems = true;
            folderPicker.Multiselect = false;
            folderPicker.DefaultDirectory = Directory.GetCurrentDirectory();
            folderPicker.Title = Properties.strings.WelcomeSelectFolder;
            CommonFileDialogResult folder = folderPicker.ShowDialog();

            if (folder == CommonFileDialogResult.Ok)
            {
                string path = folderPicker.FileName;
                string pathToCopy = null;

                if (Directory.Exists(path + "/maFiles"))
                    pathToCopy = path + "/maFiles";
                else if (File.Exists(path + "/manifest.json"))
                    pathToCopy = path;
                else
                    if (MessageBox.Show(String.Format(Properties.strings.WelcomeNoManifest,path), "", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;

                string currentPath = Manifest.GetExecutableDir();
                if (!Directory.Exists(currentPath + "/maFiles"))
                    Directory.CreateDirectory(currentPath + "/maFiles");

                foreach (string newPath in Directory.GetFiles(pathToCopy, "*.*", SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(pathToCopy, currentPath + "/maFiles"), true);

                man = Manifest.GetManifest(true);
                man.FirstRun = false;
                man.Save();

                MessageBox.Show(Properties.strings.WelcomeImportComplete, Properties.strings.WelcomeImportCompleteT, MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else return;
        }

        private void BtnImportFromDevice_Click(object sender, RoutedEventArgs e)
        {
            int oldEntries = man.Entries.Count;

            new PhoneExtract().ShowDialog();

            if (man.Entries.Count > oldEntries)
            {
                man.FirstRun = false;
                man.Save();
                Close();
            }
        }

        private void BtnSteamSignIn_Click(object sender, RoutedEventArgs e)
        {
            man.FirstRun = false;
            man.Save();

            Login mLoginForm = new Login();
            mLoginForm.ShowDialog();

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
}
