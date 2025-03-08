using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.IO;

namespace CloudyApi
{
    public static class Api
    {
        private static System.Windows.Forms.Timer time12 = new System.Windows.Forms.Timer();
        private static bool isua = false;
        private static bool _autoInject;

        [DllImport("bin\\Cloudy.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Initialize();

        [DllImport("bin\\Cloudy.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Execute(byte[] scriptSource, string[] clientUsers, int numUsers);

        [DllImport("bin\\Cloudy.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetClients();

        static Api()
        {
            AutoSetup();
        }

        public static void AutoInject(bool enable)
        {
            Api.Initialize();
            Api._autoInject = enable;
            if (!enable)
                return;
            Api.inject();
        }

        public static bool IsAutoInjectEnabled() => Api._autoInject;

        public static void inject()
        {
            Api.Initialize();
            System.Threading.Thread.Sleep(1000);
            string s = "\tgame:GetService(\"StarterGui\"):SetCore(\"SendNotification\", {\r\n\t\tTitle = \"[Cloudy API]\",\r\n\t\tText = \"Injected!\"\r\n\t})";
            string[] array = Api.GetClientsList().Select(c => c.name).ToArray();
            Api.Execute(Encoding.UTF8.GetBytes(s), array, array.Length);
        }

        public static void execute(string scriptSource)
        {
            string[] array = Api.GetClientsList().Select(c => c.name).ToArray();
            Api.Execute(Encoding.UTF8.GetBytes(scriptSource), array, array.Length);
        }

        public static List<Api.ClientInfo> GetClientsList()
        {
            List<Api.ClientInfo> clientsList = new List<Api.ClientInfo>();
            IntPtr clients = Api.GetClients();
            while (true)
            {
                Api.ClientInfo structure = Marshal.PtrToStructure<Api.ClientInfo>(clients);
                if (structure.name != null)
                {
                    clientsList.Add(structure);
                    clients += Marshal.SizeOf<Api.ClientInfo>();
                }
                else
                    break;
            }
            return clientsList;
        }

        public static bool IsInjected()
        {
            try
            {
                return Api.GetClientsList().Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public static void killRoblox()
        {
            foreach (Process process in Process.GetProcessesByName("RobloxPlayerBeta"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to kill process: " + ex.Message);
                }
            }
        }

        public struct ClientInfo
        {
            public string version;
            public string name;
            public int id;
        }

        public static string GetUsername()
        {
            string userName = Environment.UserName;
            string fp = $@"C:\\Users\\{userName}\\AppData\\Local\\Roblox\\LocalStorage\\appStorage.json";

            if (!File.Exists(fp))
                return null;

            try
            {
                string jsc = File.ReadAllText(fp);
                JObject jsd = JObject.Parse(jsc);
                if (jsd.ContainsKey("Username"))
                    return jsd["Username"]?.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting username {ex.Message}", "CloudyApi");
            }
            return null;
        }

        private static void AutoSetup()
        {
            string[] dlls = { "Cloudy.dll", "libcrypto-3-x64.dll", "libssl-3-x64.dll", "xxhash.dll", "zstd.dll" };
            string binpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");

            if (!Directory.Exists(binpath))
                Directory.CreateDirectory(binpath);

            foreach (var dll in dlls)
            {
                string dllPath = Path.Combine(binpath, dll);
                if (!File.Exists(dllPath))
                {
                    try
                    {
                        string dllurl = $"https://github.com/cloudyExecutor/webb/releases/download/dlls/{dll}";
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(dllurl, dllPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to Download {dll}: {ex.Message}", "CloudyApi");
                    }
                }
            }
        }

        public static BitmapImage GetAvatar(string username)
        {
            string userId = GetUserIdFromUsername(username);
            if (string.IsNullOrEmpty(userId))
                return null;

            string avatarUrl = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=420x420&format=png";

            try
            {
                using (var client = new WebClient())
                {
                    string response = client.DownloadString(avatarUrl);
                    JObject data = JObject.Parse(response);
                    if (data["data"] != null && data["data"].HasValues)
                    {
                        string imageUrl = data["data"][0]["imageUrl"].ToString();
                        BitmapImage bitmapImage = new BitmapImage(new Uri(imageUrl));
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting avatar for {username}: {ex.Message}", "CloudyApi");
            }

            return null;
        }

        private static string GetUserIdFromUsername(string username)
        {
            string url = "https://users.roblox.com/v1/usernames/users";
            string json = $"{{\"usernames\": [\"{username}\"]}}";

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string response = client.UploadString(url, "POST", json);
                    JObject data = JObject.Parse(response);

                    if (data["data"] != null && data["data"].HasValues)
                    {
                        string userId = data["data"][0]["id"].ToString();
                        return userId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting user ID for {username}: {ex.Message}", "CloudyApi");
            }

            return null;
        }
    }
}
