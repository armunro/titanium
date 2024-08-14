using System.CommandLine.Invocation;
using CSharpVitamins;

namespace Titanium.Commands;

public class SGuidCommandHandler : ICommandHandler
{
    public int Invoke(InvocationContext context)
    {
   
            string shortGuid = ShortGuid.NewGuid().ToString();
            Console.WriteLine(shortGuid);
            return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }
}