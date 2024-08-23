using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ProjectAdd : TitaniumCommand
{
    private readonly ConfigManager _config;
    public static readonly Option<string> ProjectNameOption = new("--name", "The name of the project");

    public ProjectAdd(ConfigManager config) : base("add", "Create a new project.")
    {
        _config = config;
    }

    public override List<Option> DefineOptions() => new() { ProjectNameOption };

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        _config.CreateProject(context.ParseResult.GetValueForOption(ProjectNameOption));
        return Task.FromResult(0);
    }
}