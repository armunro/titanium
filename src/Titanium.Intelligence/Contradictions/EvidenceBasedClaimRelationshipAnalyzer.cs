using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Intelligence.Contradictions;

public class EvidenceBasedClaimRelationshipAnalyzer : IClaimRelationshipAnalyzer
{
    public Task<IReadOnlyList<ClaimRelationship>> AnalyzeAsync(
        Claim claim,
        IReadOnlyList<Claim> candidates,
        CancellationToken cancellationToken = default)
    {
        var relationships = new List<ClaimRelationship>();

        foreach (var candidate in candidates)
        {
            var rel = EvaluateRelationship(claim, candidate);
            if (rel != null)
            {
                relationships.Add(rel);
            }
        }

        return Task.FromResult<IReadOnlyList<ClaimRelationship>>(relationships);
    }

    public static ClaimRelationship? EvaluateRelationship(Claim claimA, Claim claimB)
    {
        var textA = (claimA.NormalizedAssertion ?? claimA.Object ?? string.Empty).ToLowerInvariant();
        var textB = (claimB.NormalizedAssertion ?? claimB.Object ?? string.Empty).ToLowerInvariant();

        // 1. Check for Changed Circumstance / Justified Change
        if (textA.Contains("emergency") || textA.Contains("car broke") || textA.Contains("sick") ||
            textB.Contains("emergency") || textB.Contains("car broke") || textB.Contains("sick"))
        {
            return new ClaimRelationship
            {
                SourceClaimId = claimA.Id,
                SourceClaim = claimA,
                TargetClaimId = claimB.Id,
                TargetClaim = claimB,
                RelationshipType = ClaimRelationshipType.Qualifies,
                Explanation = "Statement clarifies or qualifies previous schedule/assertion due to changed circumstances or emergency.",
                Confidence = 0.85,
                ReviewStatus = ReviewStatus.PendingReview
            };
        }

        // 2. Direct Contradiction vs Changed Position
        bool isOppositePolarity = claimA.Polarity != claimB.Polarity;
        bool sameSubject = string.Equals(claimA.Subject, claimB.Subject, StringComparison.OrdinalIgnoreCase);

        var wordsA = textA.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var wordsB = textB.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        int sharedWords = wordsA.Count(w => wordsB.Contains(w));

        if (sharedWords >= 2)
        {
            if (isOppositePolarity)
            {
                if (sameSubject)
                {
                    // If same speaker changed mind over time
                    return new ClaimRelationship
                    {
                        SourceClaimId = claimA.Id,
                        SourceClaim = claimA,
                        TargetClaimId = claimB.Id,
                        TargetClaim = claimB,
                        RelationshipType = ClaimRelationshipType.ChangesPosition,
                        Explanation = "Speaker has stated an opposite position or stance compared to an earlier statement.",
                        Confidence = 0.88,
                        ReviewStatus = ReviewStatus.PendingReview
                    };
                }
                else
                {
                    // Different speakers giving opposing accounts of facts
                    return new ClaimRelationship
                    {
                        SourceClaimId = claimA.Id,
                        SourceClaim = claimA,
                        TargetClaimId = claimB.Id,
                        TargetClaim = claimB,
                        RelationshipType = ClaimRelationshipType.Contradicts,
                        Explanation = "Conflicting assertions identified regarding the same matter or event.",
                        Confidence = 0.90,
                        ReviewStatus = ReviewStatus.PendingReview
                    };
                }
            }
            else
            {
                // Corroboration or Supports
                var relType = sameSubject ? ClaimRelationshipType.Supports : ClaimRelationshipType.Corroborates;
                return new ClaimRelationship
                {
                    SourceClaimId = claimA.Id,
                    SourceClaim = claimA,
                    TargetClaimId = claimB.Id,
                    TargetClaim = claimB,
                    RelationshipType = relType,
                    Explanation = sameSubject
                        ? "Statement reiterates and supports prior assertion."
                        : "Independent statement corroborates information provided by another party.",
                    Confidence = 0.82,
                    ReviewStatus = ReviewStatus.PendingReview
                };
            }
        }

        return null;
    }
}
