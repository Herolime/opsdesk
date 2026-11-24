using System.Net;
using System.Text.RegularExpressions;

namespace OpsDesk.Common.Helpers;

public static partial class RichTextContentHelper
{
    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var decoded = WebUtility.HtmlDecode(HtmlTagRegex().Replace(html, " "));
        return string.Join(' ', decoded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    public static bool HasMeaningfulContent(string? html) =>
        !string.IsNullOrWhiteSpace(StripHtml(html));
}
