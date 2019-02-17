using System;
using System.Collections.Generic;
using Octokit;
using SteamAuthenticator.Forms;
using System.Reflection;
using System.Windows;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using SteamAuthenticator.Notification;

namespace SteamAuthenticator.BackEnd
{
    class UpdateMan
    {
        private Credentials tokenAuth = new Credentials("tokenAuthFromGITHUB");
        private GitHubClient github = new GitHubClient(new ProductHeaderValue("SteamAuthenticator-Updater-Watsuprico"));
        private Version latestInternal = new Version("0.0");
        private IReadOnlyList<Release> releasesInternal;

        private string currentPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
        private Window owner;

        #region Public stuff
        /// <summary>
        /// Where we save update files
        /// </summary>
        public readonly String updateFolder = @"\UpdateFiles\";
        /// <summary>
        /// Where the actual update package is saved.
        /// </summary>
        public readonly String updateFile = @"update.zip";

        public IReadOnlyList<Release> Releases { get { GetReleases(); return releasesInternal; } }
        public static Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        public Version Latest { get { GetLatest(); return latestInternal; } }
        #endregion

        #region UpdateMan setup
        public UpdateMan(BrowseUpdates own)
        {
            owner = own;
            github.Credentials = tokenAuth;
        }
        public UpdateMan(MainWindow own)
        {
            owner = own;
            github.Credentials = tokenAuth;
        }
        public UpdateMan(Downloader own)
        {
            owner = own;
            github.Credentials = tokenAuth;
        }
        #endregion


