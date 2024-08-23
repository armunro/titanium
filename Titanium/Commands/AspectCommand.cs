using System.CommandLine;
using System.CommandLine.Invocation;
using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Paths;

namespace Titanium.Commands;

public class AspectCommand : TitaniumCommand
{
    public static Argument<string> DocumentIdArg = new("doc", "The id of the document");
    public static Argument<string> AspectArg = new("aspect", "The aspect to derive.");
    private readonly DocumentProcessor _documentProcessor;
    private readonly PathScaffolder _scaffolder;
    private readonly ILogger _logger;
    private readonly ConfigManager _config;
    private readonly IEnumerable<Lazy<IAspectProcessor, AspectMetadataAttribute>> _processors;


    public AspectCommand(ConfigManager config,
        DocumentProcessor documentProcessor,
        PathScaffolder scaffolder, 
        ILogger logger,
        IEnumerable<Lazy<IAspectProcessor, AspectMetadataAttribute>> processors) : base("aspect", "Derive a document aspect.")
    {
        _config = config;
        _documentProcessor = documentProcessor;
        _scaffolder = scaffolder;
        _logger = logger;
        _processors = processors;
    }

    public override List<Argument> DefineArguments() => new() { DocumentIdArg, AspectArg };

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        string? docId = context.ParseResult.GetValueForArgument(DocumentIdArg);
        string? aspectName = context.ParseResult.GetValueForArgument(AspectArg);
        AspectMetadataAttribute meta = AspectMetadataAttribute.Parse(aspectName);
        Doc doc = _config.GetDoc(docId);


        _scaffolder.ScaffoldDocumentAspectDirectory(_config.CurrentProject, docId, "ocr.text");
        List<Aspect> aspects = _documentProcessor.ProcessDocument(doc, masterFile =>
        {
            List<Aspect> newAspects = new List<Aspect>(); 
            var enumerable = _processors.Where(x => x.Metadata.Type == meta.Type)
                .Where(x => x.Metadata.Variant == meta.Variant || meta.Variant == "*");
            foreach (Lazy<IAspectProcessor,AspectMetadataAttribute> processor in enumerable)
            {
                _logger.Information("Creating aspect `{Type}.{Variant}`", processor.Metadata.Type, processor.Metadata.Variant);
                newAspects.Add(processor.Value.ProcessAspect(doc, masterFile));
            }

            return newAspects;
        });
        doc.AddAspects(aspects);
        _config.SaveDoc(doc);

        return Task.FromResult(0);
    }
}