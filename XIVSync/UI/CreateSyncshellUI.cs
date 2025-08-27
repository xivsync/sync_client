﻿using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using XIVSync.API.Data.Extensions;
using XIVSync.API.Dto.Group;
using XIVSync.Services;
using XIVSync.Services.Mediator;
using XIVSync.WebAPI;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace XIVSync.UI;
public class CreateSyncshellUI : WindowMediatorSubscriberBase
{
    private readonly ApiController _apiController;
    private readonly UiSharedService _uiSharedService;
    private bool _errorGroupCreate;
    private GroupJoinDto? _lastCreatedGroup;

    // 👇 Add these two fields
    private bool _creating;
    private string? _errorText;

    public CreateSyncshellUI(ILogger<CreateSyncshellUI> logger, MareMediator mareMediator,
        ApiController apiController, UiSharedService uiSharedService,
        PerformanceCollectorService performanceCollectorService)
        : base(logger, mareMediator,
            "Create new Syncshell###MareSynchronosCreateSyncshell", performanceCollectorService)
    {
        _apiController = apiController;
        _uiSharedService = uiSharedService;
        SizeConstraints = new()
        {
            MinimumSize = new(550, 330),
            MaximumSize = new(550, 330)
        };

        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;

        Mediator.Subscribe<DisconnectedMessage>(this, (_) => IsOpen = false);
    }

    protected override void DrawInternal()
    {
        using (_uiSharedService.UidFont.Push())
            ImGui.TextUnformatted("Create new Syncshell");

        if (_lastCreatedGroup == null)
        {
            if (!_creating && _uiSharedService.IconTextButton(FontAwesomeIcon.Plus, "Create Syncshell"))
            {
                _creating = true;
                _errorGroupCreate = false;
                _errorText = null;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        _lastCreatedGroup = await _apiController.GroupCreate().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _lastCreatedGroup = null;
                        _errorGroupCreate = true;
                        _errorText = ex.Message; // 👈 save message
                    }
                    finally
                    {
                        _creating = false;
                    }
                });
            }
            ImGui.SameLine();
        }

        ImGui.Separator();

        if (_lastCreatedGroup == null)
        {
            UiSharedService.TextWrapped(
                "Creating a new Syncshell will create it with your current preferred permissions..." +
                Environment.NewLine +
                "- You can own up to " + _apiController.ServerInfo.MaxGroupsCreatedByUser + " Syncshells..." +
                Environment.NewLine +
                "- You can join up to " + _apiController.ServerInfo.MaxGroupsJoinedByUser + " Syncshells..." +
                Environment.NewLine +
                "- Syncshells can have a maximum of " + _apiController.ServerInfo.MaxGroupUserCount + " users");
        }
        else
        {
            _errorGroupCreate = false;
            ImGui.TextUnformatted("Syncshell ID: " + _lastCreatedGroup.Group.GID);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Syncshell Password: " + _lastCreatedGroup.Password);
            ImGui.SameLine();
            if (_uiSharedService.IconButton(FontAwesomeIcon.Copy))
                ImGui.SetClipboardText(_lastCreatedGroup.Password);
        }

        if (_errorGroupCreate)
        {
            UiSharedService.ColorTextWrapped(
                "Something went wrong during creation of a new Syncshell",
                new Vector4(1, 0, 0, 1));

            if (!string.IsNullOrEmpty(_errorText))
                UiSharedService.TextWrapped($"Details: {_errorText}");
        }
    }

    public override void OnOpen()
    {
        _lastCreatedGroup = null;
    }
}
