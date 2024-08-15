using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ConfigCommand : ICommandHandler
{
    public static readonly Option<string> ConfigKeyOption = new("--key", "The configuration key");
    public static readonly Option<string> ConfigValueOption = new("--value", "The configuration value");
    private readonly ConfigManager _config;
    private readonly ILogger _logger;

    public ConfigCommand(ConfigManager config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }


    public int Invoke(InvocationContext context)
    {
        RootConfig rootConfig = _config.RootConfig;
        string? key = context.ParseResult.GetValueForOption(ConfigKeyOption);
        string? value = context.ParseResult.GetValueForOption(ConfigValueOption);
        GetOrSetConfigProperty(rootConfig, key, value);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context) => Task.FromResult(Invoke(context));

    private void GetOrSetConfigProperty(RootConfig rootConfig, string? key, string? value)
    {
        if (key == null && value == null)
            _logger.Information(_config.GetConfigYaml());
        else if (value == null)
        {
            object? property = rootConfig.GetType().GetProperty(key)?.GetValue(rootConfig);
            _logger.Information("{Key}: {Value}", key, property);  
        }
        else
        {
            rootConfig.GetType().GetProperty(key)?.SetValue(rootConfig, value);
            _logger.Information( "Set {Key} to {Value}", key, value);
            _config.SaveConfig();
        }
    }

}