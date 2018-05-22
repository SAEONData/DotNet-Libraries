using System;
using System.IO;

namespace SAEON.Core
{
    public static class ApplicationHelper  
    {
        public static string ApplicationName { get; set; } = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
        public static string ApplicationVendor { get; } = "SAEON";

        public static string DataFolder
        {
            get { return (string)AppDomain.CurrentDomain.GetData("DataDirectory") ?? string.Empty; }
            set { AppDomain.CurrentDomain.SetData("DataDirectory", value); }
        }

        public static string DataSubFolder(string subFolder)
        {
            return Path.Combine(DataFolder, subFolder);
        }

        public static void EnsureDataFolder(string folder)
        {
            DataFolder = Path.Combine(folder, ApplicationVendor, ApplicationName);
            Directory.CreateDirectory(DataFolder);
        }
    }
}
