# Titanium Database Schema & Persistence

Titanium uses SQLite with Entity Framework Core for desktop local persistence, designed for straightforward migration to PostgreSQL with pgvector for centralized deployments.

## Tables & Indexes

- `Workspaces`: Root container for cases and matters.
- `Matters`: Legal cases, investigations, or dispute containers.
- `Communications`: Original raw communication records, body text/HTML, SHA-256 content hashes, sender/recipients, timestamps. Indexed on `SentAt`, `ReceivedAt`, `Sender`, `ExternalMessageId`, `InternetMessageId`, and `OriginalContentHash`.
- `Conversations`: Normalized threads and participant sets. Indexed on `ExternalConversationId` and `LastActivityAt`.
- `Persons`: People and multiple email address / alias resolution.
- `Statements`: Atomic expressions extracted from communications.
- `Claims`: Normalized factual assertions with polarity and temporal context.
- `ClaimRelationships`: Evidence links between claims (`Contradicts`, `Supports`, `Corroborates`, `Qualifies`, `ChangesPosition`).
- `Events`: Chronological occurrences with date precisions.
- `EvidenceLinks`: Direct traceability links between findings and exact textual source spans.
- `Narratives` & `NarrativeItems`: Chronological case narratives with supporting/conflicting evidence attachments.
- `IssueDossiers`: Subject-focused dossier groupings.
- `BackgroundJobs`: Resilient persistent queue for asynchronous sync and AI processing jobs.
