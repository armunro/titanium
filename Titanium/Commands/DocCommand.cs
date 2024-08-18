using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class DocCommand : TitaniumCommand
{
    public DocCommand(ConfigManager config) : base("doc", "Manage docs", config)
    {
    }

    public override List<Option> DefineOptions() => new();

    public override Task<int> HandleAsync(InvocationContext context) => Task.FromResult(0);
    
}