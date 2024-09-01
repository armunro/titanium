using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand("use", "Use a project")]
public class ProjectUseCommand : CliCommand
{
    [CliArgument("name", "The name of the project")]
    public static readonly Argument<string> ProjectNameArg = new("--name", "The name of the project");

    private readonly ConfigManager _config;
    private readonly ILogger _logger;


    public ProjectUseCommand(ConfigManager config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        string projectName = context.Argument<string>(ProjectNameArg)!;
        _config.UseProject(projectName);
        _logger.Information("Using project `{Project}`", projectName);
        _config.SaveConfig();
        return Task.FromResult(0);
    }

 
}