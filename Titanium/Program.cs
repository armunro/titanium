using System.ClientModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Cosmic;
using Cosmic.Aspects.Logs;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Extensions;
using OpenAI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Titanium;
using Titanium.Commands;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Paths;

CliApp app = new();

app.RegisterDependencies(builder =>
{
    builder.RegisterCosmicLogging();
    builder.RegisterCosmicCommands();
    builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();
    builder.RegisterType<DocumentProcessor>().AsSelf();
    builder.RegisterType<Installer>().AsSelf();
    builder.RegisterType<PathFinder>().AsSelf().SingleInstance();
    builder.RegisterType<PathScaffolder>().AsSelf().SingleInstance();
    builder.RegisterType<ApiKeyCredential>().AsSelf();
    builder.Register<OpenAIClient>((c, p) =>
        new OpenAIClient(new ApiKeyCredential(c.Resolve<ConfigManager>().RootConfig.OpenAIApiKey)));
    builder.RegisterModule<AttributedMetadataModule>();
    builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();

    builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly()!)
        .Where(x => x.IsAssignableTo<IAspectProcessor>())
        .As<IAspectProcessor>()
        .WithAttributedMetadata<AspectMetadataAttribute>();
}).StartsWith(container =>
{
    container.Resolve<ProjectCommand>().Add(container.Resolve<ProjectAddCommand>());
    container.Resolve<ProjectCommand>().Add(container.Resolve<ProjectUseCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<ProjectCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<ListCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<NewCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<ImportCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<ConfigCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<InstallCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<AspectCommand>());
    container.Resolve<RootCommand>().Add(container.Resolve<SampleCommand>());
    Parser parser = new CommandLineBuilder(container.Resolve<RootCommand>()).UseDefaults().Build();
    parser.InvokeAsync(args).Wait();
}).Build();
app.Start();