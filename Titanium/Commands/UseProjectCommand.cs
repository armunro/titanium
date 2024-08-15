using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class UseProjectCommand : ICommandHandler
{
    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");


    private readonly ConfigManager _config;
    private readonly ILogger _logger;


    public UseProjectCommand(ConfigManager config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        string projectName = context.ParseResult.GetValueForOption(ProjectNameOption)!;
        _config.UseProject(projectName);
        _logger.Information("Using project `{Project}`", projectName);
        _config.SaveConfig();
        return Task.FromResult(0);
    }
}