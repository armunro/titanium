namespace Titanium.Domain.Enums;

public enum MatterStatus
{
    Active = 0,
    Pending = 1,
    Archived = 2,
    Closed = 3
}

public enum CommunicationSource
{
    Microsoft365 = 0,
    ManualEntry = 1,
    FileImport = 2,
    Api = 3
}

public enum StatementType
{
    Fact = 0,
    Allegation = 1,
    Commitment = 2,
    Agreement = 3,
    Denial = 4,
    Question = 5,
    Opinion = 6,
    Instruction = 7,
    ChangedCircumstance = 8
}

public enum ClaimPolarity
{
    Positive = 0,
    Negative = 1,
    Neutral = 2
}

public enum ReviewStatus
{
    PendingReview = 0,
    ConfirmedRelationship = 1,
    RejectedRelationship = 2,
    NeedsMoreContext = 3,
    Superseded = 4
}

public enum ClaimRelationshipType
{
    Supports = 0,
    Contradicts = 1,
    Corroborates = 2,
    Clarifies = 3,
    Supersedes = 4,
    Qualifies = 5,
    RelatedTo = 6,
    ChangesPosition = 7,
    ChangesRecollection = 8,
    UnfulfilledCommitment = 9,
    Ambiguity = 10
}

public enum NarrativeAssertionType
{
    Supported = 0,
    Disputed = 1,
    Unverified = 2,
    Inferred = 3
}

public enum EventVerificationStatus
{
    Unverified = 0,
    SupportedByEvidence = 1,
    Disputed = 2,
    EstablishedFact = 3
}

public enum JobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Retry = 5
}
