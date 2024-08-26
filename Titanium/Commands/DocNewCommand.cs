using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class DocNewCommand : TitaniumCommand
{
    private readonly ILogger _logger;
    private readonly ConfigManager _config;

    public DocNewCommand(ILogger logger, ConfigManager config) : base("new", "Create new blank document(s)")
    {
        _logger = logger;
        _config = config;
    }
    protected override Task<int> HandleAsync(InvocationContext context)
    {
        Doc doc = _config.AddDoc();
        _logger.Information(doc.Id);
        return Task.FromResult(0);
    }
}