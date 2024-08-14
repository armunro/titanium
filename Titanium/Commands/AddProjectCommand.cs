using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class AddProjectCommand : ICommandHandler
{
    private readonly ConfigManager _config;

    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");

    public AddProjectCommand(ConfigManager config)
    {
        _config = config;
    }

    public int Invoke(InvocationContext context)
    {
        _config.CreateProject(context.ParseResult.GetValueForOption(ProjectNameOption));
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }
}