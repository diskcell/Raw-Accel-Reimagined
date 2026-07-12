using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace RawAccelUpdater
{
    internal static class Program
    {
        private static readonly HashSet<string> ProtectedTopLevelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "settings.json", ".config", ".reimagined.config", "backups", ".git"
        };

        [STAThread]
        private static int Main(string[] args)
        {
            string logPath = CreateLogPath();
            string target = null;
            string launch = "RawAccelReimagined.exe";
            bool noLaunch = false;
            try
            {
                Dictionary<string, string> options = ParseArguments(args);
                string package = Required(options, "package");
                target = Path.GetFullPath(Required(options, "target"));
                launch = options.ContainsKey("launch") ? options["launch"] : "RawAccelReimagined.exe";
                string version = options.ContainsKey("version") ? options["version"] : String.Empty;
                int processId = ParseProcessId(options);
                noLaunch = options.ContainsKey("no-launch");

                Log(logPath, "Starting update to " + version + ".");
                WaitForApplication(processId, logPath);
                InstallPackage(Path.GetFullPath(package), target, logPath);
                Log(logPath, "Update installed successfully.");

                if (!noLaunch)
                {
                    string executable = SafeCombine(target, launch);
                    Process.Start(new ProcessStartInfo(executable, "--updated=" + Quote(version))
                    {
                        WorkingDirectory = target,
                        UseShellExecute = true
                    });
                }
                TryDelete(package);
                return 0;
            }
            catch (Exception ex)
            {
                Log(logPath, "ERROR: " + ex);
                ShowError(logPath, ex.Message);
                if (!noLaunch && !String.IsNullOrEmpty(target)) TryRelaunch(target, launch, logPath);
                return 1;
            }
        }

        private static void TryRelaunch(string target, string launch, string logPath)
        {
            try
            {
                string executable = SafeCombine(target, launch);
                if (!File.Exists(executable)) return;
                Process.Start(new ProcessStartInfo(executable, "--update-failed")
                {
                    WorkingDirectory = target,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log(logPath, "Could not reopen the application after the update failure: " + ex.Message);
            }
        }

        private static void InstallPackage(string package, string target, string logPath)
        {
            if (!File.Exists(package)) throw new FileNotFoundException("The update package was not found.", package);
            if (!Directory.Exists(target)) throw new DirectoryNotFoundException("The application folder was not found: " + target);

            string session = Path.Combine(Path.GetTempPath(), "RawAccelReimagined-Update-" + Guid.NewGuid().ToString("N"));
            string staging = Path.Combine(session, "staging");
            string backup = Path.Combine(session, "backup");
            Directory.CreateDirectory(staging);
            Directory.CreateDirectory(backup);
            try
            {
                ExtractSafely(package, staging);
                string packageRoot = FindPackageRoot(staging);
                ApplyFiles(packageRoot, target, backup, logPath);
            }
            finally
            {
                TryDeleteDirectory(session);
            }
        }

        private static void ExtractSafely(string package, string destination)
        {
            string root = EnsureTrailingSeparator(Path.GetFullPath(destination));
            using (ZipArchive archive = ZipFile.OpenRead(package))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string path = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The update package contains an unsafe path.");
                    if (String.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(path);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    entry.ExtractToFile(path, true);
                }
            }
        }

        private static string FindPackageRoot(string staging)
        {
            string direct = Path.Combine(staging, "RawAccelReimagined.exe");
            if (File.Exists(direct)) return staging;
            string[] candidates = Directory.GetFiles(staging, "RawAccelReimagined.exe", SearchOption.AllDirectories);
            if (candidates.Length != 1)
                throw new InvalidDataException("The update package does not contain one valid RawAccelReimagined.exe.");
            return Path.GetDirectoryName(candidates[0]);
        }

        private static void ApplyFiles(string sourceRoot, string targetRoot, string backupRoot, string logPath)
        {
            List<string> installed = new List<string>();
            List<string> backedUp = new List<string>();
            string targetPrefix = EnsureTrailingSeparator(Path.GetFullPath(targetRoot));
            try
            {
                foreach (string source in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
                {
                    string relative = MakeRelativePath(sourceRoot, source);
                    string topLevel = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                    if (ProtectedTopLevelNames.Contains(topLevel)) continue;

                    string destination = Path.GetFullPath(Path.Combine(targetRoot, relative));
                    if (!destination.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The update attempted to write outside the application folder.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));

                    if (File.Exists(destination))
                    {
                        string backupPath = Path.Combine(backupRoot, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                        File.Copy(destination, backupPath, true);
                        backedUp.Add(relative);
                    }
                    File.Copy(source, destination, true);
                    installed.Add(relative);
                }
                if (!File.Exists(Path.Combine(targetRoot, "RawAccelReimagined.exe")))
                    throw new InvalidDataException("The updated application executable is missing.");
            }
            catch
            {
                Log(logPath, "Installation failed. Restoring the previous files.");
                foreach (string relative in installed.Where(relative => !backedUp.Contains(relative, StringComparer.OrdinalIgnoreCase)).Reverse<string>())
                    TryDelete(Path.Combine(targetRoot, relative));
                foreach (string relative in backedUp.AsEnumerable().Reverse())
                {
                    string source = Path.Combine(backupRoot, relative);
                    string destination = Path.Combine(targetRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, true);
                }
                throw;
            }
        }

        private static void WaitForApplication(int processId, string logPath)
        {
            if (processId <= 0) return;
            try
            {
                Process process = Process.GetProcessById(processId);
                Log(logPath, "Waiting for application process " + processId.ToString(CultureInfo.InvariantCulture) + ".");
                if (!process.WaitForExit(30000))
                    throw new TimeoutException("The application did not close in time. The update was not installed.");
                Thread.Sleep(400);
            }
            catch (ArgumentException)
            {
                // The application already exited.
            }
        }

        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index++)
            {
                string key = args[index];
                if (!key.StartsWith("--", StringComparison.Ordinal)) continue;
                key = key.Substring(2);
                if (String.Equals(key, "no-launch", StringComparison.OrdinalIgnoreCase))
                {
                    values[key] = "true";
                    continue;
                }
                if (index + 1 >= args.Length) throw new ArgumentException("Missing value for --" + key + ".");
                values[key] = args[++index];
            }
            return values;
        }

        private static string Required(Dictionary<string, string> options, string name)
        {
            string value;
            if (!options.TryGetValue(name, out value) || String.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Missing required --" + name + " argument.");
            return value;
        }

        private static int ParseProcessId(Dictionary<string, string> options)
        {
            string value;
            int processId;
            return options.TryGetValue("pid", out value) && Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out processId)
                ? processId : 0;
        }

        private static string MakeRelativePath(string root, string path)
        {
            Uri rootUri = new Uri(EnsureTrailingSeparator(Path.GetFullPath(root)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string SafeCombine(string root, string relative)
        {
            string prefix = EnsureTrailingSeparator(Path.GetFullPath(root));
            string result = Path.GetFullPath(Path.Combine(root, relative));
            if (!result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe launch path.");
            return result;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path : path + Path.DirectorySeparatorChar;
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? String.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static string CreateLogPath()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RawAccelReimagined", "Logs");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "update.log");
        }

        private static void Log(string path, string message)
        {
            try { File.AppendAllText(path, DateTime.Now.ToString("u", CultureInfo.InvariantCulture) + " " + message + Environment.NewLine); }
            catch { }
        }

        private static void ShowError(string logPath, string message)
        {
            try
            {
                System.Windows.Forms.MessageBox.Show(
                    "Raw Accel Reimagined could not be updated.\n\n" + message + "\n\nLog: " + logPath,
                    "Raw Accel Reimagined Updater", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }
    }
}
