using MudBlazor;

namespace OpsDesk.Web.Theme;

public static class OpsDeskTheme
{
    public static MudTheme CreateTheme() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#10B981",          // Emerald
            PrimaryContrastText = "#042F2E",
            Secondary = "#38BDF8",        // Sky
            Tertiary = "#A78BFA",         // Violet
            Dark = "#0B1120",
            Background = "#0F172A",       // Obsidian Slate
            BackgroundGray = "#0B1120",
            Surface = "#1E293B",          // Elevated Slate
            AppbarBackground = "#0B1120",
            AppbarText = "#F8FAFC",
            DrawerBackground = "#0B1120",
            DrawerText = "#CBD5E1",
            DrawerIcon = "#94A3B8",
            TextPrimary = "#F8FAFC",
            TextSecondary = "#94A3B8",
            TextDisabled = "#64748B",
            ActionDefault = "#10B981",
            ActionDisabled = "#475569",
            Success = "#10B981",
            Warning = "#F59E0B",
            Error = "#F43F5E",
            Info = "#38BDF8",
            Divider = "#334155",
            LinesDefault = "#334155",
            TableLines = "#334155",
            OverlayDark = "rgba(2, 6, 23, 0.72)"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "system-ui", "-apple-system", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            H1 = new H1Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "2rem", FontWeight = "700", LineHeight = "1.2" },
            H2 = new H2Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "1.625rem", FontWeight = "700", LineHeight = "1.25" },
            H3 = new H3Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "1.375rem", FontWeight = "600", LineHeight = "1.3" },
            H4 = new H4Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.35" },
            H5 = new H5Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            H6 = new H6Typography { FontFamily = ["Geist", "Inter", "sans-serif"], FontSize = "0.9375rem", FontWeight = "600", LineHeight = "1.4" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px", // ROUND_EIGHT
            AppbarHeight = "64px",
            DrawerWidthLeft = "260px"
        }
    };
}
