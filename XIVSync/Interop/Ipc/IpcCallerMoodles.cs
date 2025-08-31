using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;
using XIVSync.Services;
using XIVSync.Services.Mediator;

namespace XIVSync.Interop.Ipc;

public sealed class IpcCallerMoodles : IIpcCaller
{
    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly DalamudUtilService _dalamudUtil;
    private readonly MareMediator _mareMediator;
    private readonly IDalamudPluginInterface _pi;

    private readonly ICallGateSubscriber<int> _moodlesApiVersion;
    private readonly ICallGateSubscriber<object> _moodlesReady;
    private readonly ICallGateSubscriber<object> _moodlesUnloading;

    private readonly ICallGateSubscriber<IPlayerCharacter, object> _moodlesOnChange;

    private ICallGateSubscriber<nint, string>? _moodlesGetStatus;
    private ICallGateSubscriber<nint, string, object>? _moodlesSetStatus;
    private ICallGateSubscriber<nint, object>? _moodlesRevertStatus;

    public bool APIAvailable { get; private set; }

    public IpcCallerMoodles(
        ILogger<IpcCallerMoodles> logger,
        IDalamudPluginInterface pi,
        DalamudUtilService dalamudUtil,
        MareMediator mareMediator)
    {
        _logger = logger;
        _pi = pi;
        _dalamudUtil = dalamudUtil;
        _mareMediator = mareMediator;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");
        _moodlesReady = pi.GetIpcSubscriber<object>("Moodles.Ready");
        _moodlesUnloading = pi.GetIpcSubscriber<object>("Moodles.Unloading");

        _moodlesOnChange = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        _moodlesOnChange.Subscribe(OnMoodlesChange);

        BindGates();

        _moodlesReady.Subscribe(OnMoodlesReady);
        _moodlesUnloading.Subscribe(OnMoodlesUnloading);

        CheckAPI();
    }

    private void OnMoodlesChange(IPlayerCharacter character)
    {
        _mareMediator.Publish(new MoodlesMessage(character.Address));
    }

    private void OnMoodlesReady()
    {
        _logger.LogDebug("Moodles.Ready");
        BindGates();
        CheckAPI();
    }

    private void OnMoodlesUnloading()
    {
        _logger.LogDebug("Moodles.Unloading");
        APIAvailable = false;
    }

    private void BindGates()
    {
        int ver = 0;
        bool gotVer = false;
        try { ver = _moodlesApiVersion.InvokeFunc(); gotVer = true; }
        catch { gotVer = false; }

        bool useV2 = gotVer ? ver >= 3 : true; // assume newest when unknown
        string suffix = useV2 ? "V2" : string.Empty;

        _moodlesGetStatus = _pi.GetIpcSubscriber<nint, string>($"Moodles.GetStatusManagerByPtr{suffix}");
        _moodlesSetStatus = _pi.GetIpcSubscriber<nint, string, object>($"Moodles.SetStatusManagerByPtr{suffix}");
        _moodlesRevertStatus = _pi.GetIpcSubscriber<nint, object>($"Moodles.ClearStatusManagerByPtr{suffix}");

        _logger.LogDebug("Moodles IPC bound (Version={ver}, useV2={useV2}, suffix='{suffix}')", ver, useV2, suffix);
    }

    public void CheckAPI()
    {
        try
        {
            var ver = _moodlesApiVersion.InvokeFunc();
            APIAvailable = ver >= 1;
            _logger.LogDebug("Moodles.Version={ver}, APIAvailable={avail}", ver, APIAvailable);
        }
        catch (Exception ex)
        {
            APIAvailable = false;
            _logger.LogDebug(ex, "Moodles.Version check failed");
        }
    }

    public void Dispose()
    {
        _moodlesOnChange.Unsubscribe(OnMoodlesChange);
        _moodlesReady.Unsubscribe(OnMoodlesReady);
        _moodlesUnloading.Unsubscribe(OnMoodlesUnloading);
    }

    public async Task<string?> GetStatusAsync(nint address)
    {
        if (!APIAvailable) return null;
        try
        {
            if (_moodlesGetStatus is null) { BindGates(); if (_moodlesGetStatus is null) return null; }
            return await _dalamudUtil.RunOnFrameworkThread(() => _moodlesGetStatus!.InvokeFunc(address)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Moodles GetStatus failed");
            return null;
        }
    }

    public async Task SetStatusAsync(nint pointer, string status)
    {
        if (!APIAvailable) return;
        try
        {
            if (_moodlesSetStatus is null) { BindGates(); if (_moodlesSetStatus is null) return; }
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesSetStatus!.InvokeAction(pointer, status)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Moodles SetStatus failed");
        }
    }

    public async Task RevertStatusAsync(nint pointer)
    {
        if (!APIAvailable) return;
        try
        {
            if (_moodlesRevertStatus is null) { BindGates(); if (_moodlesRevertStatus is null) return; }
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesRevertStatus!.InvokeAction(pointer)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Moodles RevertStatus failed");
        }
    }
}
