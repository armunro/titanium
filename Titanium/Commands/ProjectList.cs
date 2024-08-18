using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ProjectList : TitaniumCommand
{
    public ProjectList(ConfigManager config) : base("list", "List all projects", config)
    {
    }

    public override List<Option> DefineOptions() => new();

    public override Task<int> HandleAsync(InvocationContext context)
    {
        List<ProjectConfig> projects = Config.GetProjects();
        foreach (ProjectConfig project in projects)
        {
            Console.WriteLine(project.Name);
        }

        return Task.FromResult(0);
    }
}