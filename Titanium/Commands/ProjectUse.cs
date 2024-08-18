using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ProjectUse : TitaniumCommand
{
    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");

    private readonly ILogger _logger;


    public ProjectUse(ConfigManager config, ILogger logger) : base("use", "Use a project", config)
    {
        _logger = logger;
    }

    public override List<Option> DefineOptions() => new() { ProjectNameOption };

    public override Task<int> HandleAsync(InvocationContext context)
    {
        string projectName = context.ParseResult.GetValueForOption(ProjectNameOption)!;
        Config.UseProject(projectName);
        _logger.Information("Using project `{Project}`", projectName);
        Config.SaveConfig();
        return Task.FromResult(0);
    }
}