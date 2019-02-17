using System;
using System.IO;
using System.Windows.Forms;

namespace SteamAuthenticator.Notification
{
    public class NotificationIcon
    {
        private NotificationIcon(int id, string location) { Location = location; Id = id; }

        public string Location { get; set; }
        public int Id { get; set; }

        public ToolTipIcon ToToolTipIcon()
        {
            if (Id == 1 || Id == 2)
                return ToolTipIcon.Info;
            else if (Id == 3)
                return ToolTipIcon.Warning;
            else if (Id == 4)
                return ToolTipIcon.Error;
            else
                return ToolTipIcon.None;
        }

        public System.Windows.MessageBoxImage ToMessageBoxImage()
        {
            if (Id == 1 || Id == 2)
                return System.Windows.MessageBoxImage.Information;
            else if (Id == 3)
                return System.Windows.MessageBoxImage.Warning;
            else if (Id == 4)
                return System.Windows.MessageBoxImage.Error;
            else
                return System.Windows.MessageBoxImage.None;
        }

        public static NotificationIcon Icon { get => new NotificationIcon(1, new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\Resources\icon.png").LocalPath); }
        public static NotificationIcon Info { get => new NotificationIcon(2, @"C:\Windows\System32\SecurityAndMaintenance.png"); }
        public static NotificationIcon Alert { get => new NotificationIcon(3, @"C:\Windows\System32\SecurityAndMaintenance_Alert.png"); }
        public static NotificationIcon Error { get => new NotificationIcon(4,@"C:\Windows\System32\SecurityAndMaintenance_Error.png"); }
        public static NotificationIcon None { get => new NotificationIcon(0, null); }
        public static NotificationIcon Update { get => new NotificationIcon(5, new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\Resources\update.png").LocalPath); }

    }
}