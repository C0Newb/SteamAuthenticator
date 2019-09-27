<h1 align="center">
  <img  src="https://i.imgur.com/nXqMgeU.gif"/>
  <br/>
  Steam Authenticator (SA)
</h1>

<p align="center">
  A Windows implementation of Steam's mobile authenticator app.
</p>
<h3 align="center">
  <a href="https://github.com/watsuprico/SteamDesktopAuthenticator/releases/latest">Download here</a>
</h3>

<!-- Disclaimers-->
<p>
  <b>Remember: You should be sure to make backups of your <code>maFiles</code> directory. If you lose your <code>maFiles</code> directory and forgot to save your revocation code, removing the authenticator will be difficult.</b>
  <br/>
  <br/>
  <i>If you do manage to lose your <code>.maFile</code>'s, or are unable to view your authentication codes, go <a href="https://store.steampowered.com/twofactor/manage">here</a> to remove the authenticator.</i>
</p>

<p>
  Table of contents:
  <br/>
  <ul>
    <li>
      <a href="#setup">Setting up SA</a>
    </li>
    <li>
      <a href="#build">Compiling SA</a>
    </li>
    <li>
      <a href="#features">Features</a>
    </li>
    <li>
      <a href="#startupargs">Command line arguments</a>
    </li>
    <li>
      <a href="#encryption">Encryption</a>
    </li>
    <li>
      <a href="#troubleshooting">(quick) Troubleshooting</a>
    </li>
    <li>
      <a href="#inspiration">Inspiration</a>
    </li>
  </ul>
</p>
<br/><br/>
<a id="setup"/>
<h2 align="center">
  Setup
</h2>
<p>
  - If you're running Windows 7 or newer, <a href="http://go.microsoft.com/fwlink/?LinkId=397707">download</a> and install .NET Framework 4.5.2. <i>If you're running Windows 8 or new, .NET 4.5.2 should already be installed</i>
  <br/>
  - Next, <a href="https://github.com/watsuprico/SteamAuthenticator/releases/latest">download</a> the latest version of Steam Authenticator from the release tab.
  <br/>
  - Once the zip file (<code>SA_2_X_X_X.zip</code>) is download, go ahead and extract somewhere safe. Anywhere should work as the application is <i>portable</i>. Just be aware that it will create a new folder called <code>maFiles</code>to store the manifest and your account details in.
  <br/>
  - Run <code>Steam Authenticator.exe</code> and add an account.
  <br/>
  - You can either add an account by logging in or by importing an account.
  <br/>
  <img src="https://i.imgur.com/tR3RWRi.png" height="150"/>
  <br/>
  - <i>If you opt to login, remember that Steam will require you add a phone number capable of receiving SMS messages. Note: Google Voice is no longer a valid option.</i>
  <br/>
  - Follow the instructions, provided in the program, to add your account.
  <br/>
  After you add your account, be sure to save a copy of your <a href="https://store.steampowered.com/twofactor/manage">Steam Guard backup codes</a>.
</p>



<a id="build"/>
<h2 align="center">
  Compiling Steam Authenticator
</h2>
<p>
  Compiling Steam Authenticator is as easy as downloading an IDE, cloning the source code, opening the project.
  <br/>
  <ol>
    <li>Start by downloading <a href="https://visualstudio.microsoft.com/downloads/">Visual Studio</a> if you do not have it already.</li>
    <li>Next, click the "Clone or download" button above to either clone the project or download a ZIP file of the source code.</li>
    <li><i>If you downloaded the ZIP file, be sure to unzip it somewhere safe.</i></li>
    <li><i>Be sure to copy the contents of <a href="https://github.com/watsuprico/SteamAuth/archive/master.zip">SteamAuth</a>, (and, if you wish, <a href="https://github.com/watsuprico/SteamAuthenticatorUpdater/archive/master.zip">Steam Authenticator Updater</a>) into <code>\lib\*</code> (Example: <code>C:\Users\%username%\Desktop\SA-Master\lib\SteamAuth</code>)</i></li>
    <li>Open <code>\Steam Authenticator.sln</code> in Visual Studio</li>
    <li>Click 'Run' or press <code>F5</code> to build and run SA</li>  
  </ol>
</p>

<br/><br/><br/>


<a id="features"/>
<h1 align="center">
  Features
</h1>

