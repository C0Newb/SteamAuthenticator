using System;
using System.Runtime.InteropServices;
using SteamAuthenticator.Notification.ShellHelpers;

namespace SteamAuthenticator.Notification
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("ddad094a-bb27-4630-a930-5a474381d246"), ComVisible(true)]
    public class NotificationActivator : INotificationActivationCallback
    {
        public static MainWindow mainWindow;

        /// <summary>
        /// This is called when a notification is activated
        /// </summary>
        public void Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, uint dataCount)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.ToastActivated(appUserModelId, invokedArgs);
            });
        }

        /// <summary>
        /// Run this once on application first start
        /// </summary>
        public static void Initialize()
        {
            regService = new RegistrationServices();

            cookie = regService.RegisterTypeForComClients(
                typeof(NotificationActivator),
                RegistrationClassContext.LocalServer,
                RegistrationConnectionType.MultipleUse);
        }
        public static void Uninitialize()
        {
            if (cookie != -1 && regService != null)
                regService.UnregisterTypeForComClients(cookie);
        }

        private static int cookie = -1;
        private static RegistrationServices regService = null;
    }
}