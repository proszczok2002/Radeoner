using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Radeoner
{
    internal static class Diagnostics
    {
        private static readonly object gate = new object();
        private static readonly Dictionary<string, DateTime> recent = new Dictionary<string, DateTime>();
        internal static string LogPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.log"); } }
        internal static void Log(string context, Exception error)
        {
            lock (gate) try
            {
                string signature = context + " " + error.GetType().FullName + " " + error.Message; DateTime previous;
                if (recent.TryGetValue(signature, out previous) && (DateTime.UtcNow - previous).TotalSeconds < 30) return;
                if (recent.Count > 128) recent.Clear(); recent[signature] = DateTime.UtcNow;
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 256 * 1024) File.WriteAllText(LogPath, "", Encoding.UTF8);
                File.AppendAllText(LogPath, DateTime.UtcNow.ToString("o") + " v" + L.Version + " " + context + ": " + error + "\r\n", Encoding.UTF8);
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
