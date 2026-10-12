using System.Text.Json;
using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Application.Services;

public class MatterManagementService
{
    private readonly ITitaniumDbContext _dbContext;

    public MatterManagementService(ITitaniumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Matter> CreateMatterAsync(Guid workspaceId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var matter = new Matter
        {
            WorkspaceId = workspaceId,
            Name = name,
            Description = description,
            Status = MatterStatus.Active
        };

        await _dbContext.SaveChangesAsync(cancellationToken);
        return matter;
    }
}
