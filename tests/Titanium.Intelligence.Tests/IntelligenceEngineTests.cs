using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Intelligence.Classification;
using Titanium.Intelligence.Contradictions;
using Titanium.Intelligence.Extraction;
using Xunit;

namespace Titanium.Intelligence.Tests;

public class IntelligenceEngineTests
{
    [Fact]
    public async Task StatementExtractor_ShouldExtractStatements()
    {
        var extractor = new RuleBasedStatementExtractor();
        var comm = new Communication
        {
            Sender = "Alice",
            BodyText = "I agree to the parenting schedule. I will pick up the children at 5 PM on Monday."
        };

        var statements = await extractor.ExtractAsync(comm);

        Assert.Equal(2, statements.Count);
        Assert.Equal(StatementType.Agreement, statements[0].StatementType);
        Assert.Equal(StatementType.Commitment, statements[1].StatementType);
    }

    [Fact]
    public void ContradictionAnalyzer_ShouldDetectOpposingAssertions()
    {
        var claimA = new Claim
        {
            Subject = "Alice",
            NormalizedAssertion = "I approved the proposed schedule",
            Polarity = ClaimPolarity.Positive
        };

        var claimB = new Claim
        {
            Subject = "Alice",
            NormalizedAssertion = "I never approved the proposed schedule",
            Polarity = ClaimPolarity.Negative
        };

        var rel = EvidenceBasedClaimRelationshipAnalyzer.EvaluateRelationship(claimA, claimB);

        Assert.NotNull(rel);
        Assert.Equal(ClaimRelationshipType.ChangesPosition, rel.RelationshipType);
    }

    [Fact]
    public void ContradictionAnalyzer_ShouldDistinguishChangedCircumstancesFromContradiction()
    {
        var claimA = new Claim
        {
            Subject = "Alice",
            NormalizedAssertion = "I will pick up the kids at 5 PM",
            Polarity = ClaimPolarity.Positive
        };

        var claimB = new Claim
        {
            Subject = "Alice",
            NormalizedAssertion = "I cannot make 5 PM because of car broke emergency",
            Polarity = ClaimPolarity.Negative
        };

        var rel = EvidenceBasedClaimRelationshipAnalyzer.EvaluateRelationship(claimA, claimB);

        Assert.NotNull(rel);
        Assert.Equal(ClaimRelationshipType.Qualifies, rel.RelationshipType);
    }

    [Fact]
    public async Task StatementExtractor_ShouldCleanResidualHtmlAndCssArtifacts()
    {
        var extractor = new RuleBasedStatementExtractor();
        var comm = new Communication
        {
            Sender = "Alice",
            BodyText = "<p>I agree to the schedule.</p><style>p.MsoNormal { margin: 0; }</style>"
        };

        var statements = await extractor.ExtractAsync(comm);

        Assert.Single(statements);
        Assert.Equal("I agree to the schedule.", statements[0].OriginalText);
        Assert.Equal(StatementType.Agreement, statements[0].StatementType);
    }

    [Fact]
    public async Task ClaimNormalizer_ShouldStripHtmlTagsFromNormalizedClaims()
    {
        var normalizer = new RuleBasedClaimNormalizer();
        var statements = new List<Statement>
        {
            new Statement
            {
                SpeakerName = "Alice",
                OriginalText = "<span>I will pay <b>$500</b> for the expenses.</span>",
                StatementType = StatementType.Commitment
            }
        };

        var claims = await normalizer.NormalizeAsync(statements);

        Assert.Single(claims);
        Assert.Equal("I will pay $500 for the expenses", claims[0].NormalizedAssertion);
        Assert.DoesNotContain("<", claims[0].NormalizedAssertion);
        Assert.DoesNotContain(">", claims[0].NormalizedAssertion);
    }

