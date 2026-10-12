using Titanium.Application.Interfaces;
using Titanium.Contracts.DTOs;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Application.Services;

public class EvidenceAnalysisOrchestrator
{
    private readonly IStatementExtractor _statementExtractor;
    private readonly IClaimNormalizer _claimNormalizer;
    private readonly IRelevantClaimRetriever _candidateRetriever;
    private readonly IClaimRelationshipAnalyzer _relationshipAnalyzer;

    public EvidenceAnalysisOrchestrator(
        IStatementExtractor statementExtractor,
        IClaimNormalizer claimNormalizer,
        IRelevantClaimRetriever candidateRetriever,
        IClaimRelationshipAnalyzer relationshipAnalyzer)
    {
        _statementExtractor = statementExtractor;
        _claimNormalizer = claimNormalizer;
        _candidateRetriever = candidateRetriever;
        _relationshipAnalyzer = relationshipAnalyzer;
    }

    public async Task<AnalysisResultDto> ProcessCommunicationAsync(
        Communication communication,
        CancellationToken cancellationToken = default)
    {
        // 1. Extract atomic statements
        var statements = await _statementExtractor.ExtractAsync(communication, cancellationToken);

        // 2. Normalize statements into claims
        var claims = await _claimNormalizer.NormalizeAsync(statements, cancellationToken);

        var discoveredRelationships = new List<ClaimRelationship>();

        // 3. Compare each claim with candidate claims
        foreach (var claim in claims)
        {
            var candidates = await _candidateRetriever.RetrieveRelevantCandidatesAsync(
                claim,
                communication.WorkspaceId,
                maxCandidates: 20,
                cancellationToken: cancellationToken);

            if (candidates.Count > 0)
            {
                var relationships = await _relationshipAnalyzer.AnalyzeAsync(claim, candidates, cancellationToken);
                discoveredRelationships.AddRange(relationships);
            }
        }

        return new AnalysisResultDto(
            communication.Id,
            statements.Count,
            claims.Count,
            discoveredRelationships.Count,
            statements,
            claims,
            discoveredRelationships);
    }
}

public record AnalysisResultDto(
    Guid CommunicationId,
    int StatementCount,
    int ClaimCount,
    int RelationshipCount,
    IReadOnlyList<Statement> Statements,
    IReadOnlyList<Claim> Claims,
    IReadOnlyList<ClaimRelationship> Relationships);
