using SteamAuthenticator.BackEnd;
using SteamAuthenticator.Forms;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;


namespace SteamAuthenticator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern
        bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern
            bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern
            bool IsIconic(IntPtr hWnd);

        /// -------------------------------------------------------------------------------------------------
        /// <summary> check if current process already running. if running, set focus to existing process and 
        ///           returns <see langword="true"/> otherwise returns <see langword="false"/>. </summary>
        /// <returns> <see langword="true"/> if it succeeds, <see langword="false"/> if it fails. </returns>
        /// -------------------------------------------------------------------------------------------------
        public static bool AlreadyRunning()
        {
            const int swRestore = 9;

            var me = Process.GetCurrentProcess();
            var arrProcesses = Process.GetProcessesByName(me.ProcessName);

            if (arrProcesses.Length > 1)
            {
                for (var i = 0; i < arrProcesses.Length; i++)
                {
                    if (arrProcesses[i].Id != me.Id)
                    {
                        // get the window handle
                        IntPtr hWnd = arrProcesses[i].MainWindowHandle;

                        // if iconic, we need to restore the window
                        if (IsIconic(hWnd))
                        {
                            ShowWindowAsync(hWnd, swRestore);
                        }

                        // bring it to the foreground
                        SetForegroundWindow(hWnd);
                        break;
                    }
                }
                return true;
            }

            return false;
        }

        public App() : base()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception err = e.Exception;
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).ToString() + "\\error.txt";
                path = new Uri(path).LocalPath;

                using (StreamWriter dumpFile = new StreamWriter(path))
                {
                    dumpFile.WriteLine("There was an error with Steam Authenticator" + Environment.NewLine + "Here's some info: " + Environment.NewLine + "Version: " + version + Environment.NewLine + "Error: " + err.Message + Environment.NewLine + "Trace: " + err.StackTrace);
                }

                MessageBoxResult result = MessageBox.Show("An error has occurred! I went ahead and dumped some info into a text file named 'error.txt'. Please report this error so we can fix it! (Include this text file, it's in the same folder as the executable)." + Environment.NewLine + "Error: " + err.Message + Environment.NewLine + "Continue?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.No)
                {
                    e.Handled = false;
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error has occurred! I tried to dump some info into a text file but failed. Please report this error so we can fix it! (Include a screenshot of this please)" + Environment.NewLine + "Error (SA): " + err.Message + Environment.NewLine + "Error (TXT): " + ex.Message + Environment.NewLine + "Error (TXT) Trace: " + ex.StackTrace, "Error (x2)", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
            //Environment.Exit(0);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            // Application is running
            if (AlreadyRunning())
                Environment.Exit(0);

            MainWindow mainWindow = new MainWindow(e.Args);
            mainWindow.Show();
        }
    }
}
