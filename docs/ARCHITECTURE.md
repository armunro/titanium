# Titanium Architecture Documentation

Titanium is a Communication Intelligence and Evidence Management Platform designed to ingest, normalize, correlate, and analyze communications (specifically Microsoft 365 Outlook emails) for legal disputes, compliance audits, and investigation reconstruction.

## 1. Clean Architecture Layers

```
Titanium.Desktop (WPF / Presentation)
       │
       ▼
Titanium.Workers (Generic Host / BackgroundService)
       │
       ├──► Titanium.Application (Use Cases, Normalization, Orchestration)
       │         ▲
       │         │
       │    Titanium.Domain (Core Entities, Value Objects, Enums, Rules)
       │    Titanium.Contracts (DTOs, Shared Contracts)
       │
       ├──► Titanium.Infrastructure (EF Core, SQLite / PostgreSQL persistence)
       ├──► Titanium.Integrations.M365 (MSAL Auth, Graph Mailbox Sync)
       └──► Titanium.Intelligence (Statement Extraction, Claim Normalization, Contradiction Engine)
```

## 2. Key Components

- **Domain Model**: Strict evidence-grounded entities (`Workspace`, `Matter`, `Communication`, `Statement`, `Claim`, `ClaimRelationship`, `Event`, `EvidenceLink`, `Narrative`, `IssueDossier`).
- **Communication Normalizer**: Removes quoted replies, forward headers, and signatures to ensure accurate speaker attribution.
- **AI Intelligence Engine**:
  - `IStatementExtractor`: Extracts atomic assertions and statements.
  - `IClaimNormalizer`: Transforms expressions into structured canonical claims with polarity.
  - `IRelevantClaimRetriever`: Filters candidate comparisons.
  - `IClaimRelationshipAnalyzer`: Distinguishes contradictions from changed positions, corroborations, and justified circumstance changes.
- **Background Worker**: Powered by Generic Host `BackgroundService` with persistent resilient SQLite job queue.
- **Presentation**: Windows WPF using CommunityToolkit.Mvvm and modern dark investigative dashboard styling.
