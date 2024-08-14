using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ListProjectCommand : ICommandHandler
{
    private ConfigManager _config;

    public ListProjectCommand(ConfigManager config)
    {
        _config = config;
    }

    public int Invoke(InvocationContext context)
    {
        List<ProjectConfig> projects =  _config.GetProjects();
        foreach (ProjectConfig project in projects)
        {
            Console.WriteLine(project.Name);
        }
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
     return Task.FromResult(Invoke(context));
    }
}