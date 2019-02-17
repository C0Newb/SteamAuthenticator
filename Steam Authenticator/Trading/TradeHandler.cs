using SteamAuth;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SteamAuthenticator.Trading
{
    class TradeHandler
    {
        private int Refreshed { get; set; }
        private DateTime InternalCachedUpdated { get; set; }

        public DateTime CachedUpdate { get => InternalCachedUpdated; }
        public string CahcedUpdateString
        {
            get
            {
                if (InternalCachedUpdated != null && Refreshed >= 1)
                    return InternalCachedUpdated.ToString("MM-dd, hh:mm:ss");
                else
                    return "Never";
            }
        }


        /// <summary>
        /// The accounts TradeHanlder works with
        /// </summary>
        public TradeAccount[] TradeAccounts { get; set; }

        /// <summary>
        /// Returns the cached confirmations from all accounts
        /// </summary>
        public Confirmation[] Confirmations
        {
            get
            {
                if (TradeAccounts != null)
                    if (TradeAccounts.Length <= 0)
                        return new Confirmation[0];
                    else
                    {
                        List<Confirmation> confs = new List<Confirmation>();
                        foreach (TradeAccount tacc in TradeAccounts)
                        {
                            if (tacc.Confirmations == null) continue;
                            foreach (Confirmation conf in tacc.Confirmations)
                                confs.Add(conf);
                        }
                        return confs.ToArray();
                    }
                else
                    return new Confirmation[0];
            }
        }

        /// <summary>
        /// Sets the accounts TradeHandler works with
        /// </summary>
        /// <param name="accounts">The accounts you'll be working with</param>
        public void SetAccounts(SteamGuardAccount[] accounts)
        {
            List<TradeAccount> newTradeAccounts = new List<TradeAccount>();
            foreach (SteamGuardAccount acc in accounts)
            {
                TradeAccount a = new TradeAccount
                {
                    Account = acc
                };
                newTradeAccounts.Add(a);
            }

            TradeAccounts = newTradeAccounts.ToArray();
        }

        /// <summary>
        /// Refreshes the confirmation cache
        /// </summary>
        public void RefreshConfirmations(bool autoCheck = false)
        {
            if ((DateTime.Now - InternalCachedUpdated).TotalSeconds < 10)
                return; // Force a 10 second cool down, if you spam refreshes Steam locks you out for a while

            if (TradeAccounts == null) return;

            int atAccount = -1;
            try
            {
                for (int i = 0; i < TradeAccounts.Length; i++)
                {
                    atAccount = i;
                    TradeAccounts[i].Confirmations = TradeAccounts[i].Account.FetchConfirmations();
                }
            }
            catch (SteamGuardAccount.WGTokenInvalidException)
            {
                TradeAccounts[atAccount].Account.RefreshSession();
                if (!autoCheck)
                    MessageBox.Show("Failed to refresh confirmation for " + TradeAccounts[atAccount].Account.AccountName + ", WGInvalidException. An attempt to refresh the account session was made. Try again.");
            }
            catch (SteamGuardAccount.WGTokenExpiredException)
            {
                TradeAccounts[atAccount].Account.RefreshSession();
                if (!autoCheck)
                    MessageBox.Show("Failed to refresh confirmation for " + TradeAccounts[atAccount].Account.AccountName + ", WGTokenExpired. Please try refreshing the sessions for your accounts");
            }
            catch (Exception e)
            {
                if (!autoCheck)
                    MessageBox.Show("Failed to refresh confirmations for " + TradeAccounts[atAccount].Account.AccountName + "!" + Environment.NewLine + e.ToString());
            }

            InternalCachedUpdated = DateTime.Now;
            Refreshed++;
        }

        /// <summary>
        /// Removes duplicated accounts
        /// </summary>
        public void CleanUp()
        {
            try
            {
                List<TradeAccount> newTradeAccounts = new List<TradeAccount>();
                List<string> accountsChecked = new List<string>();

                foreach (TradeAccount tAcc in TradeAccounts)
                {
                    bool skip = false;
                    foreach (string ac in accountsChecked)
                    {
                        if (ac == tAcc.Account.AccountName)
                            skip = true;
                        break;
                    }
                    if (skip) continue;

                    newTradeAccounts.Add(tAcc);
                }

                TradeAccounts = newTradeAccounts.ToArray();
            }
            catch (Exception) { }
        }

        public TradeAccount RemoveTradeID(string accountName, string ID)
        {
            CleanUp();
            int accountIndex = -1;
            for (var i = 0; i <= TradeAccounts.Length; i++)
                if (TradeAccounts[i].Account.AccountName == accountName)
                    accountIndex = i;

            if (accountIndex == -1) return null;

            Confirmation[] newConfs = new Confirmation[TradeAccounts[accountIndex].Confirmations.Length - 1];
            int j = 0;

            for (var i = 0; i <= TradeAccounts[accountIndex].Confirmations.Length; i++)
                if (TradeAccounts[accountIndex].Confirmations[i].ID.ToString() != ID)
                {
                    newConfs[j] = TradeAccounts[accountIndex].Confirmations[i];
                    j++;
                }

            return TradeAccounts[accountIndex];
        }
    }
}
