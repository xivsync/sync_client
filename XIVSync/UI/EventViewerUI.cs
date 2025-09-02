using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using XIVSync.Services;
using XIVSync.Services.Events;
using XIVSync.Services.Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Runtime.InteropServices;

namespace XIVSync.UI;

internal class EventViewerUI : WindowMediatorSubscriberBase
{
    private readonly EventAggregator _eventAggregator;
    private readonly UiSharedService _uiSharedService;
    private List<Event> _currentEvents = new();
    private Lazy<List<Event>> _filteredEvents;
    private string _filterFreeText = string.Empty;
    private string _filterCharacter = string.Empty;
    private string _filterUid = string.Empty;
    private string _filterSource = string.Empty;
    private string _filterEvent = string.Empty;
    
    // Category-based filtering
    private bool _filterPlugin = true;
    private bool _filterAuth = true;
    private bool _filterServices = true;
    private bool _filterNetwork = true;
    private bool _filterFile = true;
    private bool _filterOther = true;
    
    // Severity filtering
    private bool _filterInfo = true;
    private bool _filterWarning = true;
    private bool _filterError = true;

    private List<Event> CurrentEvents
    {
        get
        {
            return _currentEvents;
        }
        set
        {
            _currentEvents = value;
            _filteredEvents = RecreateFilter();
        }
    }

    public EventViewerUI(ILogger<EventViewerUI> logger, MareMediator mediator,
        EventAggregator eventAggregator, UiSharedService uiSharedService, PerformanceCollectorService performanceCollectorService)
        : base(logger, mediator, "Event Viewer", performanceCollectorService)
    {
        _eventAggregator = eventAggregator;
        _uiSharedService = uiSharedService;
        SizeConstraints = new()
        {
            MinimumSize = new(600, 500),
            MaximumSize = new(1000, 2000)
        };
        _filteredEvents = RecreateFilter();
    }

    private Lazy<List<Event>> RecreateFilter()
    {
        return new(() =>
            CurrentEvents.Where(f =>
                // Text-based filters
                (string.IsNullOrEmpty(_filterFreeText)
                || (f.EventSource.Contains(_filterFreeText, StringComparison.OrdinalIgnoreCase)
                    || f.Character.Contains(_filterFreeText, StringComparison.OrdinalIgnoreCase)
                    || f.UID.Contains(_filterFreeText, StringComparison.OrdinalIgnoreCase)
                    || f.Message.Contains(_filterFreeText, StringComparison.OrdinalIgnoreCase)
                ))
                &&
                (string.IsNullOrEmpty(_filterUid)
                    || (f.UID.Contains(_filterUid, StringComparison.OrdinalIgnoreCase))
                )
                &&
                (string.IsNullOrEmpty(_filterSource)
                    || (f.EventSource.Contains(_filterSource, StringComparison.OrdinalIgnoreCase))
                )
                &&
                (string.IsNullOrEmpty(_filterCharacter)
                    || (f.Character.Contains(_filterCharacter, StringComparison.OrdinalIgnoreCase))
                )
                &&
                (string.IsNullOrEmpty(_filterEvent)
                    || (f.Message.Contains(_filterEvent, StringComparison.OrdinalIgnoreCase))
                )
                &&
                // Category-based filters
                (GetEventCategory(f.EventSource) switch
                {
                    "Plugin" => _filterPlugin,
                    "Auth" => _filterAuth,
                    "Services" => _filterServices,
                    "Network" => _filterNetwork,
                    "File" => _filterFile,
                    _ => _filterOther
                })
                &&
                // Severity-based filters
                (f.EventSeverity switch
                {
                    EventSeverity.Informational => _filterInfo,
                    EventSeverity.Warning => _filterWarning,
                    EventSeverity.Error => _filterError,
                    _ => true
                })
             ).ToList());
    }
    
