using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Radeoner
{
    public static class L
    {
        public const string Version = BuildInfo.DisplayVersion;
        private static Dictionary<string, string> messages;
        public static string Language { get; private set; }
        public static CultureInfo Culture { get; private set; }
        static L() { Load("pl"); }
        public static Dictionary<string, string> Parse(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    int split = line.IndexOf('=');
                    if (split < 1) throw new FormatException("Invalid LNG entry: " + line);
                    string key = line.Substring(0, split).Trim();
                    if (result.ContainsKey(key)) throw new FormatException("Duplicate LNG key: " + key);
                    result.Add(key, line.Substring(split + 1).Replace("\\n", "\r\n"));
                }
            }
            return result;
        }
        private static Dictionary<string, string> BuiltIn(string language)
        {
            using (var stream = typeof(L).Assembly.GetManifestResourceStream("Radeoner.Languages." + language + ".lng"))
            {
                if (stream == null) throw new InvalidOperationException("Missing built-in language: " + language);
                using (var reader = new StreamReader(stream, Encoding.UTF8)) return Parse(reader.ReadToEnd());
            }
        }
        public static void Load(string language) { Load(language, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang")); }
        internal static void Load(string language, string directory)
        {
            if (language != "pl" && language != "en") language = "pl";
            var fallback = BuiltIn("pl");
            foreach (var pair in BuiltIn(language)) fallback[pair.Key] = pair.Value;
            string file = Path.Combine(directory, language + ".lng");
            try
            {
                if (File.Exists(file) && new FileInfo(file).Length <= 1024 * 1024)
                    foreach (var pair in Parse(File.ReadAllText(file, Encoding.UTF8))) fallback[pair.Key] = pair.Value;
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { } catch (FormatException) { }
            // Publish a complete immutable dictionary so a background read never sees half a language.
            messages = fallback; Language = language;
            Culture = CultureInfo.GetCultureInfo(language == "en" ? "en-US" : "pl-PL");
            CultureInfo.DefaultThreadCurrentCulture = Culture; CultureInfo.DefaultThreadCurrentUICulture = Culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = Culture;
        }
        public static string T(string key, params object[] args)
        {
            string value;
            if (!messages.TryGetValue(key, out value)) return "[" + key + "]";
            return args.Length == 0 ? value : string.Format(Culture, value, args);
        }
    }
}
