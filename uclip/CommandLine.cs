using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Checkers;

namespace uclip
{
    public class CommandLine
    {
        // CommandName => CommandMethod
        private Dictionary<string, MethodInfo> commandsDic;

        public List<Type> SearchInTypes { get; } = new List<Type>();
        public List<Assembly> SearchInAssemblies { get; } = new List<Assembly>();

        public string HelpStart { get; set; }

        public bool CommandsInCache { get; set; }

        public CommandLine()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            Assembly commandLineAssembly = typeof(CommandLine).Assembly;

            SearchInAssemblies.Add(commandLineAssembly);

            if(commandLineAssembly != entryAssembly)
                SearchInAssemblies.Add(entryAssembly);
        }

        public void FindAllCommands()
        {
            if (CommandsInCache)
                return;

            if (SearchInTypes.Count == 0)
                SearchInTypes.AddRange(SearchInAssemblies.SelectMany(a => a.GetTypes()));

            var commands = SearchInTypes.SelectMany(t => t.GetMethods()
                .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)).ToList();

            commandsDic = new Dictionary<string, MethodInfo>();

            foreach (MethodInfo methodInfo in commands)
            {
                string name = GetCommandName(methodInfo);
                if(commandsDic.ContainsKey(name) && commandsDic[name].DeclaringType == methodInfo.DeclaringType)
                    Error($"More than one method registered for the same command: 'name' in the same assembly");
                commandsDic[name] = methodInfo;
            }

            CommandsInCache = true;
        }

        private string GetCommandName(MethodInfo methodInfo)
        {
            var name = methodInfo.GetCustomAttribute<CommandAttribute>().Name;
            return string.IsNullOrEmpty(name) ? methodInfo.Name : name;
        }

        public void Execute(string[] args)
        {
            FindAllCommands();
            FindMatchingCommandAndRun(args);
        }

        [Command("Help", IsDefault = true, Description = "Print help")]
        public void PrintHelp()
        {
            FindAllCommands();

            Console.WriteLine(HelpStart);
            Console.WriteLine(GetCommandsDescription());
        }

        private string GetCommandsDescription()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string,MethodInfo> cmdPair in commandsDic)
            {
                var mi = cmdPair.Value;
                var attr = mi.GetCustomAttribute<CommandAttribute>();
                Check.NotNull(attr, "attr");

                sb.Append(cmdPair.Key + ": ");
                if (attr.IsDefault) sb.Append("Default. ");
                sb.AppendLine(attr.Description);
            }

            return sb.ToString();
        }


        public void FindMatchingCommandAndRun(string[] args)
        {

        }

        public static void Execute<T>(T objectWithCommands, string[] args)
        {
//            var commands = FindAllCommands(new[] {typeof(T)});
//            FindMatchingCommandAndRun(commands, args);
            try
            {
                List<MethodInfo> commandMethods = typeof(T).GetMethods()
                    .Where(m => CustomAttributeExtensions.GetCustomAttribute<CommandAttribute>(m) != null)
                    .ToList();

                if (commandMethods.Count(m => m.GetCustomAttribute<CommandAttribute>().IsDefault) > 1)
                    Error("Only one command can be marked as default");

                MethodInfo commandMethod;
                if (args.Length == 0)
                {
                    commandMethod = commandMethods.FirstOrDefault(m =>
                        m.GetCustomAttribute<CommandAttribute>().IsDefault);
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
//                PrintHelp();
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

        private static void Error(string message)
        {
            throw new CommandLineException(message);
        }
    }
}