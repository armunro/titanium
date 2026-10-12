using Titanium.Application.Services;
using Xunit;

namespace Titanium.Application.Tests;

public class CommunicationNormalizerTests
{
    private readonly CommunicationNormalizer _normalizer = new();

    [Fact]
    public void StripQuotedEmailHistory_ShouldRemoveThreadHistory()
    {
        var rawEmail = "I agree to pick up the kids at 4pm on Friday.\r\n\r\n-----Original Message-----\r\nFrom: Bob\r\nSent: Monday\r\nCan you pick up the kids?";
        var cleaned = _normalizer.StripQuotedEmailHistory(rawEmail);

        Assert.Equal("I agree to pick up the kids at 4pm on Friday.", cleaned.Trim());
    }

    [Fact]
    public void StripSignatures_ShouldRemoveSignature()
    {
        var raw = "Please review the attached settlement agreement.\r\n\r\nBest regards,\r\nJohn Doe, Esq.";
        var cleaned = _normalizer.StripSignatures(raw);

        Assert.Equal("Please review the attached settlement agreement.", cleaned.Trim());
    }

    [Fact]
    public void ComputeContentHash_ShouldBeDeterministic()
    {
        var hash1 = _normalizer.ComputeContentHash("The schedule was confirmed.");
        var hash2 = _normalizer.ComputeContentHash("The schedule was confirmed.");

        Assert.Equal(hash1, hash2);
        Assert.False(string.IsNullOrEmpty(hash1));
    }

    [Fact]
    public void NormalizeBody_ShouldStripHtmlTagsAndStyles_WhenHtmlProvided()
    {
        var html = @"<html>
<head>
<style>
p.MsoNormal { margin: 0cm; font-size: 11.0pt; }
</style>
</head>
<body>
<p class=""MsoNormal"">I agree to the proposed visitation dates for next week.&nbsp;</p>
<p class=""MsoNormal""><br></p>
<p class=""MsoNormal"">Best regards,<br>Alice</p>
</body>
</html>";

        var normalized = _normalizer.NormalizeBody(html, null);

        Assert.Equal("I agree to the proposed visitation dates for next week.", normalized);
        Assert.DoesNotContain("MsoNormal", normalized);
        Assert.DoesNotContain("<style>", normalized);
        Assert.DoesNotContain("&nbsp;", normalized);
    }

    [Fact]
    public void HtmlToPlainText_ShouldStripConditionalCommentsAndOfficeXml()
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
<!--[if !mso]><style>v\:* {behavior:url(#default#VML);}</style><![endif]-->
<!--[if gte mso 9]><xml><o:OfficeDocumentSettings><o:AllowPNG/></o:OfficeDocumentSettings></xml><![endif]-->
<style>
@media only screen and (max-width: 600px) { body { font-size: 14px; } }
div.content { margin: 10px; color: #333333; }
</style>
</head>
<body>
<![if !supportLists]><span>1. </span><![endif]>
<div class=""content"">
<p>We confirm that the payment has been sent.</p>
</div>
</body>
</html>";

        var plainText = _normalizer.HtmlToPlainText(html);

        Assert.Contains("We confirm that the payment has been sent.", plainText);
        Assert.DoesNotContain("behavior:url", plainText);
        Assert.DoesNotContain("OfficeDocumentSettings", plainText);
        Assert.DoesNotContain("@media", plainText);
        Assert.DoesNotContain("color: #333333", plainText);
        Assert.DoesNotContain("<![if", plainText);
        Assert.DoesNotContain("<![endif]", plainText);
    }
}
