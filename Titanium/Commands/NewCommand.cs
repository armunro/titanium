using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand( "new", "Document commands" )]
public class NewCommand : CliCommand
{
    private readonly ILogger _logger;
    private readonly ConfigManager _config;

    public NewCommand(ILogger logger, ConfigManager config) 
    {
        _logger = logger;
        _config = config;
    }
    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        Doc doc = _config.AddDoc();
        _logger.Information(doc.Id);
        return Task.FromResult(0);
    }
}