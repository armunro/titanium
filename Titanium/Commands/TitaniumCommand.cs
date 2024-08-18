using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public abstract class TitaniumCommand : Command, ICommandHandler
{
    public ConfigManager Config { get; set; }

    protected TitaniumCommand(string name, string? description, ConfigManager config) : base(name, description)
    {
        Config = config;
        this.Handler = this;
    }

    public abstract List<Option> DefineOptions();
    public abstract Task<int> HandleAsync(InvocationContext context);

    //CommandHandler
    public int Invoke(InvocationContext context) => HandleAsync(context).Result;
    public Task<int> InvokeAsync(InvocationContext context) => HandleAsync(context);
}