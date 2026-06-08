using Tracker.Core.Models;

namespace Tracker.Core.Interfaces;

public interface ILanguageParser
{
    string LanguageName { get; }
    IEnumerable<string> SupportedExtensions { get; }
    ParseResult Parse(string filePath, string content);
}