    private static string GetEventCategory(string eventSource)
    {
        return eventSource.ToLowerInvariant() switch
        {
            var source when source.Contains("plugin") || source.Contains("ui") || source.Contains("window") => "Plugin",
            var source when source.Contains("auth") || source.Contains("login") || source.Contains("token") => "Auth",
            var source when source.Contains("service") || source.Contains("manager") || source.Contains("controller") => "Services",
            var source when source.Contains("network") || source.Contains("connection") || source.Contains("signalr") || source.Contains("api") => "Network",
            var source when source.Contains("file") || source.Contains("cache") || source.Contains("download") || source.Contains("upload") => "File",
            _ => "Other"
        };
    }

    private void ClearFilters()
    {
        _filterFreeText = string.Empty;
        _filterCharacter = string.Empty;
        _filterUid = string.Empty;
        _filterSource = string.Empty;
        _filterEvent = string.Empty;
        
        // Reset category filters to show all
        _filterPlugin = true;
        _filterAuth = true;
        _filterServices = true;
        _filterNetwork = true;
        _filterFile = true;
        _filterOther = true;
        
        // Reset severity filters to show all
        _filterInfo = true;
        _filterWarning = true;
        _filterError = true;
        
        _filteredEvents = RecreateFilter();
    }
    
    private void CopyLogsToClipboard()
    {
        try
        {
            var logText = new StringBuilder();
            foreach (var ev in _filteredEvents.Value)
            {
                logText.AppendLine(ev.ToString());
            }
            
            if (logText.Length > 0)
            {
                // Use ImGui's built-in clipboard functionality
                ImGui.SetClipboardText(logText.ToString());
                // You could add a notification here if you have a notification system
            }
        }
        catch (Exception ex)
        {
            // Log error or show notification
            System.Diagnostics.Debug.WriteLine($"Failed to copy logs to clipboard: {ex.Message}");
        }
    }

    public override void OnOpen()
    {
        CurrentEvents = _eventAggregator.EventList.Value.OrderByDescending(f => f.EventTime).ToList();
        ClearFilters();
    }

    protected override void DrawInternal()
    {
        using (ImRaii.Disabled(!_eventAggregator.NewEventsAvailable))
        {
            if (_uiSharedService.IconTextButton(FontAwesomeIcon.ArrowsToCircle, "Refresh events"))
            {
                CurrentEvents = _eventAggregator.EventList.Value.OrderByDescending(f => f.EventTime).ToList();
            }
        }

        if (_eventAggregator.NewEventsAvailable)
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorTextWrapped("New events are available, press refresh to update", ImGuiColors.DalamudYellow);
        }

        // Add copy to clipboard button
        var copyButtonSize = _uiSharedService.GetIconTextButtonSize(FontAwesomeIcon.Copy, "Copy to Clipboard");
        var folderButtonSize = _uiSharedService.GetIconTextButtonSize(FontAwesomeIcon.FolderOpen, "Open EventLog Folder");
        var totalButtonWidth = copyButtonSize + folderButtonSize + ImGui.GetStyle().ItemSpacing.X;
        var dist = ImGui.GetWindowContentRegionMax().X - totalButtonWidth;
        
