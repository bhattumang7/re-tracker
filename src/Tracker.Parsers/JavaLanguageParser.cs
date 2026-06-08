using Tracker.Core.Interfaces;
using Tracker.Core.Models;

namespace Tracker.Parsers;

public class JavaLanguageParser : ILanguageParser
{
    public string LanguageName => "java";
    public IEnumerable<string> SupportedExtensions => [".java"];

    public ParseResult Parse(string filePath, string content)
        => new([], [], []);
}
