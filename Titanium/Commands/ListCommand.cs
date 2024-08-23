using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ListCommand : TitaniumCommand
{
    private readonly ConfigManager _config;
    private readonly ILogger _logger;

    public ListCommand(ConfigManager config, ILogger logger ) : base("list", "List documents")
    {
        _config = config;
        _logger = logger;
    }

    public override List<Option> DefineOptions() => new();

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        foreach (string docName in _config.GetDocNames())
        {
            _logger.Information(docName);
        }

        return Task.FromResult(0);
    }
    
}