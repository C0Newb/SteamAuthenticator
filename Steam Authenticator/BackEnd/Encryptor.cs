using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace SteamAuthenticator.BackEnd
{
    /// <summary>
    /// This class provides the controls that will encrypt and decrypt the *.maFile files
    /// 
    /// Passwords entered will be passed into 100k rounds of PBKDF2 (RFC2898) with a cryptographically random salt.
    /// The generated key will then be passed into AES-256 (RijndalManaged) which will encrypt the data
    /// in cypher block chaining (CBC) mode, and then write both the PBKDF2 salt and encrypted data onto the disk.
    /// </summary>
    public static class Encryptor
    {
        private const int PBKDF2_ITERATIONS = 50000; //Set to 50k to make program not unbearably slow. May increase in future.
        private const int SALT_LENGTH = 18;
        private const int KEY_SIZE_BYTES = 32;
        private const int IV_LENGTH = 16;

        // TO DO: Allow user to change entropy. Only problem preventing me from doing that now is how to securly save the 'entropy' data :(
        // ** Account entropy COULD be saved to the manifest, but if the manifest is encrypted with the default entropy, then it wont matter as anyone could decrypt the manifest and seize the 'entropy' data.
        /// <summary>
        /// The additional entropy DPAPI uses when encrypting manifest files (and non-account files)
        /// </summary>
        public static string Entropy
        {
            get
            {
                return "2849205"; // security at its finest. If you're building your own version CHANGE THIS!
                //return Properties.Settings.Default.Entropy;
            }
            set
            {
                //Properties.Settings.Default["Entropy"] = value;
                //Properties.Settings.Default.Save();
            }
        }
        /// <summary>
        /// The additional entropy DPAPI uses when encrypting account data
        /// </summary>
        public static string AccountEntropy
        {
            get
            {
                return "8294025"; // security at its finest. If you're building your own version CHANGE THIS!
                //return Properties.Settings.Default.AccountEntropy;
            }
            set
            {
                //Properties.Settings.Default["AccountEntropy"] = value;
                //Properties.Settings.Default.Save();
            }
        }

        static String SecureStringToString(SecureString value)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(value);

            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <summary>
        /// Returns an 8-byte cryptographically random salt in base64 encoding
        /// </summary>
        /// <returns></returns>
        public static string GetRandomSalt()
        {
            byte[] salt = new byte[SALT_LENGTH];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Returns a 16-byte cryptographically random initialization vector (IV) in base64 encoding
        /// </summary>
        /// <returns></returns>
        public static string GetInitializationVector()
        {
            byte[] IV = new byte[IV_LENGTH];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(IV);
            }
            return Convert.ToBase64String(IV);
        }


        /// <summary>
        /// Generates an encryption key derived using a password, a random salt, and specified number of rounds of PBKDF2
        /// 
        /// TODO: pass in password via SecureString? AM DOING
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private static byte[] GetEncryptionKey(SecureString password, string salt)
        {
            if (password.Length <= 0)
            {
                throw new ArgumentException("Password is empty");
            }
            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Salt is empty");
            }
            using (var ssb = new SecureStringBytes(password))
            {
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(ssb.GetBytes(), Convert.FromBase64String(salt), PBKDF2_ITERATIONS))
                {
                    ssb.Clear();
                    ssb.Dispose();
                    return pbkdf2.GetBytes(KEY_SIZE_BYTES);
                }
            }
        }

        /// <summary>
        /// Tries to decrypt and return data given an encrypted base64 encoded string. Must use the same
        /// password, salt, IV, and ciphertext that was used during the original encryption of the data.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordSalt"></param>
        /// <param name="iv">Initialization Vector</param>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        public static string DecryptData(SecureString password, string passwordSalt, string iv, string encryptedData)
        {
            if (password == null)
                throw new ArgumentException("Password is empty");
            if (password.Length <= 0)
            {
                throw new ArgumentException("Password is empty");
            }
            if (string.IsNullOrEmpty(passwordSalt))
            {
                throw new ArgumentException("Salt is empty");
            }
            if (string.IsNullOrEmpty(iv))
            {
                throw new ArgumentException("Initialization Vector is empty");
            }
            if (string.IsNullOrEmpty(encryptedData))
            {
                throw new ArgumentException("Encrypted data is empty");
            }

            byte[] cipherText = Convert.FromBase64String(encryptedData);
            byte[] key = GetEncryptionKey(password, passwordSalt);
            string plaintext = null;

            using (RijndaelManaged aes256 = new RijndaelManaged())
            {
                aes256.IV = Convert.FromBase64String(iv);
                aes256.Key = key;
                aes256.Padding = PaddingMode.PKCS7;
                aes256.Mode = CipherMode.CBC;

                //create decryptor to perform the stream transform
                ICryptoTransform decryptor = aes256.CreateDecryptor(aes256.Key, aes256.IV);

                //wrap in a try since a bad password yields a bad key, which would throw an exception on decrypt
                try
                {
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch (CryptographicException)
                {
                    plaintext = null;
                }
            }
            return plaintext;
        }

        /// <summary>
        /// Encrypts a string given a password, salt, and initialization vector, then returns result in base64 encoded string.
        /// 
        /// To retrieve this data, you must decrypt with the same password, salt, IV, and cyphertext that was used during encryption
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordSalt"></param>
        /// <param name="iv"></param>
        /// <param name="plaintext"></param>
        /// <returns></returns>
        public static string EncryptData(SecureString password, string passwordSalt, string iv, string plaintext)
        {
            if (password == null)
                throw new ArgumentException("Password is empty");
            if (password.Length <= 0)
            {
                throw new ArgumentException("Password is empty");
            }
            if (string.IsNullOrEmpty(passwordSalt))
            {
                throw new ArgumentException("Salt is empty");
            }
            if (string.IsNullOrEmpty(iv))
            {
                throw new ArgumentException("Initialization Vector is empty");
            }
            if (string.IsNullOrEmpty(plaintext))
            {
                throw new ArgumentException("Plain-text data is empty");
            }
            byte[] key = GetEncryptionKey(password, passwordSalt);
            byte[] ciphertext;

            using (RijndaelManaged aes256 = new RijndaelManaged())
            {
                aes256.Key = key;
                aes256.IV = Convert.FromBase64String(iv);
                aes256.Padding = PaddingMode.PKCS7;
                aes256.Mode = CipherMode.CBC;

                ICryptoTransform encryptor = aes256.CreateEncryptor(aes256.Key, aes256.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncypt = new StreamWriter(csEncrypt))
                        {
                            swEncypt.Write(plaintext);
                        }
                        ciphertext = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(ciphertext);
        }

        /// <summary>
        /// Use DPAPI to encrypt a <see cref="byte"/>[] (using optional <see langword="entropy"/> and <see langword="scope"/>)
        /// </summary>
        /// <param name="encryptThis">The <see cref="byte"/>[] you want to encrypt</param>
        /// <param name="entropy">(Optional) The entropy used. (If blank the application's entropy will be used.)</param>
        /// <param name="scope">(Optional) The scope that will be used. (If blank the scope will be set to <see cref="DataProtectionScope.CurrentUser"/></param>
        /// <returns>Protected string</returns>
        public static string DPAPIProtect(byte[] encryptThis, string entropy = "", DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (string.IsNullOrEmpty(entropy))
                entropy = Entropy;
            try
            {
                byte[] entrop = Encoding.ASCII.GetBytes(entropy);
                return "DPAPI" + Convert.ToBase64String(ProtectedData.Protect(encryptThis, entrop, scope));
            }
            catch (Exception)
            {
                return Encoding.UTF8.GetString(encryptThis);
            }
        }
        /// <summary>
        /// Use DPAPI to decrypt a <see cref="byte"/>[] (using optional <see langword="entropy"/> and <see langword="scope"/>)
        /// </summary>
        /// <param name="decryptThis">The <see cref="byte"/>[] you want to decrypt</param>
        /// <param name="entropy">(Optional) The entropy was used. (If blank the application's entropy will be used.)</param>
        /// <param name="scope">(Optional) The scope that was used. (If blank the scope will be set to <see cref="DataProtectionScope.CurrentUser"/></param>
        /// <returns>Unprotected string</returns>
        public static string DPAPIUnprotect(byte[] decryptThis, string entropy = "", DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (string.IsNullOrEmpty(entropy))
                entropy = Entropy;
            try
            {
                byte[] entrop = Encoding.ASCII.GetBytes(entropy);
                byte[] b = ProtectedData.Unprotect(decryptThis, entrop, scope);
                return Encoding.UTF8.GetString(b);
            }
            catch (Exception)
            {
                return Encoding.UTF8.GetString(decryptThis);
            }
        }

        /// <summary>
        /// Use DPAPI to encrypt a <see cref="string"/> (using optional <see langword="entropy"/> and <see langword="scope"/>)
        /// </summary>
        /// <param name="encryptThis">The <see cref="string"/> you want to encrypt</param>
        /// <param name="entropy">(Optional) The entropy used. (If blank the application's entropy will be used.)</param>
        /// <param name="scope">(Optional) The scope that will be used. (If blank the scope will be set to <see cref="DataProtectionScope.CurrentUser"/></param>
        /// <returns>Protected string</returns>
        public static string DPAPIProtect(string encryptThis, string entropy = "", DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            return DPAPIProtect(ASCIIEncoding.ASCII.GetBytes(encryptThis), entropy, scope);
        }

        /// <summary>
        /// Use DPAPI to decrypt a <see cref="string"/> (using optional <see langword="entropy"/> and <see langword="scope"/>)
        /// </summary>
        /// <param name="decryptThis">The <see cref="string"/> you want to decrypt</param>
        /// <param name="entropy">(Optional) The entropy was used. (If blank the application's entropy will be used.)</param>
        /// <param name="scope">(Optional) The scope that was used. (If blank the scope will be set to <see cref="DataProtectionScope.CurrentUser"/></param>
        /// <returns>Unprotected string</returns>
        public static string DPAPIUnprotect(string decryptThis, string entropy = "", DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (decryptThis.StartsWith("DPAPI"))
            {
                decryptThis = decryptThis.Remove(0, 5);
                return DPAPIUnprotect(Convert.FromBase64String(decryptThis), entropy, scope);
            }
            else
                return decryptThis;
        }
    }
}