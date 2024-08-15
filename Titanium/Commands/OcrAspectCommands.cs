using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Adapters.Generators;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class OcrAspectCommands : ICommandHandler
{
    public static Option<string> DocumentIdOption = new("--doc", "The id of the document");
    private readonly ConfigManager _config;
    private readonly DocumentProcessor _documentProcessor;
    private readonly OcrAspectGenerator _ocrAspectGenerator;
    private readonly PathFinder _pathfinder;


    public OcrAspectCommands(ConfigManager config,
        OcrAspectGenerator ocrAspectGenerator,
        PathFinder pathfinder,
        DocumentProcessor documentProcessor)
    {
        _config = config;
        _ocrAspectGenerator = ocrAspectGenerator;
        _pathfinder = pathfinder;
        _documentProcessor = documentProcessor;
    }


    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        string? docId = context.ParseResult.GetValueForOption(DocumentIdOption);
        Doc doc = _config.GetDoc(docId);


        Directory.CreateDirectory(_pathfinder.GetDocAspectPath(_config.CurrentProject, docId, "ocr"));
        List<BaseAspect> aspects =
            _documentProcessor.ProcessDocument(doc, masterFile =>
                _ocrAspectGenerator.GenerateAspects(doc, masterFile));
        aspects.ForEach(aspect => doc.AddAspect(aspect));
        _config.SaveDoc(doc);

        return Task.FromResult(0);
    }
}