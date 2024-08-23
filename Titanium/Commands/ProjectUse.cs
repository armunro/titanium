using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ProjectUse : TitaniumCommand
{
    public static readonly Argument<string> ProjectNameArg = new("--name", "The name of the project");

    private readonly ConfigManager _config;
    private readonly ILogger _logger;


    public ProjectUse(ConfigManager config, ILogger logger) : base("use", "Use a project")
    {
        _config = config;
        _logger = logger;
    }

    public override List<Argument> DefineArguments()
    {
        return new List<Argument>() { ProjectNameArg };
    }

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        string projectName = context.ParseResult.GetValueForArgument(ProjectNameArg)!;
        _config.UseProject(projectName);
        _logger.Information("Using project `{Project}`", projectName);
        _config.SaveConfig();
        return Task.FromResult(0);
    }
}