using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Security.Credentials;
using static SteamAuthenticator.BackEnd.Manifest;

namespace SteamAuthenticator.BackEnd
{
    class CredManifest
    {
        private static SecureString SecureKey { get; set; } = new SecureString();
        private static bool Retreiving { get; set; }

        [JsonProperty("UnsecureKey")]
        public static string UnsecureKey { get; set; }


        public SecureString Key
        {
            get {
                return SecureKey;
            }
            set {
                if (!Retreiving)
                {
                    SecureKey = value;
                    IntPtr unsecure = Marshal.SecureStringToGlobalAllocUnicode(value);
                    UnsecureKey = Marshal.PtrToStringUni(unsecure);
                }
            }
        }

        [JsonProperty("entries")]
        public List<CredManifestEntry> Entries { get; set; }

        private static string _originalCred { get; set; }
        private static CredManifest _cred { get; set; }

        public static string GetExecutableDir()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            return new Uri(path).LocalPath;
        }

        public static CredManifest GetManifest(bool forceLoad = false)
        {
            if (_cred != null && !forceLoad)
                return _cred;

            try
            {
                Retreiving = true;
                PasswordCredential credential = null; var vault = new PasswordVault();
                credential = vault.Retrieve("SteamAuthenticator", "Storage");
                if (credential != null)
                    credential.RetrievePassword();
                else
                    return _generateNewManifest(new SecureString());

                string manifestContents = credential.Password.ToString();
                manifestContents = Encryptor.DPAPIUnprotect(credential.Password.ToString());
                _cred = JsonConvert.DeserializeObject<CredManifest>(manifestContents);
                _originalCred = credential.Password.ToString();

                foreach (char c in UnsecureKey.ToCharArray())
                {
                    SecureKey.AppendChar(c);
                }

                UnsecureKey = null;
                GC.Collect(); // security at its finest
                GC.WaitForPendingFinalizers();

                Retreiving = false;

                return _cred;
            }
            catch (Exception)
            {
                Retreiving = false;
                return _generateNewManifest(new SecureString());
            }
        }

        private static CredManifest _generateNewManifest(SecureString key)
        {
            // No directory means no manifest file anyways.
            CredManifest newManifest = new CredManifest
            {
                Key = key,
                Entries = new List<CredManifestEntry>()
            };

            if (newManifest.Save())
                return newManifest;

            return null;
        }

        public bool Save()
        {
            if (_originalCred != null)
            {
                try
                {
                    try // To delete the current stuff, you have to know the old stuff...
                    {
                        new PasswordVault().Remove(new PasswordCredential("SteamAuthenticator", "Storage", Encryptor.DPAPIUnprotect(_originalCred)));
                    }
                    catch (Exception)
                    {
                    //    System.Windows.MessageBox.Show("Failed to remove existing manifest!", "CredManifest");
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            if (!Manifest.RememberPasskey)
            {
                UnsecureKey = ""; // 'Flush' it from memory then save the manifest. Note: unless the key gets resubmitted to CredManifest, it wont know the key until a program restart
            }
            else
            {
                Key = Key; // This refreshes the unsecure and secure copy
            }
            _originalCred = JsonConvert.SerializeObject(this);
            if (Manifest.UseDPAPI)
                new PasswordVault().Add(new PasswordCredential("SteamAuthenticator", "Storage", Encryptor.DPAPIProtect(_originalCred)));
            else
                new PasswordVault().Add(new PasswordCredential("SteamAuthenticator", "Storage", _originalCred));
            return true;
        }

        /// <summary>
        /// Imports accounts from maFiles to the credential manager
        /// </summary>
        /// <returns></returns>
        public bool ImportAccounts()
        {
            try
            {
                Manifest man = new Manifest();
                man = Manifest.GetManifest();
                foreach (ManifestEntry entry in man.Entries)
                {
                    string maDir = GetExecutableDir() + "/maFiles/";
                    string contents = System.IO.File.ReadAllText(maDir + entry.FileName);

                    Entries.Add(new CredManifestEntry()
                    {
                        Contents = contents,
                        IV = entry.IV,
                        Salt = entry.Salt,
                        SteamID = entry.SteamID,
                        Encrypted = entry.Encrypted,
                    });
                    System.IO.File.Delete(maDir + entry.FileName);
                }
                man.Entries = new List<ManifestEntry>();
                man.Save();
                Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Exports accounts from the credential manager to maFiles
        /// </summary>
        /// <returns>Whether or not it succeeded</returns>
        public bool ExportAccounts()
        {
            try
            {
                if (Entries.Count > 0) // Without this there'll be an infinite loop on first run
                {
                    Manifest man = new Manifest();
                    man = Manifest.GetManifest();
                    string maDir = GetExecutableDir() + "/maFiles/";
                    foreach (CredManifestEntry entry in Entries)
                    {
                        string contents = entry.Contents;
                        if (!Manifest.UseDPAPI)
                        {
                            contents = Encryptor.DPAPIUnprotect(contents, Encryptor.AccountEntropy);
                        }

                        File.WriteAllText(maDir + entry.SteamID + ".maFile", contents);
                        man.Entries.Add(new ManifestEntry()
                        {
                            FileName = entry.SteamID.ToString() + ".maFile",
                            SteamID = entry.SteamID,
                            IV = entry.IV,
                            Salt = entry.Salt,
                            Encrypted = entry.Encrypted
                        });
                        if (UseWindowsFileEncryption)
                        {
                            File.Encrypt(maDir + entry.SteamID + ".maFile");
                        }
                        else
                        {
                            File.Decrypt(maDir + entry.SteamID + ".maFile");
                        }
                    }
                    man.Save();
                    Entries = new List<CredManifestEntry>();
                    Save();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class CredManifestEntry
    {
        [JsonProperty("encryption_iv")]
        public string IV { get; set; }

        [JsonProperty("encryption_salt")]
        public string Salt { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamID { get; set; }

        /// <summary>
        /// What the maFile would contain (meaning it's also encrypted using DPAPI!)
        /// </summary>
        [JsonProperty("filecontents")]
        public string Contents { get; set; }

        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }
    }
}
