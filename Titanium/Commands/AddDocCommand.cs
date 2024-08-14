using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;
using Titanium.Domain.Document;

namespace Titanium.Commands;

public class AddDocCommand : ICommandHandler
{
    ConfigManager _config;

    public static Option<string> SourceOption = new("--source", "The source of the document");

    public AddDocCommand(ConfigManager config)
    {
        _config = config;
    }

    public int Invoke(InvocationContext context)
    {
        string? source = context.ParseResult.GetValueForOption(SourceOption);
        
        // loop
        
        //Create the blank
        Doc doc = _config.AddDoc();

        if (!string.IsNullOrWhiteSpace(source))
        {
            _config.ImportMasters(doc, source);
        }
        _config.SaveDoc(doc);

        
        Console.WriteLine(doc.Id);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }

 
}