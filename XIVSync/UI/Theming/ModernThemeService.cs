using XIVSync.MareConfiguration;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace XIVSync.UI.Theming;

/// <summary>
/// Service for managing modern themes
/// </summary>
public class ModernThemeService
{
    private readonly ILogger<ModernThemeService> _logger;
    private readonly MareConfigService _configService;
    private ModernTheme _currentTheme;
    
    // Events for theme changes
    public event Action<ModernTheme>? ThemeChanged;
    
    public ModernThemeService(ILogger<ModernThemeService> logger, MareConfigService configService)
    {
        _logger = logger;
        _configService = configService;
        
        // Initialize with default theme
        _currentTheme = ModernTheme.Themes["default"];
        
        // Load user preferences
        LoadThemeSettings();
    }
    
    public ModernTheme CurrentTheme => _currentTheme;
    
    /// <summary>
    /// Get all available theme names
    /// </summary>
    public IEnumerable<string> GetAvailableThemes() => ModernTheme.Themes.Keys;
    
    /// <summary>
    /// Get theme display names for UI (includes converted legacy themes)
    /// </summary>
    public Dictionary<string, string> GetThemeDisplayNames() => 
        GetAllAvailableThemes().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DisplayName);
    
    /// <summary>
    /// Get all available themes including converted legacy themes
    /// </summary>
    public Dictionary<string, ModernTheme> GetAllAvailableThemes()
    {
        var allThemes = new Dictionary<string, ModernTheme>(ModernTheme.Themes);
        
        // Convert legacy themes from ThemePresets
        foreach (var legacyTheme in ThemePresets.Presets)
        {
            if (!allThemes.ContainsKey(legacyTheme.Key.ToLowerInvariant()))
            {
                var convertedTheme = ConvertLegacyTheme(legacyTheme.Key, legacyTheme.Value);
                allThemes[legacyTheme.Key.ToLowerInvariant()] = convertedTheme;
            }
        }
        
        return allThemes;
    }
    
    /// <summary>
    /// Convert a legacy ThemePalette to ModernTheme
    /// </summary>
    private ModernTheme ConvertLegacyTheme(string name, ThemePalette legacy)
    {
        var modern = new ModernTheme
        {
            Name = name.ToLowerInvariant(),
            DisplayName = name,
            BackgroundOpacity = legacy.PanelBg.W, // Use alpha from panel background
            
            // Convert colors
            TextPrimary = legacy.TextPrimary,
            TextMuted = legacy.TextSecondary,
            TextMuted2 = legacy.TextDisabled,
            Accent = legacy.Accent,
            Accent2 = legacy.Link,
        };
        
        // Convert surface colors from legacy panel colors
        modern.Surface0 = new Vector4(legacy.PanelBg.X * 0.8f, legacy.PanelBg.Y * 0.8f, legacy.PanelBg.Z * 0.8f, modern.BackgroundOpacity);
        modern.Surface1 = new Vector4(legacy.PanelBg.X, legacy.PanelBg.Y, legacy.PanelBg.Z, modern.BackgroundOpacity);
        modern.Surface2 = new Vector4(legacy.HeaderBg.X, legacy.HeaderBg.Y, legacy.HeaderBg.Z, modern.BackgroundOpacity);
        modern.Surface3 = new Vector4(legacy.Btn.X, legacy.Btn.Y, legacy.Btn.Z, modern.BackgroundOpacity);
        
        return modern;
    }
    
    /// <summary>
    /// Switch to a different theme (supports both modern and legacy themes)
    /// </summary>
    public void SetTheme(string themeName)
    {
        var allThemes = GetAllAvailableThemes();
        var themeKey = themeName.ToLowerInvariant();
        
        if (!allThemes.TryGetValue(themeKey, out var theme))
        {
            _logger.LogWarning("Theme '{ThemeName}' not found, using default", themeName);
            theme = ModernTheme.Themes["default"];
        }
        
        _currentTheme = theme;
        SaveThemeSettings();
        ThemeChanged?.Invoke(_currentTheme);
        
        _logger.LogInformation("Switched to theme: {ThemeName}", theme.DisplayName);
    }
    
    /// <summary>
    /// Set background opacity (0.0 to 1.0)
    /// </summary>
    public void SetBackgroundOpacity(float opacity)
    {
        _currentTheme.BackgroundOpacity = Math.Clamp(opacity, 0.0f, 1.0f);
        _currentTheme.UpdateSurfaceOpacity();
        SaveThemeSettings();
        ThemeChanged?.Invoke(_currentTheme);
    }
    
    /// <summary>
    /// Load theme settings from configuration
    /// </summary>
    private void LoadThemeSettings()
    {
        try
        {
            var config = _configService.Current;
            
            // Load theme name
            if (!string.IsNullOrEmpty(config.ModernThemeName))
            {
                var allThemes = GetAllAvailableThemes();
                if (allThemes.TryGetValue(config.ModernThemeName.ToLowerInvariant(), out var theme))
                {
                    _currentTheme = theme;
                }
            }
            
            // Load opacity
            if (config.ModernThemeOpacity > 0)
            {
                _currentTheme.BackgroundOpacity = config.ModernThemeOpacity;
            }
            
            _logger.LogInformation("Loaded theme settings: {ThemeName}, opacity: {Opacity}",
                _currentTheme.DisplayName, _currentTheme.BackgroundOpacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load theme settings, using defaults");
        }
    }
    
    /// <summary>
    /// Save theme settings to configuration
    /// </summary>
    private void SaveThemeSettings()
    {
        try
        {
            var config = _configService.Current;
            config.ModernThemeName = _currentTheme.Name;
            config.ModernThemeOpacity = _currentTheme.BackgroundOpacity;
            _configService.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme settings");
        }
    }
}