using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Radeoner;

internal static partial class Tests
{
    private static void RunLanguageTests()
    {
        Test("All six embedded languages preserve keys, format arguments, guides and preferences", () =>
        {
            var assembly = typeof(L).Assembly; Dictionary<string, string> baseline;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("Radeoner.Languages.en.lng"))) baseline = L.Parse(reader.ReadToEnd());
            foreach (string code in L.Languages)
            {
                Dictionary<string, string> pack;
                using (var reader = new StreamReader(assembly.GetManifestResourceStream("Radeoner.Languages." + code + ".lng"))) pack = L.Parse(reader.ReadToEnd());
                Check(pack.Keys.OrderBy(x => x).SequenceEqual(baseline.Keys.OrderBy(x => x)), "Incomplete language: " + code);
                foreach (string key in baseline.Keys)
                {
                    Check(Regex.Matches(pack[key], @"\{\d+\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x).SequenceEqual(Regex.Matches(baseline[key], @"\{\d+\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x)), "Placeholder mismatch: " + code + "/" + key);
                    string.Format(pack[key], Enumerable.Repeat<object>(1, 8).ToArray());
                }
                L.Load(code, "missing-dir"); Check(L.Language == code && L.Guide().Length > 1500, "Embedded language/guide missing: " + code);
                string path = TestFile("language-" + code + ".json"); new AppSettings { Language = code }.Save(path); Check(AppSettings.Load(path).Language == code, "Language preference lost: " + code);
            }
            L.Load("pl");
        });
    }
}
