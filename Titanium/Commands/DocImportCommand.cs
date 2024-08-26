using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class DocImportCommand : TitaniumCommand
{
    private readonly ConfigManager _configManager;
    public static Argument<string> SourceArgument = new("source", "The source of the document");

    public DocImportCommand(ConfigManager configManager) : base("import", "Create a new document from existing files.")
    {
        _configManager = configManager;
    }

    public override List<Argument> DefineArguments() => new() { SourceArgument };

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        string source = context.ParseResult.GetValueForArgument(SourceArgument);
        Doc doc = _configManager.AddDoc();
        if (!string.IsNullOrWhiteSpace(source)) _configManager.ImportMasters(doc, source);
        _configManager.SaveDoc(doc);
        Console.WriteLine(doc.Id);
        return Task.FromResult(0);
    }
}