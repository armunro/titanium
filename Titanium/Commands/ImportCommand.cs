using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Titanium.Domain;
using Titanium.Domain.Config;

namespace Titanium.Commands;

[CliCommand("import", "Create a new document from existing files.")]
public class ImportCommand : CliCommand
{
    private readonly ConfigManager _configManager;

    [CliArgument("source", "The source of the document")]
    public static Argument<string> SourceArgument = new("source", "The source of the document");

    public ImportCommand(ConfigManager configManager)
    {
        _configManager = configManager;
    }
    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        string source = context.Argument<string>(SourceArgument);
        Doc doc = _configManager.AddDoc();
        if (!string.IsNullOrWhiteSpace(source)) _configManager.ImportMasters(doc, source);
        _configManager.SaveDoc(doc);
        Console.WriteLine(doc.Id);
        return Task.FromResult(0);
    }
}