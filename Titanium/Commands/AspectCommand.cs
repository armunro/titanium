﻿using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Paths;

namespace Titanium.Commands;

[CliCommand("aspect", "Derive an aspect")]
public class AspectCommand : CliCommand
{
    [CliArgument("doc", "The id of the document")]
    public static Argument<string> DocumentIdArg = new("doc", "The id of the document");

    [CliArgument("aspect", "The aspect to derive.")]
    public static Argument<string> AspectArg = new("aspect", "The aspect to derive.");

    private readonly DocumentProcessor _documentProcessor;
    private readonly PathScaffolder _scaffolder;
    private readonly ConfigManager _config;
    private readonly IEnumerable<Lazy<IAspectProcessor, AspectMetadataAttribute>> _processors;
    private readonly ILogger _logger;


    public AspectCommand(ConfigManager config,
        DocumentProcessor documentProcessor,
        PathScaffolder scaffolder,
        ILogger logger,
        IEnumerable<Lazy<IAspectProcessor, AspectMetadataAttribute>> processors)
    {
        _config = config;
        _documentProcessor = documentProcessor;
        _scaffolder = scaffolder;
        _logger = logger;
        _processors = processors;
    }


    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        string? docId = context.Argument<string>(DocumentIdArg);
        string? aspectName = context.Argument<string>(AspectArg);
        AspectMetadataAttribute meta = AspectMetadataAttribute.Parse(aspectName);
        Doc doc = _config.GetDoc(docId);


        _scaffolder.ScaffoldDocumentAspectDirectory(_config.CurrentProject, docId, "ocr.text");
        List<DocAspect> aspects = _documentProcessor.ProcessDocument(doc, masterFile =>
        {
            List<DocAspect> newAspects = new List<DocAspect>();
            IEnumerable<Lazy<IAspectProcessor, AspectMetadataAttribute>> enumerable = _processors
                .Where(x => x.Metadata.Type == meta.Type)
                .Where(x => x.Metadata.Variant == meta.Variant || meta.Variant == "*");
            foreach (Lazy<IAspectProcessor, AspectMetadataAttribute> processor in enumerable)
            {
                _logger.Information("Creating aspect `{Type}.{Variant}`", processor.Metadata.Type,
                    processor.Metadata.Variant);
                newAspects.Add(processor.Value.ProcessAspect(doc, masterFile));
            }

            return newAspects;
        });
        doc.AddAspects(aspects);
        _config.SaveDoc(doc);

        return Task.FromResult(0);
    }
}