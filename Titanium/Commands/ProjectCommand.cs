using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ProjectCommand : TitaniumCommand
{
    private readonly ConfigManager _config;

    public ProjectCommand(ConfigManager config) : base("project", "Manage project")
    {
        _config = config;
    }

    public override List<Option> DefineOptions() => new();

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        List<ProjectConfig> projects = _config.GetProjects();
        foreach (ProjectConfig project in projects)
        {
            Console.WriteLine(project.Name);
        }

        return Task.FromResult(0);
    }
}