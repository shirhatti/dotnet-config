using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Config
{
    internal class ShowCommand
    {
        internal static Command CreateCommand()
        {
            var command = new Command(
                name: "show",
                description: "Show configuration for the dotnet app")
            {
                Handler = CommandHandler.Create<IConsole, string>(Show)
            };
            command.AddArgument(new Argument<string>(
                "command",
                getDefaultValue: () => "dotnet run",
                description: "The ASP.NET Core application to extract configuration from"
            ));
            return command;
        }

        private static async Task Show(IConsole console, string command)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cts.Cancel();
            };
            var parsedCommand = command.Split(' ', 2);
            ProcessStartInfo psi = parsedCommand.Length switch
            {
                1 => new ProcessStartInfo(parsedCommand[0]),
                2 => new ProcessStartInfo(parsedCommand[0], parsedCommand[1]),
                _ => default
            };
            var currentAssembly = Assembly.GetExecutingAssembly().Location;
            var reloadIntegrationPath = Path.GetFullPath(Path.Combine(currentAssembly, "..", "ConfigurationStartupHook.dll"));
            const string dotnetStartHooksName = "DOTNET_STARTUP_HOOKS";
            psi.EnvironmentVariables[dotnetStartHooksName] = AddOrAppend(dotnetStartHooksName, reloadIntegrationPath, Path.PathSeparator);

            using var initialProcess = Process.Start(psi);
            await initialProcess.WaitForExitAsync(cts.Token);

            try
            {
                initialProcess.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // process has already exited.
            }
        }

        private static string AddOrAppend(string envVarName, string envVarValue, char separator)
        {
            var existing = Environment.GetEnvironmentVariable(envVarName);

            if (!string.IsNullOrEmpty(existing))
            {
                return $"{existing}{separator}{envVarValue}";
            }
            else
            {
                return envVarValue;
            }
        }
    }
}