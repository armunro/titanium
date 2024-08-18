using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class AspectCommand : TitaniumCommand
{
    public AspectCommand( ConfigManager config) : base("aspect", "Manage aspects on a document", config)
    {
    }

    public override List<Option> DefineOptions() => new();

    public override Task<int> HandleAsync(InvocationContext context) => Task.FromResult(0);
}