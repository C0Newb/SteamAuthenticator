using Microsoft.Win32;
using SteamAuth;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SteamAuthenticator.BackEnd
{
    class AutoEntry
    {
        #region Window stuff
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion


        private Manifest man = new Manifest();
        private SteamGuardAccount[] accs;
        public SteamGuardAccount[] Accounts
        {
            set
            {
                accs = value;
                /*for(int i=0; i<value.Count(); i++)
                {
                    accs[i].DeviceID = accs[i].IdentitySecret = accs[i].RevocationCode = accs[i].Secret1 = accs[i].SerialNumber = accs[i].TokenGID = accs[i].SharedSecret = null;
                    accs[i].URI = null;
                    accs[i].Session = null;
                }*/
            }
        }

        private bool open = false;

        public ErrorAutoEnter AutoEnter()
        {
            man = Manifest.GetManifest();

            ErrorAutoEnter err = new ErrorAutoEnter
            {
                Successful = false
            };
            if (open)
            {
                err.Reason = "In progress";
                return err;
            }

            if (!man.AutoEntry)
            {
                err.Reason = "Auto login disabled";
                return err;
            }

            #region Check if the user is trying to enter the code
            // Used in window title stuff
            StringBuilder Buff = new StringBuilder(256); IntPtr authHandle = GetForegroundWindow();
            // Get the title (for the next part)
            if (GetWindowText(authHandle, Buff, 256) <= 0)
            {
                err.Reason = "Failed to get window title";
                return err;
            }
            // Is this the right window?
            if (!Buff.ToString().StartsWith("Steam Guard - Computer Authorization Required"))
            {
                err.Reason = "Incorrect window " + Buff.ToString();
                return err;
            }
            #endregion

            // We'll use these later
            SteamGuardAccount account = null;
            string Code = "";

            // Get the username trying to login (using registry)
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("AutoLoginUser"); // the best key to exist
                        if (o != null)
                        {
                            string accountName = o as String;
                            SteamGuardAccount[] accountsListed = accs;
                            accountsListed.OrderBy(x => x.AccountName.Length);

                            foreach (SteamGuardAccount useAccount in accountsListed)
                                if (useAccount.AccountName == accountName)
                                    account = useAccount;

                            if (account == null)
                            {
                                err.Reason = "Unknown username trying to login: '" + accountName + "'";
                                return err;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                err.Reason = "Error while trying to read registry!" + Environment.NewLine + ex.Message;
                return err;
            }

            // Okay, now get the code for that user
            Code = account.GenerateSteamGuardCode();

            // Now enter the auth code
            if (!String.IsNullOrEmpty(Code))
            {
                SetForegroundWindow(authHandle); // Be sure we're in the right window
                System.Windows.Forms.SendKeys.SendWait(Code);
                System.Windows.Forms.SendKeys.Flush();
                System.Threading.Thread.Sleep(50);
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                System.Windows.Forms.SendKeys.Flush();
                return new ErrorAutoEnter();
            }
            else
            {
                err.Reason = "Code empty";
                return err;
            }

        }
    }

    public class ErrorAutoEnter
    {
        public bool Successful { get; set; } = true;
        public string Reason { get; set; } = "";
    }
}
