using Titanium.Application.Interfaces;
using Titanium.Application.Services;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Intelligence.Extraction;

namespace Titanium.Intelligence.Classification;

public class RuleBasedClaimNormalizer : IClaimNormalizer
{
    private readonly CommunicationNormalizer _normalizer = new();

    public Task<IReadOnlyList<Claim>> NormalizeAsync(IReadOnlyList<Statement> statements, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        foreach (var statement in statements)
        {
            var text = _normalizer.HtmlToPlainText(statement.OriginalText).Trim();

            if (!RuleBasedStatementExtractor.IsLinguisticStatement(text))
            {
                continue;
            }

            var lower = text.ToLowerInvariant();

            var polarity = ClaimPolarity.Positive;
            if (lower.Contains("never") || lower.Contains("not ") || lower.Contains("didn't") || lower.Contains("refuse") || lower.Contains("cannot") || lower.Contains("won't") || lower.Contains("will not"))
            {
                polarity = ClaimPolarity.Negative;
            }

            var assertion = NormalizeAssertionText(text);
            if (string.IsNullOrWhiteSpace(assertion) || assertion.Length < 4)
            {
                continue;
            }

            var claim = new Claim
            {
                WorkspaceId = statement.Communication?.WorkspaceId ?? Guid.Empty,
                MatterId = statement.Communication?.MatterId,
                Subject = statement.SpeakerName,
                Predicate = statement.StatementType.ToString(),
                Object = assertion,
                NormalizedAssertion = assertion,
                Polarity = polarity,
                ReviewStatus = ReviewStatus.PendingReview,
                Statements = new List<Statement> { statement }
            };

            claims.Add(claim);
        }

        return Task.FromResult<IReadOnlyList<Claim>>(claims);
    }

    private string NormalizeAssertionText(string text)
    {
        var normalized = _normalizer.HtmlToPlainText(text).Trim();
        if (normalized.EndsWith(".")) normalized = normalized[..^1].Trim();
        return normalized;
    }
}
