using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand("add", "Add a project")]
public class ProjectAddCommand : CliCommand
{
    private readonly ConfigManager _config;

    [CliOption("--name", "The name of the project")]
    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");

    public ProjectAddCommand(ConfigManager config)
    {
        _config = config;
    }


    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        _config.CreateProject(context.Option<string>(ProjectNameOption));
        return Task.FromResult(0);
    }
}