using Microsoft.DotNet.Config;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

new CommandLineBuilder()
    .AddCommand(ShowCommand.CreateCommand())
    .UseDefaults()
    .Build()
    .Parse(args)
    .InvokeAsync();
