#if DEBUG
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

namespace XIVSync.Utils
{
    internal sealed class DevAutoReloader : IDisposable
    {
        private readonly string pluginName = "XIVSync";
        private readonly ICommandManager commandManager;
        private readonly FileSystemWatcher? watcher;
        private DateTime lastReloadUtc = DateTime.MinValue;
        private bool disposed;

        public DevAutoReloader(ICommandManager commandManager, bool watchForDllChanges = true)
        {
            this.commandManager = commandManager;

            this.commandManager.AddHandler("/devreload", new CommandInfo(OnReloadCommand)
            {
                ShowInHelp = false
            });

            if (watchForDllChanges)
            {
                var loadedDllPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(loadedDllPath);
                var file = Path.GetFileName(loadedDllPath);

                // Debug: Log the path being watched
                System.Diagnostics.Debug.WriteLine($"[DevAutoReloader] Watching: {loadedDllPath}");

                if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(file))
                {
                    watcher = new FileSystemWatcher(dir, file)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true,
                    };
                    watcher.Changed += (_, __) => DebouncedReload();
                    watcher.Created += (_, __) => DebouncedReload();
                    watcher.Renamed += (_, __) => DebouncedReload();
                }
            }
        }

        private void OnReloadCommand(string command, string args) => TriggerReload();

        private void DebouncedReload()
        {
            var now = DateTime.UtcNow;
            if ((now - lastReloadUtc).TotalMilliseconds < 300) return;
            lastReloadUtc = now;
            TriggerReload();
        }

        private void TriggerReload()
        {
            // Fire-and-forget so we don't block file watcher or command thread
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to let the DLL finish copying
                    await Task.Delay(200);
                    commandManager.ProcessCommand($"/dalamudplugin reload {pluginName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DevAutoReloader] reload command failed: {ex}");
                }
            });
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { commandManager.RemoveHandler("/devreload"); } catch { }
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }
    }
}
#endif
