using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;

namespace Titanium.Infrastructure.Persistence;

public class TitaniumDbContext : DbContext, ITitaniumDbContext
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Matter> Matters => Set<Matter>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Statement> Statements => Set<Statement>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimRelationship> ClaimRelationships => Set<ClaimRelationship>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();
    public DbSet<Narrative> Narratives => Set<Narrative>();
    public DbSet<NarrativeItem> NarrativeItems => Set<NarrativeItem>();
    public DbSet<IssueDossier> Dossiers => Set<IssueDossier>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    IQueryable<Workspace> ITitaniumDbContext.Workspaces => Workspaces;
    IQueryable<Matter> ITitaniumDbContext.Matters => Matters;
    IQueryable<Person> ITitaniumDbContext.Persons => Persons;
    IQueryable<Communication> ITitaniumDbContext.Communications => Communications;
    IQueryable<Conversation> ITitaniumDbContext.Conversations => Conversations;
    IQueryable<Statement> ITitaniumDbContext.Statements => Statements;
    IQueryable<Claim> ITitaniumDbContext.Claims => Claims;
    IQueryable<ClaimRelationship> ITitaniumDbContext.ClaimRelationships => ClaimRelationships;
    IQueryable<Event> ITitaniumDbContext.Events => Events;
    IQueryable<EvidenceLink> ITitaniumDbContext.EvidenceLinks => EvidenceLinks;
    IQueryable<Narrative> ITitaniumDbContext.Narratives => Narratives;
    IQueryable<NarrativeItem> ITitaniumDbContext.NarrativeItems => NarrativeItems;
    IQueryable<IssueDossier> ITitaniumDbContext.Dossiers => Dossiers;
    IQueryable<AnalysisRun> ITitaniumDbContext.AnalysisRuns => AnalysisRuns;
    IQueryable<BackgroundJob> ITitaniumDbContext.BackgroundJobs => BackgroundJobs;

    public void AddEntity<T>(T entity) where T : class => Set<T>().Add(entity);
    public void UpdateEntity<T>(T entity) where T : class => Set<T>().Update(entity);
    public void RemoveEntity<T>(T entity) where T : class => Set<T>().Remove(entity);

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await Database.EnsureCreatedAsync(cancellationToken);

        if (Database.IsRelational())
        {
            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;", cancellationToken);
            }
            catch
            {
                // Ignore if pragma is unsupported
            }

            await EnsureSqliteSchemaUpdatedAsync(cancellationToken);
        }
    }

    private async Task EnsureSqliteSchemaUpdatedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            foreach (var entityType in Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName)) continue;

                var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info('{tableName}');";
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var colName = reader.GetString(1);
                        existingColumns.Add(colName);
                    }
                }

                if (existingColumns.Count == 0) continue;

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    if (string.IsNullOrEmpty(columnName)) continue;

                    if (!existingColumns.Contains(columnName))
                    {
                        var columnType = property.GetColumnType();
                        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                        string defaultClause = "";

                        if (string.IsNullOrEmpty(columnType))
                        {
                            if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) || clrType == typeof(bool) || clrType.IsEnum)
                            {
                                columnType = "INTEGER";
                                defaultClause = "DEFAULT 0";
                            }
                            else if (clrType == typeof(double) || clrType == typeof(float) || clrType == typeof(decimal))
                            {
                                columnType = "REAL";
                                defaultClause = "DEFAULT 0.0";
                            }
                            else if (clrType == typeof(byte[]))
                            {
                                columnType = "BLOB";
                            }
                            else
                            {
                                columnType = "TEXT";
                                defaultClause = columnName.Equals("TargetFolders", StringComparison.OrdinalIgnoreCase)
                                    ? "DEFAULT '[\"Inbox\"]'"
                                    : "DEFAULT ''";
                            }
                        }

                        using var alterCommand = connection.CreateCommand();
                        alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnType} {defaultClause};";
                        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
                    }

                    if (columnName.Equals("TargetFolders", StringComparison.OrdinalIgnoreCase))
                    {
                        using var updateCmd = connection.CreateCommand();
                        updateCmd.CommandText = $"UPDATE \"{tableName}\" SET \"{columnName}\" = '[\"Inbox\"]' WHERE \"{columnName}\" IS NULL OR \"{columnName}\" = '';";
                        await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
        }
        catch
        {
            // Suppress schema inspection failures
        }
    }

    public async Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var workspace = await Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
        if (workspace == null) return;

        var commIds = await Communications.Where(c => c.WorkspaceId == workspaceId).Select(c => c.Id).ToListAsync(cancellationToken);
        var claimIds = await Claims.Where(c => c.WorkspaceId == workspaceId).Select(c => c.Id).ToListAsync(cancellationToken);
        var matterIds = await Matters.Where(m => m.WorkspaceId == workspaceId).Select(m => m.Id).ToListAsync(cancellationToken);
        var narrativeIds = await Narratives.Where(n => matterIds.Contains(n.MatterId)).Select(n => n.Id).ToListAsync(cancellationToken);

        if (Database.IsRelational())
        {
            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;", cancellationToken);
            }
            catch
            {
                // Ignore if pragma is unsupported
            }

            if (claimIds.Count > 0)
            {
                await ClaimRelationships.Where(r => claimIds.Contains(r.SourceClaimId) || claimIds.Contains(r.TargetClaimId)).ExecuteDeleteAsync(cancellationToken);
            }

            if (commIds.Count > 0)
            {
                await EvidenceLinks.Where(e => commIds.Contains(e.SourceCommunicationId)).ExecuteDeleteAsync(cancellationToken);
                await Statements.Where(s => commIds.Contains(s.CommunicationId)).ExecuteDeleteAsync(cancellationToken);
            }

            if (narrativeIds.Count > 0)
            {
                await NarrativeItems.Where(ni => narrativeIds.Contains(ni.NarrativeId)).ExecuteDeleteAsync(cancellationToken);
                await Narratives.Where(n => narrativeIds.Contains(n.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            if (matterIds.Count > 0)
            {
                await Dossiers.Where(d => matterIds.Contains(d.MatterId)).ExecuteDeleteAsync(cancellationToken);
                await Matters.Where(m => matterIds.Contains(m.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            await Events.Where(e => e.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
            await Persons.Where(p => p.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
            await Claims.Where(c => c.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
            await Communications.Where(c => c.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
            await Conversations.Where(c => c.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
            await Workspaces.Where(w => w.Id == workspaceId).ExecuteDeleteAsync(cancellationToken);

            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);
            }
            catch
            {
                // Ignore pragma error
            }
        }
        else
        {
            if (claimIds.Count > 0)
            {
                var rels = await ClaimRelationships.Where(r => claimIds.Contains(r.SourceClaimId) || claimIds.Contains(r.TargetClaimId)).ToListAsync(cancellationToken);
                ClaimRelationships.RemoveRange(rels);
            }

            if (commIds.Count > 0)
            {
                var evLinks = await EvidenceLinks.Where(e => commIds.Contains(e.SourceCommunicationId)).ToListAsync(cancellationToken);
                EvidenceLinks.RemoveRange(evLinks);

                var stmts = await Statements.Where(s => commIds.Contains(s.CommunicationId)).ToListAsync(cancellationToken);
                Statements.RemoveRange(stmts);
            }

            if (narrativeIds.Count > 0)
            {
                var nItems = await NarrativeItems.Where(ni => narrativeIds.Contains(ni.NarrativeId)).ToListAsync(cancellationToken);
                NarrativeItems.RemoveRange(nItems);

                var narrs = await Narratives.Where(n => narrativeIds.Contains(n.Id)).ToListAsync(cancellationToken);
                Narratives.RemoveRange(narrs);
            }

            if (matterIds.Count > 0)
            {
                var dossiers = await Dossiers.Where(d => matterIds.Contains(d.MatterId)).ToListAsync(cancellationToken);
                Dossiers.RemoveRange(dossiers);

                var matters = await Matters.Where(m => matterIds.Contains(m.Id)).ToListAsync(cancellationToken);
                Matters.RemoveRange(matters);
            }

            var events = await Events.Where(e => e.WorkspaceId == workspaceId).ToListAsync(cancellationToken);
            Events.RemoveRange(events);

            var persons = await Persons.Where(p => p.WorkspaceId == workspaceId).ToListAsync(cancellationToken);
            Persons.RemoveRange(persons);

            var claims = await Claims.Where(c => c.WorkspaceId == workspaceId).ToListAsync(cancellationToken);
            Claims.RemoveRange(claims);

            var comms = await Communications.Where(c => c.WorkspaceId == workspaceId).ToListAsync(cancellationToken);
            Communications.RemoveRange(comms);

            var convs = await Conversations.Where(c => c.WorkspaceId == workspaceId).ToListAsync(cancellationToken);
            Conversations.RemoveRange(convs);

            Workspaces.Remove(workspace);
            await SaveChangesAsync(cancellationToken);
        }
    }

    public async Task PurgeAllDataAsync(CancellationToken cancellationToken = default)
    {
        if (Database.IsRelational())
        {
            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;", cancellationToken);
            }
            catch
            {
                // Ignore if pragma is unsupported
            }

            await ClaimRelationships.ExecuteDeleteAsync(cancellationToken);
            await EvidenceLinks.ExecuteDeleteAsync(cancellationToken);
            await NarrativeItems.ExecuteDeleteAsync(cancellationToken);
            await Narratives.ExecuteDeleteAsync(cancellationToken);
            await Dossiers.ExecuteDeleteAsync(cancellationToken);
            await AnalysisRuns.ExecuteDeleteAsync(cancellationToken);
            await Claims.ExecuteDeleteAsync(cancellationToken);
            await Statements.ExecuteDeleteAsync(cancellationToken);
            await Events.ExecuteDeleteAsync(cancellationToken);
            await Communications.ExecuteDeleteAsync(cancellationToken);
            await Conversations.ExecuteDeleteAsync(cancellationToken);
            await Matters.ExecuteDeleteAsync(cancellationToken);
            await Persons.ExecuteDeleteAsync(cancellationToken);
            await BackgroundJobs.ExecuteDeleteAsync(cancellationToken);
            await Workspaces.ExecuteDeleteAsync(cancellationToken);

            try
            {
                await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);
                await Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
            }
            catch
            {
                // Ignore vacuum/pragma errors
            }
        }
        else
        {
            ClaimRelationships.RemoveRange(ClaimRelationships);
            EvidenceLinks.RemoveRange(EvidenceLinks);
            NarrativeItems.RemoveRange(NarrativeItems);
            Narratives.RemoveRange(Narratives);
            Dossiers.RemoveRange(Dossiers);
            AnalysisRuns.RemoveRange(AnalysisRuns);
            Claims.RemoveRange(Claims);
            Statements.RemoveRange(Statements);
            Events.RemoveRange(Events);
            Communications.RemoveRange(Communications);
            Conversations.RemoveRange(Conversations);
            Matters.RemoveRange(Matters);
            Persons.RemoveRange(Persons);
            BackgroundJobs.RemoveRange(BackgroundJobs);
            Workspaces.RemoveRange(Workspaces);
            await SaveChangesAsync(cancellationToken);
        }
    }

    public TitaniumDbContext(DbContextOptions<TitaniumDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // JSON value converters for lists & dictionaries in SQLite
        var listStringConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        var listGuidConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        var dictConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Entity Configurations
        modelBuilder.Entity<Workspace>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.TargetFolders).HasConversion(listStringConverter);
        });

        modelBuilder.Entity<Communication>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Recipients).HasConversion(listStringConverter);
            b.Property(c => c.CcRecipients).HasConversion(listStringConverter);
            b.Property(c => c.BccRecipients).HasConversion(listStringConverter);
            b.Property(c => c.Metadata).HasConversion(dictConverter);

            b.HasIndex(c => c.SentAt);
            b.HasIndex(c => c.ReceivedAt);
            b.HasIndex(c => c.Sender);
            b.HasIndex(c => c.ExternalMessageId);
            b.HasIndex(c => c.InternetMessageId);
            b.HasIndex(c => c.OriginalContentHash);
        });

        modelBuilder.Entity<Conversation>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Participants).HasConversion(listStringConverter);
            b.HasIndex(c => c.ExternalConversationId);
            b.HasIndex(c => c.LastActivityAt);
        });

        modelBuilder.Entity<Person>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.EmailAddresses).HasConversion(listStringConverter);
            b.Property(p => p.Aliases).HasConversion(listStringConverter);
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Participants).HasConversion(listStringConverter);
            b.HasIndex(e => e.EventDate);
        });

        modelBuilder.Entity<ClaimRelationship>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasOne(r => r.SourceClaim)
                .WithMany(c => c.OutgoingRelationships)
                .HasForeignKey(r => r.SourceClaimId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.TargetClaim)
                .WithMany(c => c.IncomingRelationships)
                .HasForeignKey(r => r.TargetClaimId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(r => r.RelationshipType);
            b.HasIndex(r => r.ReviewStatus);
        });

        modelBuilder.Entity<NarrativeItem>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.SupportingEvidenceIds).HasConversion(listGuidConverter);
            b.Property(n => n.ConflictingEvidenceIds).HasConversion(listGuidConverter);
        });

        modelBuilder.Entity<IssueDossier>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.CommunicationIds).HasConversion(listGuidConverter);
            b.Property(d => d.ClaimIds).HasConversion(listGuidConverter);
            b.Property(d => d.EventIds).HasConversion(listGuidConverter);
        });

        modelBuilder.Entity<AnalysisRun>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.InputReferences).HasConversion(dictConverter);
            b.Property(a => a.OutputReferences).HasConversion(dictConverter);
        });

        modelBuilder.Entity<BackgroundJob>(b =>
        {
            b.HasKey(j => j.Id);
            b.HasIndex(j => j.Status);
            b.HasIndex(j => j.CreatedAt);
        });
    }
}