        /// <summary>
        /// Download the downloadURL file to CurrentDir\UpdateFiles\update.zip
        /// </summary>
        /// <param name="downloadURL">The url of the file to download</param>
        /// <param name="updateInfo">The description of the file</param>
        private bool InternalDownload(string downloadURL, string updateInfo = "")
        {
            DownloaderItem dlItem = new DownloaderItem()
            {
                DownloadUrl = downloadURL,
                UpdateInfo = updateInfo
            };
            Downloader downloader = new Downloader(dlItem);
            downloader.ShowDialog(owner);
            if (downloader.downloadItem.Complete)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Extract the updater program and then run it
        /// </summary>
        private void InternalInstall()
        {
            try
            {
                var archive = ZipFile.OpenRead(currentPath + updateFolder + updateFile);
                for (int i = 0; i < archive.Entries.Count - 1; i++)
                    try
                    {
                        ZipArchiveEntry entry = archive.Entries[i];
                        if (entry.FullName == "Extract Update.exe")
                        {
                            File.Delete(currentPath + updateFolder + entry.FullName);
                            entry.ExtractToFile(currentPath + updateFolder + entry.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Properties.strings.BackEndUpdateManErrorExtracting, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                Process.Start(currentPath + updateFolder + "Extract Update.exe");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.strings.BackendUpdateManErrorT, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// [Internal Use] Updates the releases available to us
        /// </summary>
        private void GetReleases()
        {
            IReadOnlyList<Release> allRel = github.Repository.Release.GetAll("watsuprico", "SteamAuthenticator").Result; // Change this before release!
            List<Release> newAllRel = new List<Release>();
            Manifest manifest = Manifest.GetManifest();
            foreach (Release rel in allRel)
            {
                if ((manifest.AllowBetaUpdates && rel.Prerelease) || !rel.Prerelease) // Either only add stable builds or add beta (if allowed)
                    newAllRel.Add(rel);
            }
            releasesInternal = newAllRel;
        }

        /// <summary>
        /// [Internal Use] Updates latestInternal to the latest build
        /// </summary>
        private void GetLatest()
        {
            Manifest manifest = Manifest.GetManifest();
            foreach (Release rel in Releases)
            {
                Version ver = new Version(rel.TagName);
                if (ver > latestInternal && ((manifest.AllowBetaUpdates && rel.Prerelease) || !rel.Prerelease)) // Either gets the latest stable build or latest beta (if allowed)
                    latestInternal = ver;
            }

        }

        #region Public API

        #region Auto update
        /// <summary>
        /// Checks for an update and pushes a notification out if there is one available (great for checking on startup)
        /// </summary>
        /// <returns>If there is an update available</returns>
        public bool CheckForUpdate()
        {
            if (Latest > CurrentVersion)
            {
                Notifications.Show(Properties.strings.BackEndUpdateManNewUpdateT, String.Format(Properties.strings.BackEndUpdateManNewUpdate,Latest.ToString(), CurrentVersion.ToString()), NotificationIcon.Update, "action=update");
                return true;
            }
            return false;
        }
        /// <summary>
        /// Checks for an update
        /// </summary>
        /// <returns>If there is an update available</returns>
        public bool NewUpdate()
        {
            return Latest > CurrentVersion;
        }
        #endregion

        #region Download
        /// <summary>
        /// Download an update for Steam Authenticator using the release tagName
        /// </summary>
        /// <param name="tagName">The tagName of the release wanting to be downloaded</param>
        /// <returns>Whether or not the download finished</returns>
        public bool DownloadUpdate(string tagName)
        {
            Release downloadThisRelease = new Release();
            foreach (Release rel in Releases)
            {
                if (rel.TagName == tagName)
                {
                    downloadThisRelease = rel;
                    break;
                }
            }
            if (downloadThisRelease == new Release())
                return false;

            return InternalDownload(downloadThisRelease.Assets[0].BrowserDownloadUrl, downloadThisRelease.Body);
        }
        /// <summary>
        /// Download an update for Steam Authenticator using the release ID
        /// </summary>
        /// <param name="id">The release id wanting to be downloaded</param>
        /// <returns>Whether or not the download finished</returns>
        public bool DownloadUpdate(int id)
        {
            Release downloadThisRelease = new Release();
            foreach (Release rel in Releases)
            {
                if (rel.Id == id)
                {
                    downloadThisRelease = rel;
                    break;
                }
            }
            if (downloadThisRelease == new Release())
                return false;

            return InternalDownload(downloadThisRelease.Assets[0].BrowserDownloadUrl, downloadThisRelease.Body);
        }
        /// <summary>
        /// Download an update for Steam Authenticator using the 'browser_download_url'
        /// </summary>
        /// <param name="downloadURL"></param>
        /// <returns>Whether or not the download finished</returns>
        public bool DownloadUpdateURL(string downloadURL)
        {
            return InternalDownload(downloadURL);
        }
        #endregion

        #region Install
        /// <summary>
        /// Installs an update using the release tagName
        /// </summary>
        /// <param name="tagName">The version of the release wanting to be installed</param>
        public void InstallUpdate(string tagName)
        {
            if (!DownloadUpdate(tagName))
                return;
            MessageBoxResult res = MessageBox.Show(Properties.strings.BackEndUpdateManDownloaded, Properties.strings.BackEndUpdateManDownloadedT, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
                InternalInstall();
        }
        /// <summary>
        /// Installs an update using the release id
        /// </summary>
        /// <param name="id">The id of the release wanting to be installed</param>
        public void InstallUpdate(int id)
        {
            if (!DownloadUpdate(id))
                return;
            MessageBoxResult res = MessageBox.Show(Properties.strings.BackEndUpdateManDownloaded, Properties.strings.BackEndUpdateManDownloadedT, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
                InternalInstall();
        }
        /// <summary>
        /// Installs and update using the release URL
        /// </summary>
        /// <param name="URL">The URL of the file wanting to be used to install an update</param>
        public void InstallUpdateURL(string URL)
        {
            if (!DownloadUpdateURL(URL))
                return;
            MessageBoxResult res = MessageBox.Show(Properties.strings.BackEndUpdateManDownloaded, Properties.strings.BackEndUpdateManDownloadedT, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
                InternalInstall();
        }

        /// <summary>
        /// Installs the latest update
        /// </summary>
        public void UpdateLatest()
        {
            InstallUpdate(Latest.ToString());
        }
        #endregion
        #endregion
    }
}
