using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RawAccelModern
{
    internal sealed class UpdateInfo
    {
        public Version Version { get; set; }
        public string TagName { get; set; }
        public string ReleaseName { get; set; }
        public string ReleaseNotes { get; set; }
        public string ReleaseUrl { get; set; }
        public string PackageUrl { get; set; }
        public string ChecksumUrl { get; set; }
        public long PackageSize { get; set; }
    }

    internal sealed class UpdateDownload
    {
        public string PackagePath { get; set; }
        public string Sha256 { get; set; }
    }

    internal static class UpdateService
    {
        internal const string RepositoryUrl = "https://github.com/diskcell/Raw-Accel-Reimagined";
        internal const string PackageAssetName = "Raw-Accel-Reimagined-Windows-x64.zip";
        internal const string ChecksumAssetName = PackageAssetName + ".sha256";
        private const string LatestReleaseApi = "https://api.github.com/repos/diskcell/Raw-Accel-Reimagined/releases/latest";
        private static readonly HttpClient Client = CreateClient();

        internal static Version CurrentVersion
        {
            get
            {
                Version value = Assembly.GetExecutingAssembly().GetName().Version;
                return value == null ? new Version(0, 0, 0, 0) : value;
            }
        }

        internal static string CurrentVersionText
        {
            get { return FormatVersion(CurrentVersion); }
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Raw-Accel-Reimagined/" + CurrentVersionText);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return client;
        }

        internal static async Task<UpdateInfo> GetLatestReleaseAsync()
        {
            using (HttpResponseMessage response = await Client.GetAsync(LatestReleaseApi).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject release = JObject.Parse(json);
                if (release.Value<bool?>("draft") == true || release.Value<bool?>("prerelease") == true) return null;

                string tag = release.Value<string>("tag_name");
                Version version;
                if (!TryParseVersion(tag, out version))
                    throw new InvalidDataException("The latest GitHub release has an invalid version tag.");

                JArray assets = release["assets"] as JArray;
                JObject package = FindAsset(assets, PackageAssetName);
                JObject checksum = FindAsset(assets, ChecksumAssetName);
                if (package == null || checksum == null)
                    throw new InvalidDataException("The latest release does not contain the required update package and SHA-256 checksum.");

                return new UpdateInfo
                {
                    Version = version,
                    TagName = tag,
                    ReleaseName = release.Value<string>("name") ?? tag,
                    ReleaseNotes = release.Value<string>("body") ?? String.Empty,
                    ReleaseUrl = release.Value<string>("html_url") ?? RepositoryUrl + "/releases/latest",
                    PackageUrl = package.Value<string>("browser_download_url"),
                    ChecksumUrl = checksum.Value<string>("browser_download_url"),
                    PackageSize = package.Value<long?>("size") ?? 0
                };
            }
        }

        internal static async Task<UpdateDownload> DownloadAsync(UpdateInfo update, IProgress<int> progress)
        {
            if (update == null) throw new ArgumentNullException("update");
            string updateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RawAccelReimagined", "Updates", FormatVersion(update.Version));
            Directory.CreateDirectory(updateDirectory);
            string packagePath = Path.Combine(updateDirectory, PackageAssetName);
            string temporaryPath = packagePath + ".download";

            string checksumText = await Client.GetStringAsync(update.ChecksumUrl).ConfigureAwait(false);
            string expectedHash = ParseChecksum(checksumText);
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);

            using (HttpResponseMessage response = await Client.GetAsync(update.PackageUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                long total = response.Content.Headers.ContentLength ?? update.PackageSize;
                using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (FileStream target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    byte[] buffer = new byte[81920];
                    long received = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await target.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                        received += read;
                        if (progress != null && total > 0)
                            progress.Report((int)Math.Min(100, received * 100L / total));
                    }
                }
            }

            string actualHash = ComputeSha256(temporaryPath);
            if (!String.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temporaryPath);
                throw new InvalidDataException("The downloaded package failed SHA-256 verification.");
            }

            if (File.Exists(packagePath)) File.Delete(packagePath);
            File.Move(temporaryPath, packagePath);
            if (progress != null) progress.Report(100);
            return new UpdateDownload { PackagePath = packagePath, Sha256 = actualHash };
        }

        internal static string FormatVersion(Version version)
        {
            if (version == null) return "0.0.0";
            return String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", version.Major, version.Minor, Math.Max(0, version.Build));
        }

        private static JObject FindAsset(JArray assets, string name)
        {
            if (assets == null) return null;
            return assets.OfType<JObject>().FirstOrDefault(asset =>
                String.Equals(asset.Value<string>("name"), name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (String.IsNullOrWhiteSpace(text)) return false;
            string normalized = text.Trim().TrimStart('v', 'V');
            int suffix = normalized.IndexOfAny(new[] { '-', '+' });
            if (suffix >= 0) normalized = normalized.Substring(0, suffix);
            Version parsed;
            if (!Version.TryParse(normalized, out parsed)) return false;
            version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build), Math.Max(0, parsed.Revision));
            return true;
        }

        private static string ParseChecksum(string checksumText)
        {
            if (String.IsNullOrWhiteSpace(checksumText)) throw new InvalidDataException("The release checksum is empty.");
            string candidate = new string(checksumText.TakeWhile(character => !Char.IsWhiteSpace(character)).ToArray()).Trim();
            if (candidate.Length != 64 || candidate.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException("The release checksum is invalid.");
            return candidate.ToUpperInvariant();
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString().ToUpperInvariant();
            }
        }
    }
}
