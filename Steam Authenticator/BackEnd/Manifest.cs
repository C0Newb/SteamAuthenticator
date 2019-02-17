using Newtonsoft.Json;
using SteamAuthenticator.Forms;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Security;

namespace SteamAuthenticator.BackEnd
{
    public class Manifest
    {
        // TO DO: DPAPI both the manifest but also maFiles before saving them.


        /// <summary>
        /// The version of the manifest file.
        /// </summary>
        [JsonProperty("version")]
        public Version Version { get; set; } = new Version(1, 6, 1); // If any breaking changes are made, then raise this. A breaking change will be anything that modifies previous saved settings.
        // Essentially, if you rename or delete any variable, then change the major version. If any variables are added, change the minior version.
        
        /// <summary>
        /// If <see langword="true"/>, then our maFiles are encrypted. If <see langword="false"/>,  then our files are not encrypted.
        /// </summary>
        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }

        /// <summary>
        /// If <see langword="true"/>, then every file is encrypted using DPAPI (along with any other encryption method(s)).
        /// </summary>
        [JsonProperty("UseDPAPI")]
        public static bool UseDPAPI { get; set; } = false;

        /// <summary>
        /// Not yet included in settings window, but is used. (Runs File.Encrypt(so-so file) on any file saved)
        /// </summary>
        [JsonProperty("UseWindowsFileEncryption")]
        public static bool UseWindowsFileEncryption { get; set; } = false;

        /// <summary>
        /// This toggles if we use Window's Credential Locker or not.
        /// </summary>
        [JsonProperty("credentialLocker")]
        public bool CredentialLocker { get; set; } = false;
        /// <summary>
        /// Toggles if we save the passkey with Window's Credential Locker or not
        /// </summary>
        [JsonProperty("RememberPasskey")]
        public static bool RememberPasskey { get; set; } = false;
        /// <summary>
        /// If <see langword="true"/>, Steam Authenticator will save the encryption key inside Window's Credential Locker and automatically 'enter' the passkey. If <see langword="false"/>, the user will have to enter the passkey.
        /// </summary>
        [JsonProperty("autoUnlockEncryptionManager")]
        public bool AutoUnlockEncryptionManager { get; set; } = false;

        /// <summary>
        /// If <see langword="true"/>, <see cref="SteamGuardAccount"/>'s will be saved inside 'maFiles'. If <see langword="false"/>, <see cref="SteamGuardAccount"/>'s will be saved inside Window's Credential Locker.
        /// </summary>
        [JsonProperty("useMaFiles")]
        public bool UseMaFiles { get; set; } = true;

        /// <summary>
        /// If <see langword="true"/>, Steam Authenticator will automatically enter the auth code into Steam Guard's window.
        /// </summary>
        [JsonProperty("autoEntry")]
        public bool AutoEntry { get; set; } = true;

        /// <summary>
        /// Not actually used, but checks if this is our 'first run' so we can open the <see cref="Welcome"/> window.
        /// </summary>
        [JsonProperty("first_run")]
        public bool FirstRun { get; set; } = true;

        /// <summary>
        /// Returns a list of <see cref="ManifestEntry"/>'s that have details on the accounts linked to the manifest.
        /// </summary>
        [JsonProperty("entries")]
        public List<ManifestEntry> Entries { get; set; }

        /// <summary>
        /// This ONLY works if you're <see langword="debugging"/> Steam Authenticator and not <see langword="releasing"/> it. (Set the builder to debug). Only then will this toggle showing debugging menus.
        /// </summary>
        [JsonProperty("developerMode")]
        public bool DeveloperMode { get; set; } = false;

        /// <summary>
        /// When <see langword="true"/> Steam Authenticator will check for any pending confirmations. <see cref="PeriodicCheckingInterval"/> sets the delay between checks.
        /// </summary>
        [JsonProperty("periodic_checking")]
        public bool PeriodicChecking { get; set; } = false;
        /// <summary>
        /// The interval to check trade/market transaction confirmations. See <see cref="PeriodicChecking"/> for more info.
        /// </summary>
        [JsonProperty("periodic_checking_interval")]
        public int PeriodicCheckingInterval { get; set; } = 5;

