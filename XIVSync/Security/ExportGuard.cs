using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dalamud.Plugin.Services;

namespace XIVSync.Security
{
    internal static class ExportGuard
    {
        private static readonly List<FileSystemWatcher> _watchers = new();
        private static readonly HashSet<string> _pending = new(StringComparer.OrdinalIgnoreCase);
        private static IToastGui? _toast;
        private static bool _running;

        // Call this from Plugin ctor after PluginService injection:
        // ExportGuard.Install(Toast, new[] { "<dir1>", "<dir2>" });
        public static bool Install(IToastGui toast, IEnumerable<string>? watchDirs = null)
        {
            if (_running) return true;

            _toast = toast;

            // Let admins disable quickly
            if (Environment.GetEnvironmentVariable("XIVSYNC_DISABLE_EXPORT_GUARD") == "1")
                return false;

            var dirs = (watchDirs ?? DefaultGuessExportDirs()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (dirs.Count == 0) return false;

            try
            {
                foreach (var dir in dirs)
                {
                    if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                        continue;

                    var w = new FileSystemWatcher(dir)
                    {
                        IncludeSubdirectories = true,
                        Filter = "*.zip",
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite
                    };

                    w.Created += OnZipEvent;
                    w.Changed += OnZipEvent;
                    w.Renamed += OnZipRenamed;
                    w.EnableRaisingEvents = true;
                    _watchers.Add(w);

                    // Optional: also watch .pmp if Penumbra uses that extension in your setup
                    var w2 = new FileSystemWatcher(dir)
                    {
                        IncludeSubdirectories = true,
                        Filter = "*.pmp",
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite
                    };
                    w2.Created += OnZipEvent;
                    w2.Changed += OnZipEvent;
                    w2.Renamed += OnZipRenamed;
                    w2.EnableRaisingEvents = true;
                    _watchers.Add(w2);
                }

                _running = _watchers.Count > 0;
                return _running;
            }
            catch
            {
                Uninstall();
                return false;
            }
        }

        public static void Uninstall()
        {
            foreach (var w in _watchers)
            {
                try
                {
                    w.EnableRaisingEvents = false;
                    w.Created -= OnZipEvent;
                    w.Changed -= OnZipEvent;
                    w.Renamed -= OnZipRenamed;
                    w.Dispose();
                }
                catch { /* ignore */ }
            }
            _watchers.Clear();
            _pending.Clear();
            _running = false;
        }

        private static IEnumerable<string> DefaultGuessExportDirs()
        {
            // Heuristics: common places Penumbra users export to.
            // Prefer to pass the exact export dir via Plugin config if you know it.
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            // Also try a typical Penumbra path under Dalamud
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // Roaming
            var dalamud = Path.Combine(appData, "XIVLauncher", "addon", "Hooks", "dev"); // fallback guess
            var penumbra = Path.Combine(appData, "XIVLauncher", "pluginConfigs", "Penumbra");

            // Add any folders you think your users export to
            return new[]
            {
                Path.Combine(docs, "Penumbra"),
                Path.Combine(docs, "Penumbra", "Exports"),
                desktop,
                downloads,
                penumbra,
                dalamud
            }.Where(Directory.Exists);
        }

        private static void OnZipRenamed(object sender, RenamedEventArgs e)
        {
            // Treat rename to *.zip as creation end
            if (LooksLikeCharacterPack(e.FullPath))
                TryDeleteWithRetries(e.FullPath);
        }

        private static void OnZipEvent(object sender, FileSystemEventArgs e)
        {
            if (!_running) return;
            if (!LooksLikeCharacterPack(e.FullPath)) return;

            // De-dupe rapid events
            if (!_pending.Add(e.FullPath)) return;

            // Small delay to allow the writer to finish
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { TryDeleteWithRetries(e.FullPath); }
                finally { _pending.Remove(e.FullPath); }
            });
        }

        private static bool LooksLikeCharacterPack(string path)
        {
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name)) return false;

            // Tweak these heuristics to your naming conventions
            var n = name.ToLowerInvariant();
            return n.Contains("character") || n.Contains("pcp") || n.Contains("penumbra") || n.Contains("pack");
        }

        private static void TryDeleteWithRetries(string fullPath, int tries = 10, int delayMs = 300)
        {
            for (var i = 0; i < tries; i++)
            {
                try
                {
                    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we can open without sharing, writer is done; close and delete
                    }
                    File.Delete(fullPath);
                    _toast?.ShowError("Character Pack export blocked and removed.");
                    return;
                }
                catch (FileNotFoundException)
                {
                    return; // already gone
                }
                catch (IOException)
                {
                    // still being written; wait and retry
                }
                catch (UnauthorizedAccessException)
                {
                    // might be locked by AV; retry a few times
                }
                Thread.Sleep(delayMs);
            }
            // Last-ditch: try mark for delete on next run (best effort)
            try { File.Delete(fullPath); } catch { /* ignore */ }
        }
    }
}
