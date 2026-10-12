using Titanium.Domain.Common;
using Titanium.Domain.Enums;

namespace Titanium.Domain.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerId { get; set; }
    public List<string> TargetFolders { get; set; } = new() { "Inbox" };

    public ICollection<Matter> Matters { get; set; } = new List<Matter>();
    public ICollection<Communication> Communications { get; set; } = new List<Communication>();
}

public class Matter : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MatterStatus Status { get; set; } = MatterStatus.Active;

    public ICollection<Communication> Communications { get; set; } = new List<Communication>();
    public ICollection<Narrative> Narratives { get; set; } = new List<Narrative>();
    public ICollection<IssueDossier> Dossiers { get; set; } = new List<IssueDossier>();
}

public class Person : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> EmailAddresses { get; set; } = new();
    public List<string> Aliases { get; set; } = new();
    public string? Notes { get; set; }
    public Guid? WorkspaceId { get; set; }
}

public class Communication : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public Guid? MatterId { get; set; }
    public Matter? Matter { get; set; }

    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public CommunicationSource Source { get; set; } = CommunicationSource.Microsoft365;
    public string? ExternalMessageId { get; set; } // Microsoft Graph ID
    public string? InternetMessageId { get; set; }
    public string? InReplyTo { get; set; }
    public string? References { get; set; }

    public string Sender { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public List<string> CcRecipients { get; set; } = new();
    public List<string> BccRecipients { get; set; } = new();

    public string Subject { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public string OriginalContentHash { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();

    public ICollection<Statement> Statements { get; set; } = new List<Statement>();
    public ICollection<EvidenceLink> EvidenceLinks { get; set; } = new List<EvidenceLink>();
}

public class Conversation : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public string? ExternalConversationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();

    public DateTime? StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    public ICollection<Communication> Messages { get; set; } = new List<Communication>();
}

public class Statement : BaseEntity
{
    public Guid CommunicationId { get; set; }
    public Communication? Communication { get; set; }

    public Guid? SpeakerId { get; set; }
    public Person? Speaker { get; set; }
    public string SpeakerName { get; set; } = string.Empty;

    public string OriginalText { get; set; } = string.Empty;
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }

    public StatementType StatementType { get; set; } = StatementType.Fact;
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public string ExtractionVersion { get; set; } = "1.0";

    public Guid? ClaimId { get; set; }
    public Claim? Claim { get; set; }
}

public class Claim : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid? MatterId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Predicate { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public string NormalizedAssertion { get; set; } = string.Empty;

    public ClaimPolarity Polarity { get; set; } = ClaimPolarity.Positive;
    public string? TemporalContext { get; set; }
    public string? Context { get; set; }
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.PendingReview;

    public ICollection<Statement> Statements { get; set; } = new List<Statement>();
    public ICollection<ClaimRelationship> OutgoingRelationships { get; set; } = new List<ClaimRelationship>();
    public ICollection<ClaimRelationship> IncomingRelationships { get; set; } = new List<ClaimRelationship>();
}

public class Event : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid? MatterId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string EventDatePrecision { get; set; } = "Exact"; // Exact, Day, Month, Year, Approximate
    public string? Location { get; set; }
    public List<string> Participants { get; set; } = new();
    public EventVerificationStatus VerificationStatus { get; set; } = EventVerificationStatus.Unverified;
}

public class ClaimRelationship : BaseEntity
{
    public Guid SourceClaimId { get; set; }
    public Claim? SourceClaim { get; set; }

    public Guid TargetClaimId { get; set; }
    public Claim? TargetClaim { get; set; }

    public ClaimRelationshipType RelationshipType { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0.0 - 1.0 advisory
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.PendingReview;
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }

    public Guid? AnalysisRunId { get; set; }
    public AnalysisRun? AnalysisRun { get; set; }
}

public class EvidenceLink : BaseEntity
{
    public Guid SourceCommunicationId { get; set; }
    public Communication? SourceCommunication { get; set; }

    public Guid TargetEntityId { get; set; } // ClaimId, EventId, NarrativeItemId, etc.
    public string TargetEntityType { get; set; } = "Claim";

    public string SourceText { get; set; } = string.Empty;
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
}

public class Narrative : BaseEntity
{
    public Guid MatterId { get; set; }
    public Matter? Matter { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;

    public ICollection<NarrativeItem> Items { get; set; } = new List<NarrativeItem>();
}

public class NarrativeItem : BaseEntity
{
    public Guid NarrativeId { get; set; }
    public Narrative? Narrative { get; set; }

    public int SequenceNumber { get; set; }
    public DateTime? EventDate { get; set; }
    public string AssertionText { get; set; } = string.Empty;
    public NarrativeAssertionType AssertionType { get; set; } = NarrativeAssertionType.Unverified;
    public string? Notes { get; set; }

    public List<Guid> SupportingEvidenceIds { get; set; } = new();
    public List<Guid> ConflictingEvidenceIds { get; set; } = new();
}

public class IssueDossier : BaseEntity
{
    public Guid MatterId { get; set; }
    public Matter? Matter { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<Guid> CommunicationIds { get; set; } = new();
    public List<Guid> ClaimIds { get; set; } = new();
    public List<Guid> EventIds { get; set; } = new();
    public string? UserAnnotations { get; set; }
}

public class AnalysisRun : BaseEntity
{
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = "1.0";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Completed";

    public Dictionary<string, string> InputReferences { get; set; } = new();
    public Dictionary<string, string> OutputReferences { get; set; } = new();
}

public class BackgroundJob : BaseEntity
{
    public string JobType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
