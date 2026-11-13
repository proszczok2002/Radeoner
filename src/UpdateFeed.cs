using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Radeoner
{
    [DataContract]
    public sealed class UpdateFile
    {
        [DataMember(IsRequired = true)] public string Path;
        [DataMember(IsRequired = true)] public string Sha256;
        [DataMember(IsRequired = true)] public long Size;
    }
    [DataContract]
    public sealed class UpdateManifest
    {
        [DataMember(IsRequired = true)] public string Format;
        [DataMember(IsRequired = true)] public int Schema;
        [DataMember(IsRequired = true)] public string Version;
        [DataMember(IsRequired = true)] public string DisplayVersion;
        [DataMember(IsRequired = true)] public string Asset;
        [DataMember(IsRequired = true)] public string Sha256;
        [DataMember(IsRequired = true)] public long Size;
        [DataMember(IsRequired = true)] public List<UpdateFile> Files;
    }
    [DataContract]
    internal sealed class GithubRelease
    {
        [DataMember(Name = "tag_name")] internal string Tag;
        [DataMember(Name = "draft")] internal bool Draft;
        [DataMember(Name = "prerelease")] internal bool Prerelease;
        [DataMember(Name = "html_url")] internal string Url;
        [DataMember(Name = "assets")] internal List<GithubAsset> Assets;
    }
    [DataContract]
    internal sealed class GithubAsset
    {
        [DataMember(Name = "name")] internal string Name;
        [DataMember(Name = "browser_download_url")] internal string Url;
        [DataMember(Name = "size")] internal long Size;
    }
    public sealed class AvailableUpdate
    {
        public UpdateManifest Manifest;
        public string DownloadUrl, ReleaseUrl, Tag;
    }
    public interface IUpdateTransport : IDisposable
    {
        Task<byte[]> GetAsync(Uri uri, int maximumBytes, CancellationToken cancellation);
    }
    public sealed class GithubTransport : IUpdateTransport
    {
        private readonly HttpClient client;
        public GithubTransport()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            client.Timeout = TimeSpan.FromMinutes(3);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Radeoner/" + BuildInfo.DisplayVersion);
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        }
        internal static bool TrustedDownload(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
                (uri.Host == "api.github.com" || uri.Host == "github.com" || uri.Host == "release-assets.githubusercontent.com" || uri.Host == "objects.githubusercontent.com");
        }
        public async Task<byte[]> GetAsync(Uri uri, int maximumBytes, CancellationToken cancellation)
        {
            for (int redirect = 0; redirect < 6; redirect++)
            {
                if (!TrustedDownload(uri)) throw new InvalidOperationException(L.T("update.untrustedUrl"));
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                {
                    int status = (int)response.StatusCode;
                    if (status >= 300 && status < 400 && response.Headers.Location != null)
                    { uri = new Uri(uri, response.Headers.Location); continue; }
                    if (status == 404) throw new InvalidOperationException(L.T("update.noRelease"));
                    if (status == 403 || status == 429) throw new InvalidOperationException(L.T("update.rateLimit"));
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > maximumBytes) throw new InvalidOperationException(L.T("update.tooLarge"));
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new MemoryStream())
                    {
                        byte[] buffer = new byte[81920]; int count;
                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) != 0)
                        {
                            if (output.Length + count > maximumBytes) throw new InvalidOperationException(L.T("update.tooLarge"));
                            output.Write(buffer, 0, count);
                        }
                        return output.ToArray();
                    }
                }
            }
            throw new InvalidOperationException(L.T("update.redirectLimit"));
        }
        public void Dispose() { client.Dispose(); }
    }
    public sealed class UpdateFeed
    {
        public const string Repository = "proszczok2002/Radeoner";
        public const string LatestUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
        public const int MaximumArchiveBytes = 32 * 1024 * 1024;
        internal static readonly string[] PackagePaths = { "Radeoner.exe", "Radeoner.exe.config", "Czytaj.txt", "Readme.en.txt", "LICENSE", "THIRD_PARTY_NOTICES.md", "lang/pl.lng", "lang/en.lng" };
        private readonly IUpdateTransport transport;
        public UpdateFeed(IUpdateTransport network) { transport = network; }
        internal static T Parse<T>(byte[] bytes)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, new XmlDictionaryReaderQuotas { MaxDepth = 24, MaxArrayLength = 1024, MaxStringContentLength = 65536 }))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(reader);
        }
        internal static bool ValidHash(string value) { return value != null && Regex.IsMatch(value, "\\A[0-9a-fA-F]{64}\\z"); }
        public static string Hash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(stream)); }
        internal static string Hash(byte[] data) { using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(data)); }
        private static string Hex(byte[] data) { return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant(); }
        internal static void ValidateManifest(UpdateManifest manifest)
        {
            Version version;
            if (manifest == null || manifest.Format != "Radeoner.Update" || manifest.Schema != 1 || !System.Version.TryParse(manifest.Version, out version) || version.Build < 0 || version.Revision < 0 ||
                string.IsNullOrWhiteSpace(manifest.DisplayVersion) || !Regex.IsMatch(manifest.DisplayVersion, @"\A[0-9]+(?:\.[0-9]+){1,3}\z") || manifest.DisplayVersion.Length > 30 ||
                manifest.Asset != "Radeoner-" + manifest.DisplayVersion + "-portable.zip" || !ValidHash(manifest.Sha256) || manifest.Size <= 0 || manifest.Size > MaximumArchiveBytes || manifest.Files == null || manifest.Files.Count != PackagePaths.Length)
                throw new InvalidOperationException(L.T("update.badManifest"));
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in manifest.Files)
            {
                if (file == null || !PackagePaths.Contains(file.Path, StringComparer.Ordinal) || !paths.Add(file.Path) || !ValidHash(file.Sha256) || file.Size <= 0 || file.Size > 16 * 1024 * 1024)
                    throw new InvalidOperationException(L.T("update.badManifest"));
            }
        }
        internal static bool IsReleaseAsset(string value, string tag, string asset)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme == "https" && uri.Host == "github.com" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
                uri.AbsolutePath == "/" + Repository + "/releases/download/" + Uri.EscapeDataString(tag) + "/" + Uri.EscapeDataString(asset);
        }
        public async Task<AvailableUpdate> CheckAsync(Version current, CancellationToken cancellation)
        {
            var release = Parse<GithubRelease>(await transport.GetAsync(new Uri(LatestUrl), 2 * 1024 * 1024, cancellation).ConfigureAwait(false));
            if (release == null || release.Draft || release.Prerelease || release.Assets == null || string.IsNullOrEmpty(release.Tag) || !Regex.IsMatch(release.Tag, @"\Av[0-9]+(?:\.[0-9]+){1,3}\z")) throw new InvalidOperationException(L.T("update.badRelease"));
            var metadata = release.Assets.Where(a => a.Name == "update.json").ToList();
            if (metadata.Count != 1 || !IsReleaseAsset(metadata[0].Url, release.Tag, "update.json")) throw new InvalidOperationException(L.T("update.badRelease"));
            var manifest = Parse<UpdateManifest>(await transport.GetAsync(new Uri(metadata[0].Url), 65536, cancellation).ConfigureAwait(false));
            ValidateManifest(manifest);
            if (release.Tag != "v" + manifest.DisplayVersion) throw new InvalidOperationException(L.T("update.badRelease"));
            var packages = release.Assets.Where(a => a.Name == manifest.Asset).ToList();
            if (packages.Count != 1 || packages[0].Size != manifest.Size || !IsReleaseAsset(packages[0].Url, release.Tag, manifest.Asset)) throw new InvalidOperationException(L.T("update.badRelease"));
            if (new Version(manifest.Version) <= current) return null;
            return new AvailableUpdate { Manifest = manifest, Tag = release.Tag, DownloadUrl = packages[0].Url, ReleaseUrl = "https://github.com/" + Repository + "/releases/tag/" + release.Tag };
        }
        internal async Task<byte[]> DownloadAsync(AvailableUpdate update, CancellationToken cancellation)
        {
            ValidateManifest(update.Manifest);
            if (!IsReleaseAsset(update.DownloadUrl, update.Tag, update.Manifest.Asset)) throw new InvalidOperationException(L.T("update.untrustedUrl"));
            byte[] bytes = await transport.GetAsync(new Uri(update.DownloadUrl), MaximumArchiveBytes, cancellation).ConfigureAwait(false);
            if (bytes.LongLength != update.Manifest.Size || !string.Equals(Hash(bytes), update.Manifest.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("update.hashMismatch"));
            return bytes;
        }
    }
}
