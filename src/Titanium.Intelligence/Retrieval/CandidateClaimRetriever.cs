using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;

namespace Titanium.Intelligence.Retrieval;

public class CandidateClaimRetriever : IRelevantClaimRetriever
{
    private readonly ITitaniumDbContext _context;

    public CandidateClaimRetriever(ITitaniumDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<Claim>> RetrieveRelevantCandidatesAsync(
        Claim claim,
        Guid workspaceId,
        int maxCandidates = 20,
        CancellationToken cancellationToken = default)
    {
        var words = (claim.NormalizedAssertion ?? claim.Object ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Select(w => w.ToLowerInvariant())
            .ToHashSet();

        // Retrieve existing claims in workspace
        var query = _context.Claims
            .Where(c => c.WorkspaceId == workspaceId && c.Id != claim.Id)
            .ToList();

        var ranked = query
            .Select(c =>
            {
                var candidateWords = (c.NormalizedAssertion ?? c.Object ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .Select(w => w.ToLowerInvariant());

                int overlap = candidateWords.Count(w => words.Contains(w));
                return new { Claim = c, Score = overlap };
            })
            .Where(x => x.Score > 0 || words.Count == 0)
            .OrderByDescending(x => x.Score)
            .Take(maxCandidates)
            .Select(x => x.Claim)
            .ToList();

        return Task.FromResult<IReadOnlyList<Claim>>(ranked);
    }
}