        /// <summary>
        /// Not in use
        /// </summary>
        [JsonProperty("periodic_checking_checkall")]
        public bool CheckAllAccounts { get; set; } = false;

        [JsonProperty("AutoRefreshSession")]
        public bool AutoRefreshSession { get; set; } = false;

        /// <summary>
        /// Automatically confirm market transactions for all accounts if <see langword="true"/>
        /// </summary>
        [JsonProperty("auto_confirm_market_transactions")]
        public bool AutoConfirmMarketTransactions { get; set; } = false;
        /// <summary>
        /// Automatically confirm trades for all accounts if <see langword="true"/>
        /// </summary>
        [JsonProperty("auto_confirm_trades")]
        public bool AutoConfirmTrades { get; set; } = false;

        /// <summary>
        /// Make the search box visible if <see langword="true"/>
        /// </summary>
        [JsonProperty("displaySearch")]
        public bool DisplaySearch { get; set; } = false;
        /// <summary>
        /// Sort the accounts listbox alphabetically if <see langword="true"/> (not used)
        /// </summary>
        [JsonProperty("sortAlpha")]
        public bool SortAlpha { get; set; } = false;

        /// <summary>
        /// Upon startup Steam Authenticator will check for updates if this is <see langword="true"/>.
        /// </summary>
        [JsonProperty("check_for_updates")]
        public bool CheckForUpdates { get; set; } = false; // don't want to annoy people :(

        /// <summary>
        /// If set to <see langword="true"/>, beta updates will be included with regular updates
        /// </summary>
        [JsonProperty("beta_updates")]
        public bool AllowBetaUpdates { get; set; } = false;


        private static Manifest _manifest { get; set; }

        private CredManifest credMan = new CredManifest();

