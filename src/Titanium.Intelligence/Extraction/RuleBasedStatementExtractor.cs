using System.Text.RegularExpressions;
using Titanium.Application.Interfaces;
using Titanium.Application.Services;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Intelligence.Extraction;

public class RuleBasedStatementExtractor : IStatementExtractor
{
    private readonly CommunicationNormalizer _normalizer = new();

    public Task<IReadOnlyList<Statement>> ExtractAsync(Communication communication, CancellationToken cancellationToken = default)
    {
        var statements = new List<Statement>();

        // Step 1: Extract only the clean plain text from the email first
        var body = ExtractPureText(communication);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Task.FromResult<IReadOnlyList<Statement>>(statements);
        }

        // Step 2: Process the clean plain text to extract statements
        var lines = body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var sentenceRegex = new Regex(@"(?<!\w\.\w.)(?<![A-Z][a-z]\.)(?<=\.|\?|\!)\s+", RegexOptions.Compiled);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Strip bullet markers
            if (line.StartsWith("• ") || line.StartsWith("- ") || line.StartsWith("* "))
            {
                line = line[2..].Trim();
            }

            // Split line into sentences
            var sentenceCandidates = sentenceRegex.Split(line);
            foreach (var candidate in sentenceCandidates)
            {
                var sentence = candidate.Trim();
                if (!IsLinguisticStatement(sentence))
                {
                    continue;
                }

                int offset = body.IndexOf(sentence, StringComparison.Ordinal);
                if (offset < 0) offset = 0;

                var statementType = ClassifySentence(sentence);
                var stmt = new Statement
                {
                    CommunicationId = communication.Id,
                    SpeakerName = communication.Sender,
                    OriginalText = sentence,
                    StartOffset = offset,
                    EndOffset = offset + sentence.Length,
                    StatementType = statementType,
                    ExtractedAt = DateTime.UtcNow,
                    ExtractionVersion = "2.0"
                };

                statements.Add(stmt);
            }
        }

        return Task.FromResult<IReadOnlyList<Statement>>(statements);
    }

    public string ExtractPureText(Communication communication)
    {
        string rawText = string.Empty;

        // Try extracting from BodyHtml first if available
        if (!string.IsNullOrWhiteSpace(communication.BodyHtml))
        {
            rawText = _normalizer.NormalizeBody(communication.BodyHtml, null);
        }

        // If BodyHtml wasn't available or returned empty, extract from BodyText
        if (string.IsNullOrWhiteSpace(rawText) && !string.IsNullOrWhiteSpace(communication.BodyText))
        {
            rawText = _normalizer.NormalizeBody(null, communication.BodyText);
        }
        else if (!string.IsNullOrWhiteSpace(communication.BodyText) && string.IsNullOrWhiteSpace(communication.BodyHtml))
        {
            rawText = _normalizer.NormalizeBody(null, communication.BodyText);
        }

        // Extra sanitization pass to strip any remaining HTML/CSS tags
        if (!string.IsNullOrWhiteSpace(rawText))
        {
            rawText = _normalizer.HtmlToPlainText(rawText);
        }

        return rawText.Trim();
    }

    public static bool IsLinguisticStatement(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();

        // 1. Minimum length & token count: Real assertions have at least ~6 chars and 2 distinct words
        if (trimmed.Length < 6) return false;

        var tokens = Regex.Split(trimmed, @"\s+").Where(t => t.Length > 0).ToArray();
        if (tokens.Length < 2) return false;

        // 2. Reject if HTML / XML remnants are present
        if (trimmed.Contains('<') || trimmed.Contains('>') ||
            trimmed.Contains("xmlns:") || trimmed.Contains("</") || trimmed.Contains("/>") ||
            trimmed.Contains("&nbsp;") || trimmed.Contains("&quot;") || trimmed.Contains("&amp;") ||
            trimmed.Contains("&lt;") || trimmed.Contains("&gt;") || trimmed.Contains("&#"))
        {
            return false;
        }

        // 3. Reject if CSS syntax, selectors, or style properties are present
        if (trimmed.Contains('{') || trimmed.Contains('}') ||
            trimmed.Contains("/*") || trimmed.Contains("*/") ||
            trimmed.StartsWith("@") || trimmed.Contains("@media") || trimmed.Contains("@font-face") ||
            trimmed.Contains("@page") || trimmed.Contains("@keyframes") || trimmed.Contains("@import") ||
            trimmed.Contains("mso-") || trimmed.Contains("!important") || trimmed.Contains("calc(") ||
            trimmed.Contains("var(") || trimmed.Contains("url(") || trimmed.Contains("rgb(") || trimmed.Contains("rgba("))
        {
            return false;
        }

        // Check for common CSS property patterns (e.g. "color: #fff", "margin-top: 10px", "border: none")
        if (Regex.IsMatch(trimmed, @"\b(margin|padding|border|font|color|background|display|width|height|line-height|text-align|table-layout|box-sizing|overflow|z-index|opacity|cursor|flex|grid|position)\s*:\s*[^;]+;?", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // Check for CSS class/id rules like "p.MsoNormal, li.MsoNormal"
        if (Regex.IsMatch(trimmed, @"\b(MsoNormal|MsoChpDefault|MsoHyperlink|WordSection\d+|EmailStyle\d+)\b", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // 4. Reject URLs, file paths, and web schemes
        if (Regex.IsMatch(trimmed, @"^(https?://|ftp://|file://|mailto:|www\.)", RegexOptions.IgnoreCase) ||
            trimmed.Contains("http://schemas.") || trimmed.Contains("urn:schemas-") || trimmed.Contains("safelinks.protection.outlook.com"))
        {
            return false;
        }

        // 5. Reject email headers
        if (Regex.IsMatch(trimmed, @"^(From|To|Cc|Bcc|Sent|Date|Subject|Reply-To)\s*:\s*", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // 6. Reject greetings / salutations
        if (Regex.IsMatch(trimmed, @"^(hi|hello|hey|dear|good (morning|afternoon|evening)|to whom it may concern)\b[\s,.:!]*$", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(trimmed, @"^(hi|hello|hey|dear)\s+[a-zA-Z\s]{1,30}[,.:!]*$", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // 7. Reject signoffs / closings
        if (Regex.IsMatch(trimmed, @"^(thanks|thank you|thanks & regards|best regards|regards|sincerely|cheers|warm regards|yours truly|best|kind regards|all the best|cordially)[\s,.:!]*$", RegexOptions.IgnoreCase))
        {
            return false;
        }

        // 8. Reject email client signatures & disclaimers
        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("sent from my iphone") || lower.Contains("sent from my ipad") ||
            lower.Contains("sent from outlook") || lower.Contains("get outlook for") ||
            lower.Contains("this email and any attachments are confidential") ||
            lower.Contains("intended solely for the use") || lower.Contains("received this message in error") ||
            lower.Contains("confidentiality notice") || lower.Contains("privacy policy") ||
            lower.Contains("click here to unsubscribe") || lower.Contains("view in browser") ||
            lower.Contains("all rights reserved"))
        {
            return false;
        }

        // 9. Ratio of alphanumeric characters: Real natural sentences have mostly letters/digits/spaces
        int letterOrDigitCount = trimmed.Count(char.IsLetterOrDigit);
        if ((double)letterOrDigitCount / trimmed.Length < 0.60)
        {
            return false;
        }

        // 10. Check that statement contains at least some natural language communication indicators
        if (trimmed.EndsWith("?"))
        {
            return true;
        }

        // Common communication verbs, auxiliaries, or assertion keywords
        var hasCommunicativeVerb = Regex.IsMatch(lower,
            @"\b(is|am|are|was|were|be|been|being|have|has|had|do|does|did|will|would|shall|should|can|could|may|might|must|" +
            @"agree|agreed|approv|confirm|deal|promis|pay|paid|sent|send|receiv|meet|state|stat|said|told|tell|claim|deni|refus|" +
            @"want|need|order|cancel|schedul|call|ask|think|believ|know|expect|inform|notif|propos|accept|sign|authoriz|resolv|" +
            @"work|occur|happen|complet|delay|fail|refus|request|submit|provid|discuss|contact)\w*\b",
            RegexOptions.IgnoreCase);

        return hasCommunicativeVerb;
    }

    private static StatementType ClassifySentence(string sentence)
    {
        var lower = sentence.ToLowerInvariant();
        if (lower.EndsWith("?") || lower.StartsWith("can you") || lower.StartsWith("could you") || lower.StartsWith("would you")) return StatementType.Question;
        if (lower.Contains("agree") || lower.Contains("approved") || lower.Contains("confirm") || lower.Contains("deal") || lower.Contains("accept") || lower.Contains("sounds good") || lower.Contains("fine with me") || lower.Contains("that works")) return StatementType.Agreement;
        if (lower.Contains("will ") || lower.Contains("shall") || lower.Contains("promise") || lower.Contains("i can pick up") || lower.Contains("i will pay") || lower.Contains("i will ") || lower.Contains("i'll ") || lower.Contains("i am going to") || lower.Contains("i plan to")) return StatementType.Commitment;
        if (lower.Contains("never ") || lower.Contains("did not") || lower.Contains("didn't") || lower.Contains("denied") || lower.Contains("refuse") || lower.Contains("do not agree") || lower.Contains("don't agree") || lower.Contains("cannot") || lower.Contains("won't") || lower.Contains("will not")) return StatementType.Denial;
        if (lower.Contains("because of") || lower.Contains("due to unforeseen") || lower.Contains("car broke down") || lower.Contains("emergency") || lower.Contains("fell ill") || lower.Contains("traffic delay") || lower.Contains("cancelled")) return StatementType.ChangedCircumstance;
        if (lower.Contains("he said") || lower.Contains("she said") || lower.Contains("they said") || lower.Contains("she claims") || lower.Contains("he claims") || lower.Contains("you told me") || lower.Contains("alleged")) return StatementType.Allegation;
        return StatementType.Fact;
    }
}
