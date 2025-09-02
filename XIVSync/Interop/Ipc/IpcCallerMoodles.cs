﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using XIVSync.Services;
using XIVSync.Services.Mediator;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace XIVSync.Interop.Ipc;

public sealed class IpcCallerMoodles : IIpcCaller
{
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;
    private readonly ICallGateSubscriber<IPlayerCharacter, object> _moodlesOnChange;
    private readonly ICallGateSubscriber<nint, string> _moodlesGetStatus;
    private readonly ICallGateSubscriber<nint, string, object> _moodlesSetStatus;
    private readonly ICallGateSubscriber<nint, object> _moodlesRevertStatus;
    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly DalamudUtilService _dalamudUtil;
    private readonly MareMediator _mareMediator;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, IDalamudPluginInterface pi, DalamudUtilService dalamudUtil,
        MareMediator mareMediator)
    {
        _logger = logger;
        _dalamudUtil = dalamudUtil;
        _mareMediator = mareMediator;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");
        _moodlesOnChange = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        _moodlesGetStatus = pi.GetIpcSubscriber<nint, string>("Moodles.GetStatusManagerByPtrV2");
        _moodlesSetStatus = pi.GetIpcSubscriber<nint, string, object>("Moodles.SetStatusManagerByPtrV2");
        _moodlesRevertStatus = pi.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");

        _moodlesOnChange.Subscribe(OnMoodlesChange);
        CheckAPI();
    }

    private void OnMoodlesChange(IPlayerCharacter character)
    {
        _mareMediator.Publish(new MoodlesMessage(character.Address));
    }

    public bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        // Support multiple known API versions (add new versions as Moodles updates)
        var supportedVersions = new[] { 1, 2, 3 }; // Add more versions as they become available
        
        try
        {
            var moodlesVersion = _moodlesApiVersion.InvokeFunc();
            APIAvailable = supportedVersions.Contains(moodlesVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get Moodles API version: {Exception}", ex.Message);
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        _moodlesOnChange.Unsubscribe(OnMoodlesChange);
    }

    public async Task<string?> GetStatusAsync(nint address)
    {
        if (!APIAvailable) return null;

        try
        {
            var result = await _dalamudUtil.RunOnFrameworkThread(() => _moodlesGetStatus.InvokeFunc(address)).ConfigureAwait(false);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Get Moodles Status");
            return null;
        }
    }

    public async Task SetStatusAsync(nint pointer, string status)
    {
        if (!APIAvailable) return;
        
        try
        {
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesSetStatus.InvokeAction(pointer, status)).ConfigureAwait(false);
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
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesRevertStatus.InvokeAction(pointer)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Revert Moodles Status");
        }
    }
}