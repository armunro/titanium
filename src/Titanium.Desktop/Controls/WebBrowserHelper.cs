using System.Windows;
using System.Windows.Controls;

namespace Titanium.Desktop.Controls;

public static class WebBrowserHelper
{
    public static readonly DependencyProperty HtmlProperty =
        DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(WebBrowserHelper),
            new FrameworkPropertyMetadata(OnHtmlChanged));

    public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
    public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebBrowser browser)
        {
            var html = e.NewValue as string;
            if (!string.IsNullOrWhiteSpace(html))
            {
                try
                {
                    browser.NavigateToString(html);
                }
                catch
                {
                    // Fallback in case of malformed html string
                }
            }
            else
            {
                browser.NavigateToString("<html><body style=\"background-color:#1E1E1E;color:#888888;font-family:Segoe UI,sans-serif;padding:12px;\"><p>No HTML body available.</p></body></html>");
            }
        }
    }
}
