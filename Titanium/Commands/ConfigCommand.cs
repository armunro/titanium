using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ConfigCommand : ICommandHandler
{
    private readonly ConfigManager _config;

    public ConfigCommand(ConfigManager config)
    {
        _config = config;
    }


    public int Invoke(InvocationContext context)
    {
        Console.WriteLine(_config.GetConfigYaml());
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        Console.WriteLine(_config.GetConfigYaml());
        return Task.FromResult(0);        
    }
}