<ul>
  <li>
    <b>Auto entry</b>
    - Automatically have your authentication code entered into Steam! <a target="_blank" href="http://www.codynewberry.com/SteamAuthenticator.html#features11-10">Learn more.</a>
  </li>
  <li>
    <b>Security</b>
    - Multiple ways of protecting your account details. Read more about this <a href="#encryption">below</a>.
  </li>
  <li>
    <b>Simple</b>
    - Navigate and get things done quickly and easily.
  </li>
  <li>
    <b>One-Click Updating</b>
    - The included 'update manager' allows you to download and 'update' to any release of SA without leaving the application.
  </li>
  <li>
    <b>Auto confirm trade/market confirmations</b>
    - You can either opt-in to being automatically notified when new requests come in, or have them automatically accepted.
  </li>
  <li>
    <b>Simple confirmations viewer</b>
    - Rather than including an entire browser module in SA, we opted to scrape the confirmation info off the page and display it to you. This also allows you to quickly switch which account you're viewing.
  </li>
</ul>

<br/><br/><br/>


<a id="startupargs"/>
<h1 align="center">
  Command line arguments
</h1>

<code>-t, -trades, -viewtrades &lt;accountName&gt;</code>
<br/>
  Open the trades window and focus on &lt;accountName&gt; (view the trades for &lt;accountName&gt;)
<br/>

<code>-RefreshSession, -rs &lt;accountName&gt;</code>
<br/>
  Refresh &lt;accountName&gt;'s session
<br/>

<code>-RefreshLogin, -relogin, -rl &lt;accountName&gt;</code>
<br/>
  Open the 'login again' window for &lt;accountName&gt;
<br/>

<code>-RemoveAccount, -rm &lt;accountName&gt;</code>
<br/>
  Remove &lt;accountName&gt; from the manifest (only if 'ArgAllowRemove' is true)
<br/>

<code>-copycode, -cc &lt;accountName&gt;</code>
<br/>
  If allowed via the manifest, use this to copy the current authcode for &lt;accountName&gt; into the clipboard. MUST ENABLE 'ArgAllowAuthCopying'
<br/>

<code>-exit, -e</code>
<br/>
  Exit Steam Authenticator after processing all arguments
<br/>

<code>-q</code>
<br/>
  Quiet startup


<br/><br/><br/>


<a id="encryption"/>
<h1 align="center">
  Encryption
</h1>
<br/>
SA offers four different ways to secure your account details.
<br/>
<ul>
  <li>
    <b>Passkey</b>
    - Plain and simple. Like many other solutions, we allow you to set your own passkey to encrypt your account details.
  </li>
  <li>
    <b>Windows DPAPI</b>
    - Using DPAPI allows for encryption of the manifest and maFiles to be offloaded to Windows. Using DPAPI will encrypt and decrypt your account details automatically using your Windows account. <i>Only your Windows account can decrypt the contents</i>.
  </li>
  <li>
    <b>Windows File Encryption</b>
    - This option encrypts your manifest and maFiles on the filesystem level. Doing so makes it very difficult to decrypt the saved data on the disk. Note: Windows automatically decrypts the data to whoever opens the file, meaning this is only useful for protecting against your storage drive being stolen.
  </li>
  <li>
    <b>Windows Credentials Manager</b>
    - With Windows Credential Manager you can save your maFiles and your passkey encrypted away with Windows. Saving your passkey in the cred. manager allows for the key to automatically be entered.
  </li>
</ul>

<br/><br/><br/>


<a id="troubleshooting"/>
<h1 align="center">
  Troubleshooting
</h1>
<p>Very limited as of now, but this will be filled later on.</p>
<p><b>My confirmations aren't appearing!</b><br/>Try to 'refresh' your session or 'login again' from the main window.</p>


<br/><br/><br/>


<a id="inspiration"/>
<h2 align="center">
  Inspiration
</h2>
<p>
  Steam Authenticator was inspired by <a href="https://github.com/Jessecar96/">Jessecar96's</a> <a href="https://github.com/Jessecar96/SteamDesktopAuthenticator">Steam Desktop Authenticator</a>, the original 'Steam desktop authentication app'.
  <br/>
  While I have forked SDA, I decided to redo everything with SA. I built SA using WPF as opposed to WinForms because WPF had a 'nicer' design. This would allow me to construct a beautiful authentication app, something that pushed me to fork SDA in the first place.
  <br/>
  Regardless, SDA was the first on the scene to provide a desktop variant of Steam's mobile authentication app.
</p>

