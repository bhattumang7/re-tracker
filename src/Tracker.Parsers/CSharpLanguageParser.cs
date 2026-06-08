using Tracker.Core.Interfaces;
using Tracker.Core.Models;

namespace Tracker.Parsers;

public class CSharpLanguageParser : ILanguageParser
{
    public string LanguageName => "csharp";
    public IEnumerable<string> SupportedExtensions => [".cs"];

    public ParseResult Parse(string filePath, string content)
        => new([], [], []);
}