        ImGui.SameLine(dist);
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.Copy, "Copy to Clipboard"))
        {
            CopyLogsToClipboard();
        }
        UiSharedService.AttachToolTip("Copy filtered logs to clipboard");
        
        ImGui.SameLine();
        if (_uiSharedService.IconTextButton(FontAwesomeIcon.FolderOpen, "Open EventLog folder"))
        {
            ProcessStartInfo ps = new()
            {
                FileName = _eventAggregator.EventLogFolder,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process.Start(ps);
        }

        _uiSharedService.BigText("Last Events");
        var foldOut = ImRaii.TreeNode("Filter");
        if (foldOut)
        {
            if (_uiSharedService.IconTextButton(FontAwesomeIcon.Ban, "Clear Filters"))
            {
                ClearFilters();
            }
            
            bool changedFilter = false;
            
            // Text-based filters
            ImGui.Text("Text Filters:");
            ImGui.SetNextItemWidth(200);
            changedFilter |= ImGui.InputText("Search all columns", ref _filterFreeText, 50);
            ImGui.SetNextItemWidth(200);
            changedFilter |= ImGui.InputText("Filter by Source", ref _filterSource, 50);
            ImGui.SetNextItemWidth(200);
            changedFilter |= ImGui.InputText("Filter by UID", ref _filterUid, 50);
            ImGui.SetNextItemWidth(200);
            changedFilter |= ImGui.InputText("Filter by Character", ref _filterCharacter, 50);
            ImGui.SetNextItemWidth(200);
            changedFilter |= ImGui.InputText("Filter by Event", ref _filterEvent, 50);
            
            ImGui.Separator();
            
            // Category filters
            ImGui.Text("Categories:");
            ImGui.Columns(3, "CategoryColumns", false);
            changedFilter |= ImGui.Checkbox("Plugin", ref _filterPlugin);
            changedFilter |= ImGui.Checkbox("Auth", ref _filterAuth);
            changedFilter |= ImGui.Checkbox("Services", ref _filterServices);
            ImGui.NextColumn();
            changedFilter |= ImGui.Checkbox("Network", ref _filterNetwork);
            changedFilter |= ImGui.Checkbox("File", ref _filterFile);
            changedFilter |= ImGui.Checkbox("Other", ref _filterOther);
            ImGui.Columns(1);
            
            ImGui.Separator();
            
            // Severity filters
            ImGui.Text("Severity:");
            ImGui.Columns(3, "SeverityColumns", false);
            changedFilter |= ImGui.Checkbox("Info", ref _filterInfo);
            changedFilter |= ImGui.Checkbox("Warning", ref _filterWarning);
            changedFilter |= ImGui.Checkbox("Error", ref _filterError);
            ImGui.Columns(1);
            
            if (changedFilter) _filteredEvents = RecreateFilter();
        }
        foldOut.Dispose();

        var cursorPos = ImGui.GetCursorPosY();
        var max = ImGui.GetWindowContentRegionMax();
        var min = ImGui.GetWindowContentRegionMin();
        var width = max.X - min.X;
        var height = max.Y - cursorPos;
        using var table = ImRaii.Table("eventTable", 6, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg,
            new Vector2(width, height));
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Time");
            ImGui.TableSetupColumn("Source");
            ImGui.TableSetupColumn("UID");
            ImGui.TableSetupColumn("Character");
            ImGui.TableSetupColumn("Event");
            ImGui.TableHeadersRow();
            foreach (var ev in _filteredEvents.Value)
            {
                var icon = ev.EventSeverity switch
                {
                    EventSeverity.Informational => FontAwesomeIcon.InfoCircle,
                    EventSeverity.Warning => FontAwesomeIcon.ExclamationTriangle,
                    EventSeverity.Error => FontAwesomeIcon.Cross,
                    _ => FontAwesomeIcon.QuestionCircle
                };

                var iconColor = ev.EventSeverity switch
                {
                    EventSeverity.Informational => new Vector4(),
                    EventSeverity.Warning => ImGuiColors.DalamudYellow,
                    EventSeverity.Error => ImGuiColors.DalamudRed,
                    _ => new Vector4()
                };

                ImGui.TableNextColumn();
                _uiSharedService.IconText(icon, iconColor == new Vector4() ? null : iconColor);
                UiSharedService.AttachToolTip(ev.EventSeverity.ToString());
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(ev.EventTime.ToString("G", CultureInfo.CurrentCulture));
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(ev.EventSource);
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(string.IsNullOrEmpty(ev.UID) ? "--" : ev.UID);
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(string.IsNullOrEmpty(ev.Character) ? "--" : ev.Character);
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                var posX = ImGui.GetCursorPosX();
                var maxTextLength = ImGui.GetWindowContentRegionMax().X - posX;
                var textSize = ImGui.CalcTextSize(ev.Message).X;
                var msg = ev.Message;
                while (textSize > maxTextLength)
                {
                    msg = msg[..^5] + "...";
                    textSize = ImGui.CalcTextSize(msg).X;
                }
                ImGui.TextUnformatted(msg);
                if (!string.Equals(msg, ev.Message, StringComparison.Ordinal))
                {
                    UiSharedService.AttachToolTip(ev.Message);
                }
            }
        }
    }
}
