using System.CommandLine;

namespace Titanium.Extensions;

public static class CommandLineExtensions
{
    public static void AddSubcommands(this Command command, params Command[] subcommands)
    {
        foreach (Command subcommand in subcommands)
        {
            command.AddCommand(subcommand);
        }
        
    }
    public static void AddOptions(this Command command, params Option[] subcommands)
    {
        foreach (Option subcommand in subcommands)
        {
            command.AddOption(subcommand);
        }
        
    }
}