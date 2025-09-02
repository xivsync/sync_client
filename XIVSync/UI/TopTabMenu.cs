using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Microsoft.Extensions.Logging;
using XIVSync.API.Data.Enum;
using XIVSync.API.Data.Extensions;
using XIVSync.MareConfiguration;
using XIVSync.PlayerData.Pairs;
using XIVSync.Services.Mediator;
using XIVSync.WebAPI;
using System.Numerics;

namespace XIVSync.UI;

public class TopTabMenu
{
    private readonly ApiController _apiController;
    private readonly ILogger _logger;
    private readonly MareConfigService _mareConfigService;
    private readonly MareMediator _mareMediator;
    private readonly PairManager _pairManager;
    private readonly UiSharedService _uiSharedService;
    private readonly WindowMediatorSubscriberBase? _parentWindow;
    private string _filter = string.Empty;
    private int _globalControlCountdown = 0;

    private string _pairToAdd = string.Empty;

    private SelectedTab _selectedTab = SelectedTab.None;
    public TopTabMenu(ILogger logger, MareMediator mareMediator, ApiController apiController, PairManager pairManager, UiSharedService uiSharedService, MareConfigService mareConfigService, WindowMediatorSubscriberBase? parentWindow = null)
    {
        _logger = logger;
        _mareMediator = mareMediator;
        _apiController = apiController;
        _pairManager = pairManager;
        _uiSharedService = uiSharedService;
        _mareConfigService = mareConfigService;
        _parentWindow = parentWindow;
        
        // Test if logger is working
        _logger.LogInformation("[Self-Mute] TopTabMenu initialized successfully");
    }

    private enum SelectedTab
    {
        None,
        Individual,
        Syncshell,
        Filter,
        UserConfig
    }

    public string Filter
    {
        get => _filter;
        private set
        {
            if (!string.Equals(_filter, value, StringComparison.OrdinalIgnoreCase))
            {
                _mareMediator.Publish(new RefreshUiMessage());
            }

            _filter = value;
        }
    }
    private SelectedTab TabSelection
    {
        get => _selectedTab; set
        {
            if (_selectedTab == SelectedTab.Filter && value != SelectedTab.Filter)
            {
                Filter = string.Empty;
            }

            _selectedTab = value;
        }
    }
    private void AttachTooltip(string text, ThemePalette? theme)
    {
        if (theme != null)
            UiSharedService.AttachThemedToolTip(text, theme);
        else
            UiSharedService.AttachToolTip(text);
    }

