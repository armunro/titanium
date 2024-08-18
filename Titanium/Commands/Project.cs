using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class Project : TitaniumCommand
{
    public Project(ConfigManager config) : base("project", "Manage project", config)
    {
    }

    public override List<Option> DefineOptions() => new();

    public override Task<int> HandleAsync(InvocationContext context)
    {
        return  Task.FromResult(0);
    }
}