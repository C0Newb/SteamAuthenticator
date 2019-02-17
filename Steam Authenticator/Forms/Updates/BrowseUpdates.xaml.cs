using Octokit;
using SteamAuthenticator.BackEnd;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for BrowseUpdates.xaml
    /// </summary>
    public partial class BrowseUpdates : Window
    {
        private IReadOnlyList<Release> releases;

        private UpdateMan UpdateManager;

        public BrowseUpdates()
        {
            UpdateManager = new UpdateMan(this);
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);

            lblInfo.Content = Properties.strings.BrowseUpdateAvailable;

            Refresh();
        }

        new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
            else if (e.Key == Key.F5)
                Refresh();
        }

        private void Refresh()
        {
            releases = UpdateManager.Releases;
            releaseList.Items.Clear();
            for (int i = 0; i < releases.Count; i++)
            {
                Release release = releases[i];

                // Adds the account into the listbox
                ListBoxItem mainItem = new ListBoxItem
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Content = new Grid()
                };
                // Grid columns
                ColumnDefinition def1Star = new ColumnDefinition
                {
                    Width = new GridLength(2, GridUnitType.Star)
                };
                ColumnDefinition defAuto = new ColumnDefinition
                {
                    Width = new GridLength(0, GridUnitType.Auto)
                };
                ((Grid)mainItem.Content).ColumnDefinitions.Add(def1Star);
                ((Grid)mainItem.Content).ColumnDefinitions.Add(defAuto);

                // Make our stuff
                StackPanel leftSide = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                leftSide.SetValue(Grid.ColumnProperty, 0); // Stack panel

                System.Windows.Controls.Label version = new System.Windows.Controls.Label()
                {
                    Content = release.TagName
                };
                System.Windows.Controls.Label type = new System.Windows.Controls.Label()
                {
                    Content = "(stable)",
                    Foreground = new SolidColorBrush(Colors.Green)
                };
                if (release.Prerelease)
                {
                    type.Foreground = new SolidColorBrush(Colors.Red);
                    type.Content = "(beta)";
                }
                leftSide.Children.Add(version); // Account name (label)
                leftSide.Children.Add(type);


                DockPanel rightSide = new DockPanel()
                {
                    VerticalAlignment = VerticalAlignment.Center
                };
                rightSide.SetValue(Grid.ColumnProperty, 1); // Dock panel
                Button btnInstallItem = new Button()
                {
                    Style = FindResource("MaterialDesignFlatButton") as Style,
                    Content = Properties.strings.BrowseUpdateInstall,
                };
                btnInstallItem.Click += InstallUpdate2_Click;
                rightSide.Children.Add(btnInstallItem); // Now add it

                ((Grid)mainItem.Content).Children.Add(leftSide);
                ((Grid)mainItem.Content).Children.Add(rightSide);
                releaseList.Items.Add(mainItem);
            }
            if (releaseList.Items.Count > 0)
                releaseList.SelectedIndex = 0;
        }

        private void ViewMoreInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(releases[releaseList.SelectedIndex].Body.ToString(), Properties.strings.BrowseUpdateInfo);
        }
        private void InstallUpdate2_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.InstallUpdate(((System.Windows.Controls.Label)((StackPanel)((Grid)((DockPanel)((Button)sender).Parent).Parent).Children[0]).Children[0]).Content.ToString());
        }
        private void InstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.InstallUpdate(releases[releaseList.SelectedIndex].TagName);
        }
        private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.DownloadUpdate(releases[releaseList.SelectedIndex].TagName);
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