        public static string GetExecutableDir()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            return new Uri(path).LocalPath;
        }

        public static Manifest GetManifest(bool forceLoad = false)
        {
            // Find config dir and manifest file

            string maDir = GetExecutableDir() + "\\maFiles\\";
            string manifestFile = maDir + "manifest.json";
            // Check if already statically loaded
            if (_manifest != null && !forceLoad)
                return _manifest;

            // If there's no config dir, create it
            if (!Directory.Exists(maDir))
                return _generateNewManifest();

            // If there's no manifest, create it
            if (!File.Exists(manifestFile))
            {
                _manifest = _generateNewManifest(true);
                return _manifest;
            }

            try
            {
                string manifestContents = File.ReadAllText(manifestFile);
                manifestContents = Encryptor.DPAPIUnprotect(manifestContents);
                _manifest = JsonConvert.DeserializeObject<Manifest>(manifestContents);

                if (_manifest.Encrypted && _manifest.Entries.Count == 0 && _manifest.UseMaFiles)
                {
                    _manifest.Encrypted = false;
                    _manifest.Save();
                }
                else if (_manifest.Encrypted && !_manifest.UseMaFiles)
                {
                    CredManifest credMan = new CredManifest();
                    credMan = CredManifest.GetManifest();
                    if (credMan.Entries.Count == 0)
                    {
                        _manifest.Encrypted = false;
                        _manifest.Save();
                    }
                }

                _manifest.RecomputeExistingEntries();

                return _manifest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Manifest _generateNewManifest(bool scanDir = false)
        {
            // No directory means no manifest file anyways.
            Manifest newManifest = new Manifest
            {
                Version = new Version(1, 0),
                Encrypted = false,
                CredentialLocker = false,
                AutoUnlockEncryptionManager = false,
                UseMaFiles = true,
                AutoEntry = true,
                DeveloperMode = false,
                AutoRefreshSession = false,
                PeriodicCheckingInterval = 5,
                PeriodicChecking = false,
                AutoConfirmMarketTransactions = false,
                AutoConfirmTrades = false,
                DisplaySearch = false,
                SortAlpha = false,
                CheckForUpdates = true,
                AllowBetaUpdates = false,
                Entries = new List<ManifestEntry>(),
                FirstRun = true
            };


            // Take a pre-manifest version and generate a manifest for it.
            if (scanDir)
            {
                string maDir = GetExecutableDir() + "\\maFiles\\";
                if (Directory.Exists(maDir))
                {
                    DirectoryInfo dir = new DirectoryInfo(maDir);
                    var files = dir.GetFiles();

                    foreach (var file in files)
                    {
                        if (file.Extension != ".maFile") continue;

                        string contents = File.ReadAllText(file.FullName);
                        SteamGuardAccount account = new SteamGuardAccount();
                        account = JsonConvert.DeserializeObject<SteamGuardAccount>(Encryptor.DPAPIUnprotect(contents, Encryptor.AccountEntropy));

                        ManifestEntry newEntry = new ManifestEntry()
                        {
                            FileName = file.Name,
                            SteamID = account.Session.SteamID,
                            Encrypted = false,
                        };
                        newManifest.Entries.Add(newEntry);
                    }

                    if (newManifest.Entries.Count > 0)
                    {
                        newManifest.Save();
                        newManifest.PromptSetupPasskey(Properties.strings.ManifestPropmptSetupKey);
                    }
                }
            }

            if (newManifest.Save())
                return newManifest;

            return null;
        }

        public class IncorrectPasskeyException : Exception { }
        public class ManifestNotEncryptedException : Exception { }

        #region Key stuff
        public SecureString PromptForPasskey()
        {
            if (!Encrypted)
                throw new ManifestNotEncryptedException();


            bool passkeyValid = false;
            SecureString passkey = null;

            try
            {
                if (CredentialLocker)
                {
                    credMan = CredManifest.GetManifest();
                    if (credMan.Key.Length >= 1)
                        if (VerifyPasskey(credMan.Key))
                        {
                            passkeyValid = true;
                            passkey = credMan.Key;
                        }
                }
            }
            catch (Exception) { }

            while (!passkeyValid)
            {
                InputForm passkeyForm = new InputForm(Properties.strings.ManifestEnterKey, true);
                passkeyForm.ShowDialog(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive)); // Gets the current active window and passes that to the input form (so it can center)
                if (!passkeyForm.Canceled)
                {
                    passkey = passkeyForm.txtPass.SecurePassword;
                    if (!VerifyPasskey(passkey))
                        MessageBox.Show(Properties.strings.ManifestKeyInvalid);
                    else
                    {
                        passkeyValid = true;
                        if (CredentialLocker)
                        {
                            credMan = CredManifest.GetManifest();
                            credMan.Key = passkey;
                            credMan.Save();
                        }
                    }
                }
                else
                    return null;
            }
            return passkey;
        }

        public SecureString PromptSetupPasskey(string initialPrompt = "123")
        {
            if (initialPrompt == "123")
            {
                initialPrompt = Properties.strings.ManifestPropmptSetupKey;
            }
            InputForm newpasskeyForm = new InputForm(initialPrompt, true);
            newpasskeyForm.ShowDialog(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive)); // Get the active window to center
            if (newpasskeyForm.Canceled || newpasskeyForm.GetText().Length == 0)
            {
                MessageBox.Show(Properties.strings.ManifestSetupCanceled, Properties.strings.ManifestSetupCanceledT, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            InputForm newpasskeyForm2 = new InputForm(Properties.strings.ManifestSetupConfirmKey, true);
            newpasskeyForm2.ShowDialog(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive));
            if (newpasskeyForm2.Canceled || newpasskeyForm2.GetText().Length == 0)
            {
                MessageBox.Show(Properties.strings.ManifestSetupCanceled, Properties.strings.ManifestSetupCanceledT, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            SecureString newpasskey = newpasskeyForm.GetPassword();
            SecureString confirmpasskey = newpasskeyForm2.GetPassword();

            if (newpasskey.Equals(confirmpasskey)) { MessageBox.Show(Properties.strings.ManifestSetupKeysDoNotMatch, "", MessageBoxButton.OK, MessageBoxImage.Error); return null; }

            if (!ChangeEncryptionKey(null, newpasskey))
            {
                MessageBox.Show(Properties.strings.ManifestSetupFailed, "", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            else
                MessageBox.Show(Properties.strings.ManifestSetupComplete, "", MessageBoxButton.OK, MessageBoxImage.Information);

            return newpasskey;
        }

        /// <summary>
        /// Decrypts/Encrypts all account files (depending on DPAPI setting)
        /// </summary>
        /// <returns></returns>
        public bool UpdateDPAPI()
        {

            credMan = CredManifest.GetManifest();
            if (UseMaFiles)
            {
                string maDir = GetExecutableDir() + "/maFiles/";
                for (int i = 0; i < Entries.Count; i++)
                {
                    ManifestEntry entry = Entries[i];
                    string filename = maDir + entry.FileName;
                    if (!File.Exists(filename)) continue;

                    string fileContents = File.ReadAllText(filename);

                    fileContents = Encryptor.DPAPIUnprotect(fileContents, Encryptor.AccountEntropy);

                    string toWriteFileContents = fileContents;

                    if (UseDPAPI == true)
                        toWriteFileContents = Encryptor.DPAPIProtect(toWriteFileContents, Encryptor.AccountEntropy);

                    File.WriteAllText(filename, toWriteFileContents);
                    if (UseWindowsFileEncryption)
                    {
                        File.Encrypt(filename);
                    }
                    else
                    {
                        File.Decrypt(filename);
                    }
                }
            }
            else
            {
                foreach (CredManifestEntry entry in credMan.Entries)
                {
                    string fileContents = entry.Contents;

                    string toWriteFileContents = Encryptor.DPAPIUnprotect(fileContents, Encryptor.AccountEntropy);

                    if (UseDPAPI)
                        toWriteFileContents = Encryptor.DPAPIProtect(toWriteFileContents, Encryptor.AccountEntropy);

                    entry.Contents = toWriteFileContents;
                }
                credMan.Save();
            }

            Save();

            return true;

        }

        public bool ChangeEncryptionKey(SecureString oldKey, SecureString newKey)
        {
            if (Encrypted)
                if (!VerifyPasskey(oldKey))
                    return false;
            bool toEncrypt = newKey != null;
            credMan = CredManifest.GetManifest();
            if (UseMaFiles)
            {
                string maDir = GetExecutableDir() + "/maFiles/";
                for (int i = 0; i < Entries.Count; i++)
                {
                    ManifestEntry entry = Entries[i];
                    string filename = maDir + entry.FileName;
                    if (!File.Exists(filename)) continue;

                    string fileContents = File.ReadAllText(filename);

                    fileContents = Encryptor.DPAPIUnprotect(fileContents, Encryptor.AccountEntropy);

                    if (fileContents.StartsWith("Encrypted"))
                    {
                        fileContents = Encryptor.DecryptData(oldKey, entry.Salt, entry.IV, fileContents.Remove(0, 9));
                    }

                    string newSalt = null;
                    string newIV = null;
                    string toWriteFileContents = fileContents;

                    if (toEncrypt)
                    {
                        newSalt = Encryptor.GetRandomSalt();
                        newIV = Encryptor.GetInitializationVector();
                        toWriteFileContents = "Encrypted" + Encryptor.EncryptData(newKey, newSalt, newIV, fileContents);
                    }

                    if (UseDPAPI)
                    {
                        toWriteFileContents = Encryptor.DPAPIProtect(toWriteFileContents, Encryptor.AccountEntropy);
                    }


                    File.WriteAllText(filename, toWriteFileContents);
                    if (UseWindowsFileEncryption)
                    {
                        File.Encrypt(filename);
                    }
                    else
                    {
                        File.Decrypt(filename);
                    }
                    entry.IV = newIV;
                    entry.Salt = newSalt;
                }
            }
            else
            {
                foreach (CredManifestEntry entry in credMan.Entries)
                {
                    string fileContents = entry.Contents;

                    fileContents = Encryptor.DPAPIUnprotect(fileContents, Encryptor.AccountEntropy);

                    if (fileContents.StartsWith("Encrypted"))
                    {
                        fileContents = Encryptor.DecryptData(oldKey, entry.Salt, entry.IV, fileContents.Remove(0,9));
                    }

                    string newSalt = null;
                    string newIV = null;
                    string toWriteFileContents = fileContents;

                    if (toEncrypt)
                    {
                        newSalt = Encryptor.GetRandomSalt();
                        newIV = Encryptor.GetInitializationVector();
                        toWriteFileContents = "Encrypted" + Encryptor.EncryptData(newKey, newSalt, newIV, fileContents);
                    }                    
                    else
                        entry.Encrypted = false;

                    if (UseDPAPI)
                    {
                        toWriteFileContents = Encryptor.DPAPIProtect(toWriteFileContents, Encryptor.AccountEntropy);
                    }

                    entry.Contents = toWriteFileContents;
                    entry.IV = newIV;
                    entry.Salt = newSalt;
                }
                credMan.Key = newKey;
                credMan.Save();
            }

            Encrypted = toEncrypt;

            Save();
            return true;
        }

        public bool VerifyPasskey(SecureString passkey)
        {
            if (!this.Encrypted || this.Entries.Count == 0)
                return true;

            var accounts = this.GetAllAccounts(passkey, 1);
            if (accounts != null && accounts.Length == 1)
                return accounts[0].AccountName != null; // It'll load an "account" no matter what you enter, but the account details will be all null.
            else
                return false;
        }
        #endregion

        public int GetAccountCount()
        {
            credMan = CredManifest.GetManifest();
            if (UseMaFiles)
                return Entries.Count;
            else
                return credMan.Entries.Count;
        }

        public SteamGuardAccount GetAccount(SecureString passkey = null, int index = 0)
        {
            SteamGuardAccount account = new SteamGuardAccount();
            if (passkey == null && Encrypted) return account;
            if (!UseMaFiles)
            {
                credMan = CredManifest.GetManifest();
                string fileText = credMan.Entries[index].Contents;

                fileText = Encryptor.DPAPIUnprotect(fileText, Encryptor.AccountEntropy);

                if (fileText.StartsWith("Encrypted"))
                {
                    string decryptedText = Encryptor.DecryptData(passkey, credMan.Entries[index].Salt, credMan.Entries[index].IV, fileText.Remove(0,9)); if (decryptedText == null) return account;
                    fileText = decryptedText;   
                }

                var acc = JsonConvert.DeserializeObject<SteamGuardAccount>(fileText);
                if (acc == null)
                    return account;
                return acc;
            }
            else
            {
                if (passkey == null && Encrypted) return new SteamGuardAccount();
                string maDir = GetExecutableDir() + "/maFiles/";

                string fileText = File.ReadAllText(maDir + Entries[index].FileName);

                fileText = Encryptor.DPAPIUnprotect(fileText, Encryptor.AccountEntropy);

                if (fileText.StartsWith("Encrypted"))
                {
                    string decryptedText = Encryptor.DecryptData(passkey, Entries[index].Salt, Entries[index].IV, fileText.Remove(0,9)); if (decryptedText == null) return account;
                    fileText = decryptedText;
                }

                var acc = JsonConvert.DeserializeObject<SteamGuardAccount>(fileText);
                if (acc == null)
                    return account;
                return acc;
            }
        }

        public SteamGuardAccount[] GetAllAccounts(SecureString passkey = null, int limit = -1)
        {
            List<SteamGuardAccount> accounts = new List<SteamGuardAccount>();
            if (passkey == null && Encrypted) return new SteamGuardAccount[0];
            if (!UseMaFiles)
            {
                credMan = CredManifest.GetManifest();
                for (int i = 0; i < credMan.Entries.Count; i++)
                {
                    accounts.Add(GetAccount(passkey, i));

                    if (limit != -1 && limit >= accounts.Count)
                        break;
                }
            }
            else
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    accounts.Add(GetAccount(passkey, i));

                    if (limit != -1 && limit >= accounts.Count)
                        break;
                }
            }
            return accounts.ToArray();
        }

        public bool RemoveAccount(SteamGuardAccount account, bool deleteMaFile = true)
        {
            if (UseMaFiles)
            {
                ManifestEntry entry = (from e in this.Entries where e.SteamID == account.Session.SteamID select e).FirstOrDefault();
                if (entry == null)
                    return true; // If something never existed, did you do what they asked?
                string maDir = GetExecutableDir() + "/maFiles/";
                string filename = maDir + entry.FileName;
                this.Entries.Remove(entry);
                if (Entries.Count == 0) Encrypted = false;
                if (Save() && deleteMaFile)
                {
                    try
                    {
                        File.Delete(filename);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            else
            {
                credMan = CredManifest.GetManifest();
                CredManifestEntry entry = (from e in credMan.Entries where e.SteamID == account.Session.SteamID select e).FirstOrDefault();
                if (entry == null)
                    return true;
                credMan.Entries.Remove(entry);
                if (credMan.Entries.Count == 0)
                    Encrypted = false;
                return credMan.Save();
            }
            return false;
        }

        public bool SaveAccount(SteamGuardAccount account, bool encrypt, SecureString passkey = null)
        {
            if (encrypt && passkey != null && passkey.Length >=1)
                return false;
            if (!encrypt && this.Encrypted)
                return false;
            if (account == null)
                return false;

            if (UseMaFiles)
            {
                string salt = null;
                string iV = null;
                string jsonAccount = JsonConvert.SerializeObject(account);
                              
                if (encrypt)
                {
                    salt = Encryptor.GetRandomSalt();
                    iV = Encryptor.GetInitializationVector();
                    string encrypted = "Encrypted" + Encryptor.EncryptData(passkey, salt, iV, jsonAccount);
                    if (encrypted == null)
                        return false;
                    jsonAccount = encrypted;
                }

                if (UseDPAPI)
                {
                    jsonAccount = Encryptor.DPAPIProtect(jsonAccount, Encryptor.AccountEntropy);
                }

                string maDir = GetExecutableDir() + "/maFiles/";
                string filename = account.Session.SteamID.ToString() + ".maFile";

                ManifestEntry newEntry = new ManifestEntry()
                {
                    SteamID = account.Session.SteamID,
                    IV = iV,
                    Salt = salt,
                    FileName = filename,
                };

                bool foundExistingEntry = false;
                for (int i = 0; i < this.Entries.Count; i++)
                {
                    if (Entries[i].SteamID == account.Session.SteamID)
                    {
                        Entries[i] = newEntry;
                        foundExistingEntry = true;
                        break;
                    }
                }

                if (!foundExistingEntry)
                {
                    Entries.Add(newEntry);
                }

                bool wasEncrypted = Encrypted;
                Encrypted = encrypt || Encrypted;

                if (!Save())
                {
                    Encrypted = wasEncrypted;
                    return false;
                }

                try
                {
                    File.WriteAllText(maDir + filename, jsonAccount);
                    if (UseWindowsFileEncryption)
                    {
                        File.Encrypt(maDir + filename);
                    }
                    else
                    {
                        File.Decrypt(maDir + filename);
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    credMan = CredManifest.GetManifest();
                    string salt = null; string iV = null;
                    string jsonAccount = JsonConvert.SerializeObject(account);
                    bool fileEncrypted = false;

                    if (encrypt)
                    {
                        salt = Encryptor.GetRandomSalt();
                        iV = Encryptor.GetInitializationVector();
                        string encrypted = "Encrypted" + Encryptor.EncryptData(passkey, salt, iV, jsonAccount);
                        if (encrypted == null)
                            return false;
                        jsonAccount = encrypted;
                        fileEncrypted = true;
                    }

                    if (UseDPAPI)
                    {
                        jsonAccount = Encryptor.DPAPIProtect(jsonAccount, Encryptor.AccountEntropy);
                    }

                    CredManifestEntry newEntry = new CredManifestEntry()
                    {
                        SteamID = account.Session.SteamID,
                        IV = iV,
                        Salt = salt,
                        Contents = jsonAccount,
                        Encrypted = fileEncrypted,
                    };
                    bool foundExistingEntry = false;
                    for (int i = 0; i < credMan.Entries.Count; i++)
                    {
                        if (credMan.Entries[i].SteamID == account.Session.SteamID)
                        {
                            credMan.Entries[i] = newEntry;
                            foundExistingEntry = true;
                            break;
                        }
                    }

                    if (!foundExistingEntry)
                        credMan.Entries.Add(newEntry);

                    credMan.Save();

                    bool wasEncrypted = Encrypted;
                    Encrypted = encrypt || Encrypted;

                    if (!Save())
                    {
                        Encrypted = wasEncrypted;
                        return false;
                    }
                    return true;
                }
                catch (Exception) { return false; }
            }
        }

        public bool Save()
        {
            string maDir = GetExecutableDir() + @"\maFiles\";
            string filename = maDir + "manifest.json";
            bool notFirstRun = true;
            if (!Directory.Exists(maDir))
                try
                {
                    notFirstRun = false;
                    Directory.CreateDirectory(maDir);
                }
                catch (Exception)
                {
                    return false;
                }
            try
            {
                try
                {
                    if (!UseMaFiles && Entries.Count > 0 && credMan.Entries.Count < Entries.Count) // Move the accounts over to CredMan
                    {
                        credMan = CredManifest.GetManifest();
                        if (!credMan.ImportAccounts())
                            UseMaFiles = true;
                    }
                    else if (UseMaFiles && notFirstRun)
                    {
                        credMan = CredManifest.GetManifest();
                        if (credMan.Entries.Count > 0 && Entries.Count < credMan.Entries.Count)
                            if (!credMan.ExportAccounts())
                                UseMaFiles = false;
                    }
                }
                catch { }

                if (UseDPAPI)
                {
                    File.WriteAllText(filename, Encryptor.DPAPIProtect(JsonConvert.SerializeObject(this)));
                }
                else
                {
                    File.WriteAllText(filename, JsonConvert.SerializeObject(this));
                }

                if (UseWindowsFileEncryption)
                {
                    File.Encrypt(filename);
                }
                else
                {
                    File.Decrypt(filename);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RecomputeExistingEntries()
        {
            if (UseMaFiles)
            {
                List<ManifestEntry> newEntries = new List<ManifestEntry>();
                string maDir = Manifest.GetExecutableDir() + "/maFiles/";

                foreach (var entry in this.Entries)
                {
                    string filename = maDir + entry.FileName;
                    if (File.Exists(filename))
                        newEntries.Add(entry);
                }

                Entries = newEntries;

                if (Entries.Count == 0)
                    Encrypted = false;
            }
        }

        public void MoveEntry(int from, int to)
        {
            if (UseMaFiles)
            {
                if (from < 0 || to < 0 || from > Entries.Count || to > Entries.Count - 1) return;
                ManifestEntry sel = Entries[from];
                Entries.RemoveAt(from);
                Entries.Insert(to, sel);
                Save();
            }
            else
            {
                credMan = CredManifest.GetManifest();
                if (from < 0 || to < 0 || from > credMan.Entries.Count || to > credMan.Entries.Count - 1) return;
                CredManifestEntry sel = credMan.Entries[from];
                credMan.Entries.RemoveAt(from);
                credMan.Entries.Insert(to, sel);
                credMan.Save();
            }
        }

        public class ManifestEntry
        {
            [JsonProperty("encryption_iv")]
            public string IV { get; set; }

            [JsonProperty("encryption_salt")]
            public string Salt { get; set; }

            [JsonProperty("filename")]
            public string FileName { get; set; }

            [JsonProperty("steamid")]
            public ulong SteamID { get; set; }

            [JsonProperty("encrypted")]
            public bool Encrypted { get; set; }
        }
    }
}
