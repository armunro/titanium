using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Titanium.Commands;
using Titanium.Domain.Aspect;

namespace Titanium.Domain.Extensions;

public static class CommandLineExtensions
{
    public static void RegisterCosmicCommands(this ContainerBuilder builder)
    {

        builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
            .Where(t => t.IsAssignableTo<TitaniumCommand>());

    }
    
    public static void RegisterProcessors(this ContainerBuilder builder)
    {

        builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
            .Where(t => t.IsAssignableTo<IAspectProcessor>()).WithAttributedMetadata<AspectMetadataAttribute>();

    }
}