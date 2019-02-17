using System;
using System.Windows.Forms;

namespace SteamAuthenticator.BackEnd
{
    class TrayIcon
    {

        public static NotifyIcon icon = new NotifyIcon
        {
            Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico")).Stream),
            Visible = true
        };

        public static Tuple<NotifyIcon,MenuItem> Setup (EventHandler restoreClick, EventHandler tradesClick, EventHandler copyCode, EventHandler quit, EventHandler doubleClick)
        {
            MenuItem trayIconMenuRestore = new MenuItem()
            {
                Text = Properties.strings.TrayIconRestore,
            };
            trayIconMenuRestore.Click += restoreClick;

            MenuItem trayIconMenuTrades = new MenuItem
            {
                Text = Properties.strings.TrayIconTrades
            };
            trayIconMenuTrades.Click += tradesClick;

            MenuItem trayIconMenuCopyCode = new MenuItem
            {
                Text = Properties.strings.TrayIconCopyCode
            };
            trayIconMenuCopyCode.Click += copyCode;

            MenuItem trayIconMenuQuit = new MenuItem
            {
                Text = Properties.strings.TrayIconQuit
            };
            trayIconMenuQuit.Click += quit;

            ContextMenu trayIconMenu = new ContextMenu();

            trayIconMenu.MenuItems.AddRange(new MenuItem[] { trayIconMenuRestore });
            trayIconMenu.MenuItems.AddRange(new MenuItem[] { trayIconMenuTrades });
            trayIconMenu.MenuItems.AddRange(new MenuItem[] { trayIconMenuCopyCode });
            trayIconMenu.MenuItems.AddRange(new MenuItem[] { trayIconMenuQuit });

            icon.DoubleClick += doubleClick;
            icon.ContextMenu = trayIconMenu;

            return new Tuple<NotifyIcon, MenuItem>(icon, trayIconMenuRestore);
        }
    }
}
