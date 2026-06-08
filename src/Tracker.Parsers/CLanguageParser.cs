using Tracker.Core.Interfaces;
using Tracker.Core.Models;

namespace Tracker.Parsers;

// Stub — parser implementation to be built separately
public class CLanguageParser : ILanguageParser
{
    public string LanguageName => "c";
    public IEnumerable<string> SupportedExtensions => [".c", ".h"];

    public ParseResult Parse(string filePath, string content)
        => new([], [], []);
}