    public void Draw(ThemePalette? theme = null)
    {
        var availableWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * 3)) / 4f;
        var buttonY = _uiSharedService.GetIconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);
        
        // Apply theme button colors if available  
        int colorsPushed = 0;
        if (theme != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, theme.Btn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, theme.BtnHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, theme.BtnActive);
            ImGui.PushStyleColor(ImGuiCol.Text, theme.BtnText);
            colorsPushed = 4;
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
            colorsPushed = 1;
        }

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var x = ImGui.GetCursorScreenPos();
            if (ImGui.Button(FontAwesomeIcon.User.ToIconString(), buttonSize))
            {
                TabSelection = TabSelection == SelectedTab.Individual ? SelectedTab.None : SelectedTab.Individual;
            }
            ImGui.SameLine();
            var xAfter = ImGui.GetCursorScreenPos();
            if (TabSelection == SelectedTab.Individual)
                drawList.AddLine(x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xAfter with { Y = xAfter.Y + buttonSize.Y + spacing.Y, X = xAfter.X - spacing.X },
                    underlineColor, 2);
        }
        AttachTooltip("Individual Pair Menu", theme);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var x = ImGui.GetCursorScreenPos();
            if (ImGui.Button(FontAwesomeIcon.Users.ToIconString(), buttonSize))
            {
                TabSelection = TabSelection == SelectedTab.Syncshell ? SelectedTab.None : SelectedTab.Syncshell;
            }
            ImGui.SameLine();
            var xAfter = ImGui.GetCursorScreenPos();
            if (TabSelection == SelectedTab.Syncshell)
                drawList.AddLine(x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xAfter with { Y = xAfter.Y + buttonSize.Y + spacing.Y, X = xAfter.X - spacing.X },
                    underlineColor, 2);
        }
        AttachTooltip("Syncshell Menu", theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var x = ImGui.GetCursorScreenPos();
            if (ImGui.Button(FontAwesomeIcon.Filter.ToIconString(), buttonSize))
            {
                TabSelection = TabSelection == SelectedTab.Filter ? SelectedTab.None : SelectedTab.Filter;
            }

            ImGui.SameLine();
            var xAfter = ImGui.GetCursorScreenPos();
            if (TabSelection == SelectedTab.Filter)
                drawList.AddLine(x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xAfter with { Y = xAfter.Y + buttonSize.Y + spacing.Y, X = xAfter.X - spacing.X },
                    underlineColor, 2);
        }
        AttachTooltip("Filter", theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var x = ImGui.GetCursorScreenPos();
            if (ImGui.Button(FontAwesomeIcon.UserCog.ToIconString(), buttonSize))
            {
                TabSelection = TabSelection == SelectedTab.UserConfig ? SelectedTab.None : SelectedTab.UserConfig;
            }

            ImGui.SameLine();
            var xAfter = ImGui.GetCursorScreenPos();
            if (TabSelection == SelectedTab.UserConfig)
                drawList.AddLine(x with { Y = x.Y + buttonSize.Y + spacing.Y },
                    xAfter with { Y = xAfter.Y + buttonSize.Y + spacing.Y, X = xAfter.X - spacing.X },
                    underlineColor, 2);
        }
        AttachTooltip("Your User Menu", theme);

        ImGui.NewLine();
        if (colorsPushed > 0)
        {
            ImGui.PopStyleColor(colorsPushed);
        }

        ImGuiHelpers.ScaledDummy(spacing);

        if (TabSelection == SelectedTab.Individual)
        {
            DrawAddPair(availableWidth, spacing.X, theme);
            DrawGlobalIndividualButtons(availableWidth, spacing.X, theme);
        }
        else if (TabSelection == SelectedTab.Syncshell)
        {
            DrawSyncshellMenu(availableWidth, spacing.X, theme);
            DrawGlobalSyncshellButtons(availableWidth, spacing.X, theme);
        }
        else if (TabSelection == SelectedTab.Filter)
        {
            DrawFilter(availableWidth, spacing.X, theme);
        }
        else if (TabSelection == SelectedTab.UserConfig)
        {
            DrawUserConfig(availableWidth, spacing.X, theme);
        }

        if (TabSelection != SelectedTab.None) ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

    private void DrawAddPair(float availableXWidth, float spacingX, ThemePalette? theme)
    {
        var buttonSize = _uiSharedService.GetIconTextButtonSize(FontAwesomeIcon.UserPlus, "Add");
        ImGui.SetNextItemWidth(availableXWidth - buttonSize - spacingX);
        ImGui.InputTextWithHint("##otheruid", "Other players UID/Alias", ref _pairToAdd, 20);
        ImGui.SameLine();
        var alreadyExisting = _pairManager.DirectPairs.Exists(p => string.Equals(p.UserData.UID, _pairToAdd, StringComparison.Ordinal) || string.Equals(p.UserData.Alias, _pairToAdd, StringComparison.Ordinal));
        using (ImRaii.Disabled(alreadyExisting || string.IsNullOrEmpty(_pairToAdd)))
        {
            if (_uiSharedService.IconTextButton(FontAwesomeIcon.UserPlus, "Add"))
            {
                _ = _apiController.UserAddPair(new(new(_pairToAdd)));
                _pairToAdd = string.Empty;
            }
        }
        AttachTooltip("Pair with " + (_pairToAdd.IsNullOrEmpty() ? "other user" : _pairToAdd), theme);
    }

    private void DrawFilter(float availableWidth, float spacingX, ThemePalette? theme)
    {
        var buttonSize = _uiSharedService.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = Filter;
        if (ImGui.InputTextWithHint("##filter", "Filter for UID/notes", ref filter, 255))
        {
            Filter = filter;
        }
        ImGui.SameLine();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(Filter));
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            Filter = string.Empty;
        }
    }

    private void DrawGlobalIndividualButtons(float availableXWidth, float spacingX, ThemePalette? theme)
    {
        var buttonX = (availableXWidth - (spacingX * 3)) / 4f; // 4 buttons with proper spacing  
        var buttonY = _uiSharedService.GetIconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Pause.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Individual Pause");
            }
        }
        AttachTooltip("Globally resume or pause all individual pairs." + UiSharedService.TooltipSeparator
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.VolumeUp.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Individual Sounds");
            }
        }
        AttachTooltip("Globally enable or disable sound sync with all individual pairs."
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Running.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Individual Animations");
            }
        }
        AttachTooltip("Globally enable or disable animation sync with all individual pairs." + UiSharedService.TooltipSeparator
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Sun.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Individual VFX");
            }
        }
        AttachTooltip("Globally enable or disable VFX sync with all individual pairs." + UiSharedService.TooltipSeparator
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        var isMuted = _mareConfigService.Current.MuteOwnSounds;
        
        // Self-mute button for network muting only
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var selfMuteIcon = isMuted ? FontAwesomeIcon.UserSlash : FontAwesomeIcon.User;
            var iconString = selfMuteIcon.ToIconString();
            
            var buttonResult = ImGui.Button(iconString, buttonSize);
            
            // Handle button click with workaround for ImGui issues
            var isHoveredAfter = ImGui.IsItemHovered();
            var isClicked = ImGui.IsItemClicked();
            
            if (!buttonResult && isClicked && isHoveredAfter)
            {
                buttonResult = true;
            }
            
            if (buttonResult)
            {
                _mareConfigService.Current.MuteOwnSounds = !_mareConfigService.Current.MuteOwnSounds;
                _mareConfigService.Save();
                _mareMediator.Publish(new SelfMuteSettingChangedMessage());
            }
        }
        
        AttachTooltip(isMuted ? "Unmute: Allow others to hear your sounds" : "Mute: Prevent others from hearing your sounds", theme);


        PopupIndividualSetting("Individual Pause", "Unpause all individuals", "Pause all individuals",
            FontAwesomeIcon.Play, FontAwesomeIcon.Pause,
            (perm) =>
            {
                perm.SetPaused(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetPaused(true);
                return perm;
            });
        PopupIndividualSetting("Individual Sounds", "Enable sounds for all individuals", "Disable sounds for all individuals",
            FontAwesomeIcon.VolumeUp, FontAwesomeIcon.VolumeMute,
            (perm) =>
            {
                perm.SetDisableSounds(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableSounds(true);
                return perm;
            });
        PopupIndividualSetting("Individual Animations", "Enable animations for all individuals", "Disable animations for all individuals",
            FontAwesomeIcon.Running, FontAwesomeIcon.Stop,
            (perm) =>
            {
                perm.SetDisableAnimations(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableAnimations(true);
                return perm;
            });
        PopupIndividualSetting("Individual VFX", "Enable VFX for all individuals", "Disable VFX for all individuals",
            FontAwesomeIcon.Sun, FontAwesomeIcon.Circle,
            (perm) =>
            {
                perm.SetDisableVFX(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableVFX(true);
                return perm;
            });
    }

    private void DrawGlobalSyncshellButtons(float availableXWidth, float spacingX, ThemePalette? theme)
    {
        var buttonX = (availableXWidth - (spacingX * 4)) / 5f;
        var buttonY = _uiSharedService.GetIconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Pause.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Syncshell Pause");
            }
        }
        AttachTooltip("Globally resume or pause all syncshells." + UiSharedService.TooltipSeparator
                        + "Note: This will not affect users with preferred permissions in syncshells."
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.VolumeUp.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Syncshell Sounds");
            }
        }
        AttachTooltip("Globally enable or disable sound sync with all syncshells." + UiSharedService.TooltipSeparator
                        + "Note: This will not affect users with preferred permissions in syncshells."
                        + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Running.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Syncshell Animations");
            }
        }
        AttachTooltip("Globally enable or disable animation sync with all syncshells." + UiSharedService.TooltipSeparator
                        + "Note: This will not affect users with preferred permissions in syncshells."
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0);

            if (ImGui.Button(FontAwesomeIcon.Sun.ToIconString(), buttonSize))
            {
                ImGui.OpenPopup("Syncshell VFX");
            }
        }
        AttachTooltip("Globally enable or disable VFX sync with all syncshells." + UiSharedService.TooltipSeparator
                        + "Note: This will not affect users with preferred permissions in syncshells."
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);


        PopupSyncshellSetting("Syncshell Pause", "Unpause all syncshells", "Pause all syncshells",
            FontAwesomeIcon.Play, FontAwesomeIcon.Pause,
            (perm) =>
            {
                perm.SetPaused(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetPaused(true);
                return perm;
            });
        PopupSyncshellSetting("Syncshell Sounds", "Enable sounds for all syncshells", "Disable sounds for all syncshells",
            FontAwesomeIcon.VolumeUp, FontAwesomeIcon.VolumeMute,
            (perm) =>
            {
                perm.SetDisableSounds(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableSounds(true);
                return perm;
            });
        PopupSyncshellSetting("Syncshell Animations", "Enable animations for all syncshells", "Disable animations for all syncshells",
            FontAwesomeIcon.Running, FontAwesomeIcon.Stop,
            (perm) =>
            {
                perm.SetDisableAnimations(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableAnimations(true);
                return perm;
            });
        PopupSyncshellSetting("Syncshell VFX", "Enable VFX for all syncshells", "Disable VFX for all syncshells",
            FontAwesomeIcon.Sun, FontAwesomeIcon.Circle,
            (perm) =>
            {
                perm.SetDisableVFX(false);
                return perm;
            },
            (perm) =>
            {
                perm.SetDisableVFX(true);
                return perm;
            });

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var disabled = ImRaii.Disabled(_globalControlCountdown > 0 || !UiSharedService.CtrlPressed());

            if (ImGui.Button(FontAwesomeIcon.Check.ToIconString(), buttonSize))
            {
                _ = GlobalControlCountdown(10);
                var bulkSyncshells = _pairManager.GroupPairs.Keys.OrderBy(g => g.GroupAliasOrGID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Group.GID, g =>
                    {
                        var perm = g.GroupUserPermissions;
                        perm.SetDisableSounds(g.GroupPermissions.IsPreferDisableSounds());
                        perm.SetDisableAnimations(g.GroupPermissions.IsPreferDisableAnimations());
                        perm.SetDisableVFX(g.GroupPermissions.IsPreferDisableVFX());
                        return perm;
                    }, StringComparer.Ordinal);

                _ = _apiController.SetBulkPermissions(new(new(StringComparer.Ordinal), bulkSyncshells)).ConfigureAwait(false);
            }
        }
        AttachTooltip("Globally align syncshell permissions to suggested syncshell permissions." + UiSharedService.TooltipSeparator
            + "Note: This will not affect users with preferred permissions in syncshells." + Environment.NewLine
            + "Note: If multiple users share one syncshell the permissions to that user will be set to " + Environment.NewLine
            + "the ones of the last applied syncshell in alphabetical order." + UiSharedService.TooltipSeparator
            + "Hold CTRL to enable this button"
            + (_globalControlCountdown > 0 ? UiSharedService.TooltipSeparator + "Available again in " + _globalControlCountdown + " seconds." : string.Empty), theme);
    }

    private void DrawSyncshellMenu(float availableWidth, float spacingX, ThemePalette? theme)
    {
        var buttonX = (availableWidth - (spacingX)) / 2f;

        using (ImRaii.Disabled(_pairManager.GroupPairs.Select(k => k.Key).Distinct()
            .Count(g => string.Equals(g.OwnerUID, _apiController.UID, StringComparison.Ordinal)) >= _apiController.ServerInfo.MaxGroupsCreatedByUser))
        {
            if (_uiSharedService.IconTextButton(FontAwesomeIcon.Plus, "Create new Syncshell", buttonX))
            {
                _mareMediator.Publish(new UiToggleMessage(typeof(CreateSyncshellUI)));
            }
            ImGui.SameLine();
        }

        using (ImRaii.Disabled(_pairManager.GroupPairs.Select(k => k.Key).Distinct().Count() >= _apiController.ServerInfo.MaxGroupsJoinedByUser))
        {
            if (_uiSharedService.IconTextButton(FontAwesomeIcon.Users, "Join existing Syncshell", buttonX))
            {
                _mareMediator.Publish(new UiToggleMessage(typeof(JoinSyncshellUI)));
            }
        }
    }

    private void DrawUserConfig(float availableWidth, float spacingX, ThemePalette? theme)
    {
        var buttonX = (availableWidth - spacingX) / 2f;
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.UserCircle, "Edit Mare Profile", buttonX))
        {
            _mareMediator.Publish(new UiToggleMessage(typeof(EditProfileUi)));
        }
        AttachTooltip("Edit your Mare Profile", theme);
        ImGui.SameLine();
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.PersonCircleQuestion, "Chara Data Analysis", buttonX))
        {
            _mareMediator.Publish(new UiToggleMessage(typeof(DataAnalysisUi)));
        }
        AttachTooltip("View and analyze your generated character data", theme);
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.Running, "Character Data Hub", availableWidth))
        {
            _mareMediator.Publish(new UiToggleMessage(typeof(CharaDataHubUi)));
        }
        
        // Add Settings and Customize Theme buttons
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.Cog, "Settings", buttonX))
        {
            _mareMediator.Publish(new UiToggleMessage(typeof(ModernSettingsUi)));
        }
        AttachTooltip("Open Mare Settings", theme);
        ImGui.SameLine();
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.Palette, "Customize Theme", buttonX))
        {
            // Toggle the theme inline editor in CompactUI
            if (_parentWindow is CompactUi compactUi)
            {
                compactUi.ToggleThemeInline();
            }
        }
        AttachTooltip("Customize appearance and themes", theme);
        
        // Add Pin Window and Click Through toggles
        if (_parentWindow != null)
        {
            ImGui.Separator();
            
            // Apply theme button colors to checkboxes
            int checkboxColorsPushed = 0;
            if (theme != null)
            {
                ImGui.PushStyleColor(ImGuiCol.CheckMark, theme.BtnText);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, theme.Btn);
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, theme.BtnHovered);
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, theme.BtnActive);
                ImGui.PushStyleColor(ImGuiCol.Text, theme.BtnText);
                checkboxColorsPushed = 5;
            }
            
            // Pin Window and Click Through toggles (side by side)
            bool isPinned = _parentWindow.AllowPinning;
            if (ImGui.Checkbox("Pin Window", ref isPinned))
            {
                _parentWindow.AllowPinning = isPinned;
            }
            AttachTooltip("Prevent the window from being moved", theme);
            
            ImGui.SameLine(); // Put the next checkbox on the same line
            
            bool isClickThrough = _parentWindow.AllowClickthrough;
            if (ImGui.Checkbox("Click Through", ref isClickThrough))
            {
                _parentWindow.AllowClickthrough = isClickThrough;
                if (isClickThrough)
                {
                    // Auto-enable pin window when click-through is enabled
                    _parentWindow.AllowPinning = true;
                    // Collapse the user menu
                    _selectedTab = SelectedTab.None;
                    _filter = string.Empty;
                }
            }
            AttachTooltip("Allow clicks to pass through the window", theme);
            
            // Pop checkbox colors
            if (checkboxColorsPushed > 0)
            {
                ImGui.PopStyleColor(checkboxColorsPushed);
            }
        }
    }

    public void ClearUserFilter()
    {
        _filter = string.Empty;
        _selectedTab = SelectedTab.None;
        _logger.LogInformation("Reset button clicked - cleared filter and user selection");
    }

    private async Task GlobalControlCountdown(int countdown)
    {

        _globalControlCountdown = countdown;
        while (_globalControlCountdown > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            _globalControlCountdown--;
        }
    }

    private void PopupIndividualSetting(string popupTitle, string enableText, string disableText,
                    FontAwesomeIcon enableIcon, FontAwesomeIcon disableIcon,
        Func<UserPermissions, UserPermissions> actEnable, Func<UserPermissions, UserPermissions> actDisable)
    {
        if (ImGui.BeginPopup(popupTitle))
        {
            if (_uiSharedService.IconTextButton(enableIcon, enableText, null, true))
            {
                _ = GlobalControlCountdown(10);
                var bulkIndividualPairs = _pairManager.PairsWithGroups.Keys
                    .Where(g => g.IndividualPairStatus == IndividualPairStatus.Bidirectional)
                    .ToDictionary(g => g.UserPair.User.UID, g =>
                    {
                        return actEnable(g.UserPair.OwnPermissions);
                    }, StringComparer.Ordinal);

                _ = _apiController.SetBulkPermissions(new(bulkIndividualPairs, new(StringComparer.Ordinal))).ConfigureAwait(false);
                ImGui.CloseCurrentPopup();
            }

            if (_uiSharedService.IconTextButton(disableIcon, disableText, null, true))
            {
                _ = GlobalControlCountdown(10);
                var bulkIndividualPairs = _pairManager.PairsWithGroups.Keys
                    .Where(g => g.IndividualPairStatus == IndividualPairStatus.Bidirectional)
                    .ToDictionary(g => g.UserPair.User.UID, g =>
                    {
                        return actDisable(g.UserPair.OwnPermissions);
                    }, StringComparer.Ordinal);

                _ = _apiController.SetBulkPermissions(new(bulkIndividualPairs, new(StringComparer.Ordinal))).ConfigureAwait(false);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    private void PopupSyncshellSetting(string popupTitle, string enableText, string disableText,
        FontAwesomeIcon enableIcon, FontAwesomeIcon disableIcon,
        Func<GroupUserPreferredPermissions, GroupUserPreferredPermissions> actEnable,
        Func<GroupUserPreferredPermissions, GroupUserPreferredPermissions> actDisable)
    {
        if (ImGui.BeginPopup(popupTitle))
        {

            if (_uiSharedService.IconTextButton(enableIcon, enableText, null, true))
            {
                _ = GlobalControlCountdown(10);
                var bulkSyncshells = _pairManager.GroupPairs.Keys
                    .OrderBy(u => u.GroupAliasOrGID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Group.GID, g =>
                    {
                        return actEnable(g.GroupUserPermissions);
                    }, StringComparer.Ordinal);

                _ = _apiController.SetBulkPermissions(new(new(StringComparer.Ordinal), bulkSyncshells)).ConfigureAwait(false);
                ImGui.CloseCurrentPopup();
            }

            if (_uiSharedService.IconTextButton(disableIcon, disableText, null, true))
            {
                _ = GlobalControlCountdown(10);
                var bulkSyncshells = _pairManager.GroupPairs.Keys
                    .OrderBy(u => u.GroupAliasOrGID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Group.GID, g =>
                    {
                        return actDisable(g.GroupUserPermissions);
                    }, StringComparer.Ordinal);

                _ = _apiController.SetBulkPermissions(new(new(StringComparer.Ordinal), bulkSyncshells)).ConfigureAwait(false);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}
