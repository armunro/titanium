using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class UseProjectCommand : ICommandHandler
{
    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");


    private readonly ConfigManager _config;


    public UseProjectCommand(ConfigManager config)
    {
        _config = config;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        _config.UseProject(context.ParseResult.GetValueForOption(ProjectNameOption)!);
        _config.SaveConfig();
        return Task.FromResult(0);
    }
}