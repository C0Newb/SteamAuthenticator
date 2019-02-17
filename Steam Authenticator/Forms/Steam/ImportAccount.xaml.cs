using Newtonsoft.Json;
using SteamAuthenticator.BackEnd;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Security;

namespace SteamAuthenticator.Forms
{
    /// <summary>
    /// Interaction logic for ImportAccount.xaml
    /// </summary>
    public partial class ImportAccount : Window
    {
        private Manifest mManifest;

        private SecureString passKey;

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

        public ImportAccount(SecureString pass)
        {
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, new KeyEventHandler(KeyUp), true);
            InitializeComponent();
            mManifest = Manifest.GetManifest();
            passKey = pass;

            label.Content = Properties.strings.ImportEnterPasskey;
            label1.Content = Properties.strings.ImportNotice;
            btnImport.Content = Properties.strings.ImportSelect;
            btnCancel.Content = Properties.strings.btnCancel;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }


        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            #region Import
                //read EncryptionKey from input box
                SecureString ImportUsingEncriptionKey = txtPass.SecurePassword;

            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "maFile (.maFile)|*.maFile|AllFiles (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
                Title = Properties.strings.ImportSelectT,
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Stream fileStream = openFileDialog1.OpenFile();
                string fileContents = null;

                using (System.IO.StreamReader reader = new System.IO.StreamReader(fileStream))
                {
                    fileContents = reader.ReadToEnd();
                }
                fileStream.Close();

                try
                {
                    if (ImportUsingEncriptionKey.Length <= 0)
                    {
                        // Import maFile
                        #region Import maFile
                        SteamGuardAccount maFile = JsonConvert.DeserializeObject<SteamGuardAccount>(fileContents);
                        if (maFile.Session.SteamID != 0)
                        {
                            mManifest.SaveAccount(maFile, mManifest.Encrypted, passKey);
                            MessageBox.Show(Properties.strings.ImportComplete);
                        }
                        else
                            throw new Exception(Properties.strings.ImportInvaildSteamID);
                        #endregion
                    }
                    else
                    {
                        // Import Encrypted maFile
                        #region Import Encrypted maFile
                        //Read manifest.json encryption_iv encryption_salt
                        string ImportFileName_Found = "0";
                        string Salt_Found = null;
                        string IV_Found = null;
                        string ReadManifestEx = "0";


                        // extract folder path
                        string fullPath = openFileDialog1.FileName;
                        string fileName = openFileDialog1.SafeFileName;
                        string path = fullPath.Replace(fileName, "");

                        // extract fileName
                        string ImportFileName = fullPath.Replace(path, "");

                        string ImportManifestFile = path + "manifest.json";


                        if (File.Exists(ImportManifestFile))
                        {
                            string ImportManifestContents = File.ReadAllText(ImportManifestFile);


                            try
                            {
                                ImportManifest account = JsonConvert.DeserializeObject<ImportManifest>(ImportManifestContents);
                                //bool Import_encrypted = account.Encrypted;


                                foreach (var entry in account.Entries)
                                {
                                    string FileName = entry.FileName;

                                    if (ImportFileName == FileName)
                                    {
                                        ImportFileName_Found = "1";
                                        IV_Found = entry.IV;
                                        Salt_Found = entry.Salt;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                ReadManifestEx = "1";
                                MessageBox.Show(Properties.strings.ImportFailedEncrypted);
                            }


                            // DECRIPT & Import
                            #region DECRIPT & Import
                            if (ReadManifestEx == "0")
                            {
                                if (ImportFileName_Found == "1" && Salt_Found != null && IV_Found != null)
                                {
                                    string decryptedText = Encryptor.DecryptData(ImportUsingEncriptionKey, Salt_Found, IV_Found, fileContents);

                                    if (decryptedText == null)
                                        MessageBox.Show(Properties.strings.ImportFailedEncryptedD);
                                    else
                                    {
                                        string fileText = decryptedText;

                                        SteamGuardAccount maFile = JsonConvert.DeserializeObject<SteamGuardAccount>(fileText);
                                        if (maFile.Session.SteamID != 0)
                                        {
                                            mManifest.SaveAccount(maFile, mManifest.Encrypted, passKey);
                                            MessageBox.Show(Properties.strings.ImportSuccessD);
                                            //MainForm.loadAccountsList();
                                        }
                                        else
                                            MessageBox.Show(Properties.strings.ImportInvaildSteamID);
                                    }
                                }
                                else
                                {
                                    if (ImportFileName_Found == "0")
                                        MessageBox.Show(Properties.strings.ImportAccountNotFound);
                                    else if (Salt_Found == null && IV_Found == null)
                                        MessageBox.Show(Properties.strings.ImportNoEncryptionData);
                                    else
                                    {
                                        if (IV_Found == null)
                                            MessageBox.Show(String.Format(Properties.strings.ImportMissingEncryptionData, "encryption_iv")); // "manifest.json does not contain: encryption_iv\nImport Failed.");
                                        else if (IV_Found == null)
                                            MessageBox.Show(String.Format(Properties.strings.ImportMissingEncryptionData, "encryption_salt")); // "manifest.json does not contain: encryption_salt\nImport Failed.");
                                    }
                                }
                            }
                            #endregion //DECRIPT & Import END


                        }
                        else
                        {
                            MessageBox.Show(Properties.strings.ImportMissingManifest);
                        }
                        #endregion //Import Encrypted maFile END
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show(String.Format(Properties.strings.ImportInvaildMaFile, Ex.Message)); // "This file is not a valid SteamAuth maFile.\nImport Failed." + Environment.NewLine + Ex.Message);
                }
            }
            #endregion
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class AppManifest
    {
        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }
    }


    public class ImportManifest
    {
        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }

        [JsonProperty("entries")]
        public List<ImportManifestEntry> Entries { get; set; }
    }

    public class ImportManifestEntry
    {
        [JsonProperty("encryption_iv")]
        public string IV { get; set; }

        [JsonProperty("encryption_salt")]
        public string Salt { get; set; }

        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamID { get; set; }
    }
}
