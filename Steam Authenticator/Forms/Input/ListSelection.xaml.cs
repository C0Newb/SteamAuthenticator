using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for ListSelection.xaml
    /// </summary>
    public partial class ListSelection : Window
    {
        public int SelectedIndex;
        List<string> Items;

        public ListSelection(List<string> options)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(keyUp), true);
            Items = options;
            InitializeComponent();
        }
        private void keyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }


        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (lblItems.SelectedIndex != -1)
            {
                SelectedIndex = lblItems.SelectedIndex;
                Close();
            }
            else
                MessageBox.Show(Properties.strings.ListSelectionPick);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in Items)
                lblItems.Items.Add(item);
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
