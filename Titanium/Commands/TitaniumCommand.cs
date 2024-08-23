using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public abstract class TitaniumCommand : Command, ICommandHandler
{
    protected TitaniumCommand(string name, string? description) : base(name, description)
    {
        Handler = this;
        DefineArguments().ForEach(AddArgument);
        DefineOptions().ForEach(AddOption);
    }

    public virtual List<Option> DefineOptions() => new();
    public virtual List<Argument> DefineArguments() => new();
    protected abstract Task<int> HandleAsync(InvocationContext context);
    public int Invoke(InvocationContext context) => HandleAsync(context).Result;
    public Task<int> InvokeAsync(InvocationContext context) => HandleAsync(context);
}