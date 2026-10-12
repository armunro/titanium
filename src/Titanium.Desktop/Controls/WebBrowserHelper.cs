using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Titanium.Desktop.Controls;

public static class WebBrowserHelper
{
    private const string BlankDarkHtml = "<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" /><style type=\"text/css\">html, body { background-color: #1E1E1E !important; background: #1E1E1E !important; color: #888888 !important; font-family: 'Segoe UI', Tahoma, Arial, sans-serif !important; margin: 0; padding: 16px; }</style></head><body><p>No communication selected.</p></body></html>";

    public static readonly DependencyProperty HtmlProperty =
        DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(WebBrowserHelper),
            new FrameworkPropertyMetadata(null, OnHtmlChanged));

    public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
    public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebBrowser browser)
        {
            // Attach Loaded handler to ensure initial navigation happens once the browser control is ready
            browser.Loaded -= Browser_Loaded;
            browser.Loaded += Browser_Loaded;

            RenderContent(browser, e.NewValue as string);
        }
    }

    private static void Browser_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is WebBrowser browser)
        {
            var html = GetHtml(browser);
            RenderContent(browser, html);
        }
    }

    private static void RenderContent(WebBrowser browser, string? html)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(html))
            {
                var darkThemedHtml = ApplyDarkTheme(html);
                browser.NavigateToString(darkThemedHtml);
            }
            else
            {
                browser.NavigateToString(BlankDarkHtml);
            }
        }
        catch
        {
            // Fallback in case of navigation error
        }
    }

    public static string ApplyDarkTheme(string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return "<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" /></head><body style=\"background-color:#1E1E1E;color:#D4D4D4;font-family:Segoe UI,sans-serif;padding:12px;\"></body></html>";

        // If it's plain text without tags, wrap in preformatted dark container
        if (!rawHtml.Contains('<') && !rawHtml.Contains('>'))
        {
            var encoded = WebUtility.HtmlEncode(rawHtml);
            return $"<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" /></head><body style=\"background-color:#1E1E1E;color:#D4D4D4;font-family:Segoe UI,sans-serif;padding:12px;\"><pre style=\"white-space:pre-wrap;font-family:'Segoe UI',sans-serif;color:#D4D4D4;\">{encoded}</pre></body></html>";
        }

        // 1. Strip bgcolor and background attributes on tags (e.g. <body bgcolor="#fff">, <td bgcolor="white">)
        var sanitized = Regex.Replace(rawHtml, @"\s+bgcolor=(?:""[^""]*""|'[^']*'|[^\s>]+)", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\s+background=(?:""[^""]*""|'[^']*'|[^\s>]+)", "", RegexOptions.IgnoreCase);

        // 2. Strip color attribute on <font> or other tags
        sanitized = Regex.Replace(sanitized, @"\s+color=(?:""[^""]*""|'[^']*'|[^\s>]+)", "", RegexOptions.IgnoreCase);

        // 3. Remove inline color, background, and background-color styles from style="..." attributes so CSS stylesheet takes priority
        sanitized = Regex.Replace(sanitized, @"style=""([^""]*)""", m =>
        {
            var styleContent = m.Groups[1].Value;
            var cleanedStyle = Regex.Replace(styleContent, @"\b(?:background|background-color|color)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            return $"style=\"{cleanedStyle}\"";
        }, RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(sanitized, @"style='([^']*)'", m =>
        {
            var styleContent = m.Groups[1].Value;
            var cleanedStyle = Regex.Replace(styleContent, @"\b(?:background|background-color|color)\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            return $"style='{cleanedStyle}'";
        }, RegexOptions.IgnoreCase);

        const string headInjection = @"
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
<style type=""text/css"">
    html, body {
        background-color: #1E1E1E !important;
        background: #1E1E1E !important;
        color: #D4D4D4 !important;
        font-family: 'Segoe UI', Tahoma, Arial, sans-serif !important;
        margin: 8px !important;
        padding: 0 !important;
    }
    * {
        color: #D4D4D4 !important;
        background-color: transparent !important;
    }
    table, tbody, tr, td, th, div, p, span, li, ul, ol, section, article, header, footer, center, font, pre, code {
        background-color: transparent !important;
        background: transparent !important;
        color: #D4D4D4 !important;
    }
    a, a * {
        color: #4EC9B0 !important;
        text-decoration: underline !important;
    }
    blockquote {
        border-left: 3px solid #3F3F46 !important;
        padding-left: 8px !important;
        margin-left: 0 !important;
        color: #9CDCFE !important;
        background-color: transparent !important;
    }
    table, th, td {
        border-color: #3F3F46 !important;
    }
    hr {
        border: none !important;
        border-top: 1px solid #3F3F46 !important;
    }
    img {
        max-width: 100% !important;
        height: auto !important;
    }
</style>
";

        if (Regex.IsMatch(sanitized, @"<head\b[^>]*>", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(sanitized, @"<head\b[^>]*>", m => m.Value + "\n" + headInjection, RegexOptions.IgnoreCase);
        }

        if (Regex.IsMatch(sanitized, @"<html\b[^>]*>", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(sanitized, @"<html\b[^>]*>", m => m.Value + "\n<head>" + headInjection + "</head>", RegexOptions.IgnoreCase);
        }

        return "<!DOCTYPE html><html><head>" + headInjection + "</head><body>" + sanitized + "</body></html>";
    }
}
