using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using Microsoft.Win32;

namespace Radeoner
{
    internal static class JsonFile
    {
        internal static T Read<T>(string path, int maximumBytes)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length == 0 || input.Length > maximumBytes) throw new InvalidOperationException(L.T("file.invalidSize"));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(input, Encoding.UTF8, new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxArrayLength = 4096, MaxStringContentLength = 65536 }, null))
                {
                    var result = (T)new DataContractJsonSerializer(typeof(T)).ReadObject(reader);
                    while (reader.Read()) if (reader.NodeType != XmlNodeType.Whitespace) throw new SerializationException("Unexpected data after JSON object.");
                    return result;
                }
            }
        }
        internal static void Write<T>(string path, T value)
        {
            string fullPath = Path.GetFullPath(path);
            string temp = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                { new DataContractJsonSerializer(typeof(T)).WriteObject(writer, value); writer.Flush(); stream.Flush(true); }
                if (File.Exists(fullPath)) File.Replace(temp, fullPath, null); else File.Move(temp, fullPath);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember(Order = 0)] public int Version = 1;
        [DataMember(Order = 1)] public string Language = "pl";
        [DataMember(Order = 2)] public string Theme = "system";
        [DataMember(Order = 3)] public int TextScale = 100;
        [DataMember(Order = 4)] public bool RefreshSensors = true;
        [DataMember(Order = 5)] public bool CheckUpdates = true;
        [DataMember(Order = 6)] public bool CheckUpdatesOnStartup = true;
        [DataMember(Order = 7)] public int UpdateIntervalMinutes = 60;
        [OnDeserializing]
        private void Defaults(StreamingContext context)
        { RefreshSensors = true; CheckUpdates = true; CheckUpdatesOnStartup = true; UpdateIntervalMinutes = 60; }
        internal AppSettings Copy() { return (AppSettings)MemberwiseClone(); }
        public static string DefaultPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.settings.json"); } }
        public static AppSettings Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new AppSettings();
                var s = JsonFile.Read<AppSettings>(path, 65536);
                if (s == null || s.Version != 1) return new AppSettings();
                if (s.Language != "pl" && s.Language != "en") s.Language = "pl";
                if (s.Theme != "light" && s.Theme != "dark" && s.Theme != "system") s.Theme = "system";
                if (s.TextScale < 100 || s.TextScale > 200 || s.TextScale % 25 != 0) s.TextScale = 100;
                if (s.UpdateIntervalMinutes < 5 || s.UpdateIntervalMinutes > 10080) s.UpdateIntervalMinutes = 60;
                return s;
            }
            catch (Exception)
            { return new AppSettings(); }
        }
        public void Save(string path) { JsonFile.Write(path, this); }
    }
    public sealed class StartupRegistration
    {
        private readonly string keyPath, valueName, executable;
        public StartupRegistration(string executablePath) : this(executablePath, @"Software\Microsoft\Windows\CurrentVersion\Run", "Radeoner") { }
        internal StartupRegistration(string executablePath, string registryPath, string name)
        { executable = Path.GetFullPath(executablePath); keyPath = registryPath; valueName = name; }
        public string Command { get { return "\"" + executable + "\" --tray"; } }
        public bool Enabled
        {
            get { using (var key = Registry.CurrentUser.OpenSubKey(keyPath)) return key != null && key.GetValue(valueName) is string; }
        }
        public bool UsesCurrentLocation
        {
            get { using (var key = Registry.CurrentUser.OpenSubKey(keyPath)) return key != null && string.Equals(key.GetValue(valueName) as string, Command, StringComparison.OrdinalIgnoreCase); }
        }
        public void SetEnabled(bool enabled)
        {
            if (enabled && (Command.Length > 260 || executable.IndexOf('"') >= 0)) throw new InvalidOperationException(L.T("startup.pathLong"));
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            { if (enabled) key.SetValue(valueName, Command, RegistryValueKind.String); else key.DeleteValue(valueName, false); }
        }
    }
}
