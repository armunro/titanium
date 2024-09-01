using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand( "config", "Get or set configuration values" )]
public class ConfigCommand : CliCommand
{
    [CliOption( "--key", "The configuration key" )]
    public static readonly Option<string> ConfigKeyOption = new("--key", "The configuration key");
    [CliOption( "--value", "The configuration value" )]
    public static readonly Option<string> ConfigValueOption = new("--value", "The configuration value");

    private readonly ConfigManager _config;
    private readonly ILogger _logger;

    public ConfigCommand(ConfigManager config, ILogger logger) 
    {
        _config = config;
        _logger = logger;
    }


   

    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        RootConfig rootConfig = _config.RootConfig;
        string? key = context.Option<string>(ConfigKeyOption);
        string? value = context.Option<string>(ConfigValueOption);
        GetOrSetConfigProperty(rootConfig, key, value);
        return Task.FromResult(0);
    }


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
            _logger.Information("Set {Key} to {Value}", key, value);
            _config.SaveConfig();
        }
    }
}