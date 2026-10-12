using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Titanium.Application.Interfaces;

namespace Titanium.Application.Services;

public partial class CommunicationNormalizer : ICommunicationNormalizer
{
    public string NormalizeBody(string? bodyHtml, string? bodyText)
    {
        string rawText;
        if (!string.IsNullOrWhiteSpace(bodyHtml))
        {
            rawText = HtmlToPlainText(bodyHtml);
        }
        else if (!string.IsNullOrWhiteSpace(bodyText))
        {
            rawText = ContainsHtml(bodyText) ? HtmlToPlainText(bodyText) : bodyText;
        }
        else
        {
            return string.Empty;
        }

        var withoutHistory = StripQuotedEmailHistory(rawText);
        var withoutSignatures = StripSignatures(withoutHistory);
        return withoutSignatures.Trim();
    }

    private static bool ContainsHtml(string text)
    {
        return Regex.IsMatch(text, @"<(!DOCTYPE|html|head|body|p|div|br|table|tr|td|span|a|font|style|script|xml)\b", RegexOptions.IgnoreCase);
    }

    public string StripQuotedEmailHistory(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText)) return string.Empty;

        var lines = bodyText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Check for quoted header indicators
            if (trimmed.StartsWith("-----Original Message-----", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("________________________________", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("From:", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("Sent:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("On ", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(" wrote:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(">"))
            {
                break; // Stop at first quoted email boundary to preserve modern message content
            }

            cleanLines.Add(line);
        }

        return string.Join(Environment.NewLine, cleanLines);
    }

    public string StripSignatures(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText)) return string.Empty;

        var signatureDelimiters = new[]
        {
            "--", "-- ", "Regards,", "Regards", "Best regards,", "Best regards",
            "Thanks & Regards,", "Thanks & Regards", "Sincerely,", "Sincerely",
            "Kind regards,", "Kind regards", "Warm regards,", "Warm regards",
            "Cheers,", "Cheers", "Yours truly,", "Yours sincerely,",
            "Sent from my iPhone", "Sent from my iPad", "Sent from Outlook",
            "Get Outlook for iOS", "Get Outlook for Android", "Sent from Mail for Windows"
        };
        var lines = bodyText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            bool isSignature = false;
            foreach (var sig in signatureDelimiters)
            {
                if (trimmed.Equals(sig, StringComparison.OrdinalIgnoreCase))
                {
                    isSignature = true;
                    break;
                }
            }

            if (isSignature)
            {
                break;
            }

            cleanLines.Add(line);
        }

        return string.Join(Environment.NewLine, cleanLines);
    }

    public string ComputeContentHash(string content)
    {
        var normalized = (content ?? string.Empty).Trim().Replace("\r\n", "\n");
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // 1. Remove HTML comments (standard, multi-line)
        var text = Regex.Replace(html, @"<!--[\s\S]*?-->", string.Empty);

        // 2. Remove Microsoft Office conditional comments (e.g., <![if !supportLists]>...<![endif]>)
        text = Regex.Replace(text, @"<!\[if[\s\S]*?!\[endif\]>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<!\[endif\]>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<!\[if[^\]]*\]>", string.Empty, RegexOptions.IgnoreCase);

        // 3. Remove DOCTYPE declarations
        text = Regex.Replace(text, @"<!DOCTYPE[^>]*>", string.Empty, RegexOptions.IgnoreCase);

        // 4. Remove script, style, head, xml, title, svg, canvas, noscript, form, applet, object, embed blocks with contents
        text = Regex.Replace(text, @"<(script|style|head|xml|title|svg|canvas|noscript|form|applet|object|embed)[^>]*?>[\s\S]*?</\1>", string.Empty, RegexOptions.IgnoreCase);
        // Handle unclosed <style> or <script> tags
        text = Regex.Replace(text, @"<style[^>]*?>[\s\S]*", string.Empty, RegexOptions.IgnoreCase);

        // 5. Remove any loose CSS blocks (@media, @font-face, @page, CSS rule selectors)
        text = Regex.Replace(text, @"@(media|font-face|page|keyframes|import|charset|namespace)[^{]*\{[\s\S]*?\}", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?:\r?\n|^)[ \t]*[.#a-zA-Z0-9_\-:\s,>+~*\[\]=""'^$*]+\{[\s\S]*?\}", string.Empty);

        // 6. Handle paragraph, block elements, and line breaks
        text = Regex.Replace(text, @"<(br|BR)\s*/?>", Environment.NewLine);
        text = Regex.Replace(text, @"</(p|div|tr|h[1-6]|table|blockquote|header|footer|section|article|aside|nav|P|DIV|TR|H[1-6])>", Environment.NewLine);
        text = Regex.Replace(text, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</li>", Environment.NewLine, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</td>", "\t", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<hr[^>]*>", Environment.NewLine, RegexOptions.IgnoreCase);

        // 7. Strip tags and decode entities in a loop to handle nested or encoded tags
        for (int i = 0; i < 3; i++)
        {
            var prev = text;
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);
            if (text == prev) break;
        }
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);

        // 8. Clean up non-breaking spaces, zero-width spaces, and invisible unicode artifacts
        text = text.Replace('\u00A0', ' ')
                   .Replace('\u200B', ' ')
                   .Replace('\u200C', ' ')
                   .Replace('\u200D', ' ')
                   .Replace('\uFEFF', ' ')
                   .Replace('\u00AD', ' ');

        // 9. Filter out lines that are pure CSS property declarations, class names, or link tracking artifacts
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var filteredLines = new List<string>();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Reject standalone CSS property lines or class rules
            if ((Regex.IsMatch(line, @"^[a-zA-Z\-]+\s*:\s*[^;\r\n]+;?\s*$") &&
                 (line.Contains("margin") || line.Contains("font") || line.Contains("color") ||
                  line.Contains("padding") || line.Contains("border") || line.Contains("display") ||
                  line.Contains("width") || line.Contains("height") || line.Contains("align") ||
                  line.Contains("mso-") || line.Contains("background") || line.Contains("line-height"))) ||
                Regex.IsMatch(line, @"^[.#]?[a-zA-Z0-9_\-,\s>]+\s*\{?$") ||
                line.StartsWith("/*") || line.EndsWith("*/"))
            {
                continue;
            }

            // Reject standalone URLs
            if (Regex.IsMatch(line, @"^(https?://|www\.)\S+$", RegexOptions.IgnoreCase))
            {
                continue;
            }

            filteredLines.Add(rawLine);
        }

        var result = string.Join(Environment.NewLine, filteredLines);

        // 10. Collapse excess consecutive blank lines
        result = Regex.Replace(result, @"(\r?\n[ \t]*){3,}", $"{Environment.NewLine}{Environment.NewLine}");

        return result.Trim();
    }
}
