using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;


public class DocAdd : TitaniumCommand
{
    public static Option<string> SourceOption = new("--source", "The source of the document");


    public DocAdd(ConfigManager configManager) : base("add", "Add a new document", configManager)
    {
    }

    public override List<Option> DefineOptions() => new() { SourceOption };

    public override Task<int> HandleAsync(InvocationContext context)
    {
        string? source = context.ParseResult.GetValueForOption(SourceOption);
        Doc doc = Config.AddDoc();
        if (!string.IsNullOrWhiteSpace(source)) Config.ImportMasters(doc, source);
        Config.SaveDoc(doc);
        Console.WriteLine(doc.Id);
        return Task.FromResult(0);
    }
    
}