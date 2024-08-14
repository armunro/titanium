using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ViewDocCommand : ICommandHandler
{
    public static Option<string> DocumentIdOption = new("--doc", "The id of the document");
    private readonly ConfigManager _config;

    public ViewDocCommand(ConfigManager config)
    {
        _config = config;
    }
    public int Invoke(InvocationContext context)
    {
        ViewRenderer render = new(_config);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        
        return Task.FromResult(Invoke(context));
    }
}