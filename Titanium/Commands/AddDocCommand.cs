using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class AddDocCommand : ICommandHandler
{
    public static Option<string> SourceOption = new("--source", "The source of the document");
    private readonly ILogger _logger;
    private readonly ConfigManager _config;

    public AddDocCommand(ConfigManager config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public int Invoke(InvocationContext context)
    {
        string? source = context.ParseResult.GetValueForOption(SourceOption);

        // loop

        //Create the blank
        Doc doc = _config.AddDoc();

        if (!string.IsNullOrWhiteSpace(source)) _config.ImportMasters(doc, source);

        _config.SaveDoc(doc);


        Console.WriteLine(doc.Id);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }
}