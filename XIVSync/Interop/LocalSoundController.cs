using XIVSync.Services.Mediator;
using XIVSync.MareConfiguration;
using Microsoft.Extensions.Logging;

namespace XIVSync.Interop;

/// <summary>
/// Manages local sound muting notification and UI feedback.
/// Note: Technical limitations prevent true local sound interception.
/// This provides UI feedback and user information about the limitation.
/// </summary>
public class LocalSoundController : DisposableMediatorSubscriberBase
{
    private readonly MareConfigService _mareConfigService;
    private bool _isLocallyMuted = false;

    public LocalSoundController(ILogger<LocalSoundController> logger, MareConfigService mareConfigService, MareMediator mareMediator)
        : base(logger, mareMediator)
    {
        _mareConfigService = mareConfigService;
        _isLocallyMuted = _mareConfigService.Current.MuteOwnSoundsLocally;

        Logger.LogInformation("[Local Sound] Sound controller initialized - Local muting is UI-only due to technical limitations");
        Logger.LogInformation("[Local Sound] To truly mute mod sounds locally, manually adjust FFXIV volume in Windows Volume Mixer when this option is enabled");

        Mediator.Subscribe<LocalSelfMuteSettingChangedMessage>(this, (msg) =>
        {
            var newMuteState = _mareConfigService.Current.MuteOwnSoundsLocally;
            if (newMuteState != _isLocallyMuted)
            {
                _isLocallyMuted = newMuteState;
                Logger.LogInformation("[Local Sound] Local mute setting changed to: {muted}", _isLocallyMuted);
                
                if (_isLocallyMuted)
                {
                    Logger.LogInformation("[Local Sound] Local mute enabled - Please manually lower FFXIV volume in Windows Volume Mixer for full effect");
                }
                else
                {
                    Logger.LogInformation("[Local Sound] Local mute disabled - You can restore FFXIV volume in Windows Volume Mixer");
                }
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Logger.LogTrace("[Local Sound] Sound controller disposed");
        }
        base.Dispose(disposing);
    }
}
