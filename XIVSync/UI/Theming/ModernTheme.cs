using System.Numerics;

namespace XIVSync.UI.Theming;

/// <summary>
/// Modern theme system with CSS-like design tokens for glassmorphism UI
/// Based on the redesign mockup with surface layers, opacity, and modern aesthetics
/// </summary>
public class ModernTheme
{
    // Opacity control for glassmorphism effect
    public float BackgroundOpacity { get; set; } = 0.40f;
    
    // Surface layers (using opacity from BackgroundOpacity)
    public Vector4 Surface0 { get; set; }
    public Vector4 Surface1 { get; set; }
    public Vector4 Surface2 { get; set; }
    public Vector4 Surface3 { get; set; }
    
    // Initialize surfaces based on opacity
    public void UpdateSurfaceOpacity()
    {
        Surface0 = new(0.035f, 0.039f, 0.055f, BackgroundOpacity);  // rgba(9, 10, 14, opacity)
        Surface1 = new(0.071f, 0.078f, 0.110f, BackgroundOpacity);  // rgba(18, 20, 28, opacity)
        Surface2 = new(0.110f, 0.118f, 0.165f, BackgroundOpacity);  // rgba(28, 30, 42, opacity)
        Surface3 = new(0.141f, 0.149f, 0.220f, BackgroundOpacity);  // rgba(36, 38, 56, opacity)
    }
    
    // Borders & separators
    public Vector4 Border => new(1f, 1f, 1f, 0.08f);        // rgba(255,255,255,.08)
    public Vector4 Separator => new(1f, 1f, 1f, 0.07f);     // rgba(255,255,255,.07)
    
    // Text colors
    public Vector4 TextPrimary { get; set; } = new(0.914f, 0.929f, 0.945f, 1f);   // #e9edf1
    public Vector4 TextMuted { get; set; } = new(0.604f, 0.639f, 0.678f, 1f);     // #9aa3ad
    public Vector4 TextMuted2 { get; set; } = new(0.420f, 0.459f, 0.502f, 1f);    // #6b7580
    
    // Accent colors (customizable per theme)
    public Vector4 Accent { get; set; } = new(0.290f, 0.424f, 0.969f, 1f);        // #4a6cf7 (default blue)
    public Vector4 Accent2 { get; set; } = new(0.608f, 0.694f, 1f, 1f);           // #9bb1ff
    
    // Status colors
    public Vector4 StatusOk => new(0.212f, 0.773f, 0.416f, 1f);      // #36c56a
    public Vector4 StatusWarn => new(1f, 0.690f, 0.125f, 1f);        // #ffb020
    public Vector4 StatusError => new(1f, 0.322f, 0.322f, 1f);       // #ff5252
    public Vector4 StatusPaused => new(1f, 0.420f, 0.208f, 1f);      // #ff6b35
    public Vector4 StatusInfo => new(0.239f, 0.694f, 1f, 1f);        // #3db1ff
    
    // Radius values (scaled appropriately for ImGui)
    public float RadiusSmall => 8f;
    public float RadiusMedium => 14f;
    public float RadiusLarge => 20f;
    
    // Spacing values
    public float SpacingXS => 6f;
    public float SpacingS => 10f;
    public float SpacingM => 14f;
    public float SpacingL => 18f;
    
    // Theme identification
    public string Name { get; set; } = "Default";
    public string DisplayName { get; set; } = "Default Blue";
    
    // Available themes
    public static readonly Dictionary<string, ModernTheme> Themes = new()
    {
        ["default"] = new ModernTheme
        {
            Name = "default",
            DisplayName = "Default Blue",
            Accent = new(0.290f, 0.424f, 0.969f, 1f),        // #4a6cf7
            Accent2 = new(0.608f, 0.694f, 1f, 1f),           // #9bb1ff
            BackgroundOpacity = 0.40f
        },
        ["green"] = new ModernTheme
        {
            Name = "green",
            DisplayName = "Forest Green",
            Accent = new(0.212f, 0.773f, 0.416f, 1f),        // #36c56a
            Accent2 = new(0.416f, 0.886f, 0.608f, 1f),       // #6ae29b
            BackgroundOpacity = 0.40f
        },
        ["red"] = new ModernTheme
        {
            Name = "red",
            DisplayName = "Crimson Red",
            Accent = new(1f, 0.322f, 0.322f, 1f),            // #ff5252
            Accent2 = new(1f, 0.608f, 0.608f, 1f),           // #ff9b9b
            BackgroundOpacity = 0.40f
        },
        ["purple"] = new ModernTheme
        {
            Name = "purple",
            DisplayName = "Royal Purple",
            Accent = new(0.690f, 0.322f, 1f, 1f),            // #b052ff
            Accent2 = new(0.835f, 0.608f, 1f, 1f),           // #d59bff
            BackgroundOpacity = 0.40f
        },
        ["orange"] = new ModernTheme
        {
            Name = "orange",
            DisplayName = "Sunset Orange",
            Accent = new(1f, 0.420f, 0.208f, 1f),            // #ff6b35
            Accent2 = new(1f, 0.690f, 0.125f, 1f),           // #ffb020
            BackgroundOpacity = 0.40f
        },
        ["light"] = new ModernTheme
        {
            Name = "light",
            DisplayName = "Light Theme",
            Accent = new(0.239f, 0.694f, 1f, 1f),            // #3db1ff
            Accent2 = new(0.608f, 0.835f, 1f, 1f),           // #9bd5ff
            TextPrimary = new(0.125f, 0.125f, 0.125f, 1f),   // #202020
            TextMuted = new(0.420f, 0.420f, 0.420f, 1f),     // #6b6b6b
            TextMuted2 = new(0.604f, 0.604f, 0.604f, 1f),    // #9a9a9a
            BackgroundOpacity = 0.85f
        }
    };
    
    // Initialize default theme
    public ModernTheme()
    {
        UpdateSurfaceOpacity();
    }
    
    // Apply theme-specific surface colors
    public static void ApplyThemeColors(ModernTheme theme)
    {
        switch (theme.Name)
        {
            case "green":
                theme.Surface0 = new(0.012f, 0.020f, 0.024f, theme.BackgroundOpacity);
                theme.Surface1 = new(0.024f, 0.039f, 0.031f, theme.BackgroundOpacity);
                theme.Surface2 = new(0.031f, 0.063f, 0.047f, theme.BackgroundOpacity);
                theme.Surface3 = new(0.047f, 0.086f, 0.063f, theme.BackgroundOpacity);
                break;
                
            case "red":
                theme.Surface0 = new(0.024f, 0f, 0f, theme.BackgroundOpacity);
                theme.Surface1 = new(0.063f, 0.008f, 0.008f, theme.BackgroundOpacity);
                theme.Surface2 = new(0.094f, 0.024f, 0.024f, theme.BackgroundOpacity);
                theme.Surface3 = new(0.141f, 0.039f, 0.039f, theme.BackgroundOpacity);
                break;
                
            case "light":
                theme.Surface0 = new(0.984f, 0.988f, 1f, theme.BackgroundOpacity);
                theme.Surface1 = new(0.961f, 0.969f, 0.988f, theme.BackgroundOpacity);
                theme.Surface2 = new(0.933f, 0.945f, 0.965f, theme.BackgroundOpacity);
                theme.Surface3 = new(0.902f, 0.922f, 0.953f, theme.BackgroundOpacity);
                break;
                
            default:
                theme.UpdateSurfaceOpacity();
                break;
        }
    }
}