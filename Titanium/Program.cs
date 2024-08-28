using System.ClientModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Cosmic;
using OpenAI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Titanium;
using Titanium.Commands;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Paths;
ILogger Logger(IComponentContext componentContext) =>
    new LoggerConfiguration().MinimumLevel.Information()
        .WriteTo.File(Path.Combine(componentContext.Resolve<PathFinder>().GetLogPath(), "log-.txt"),
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code).CreateLogger();

ContainerBuilder builder = new();
builder.RegisterModule<AttributedMetadataModule>();
builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();

builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly()!)
    .Where(t => t.IsAssignableTo<TitaniumCommand>());
builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly()! )
    .Where(x=>x.IsAssignableTo<IAspectProcessor>())
    .As<IAspectProcessor>()
    .WithAttributedMetadata<AspectMetadataAttribute>();


builder.RegisterType<DocumentProcessor>().AsSelf();
builder.RegisterType<Installer>().AsSelf();
builder.RegisterType<PathFinder>().AsSelf().SingleInstance();
builder.RegisterType<PathScaffolder>().AsSelf().SingleInstance();
builder.RegisterType<ApiKeyCredential>().AsSelf();
builder.Register<OpenAIClient>((c, p) =>
    new OpenAIClient(new ApiKeyCredential(c.Resolve<ConfigManager>().RootConfig.OpenAIApiKey)));
builder.Register<ILogger>((c, p) => { return Logger(c); }).SingleInstance();

IContainer container = builder.Build();

RootCommand rootCommand = new("[Ti]tanium - Lightweight document management & processing");

Command listCommand = container.Resolve<ListCommand>();
Command projectCommand = container.Resolve<ProjectCommand>();
Command addProjectCommand = container.Resolve<ProjectAddCommand>();
Command useProjectCommand = container.Resolve<ProjectUseCommand>();
Command aspectCommand = container.Resolve<AspectCommand>();
Command configCommand = container.Resolve<ConfigCommand>();
Command docImportCommand = container.Resolve<DocImportCommand>();
Command docNewCommand = container.Resolve<DocNewCommand>();
Command installCommand = container.Resolve<InstallCommand>();
Command sampleCommand = container.Resolve<SampleCommand>();

projectCommand.AddCommand(addProjectCommand);
projectCommand.AddCommand(useProjectCommand);


rootCommand.AddCommand(projectCommand);
rootCommand.AddCommand(listCommand);
rootCommand.AddCommand(docNewCommand);
rootCommand.Add(docImportCommand);
rootCommand.AddCommand(configCommand);
rootCommand.AddCommand(installCommand);
rootCommand.AddCommand(aspectCommand);
rootCommand.AddCommand(sampleCommand);

Parser parser = new CommandLineBuilder(rootCommand).UseDefaults().Build();

await parser.InvokeAsync(args);