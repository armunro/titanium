﻿using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand( "list", "List documents" )]
public class ListCommand : CliCommand
{
    private readonly ConfigManager _config;
    private readonly ILogger _logger;

    public ListCommand(ConfigManager config, ILogger logger ) 
    {
        _config = config;
        _logger = logger;
    }

    

    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        foreach (string docName in _config.GetDocNames())
        {
            _logger.Information(docName);
        }

        return Task.FromResult(0);
    }
    
}