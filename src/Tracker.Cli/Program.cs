using System.CommandLine;
using System.CommandLine.Parsing;
using Tracker.Cli.Commands;

var root = new RootCommand("re-tracker — reverse-engineering readability tracker");
root.Add(NextCommand.Build());
root.Add(DoneCommand.Build());
root.Add(SkipCommand.Build());
root.Add(DeferCommand.Build());
root.Add(ReviewCommand.Build());
root.Add(StartCommand.Build());
root.Add(RenameCommand.Build());
root.Add(ScanCommand.Build());
root.Add(StatusCommand.Build());
root.Add(SearchCommand.Build());
root.Add(InfoCommand.Build());

var parseResult = CommandLineParser.Parse(root, args, new ParserConfiguration());
return await parseResult.InvokeAsync(new InvocationConfiguration(), CancellationToken.None);
