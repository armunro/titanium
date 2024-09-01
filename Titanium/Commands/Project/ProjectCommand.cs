using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand("project", "Project commands")]
public class ProjectCommand : CliCommand
{
    private readonly ConfigManager _config;

    public ProjectCommand(ConfigManager config)
    {
        _config = config;
    }
    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        List<ProjectConfig> projects = _config.GetProjects();
        foreach (ProjectConfig project in projects)
        {
            Console.WriteLine(project.Name);
        }

        return Task.FromResult(0);
    }
}