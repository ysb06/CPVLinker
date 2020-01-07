using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using CPVLinker.Tools.Ini;

namespace CPVLinker.Tools
{
    public class SettingManager
    {
        public const string Setting_File_Name = "Settings.ini";

        public static string Path 
        {
            get
            {
                if(isInitialized == false) { Load(); }
                return settings["General"]["Path"].Value;
            }
            set
            {
                if (isInitialized == false) { Load(); }
                settings["General"]["Path"] = value;
            }
        }
        public static string ClientID
        {
            get
            {
                if (isInitialized == false) { Load(); }
                return settings["General"]["ClientID"].Value;
            }
            set
            {
                if (isInitialized == false) { Load(); }
                settings["General"]["ClientID"] = value;
            }
        }
        public static string ClientSecret
        {
            get
            {
                if (isInitialized == false) { Load(); }
                return settings["General"]["ClientSecret"].Value;
            }
            set
            {
                if (isInitialized == false) { Load(); }
                settings["General"]["ClientSecret"] = value;
            }
        }
        public static bool IsEncrypted
        {
            get
            {
                if (isInitialized == false) { Load(); }
                return settings["General"]["IsEncrypted"].ToBool();
            }
            set
            {
                if (isInitialized == false) { Load(); }
                settings["General"]["IsEncrypted"] = value;
            }
        }

        private static readonly IniFile settings = new IniFile();
        private static bool isInitialized = false;

        public static void Load()
        {
            if(File.Exists(Directory.GetCurrentDirectory() + "\\" + Setting_File_Name))
            {
                settings.Load(Directory.GetCurrentDirectory() + "\\" + Setting_File_Name);
            }

            if (settings.TryGetSection("General", out IniSection generalSection) == false)
            {
                generalSection = settings.Add("General");
            }

            if (generalSection.TryGetValue("Path", out IniValue path) == false)
            {
                generalSection.Add("Path", "Voices\\");
            }

            if (generalSection.TryGetValue("ClientID", out IniValue clientID) == false)
            {
                generalSection.Add("ClientID", "NaN");
            }

            if (generalSection.TryGetValue("ClientSecret", out IniValue clientSecret) == false)
            {
                generalSection.Add("ClientSecret", "NaN");
            }

            if (generalSection.TryGetValue("IsEncrypted", out IniValue isEncrypted) == false)
            {
                generalSection.Add("IsEncrypted", false);
            }

            isInitialized = true;
        }

        public static void Save()
        {
            settings.Save(Directory.GetCurrentDirectory() + "\\" + Setting_File_Name);

            string firstMacAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            Console.WriteLine(firstMacAddress);
        }

        //Todo: Encrypt, Decrypt 속성 수정하기(암호화 메서드 추가)
    }
}
