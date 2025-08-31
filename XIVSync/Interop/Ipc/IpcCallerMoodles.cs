using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using XIVSync.Services;
using XIVSync.Services.Mediator;

namespace XIVSync.Interop.Ipc;

public sealed class IpcCallerMoodles : IIpcCaller, IDisposable
{
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;
    private readonly ICallGateSubscriber<IPlayerCharacter, object> _moodlesOnChange;
    private readonly ICallGateSubscriber<nint, string> _moodlesGetStatus;
    private readonly ICallGateSubscriber<string, string, object> _moodlesSetStatusByName;
    private readonly ICallGateSubscriber<string, object> _moodlesClearStatusByName;
    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly DalamudUtilService _dalamudUtil;
    private readonly MareMediator _mareMediator;
    private readonly IObjectTable _objectTable;

    public IpcCallerMoodles(
        ILogger<IpcCallerMoodles> logger,
        IDalamudPluginInterface pi,
        DalamudUtilService dalamudUtil,
        MareMediator mareMediator,
        IObjectTable objectTable)
    {
        _logger = logger;
        _dalamudUtil = dalamudUtil;
        _mareMediator = mareMediator;
        _objectTable = objectTable;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");
        _moodlesOnChange = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        _moodlesGetStatus = pi.GetIpcSubscriber<nint, string>("Moodles.GetStatusManagerByPtr");
        _moodlesSetStatusByName = pi.GetIpcSubscriber<string, string, object>("GagSpeak.SetStatusManagerByName");
        _moodlesClearStatusByName = pi.GetIpcSubscriber<string, object>("GagSpeak.ClearStatusManagerByName");

        _moodlesOnChange.Subscribe(OnMoodlesChange);
        CheckAPI();
    }

    private void OnMoodlesChange(IPlayerCharacter character)
        => _mareMediator.Publish(new MoodlesMessage(character.Address));

    public bool APIAvailable { get; private set; }

    public void CheckAPI()
    {
        try
        {
            var v = _moodlesApiVersion.InvokeFunc();
            APIAvailable = v == 1;
            _logger.LogInformation("Moodles IPC version: {Version}, available: {Available}", v, APIAvailable);
        }
        catch (Exception ex)
        {
            APIAvailable = false;
            _logger.LogDebug(ex, "Moodles IPC version probe failed");
        }
    }

    public void Dispose()
        => _moodlesOnChange.Unsubscribe(OnMoodlesChange);

    public async Task<string?> GetStatusAsync(nint address)
    {
        if (!APIAvailable) return null;
        try
        {
            return await _dalamudUtil
                .RunOnFrameworkThread(() => _moodlesGetStatus.InvokeFunc(address))
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Get Moodles Status");
            return null;
        }
    }

    private IPlayerCharacter? GetPcFromAddress(nint address)
    {
        foreach (var obj in _objectTable)
        {
            if (obj.Address == address && obj is IPlayerCharacter pc)
                return pc;
        }
        return null;
    }

    private static string ResolvePcName(IPlayerCharacter pc)
    {
        var name = pc.Name?.TextValue ?? pc.Name?.ToString() ?? string.Empty;
        try
        {
            var worldName = pc.HomeWorld.Value.Name.ToString();
            return string.IsNullOrEmpty(worldName) ? name : $"{name}@{worldName}";
        }
        catch
        {
            return name;
        }
    }

    public async Task SetStatusAsync(nint pointer, string status)
    {
        if (!APIAvailable) return;
        try
        {
            await _dalamudUtil.RunOnFrameworkThread(() =>
            {
                var pc = GetPcFromAddress(pointer);
                if (pc is null) return;
                var name = ResolvePcName(pc);
                if (!string.IsNullOrEmpty(name))
                    _moodlesSetStatusByName.InvokeAction(name, status);
            }).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Set Moodles Status");
        }
    }

    public async Task RevertStatusAsync(nint pointer)
    {
        if (!APIAvailable) return;
        try
        {
            await _dalamudUtil.RunOnFrameworkThread(() =>
            {
                var pc = GetPcFromAddress(pointer);
                if (pc is null) return;
                var name = ResolvePcName(pc);
                if (!string.IsNullOrEmpty(name))
                    _moodlesClearStatusByName.InvokeAction(name);
            }).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Clear Moodles Status");
        }
    }
}
