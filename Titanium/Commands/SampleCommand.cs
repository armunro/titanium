using System.CommandLine.Invocation;

namespace Titanium.Commands;

public class SampleCommand : TitaniumCommand
{
    public SampleCommand() : base("sample", "Generate samaple images.") { }
    protected override Task<int> HandleAsync(InvocationContext context)
    {
        
        // generate sample image
        return Task.FromResult(0);
    }
}