    [Fact]
    public async Task StatementExtractor_ComplexOutlookHtml_ExtractsOnlyActualStatements()
    {
        var extractor = new RuleBasedStatementExtractor();
        var html = @"<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" xmlns:w=""urn:schemas-microsoft-com:office:word"">
<head>
<!--[if !mso]><style>
v\:* {behavior:url(#default#VML);}
o\:* {behavior:url(#default#VML);}
.shape {behavior:url(#default#VML);}
</style><![endif]-->
<style>
/* Font Definitions */
@font-face {font-family:""Cambria Math""; panose-1:2 4 5 3 5 4 6 3 2 4;}
@font-face {font-family:Calibri; panose-1:2 15 5 2 2 2 4 3 2 4;}
/* Style Definitions */
p.MsoNormal, li.MsoNormal, div.MsoNormal {margin:0cm; font-size:11.0pt; font-family:""Calibri"",sans-serif;}
a:link, span.MsoHyperlink {color:blue; text-decoration:underline;}
.WordSection1 {page:WordSection1;}
</style>
<!--[if gte mso 9]><xml>
<o:OfficeDocumentSettings><o:AllowPNG/></o:OfficeDocumentSettings>
</xml><![endif]-->
</head>
<body lang=""EN-US"">
<div class=""WordSection1"">
<p class=""MsoNormal"">Hi Bob,</p>
<p class=""MsoNormal"">&nbsp;</p>
<p class=""MsoNormal"">I will transfer the $1,200 payment on Monday morning.<o:p></o:p></p>
<p class=""MsoNormal"">We agreed to the 50/50 split during our last meeting.<o:p></o:p></p>
<p class=""MsoNormal"">&nbsp;</p>
<p class=""MsoNormal"">Thanks,<br>Alice</p>
<p class=""MsoNormal""><span style=""font-size:8.0pt;color:#7F7F7F"">CONFIDENTIALITY NOTICE: This transmission is intended only for the use of the individual named above.</span></p>
</div>
</body>
</html>";

        var comm = new Communication
        {
            Sender = "Alice",
            BodyHtml = html,
            BodyText = null
        };

        var statements = await extractor.ExtractAsync(comm);

        Assert.Equal(2, statements.Count);
        Assert.Equal("I will transfer the $1,200 payment on Monday morning.", statements[0].OriginalText);
        Assert.Equal(StatementType.Commitment, statements[0].StatementType);
        Assert.Equal("We agreed to the 50/50 split during our last meeting.", statements[1].OriginalText);
        Assert.Equal(StatementType.Agreement, statements[1].StatementType);

        var normalizer = new RuleBasedClaimNormalizer();
        var claims = await normalizer.NormalizeAsync(statements);
        Assert.Equal(2, claims.Count);
        Assert.Equal("I will transfer the $1,200 payment on Monday morning", claims[0].NormalizedAssertion);
        Assert.Equal("We agreed to the 50/50 split during our last meeting", claims[1].NormalizedAssertion);
    }

    [Fact]
    public async Task StatementExtractor_ExtractsCleanTextFirst_FromRawHtmlInBodyText()
    {
        var extractor = new RuleBasedStatementExtractor();
        var comm = new Communication
        {
            Sender = "Bob",
            BodyText = "<style type=\"text/css\">body { color: #333; } .header { font-size: 14px; }</style><div>&lt;p&gt;I promise to deliver the signed agreement by Friday.&lt;/p&gt;</div>"
        };

        var statements = await extractor.ExtractAsync(comm);

        Assert.Single(statements);
        Assert.Equal("I promise to deliver the signed agreement by Friday.", statements[0].OriginalText);
        Assert.Equal(StatementType.Agreement, statements[0].StatementType);

        var normalizer = new RuleBasedClaimNormalizer();
        var claims = await normalizer.NormalizeAsync(statements);
        Assert.Single(claims);
        Assert.Equal("I promise to deliver the signed agreement by Friday", claims[0].NormalizedAssertion);
        Assert.DoesNotContain("<", claims[0].NormalizedAssertion);
        Assert.DoesNotContain(">", claims[0].NormalizedAssertion);
    }
}
