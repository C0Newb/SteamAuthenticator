using SteamAuthenticator.Notification.ShellHelpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using SteamAuthenticator.BackEnd;
using System.Windows;

namespace SteamAuthenticator.Notification
{
    class Notifications
    {
        private const String APP_ID = "Watsuprico.SteamAuthenticator";

        /// <summary>
        /// Registers the current running application with the COM server and creates a shortcut in the start menu to authorize notifications
        /// </summary>
        public static void RegisterAppForNotificationSupport()
        {
            String shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\Steam Authenticator.lnk";
            if (!File.Exists(shortcutPath))
            {
                // Find the path to the current executable
                String exePath = Process.GetCurrentProcess().MainModule.FileName;
                InstallShortcut(shortcutPath, exePath);
                RegisterComServer(exePath);
            }
        }

        /// <summary>
        /// Registers the COM server for the running program
        /// </summary>
        /// <param name="exePath">The current path of the running program</param>
        private static void RegisterComServer(string exePath)
        {
            // We register the app process itself to start up when the notification is activated, but
            // other options like launching a background process instead that then decides to launch
            // the UI as needed.
            string regString = String.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(NotificationActivator).GUID);
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);
            key.SetValue(null, exePath);
        }

        /// <summary>
        /// Install the applications shortcut into the start menu for notification support
        /// </summary>
        /// <param name="shortcutPath">Were the shortcut is being saved</param>
        /// <param name="exePath">The current path of the running program</param>
        private static void InstallShortcut(string shortcutPath, string exePath)
        {
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            newShortcut.SetPath(exePath);

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            PropVariantHelper varAppId = new PropVariantHelper();
            varAppId.SetValue(APP_ID);
            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ID, varAppId.Propvariant);

            PropVariantHelper varToastId = new PropVariantHelper
            {
                VarType = VarEnum.VT_CLSID
            };
            varToastId.SetValue(typeof(NotificationActivator).GUID);

            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ToastActivatorCLSID, varToastId.Propvariant);

            // Commit the shortcut to disk
            ShellHelpers.IPersistFile newShortcutSave = (ShellHelpers.IPersistFile)newShortcut;

            newShortcutSave.Save(shortcutPath, true);
        }




        /// <summary>
        /// Sends a notification to the action center
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification contents</param>
        /// <param name="location">The notification image location</param>
        /// <param name="activeArgs">The argument that get's passed to the handler when the notification is clicked</param>
        private static void NewToast(string title, string message, string location, string activeArgs)
        {
            XmlDocument toastXml = new XmlDocument();
            toastXml.LoadXml(@"<toast launch='"+ activeArgs +"'>" +
    "<visual>" +
        "<binding template='ToastGeneric'>" +
            "<text>" + title + "</text>" +
            "<text>" + message + "</text>" +
            "<image placement='appLogoOverride' src='" + location + "'/>" +
        "</binding>" +
    "</visual>" +
"</toast>");

            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += Toast_Activated;
            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private static void Toast_Activated(ToastNotification sender, object args)
        {
#if DEBUG
            System.Windows.MessageBox.Show("Active (2)");
#endif
        }



        /// <summary>
        /// Display a notification if supported or a balloon tip
        /// </summary>
        /// <param name="title">The title of the notification</param>
        /// <param name="message">The message of the notification</param>
        /// <param name="useIcon">The icon of the notification, info or warning/error</param>
        /// <param name="action">The argument that get's passed to the handler when the notification is clicked</param>
        public static void Show(string title, string message, NotificationIcon useIcon = null, string action = "action=dismiss")
        {
            if (useIcon == null)
                useIcon = NotificationIcon.Info;

            if (Environment.OSVersion.Version < new Version("6.2.0.0") && TrayIcon.icon != null) // Doesn't even know toast notifications exist (or tray icon set)
            {
                TrayIcon.icon.BalloonTipIcon = useIcon.ToToolTipIcon();
                TrayIcon.icon.BalloonTipTitle = title;
                TrayIcon.icon.BalloonTipText = message;
                TrayIcon.icon.ShowBalloonTip(3);
            }
            else if (Environment.OSVersion.Version >= new Version("6.2.0.0")) // Can use toast notifications
                NewToast(title, message, useIcon.Location, action);
            else
                MessageBox.Show(message, "Steam Authenticator - " + title, MessageBoxButton.OKCancel, useIcon.ToMessageBoxImage());
        }
    }
}