using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace uclip
{
    public class CommandLine
    {
        public static void Execute<T>(T objectWithCommands, string[] args)
        {
            try
            {
                List<MethodInfo> commandMethods = typeof(T).GetMethods()
                    .Where(m => CustomAttributeExtensions.GetCustomAttribute<CommandAttribute>(m) != null)
                    .ToList();

                if (commandMethods.Count(m => m.GetCustomAttribute<CommandAttribute>().DefaultCommand) > 1)
                    Error("Only one command can be marked as default");

                MethodInfo commandMethod;
                if (args.Length == 0)
                {
                    commandMethod = commandMethods.FirstOrDefault(m =>
                        m.GetCustomAttribute<CommandAttribute>().DefaultCommand);
                }
                else
                {
                    var command = args[0];
                    var foundcommands = commandMethods.Where(mi => { return IsMethodMatchCommand(mi, command); })
                        .ToList();
                    if (foundcommands.Count > 1) Error("More than one method match command " + command);
                    if (foundcommands.Count == 0) Error($"Command {command} is not found");
                    commandMethod = foundcommands[0];
                }

                if (commandMethod == null)
                    Error("No command specified");

                var pars = FillParameters(commandMethod, args.Skip(1).ToList());
                commandMethod.Invoke(objectWithCommands, pars);
            }
            catch (CommandLineException exception)
            {
                Console.WriteLine("Invalid command line: " + exception.Message);
                PrintHelp();
            }
        }

        private static object[] FillParameters(MethodInfo methodInfo, IList<string> args)
        {
            int num = 0;
            return methodInfo.GetParameters().Select(pi =>
            {
                try
                {
                    if (pi.HasDefaultValue && num >= args.Count)
                    {
                        return Type.Missing;
                    }

                    // todo convert to arg's type
                    return args[num];
                }
                finally
                {
                    num++;                    
                }

            }).ToArray();
        }

        private static bool IsMethodMatchCommand(MethodInfo mi, string command)
        {
            return mi.Name.ToLowerInvariant() == command.ToLowerInvariant();
        }

        private static void PrintHelp()
        {
        }

        private static void Error(string message)
        {
            throw new CommandLineException(message);
        }
    }
}