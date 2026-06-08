using System.Text.RegularExpressions;
using Tracker.Core.Interfaces;
using Tracker.Core.Models;

namespace Tracker.Parsers;

public class CLanguageParser : ILanguageParser
{
    public string LanguageName => "c";
    public IEnumerable<string> SupportedExtensions => [".c", ".h"];

    // // === mem_alloc @ 0fa916b8  (size=18) ===
    private static readonly Regex MarkerRx = new(
        @"^// === (\w+) @ [0-9a-f]+\s+\(size=\d+\) ===$",
        RegexOptions.Compiled);

    // undefined4 mem_alloc(undefined4 param_1)
    private static readonly Regex SignatureRx = new(
        @"^(.+?)\s+(\w+)\s*\(([^)]*)\)\s*$",
        RegexOptions.Compiled);

    // short *pExcepInfo  →  type="short *"  name="pExcepInfo"
    private static readonly Regex ParamRx = new(
        @"^(.*?)\s*(\*+)?\s*(\w+)\s*$",
        RegexOptions.Compiled);

    public ParseResult Parse(string filePath, string content)
    {
        var lines = content.Split('\n');
        var methods = new List<ParsedMethod>();

        int i = 0;
        while (i < lines.Length)
        {
            var markerMatch = MarkerRx.Match(lines[i].TrimEnd());
            if (!markerMatch.Success) { i++; continue; }

            var expectedName = markerMatch.Groups[1].Value;

            // First non-blank, non-comment line after the marker is the signature.
            // Ghidra emits /* WARNING: ... */ blocks between the marker and signature.
            int sigIdx = i + 1;
            while (sigIdx < lines.Length)
            {
                var trim = lines[sigIdx].TrimStart();
                if (string.IsNullOrWhiteSpace(trim))        { sigIdx++; continue; }
                if (trim.StartsWith("/*"))
                {
                    while (sigIdx < lines.Length && !lines[sigIdx].Contains("*/")) sigIdx++;
                    sigIdx++;
                    continue;
                }
                if (trim.StartsWith("//"))                  { sigIdx++; continue; }
                break;
            }

            if (sigIdx >= lines.Length) break;

            // Ghidra occasionally wraps long parameter lists onto the next line.
            // Collect lines until we see the closing ')'.
            var sigBuilder = new System.Text.StringBuilder(lines[sigIdx].TrimEnd());
            for (int k = sigIdx + 1; !sigBuilder.ToString().Contains(')') && k < lines.Length; k++)
            {
                var nl = lines[k].Trim();
                if (string.IsNullOrEmpty(nl) || MarkerRx.IsMatch(nl)) break;
                sigBuilder.Append(' ').Append(nl);
            }
            var sigText  = sigBuilder.ToString();
            var sigMatch = SignatureRx.Match(sigText);

            if (!sigMatch.Success)
            {
                i = sigIdx;
                continue;
            }

            var returnType = sigMatch.Groups[1].Value.Trim();
            var name       = sigMatch.Groups[2].Value;
            var paramsRaw  = sigMatch.Groups[3].Value.Trim();
            int parenCol   = sigText.IndexOf('(');

            // Find opening brace — bail if we hit the next marker first
            int bodyStartIdx = sigIdx + 1;
            while (bodyStartIdx < lines.Length)
            {
                var l = lines[bodyStartIdx].TrimEnd();
                if (l == "{") break;
                if (MarkerRx.IsMatch(l)) { bodyStartIdx = -1; break; }
                bodyStartIdx++;
            }

            if (bodyStartIdx < 0 || bodyStartIdx >= lines.Length)
            {
                i = sigIdx + 1;
                continue;
            }

            // Track brace depth line-by-line to find the closing brace
            int depth = 0, bodyEndIdx = bodyStartIdx;
            for (int j = bodyStartIdx; j < lines.Length; j++)
            {
                foreach (char c in lines[j])
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                if (depth == 0) { bodyEndIdx = j; break; }
            }

            methods.Add(new ParsedMethod(
                name,
                ClassName:       null,
                ReturnType:      returnType,
                StartLine:       sigIdx + 1,        // 1-indexed
                StartColumn:     0,
                EndLine:         sigIdx + 1,
                EndColumn:       sigText.Length,
                BodyStartLine:   bodyStartIdx + 1,
                BodyStartColumn: 0,
                BodyEndLine:     bodyEndIdx + 1,
                BodyEndColumn:   1,
                Parameters:      ParseParameters(paramsRaw, sigIdx, parenCol + 1)
            ));

            i = bodyEndIdx + 1;
        }

        return new ParseResult([], methods, []);
    }

    // baseCol = column of the first character inside the opening '('
    private IReadOnlyList<ParsedParameter> ParseParameters(string raw, int sigIdx, int baseCol)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var result  = new List<ParsedParameter>();
        int pos     = 0;   // current offset within `raw`
        int ordinal = 0;

        while (pos <= raw.Length)
        {
            int commaIdx = raw.IndexOf(',', pos);
            if (commaIdx < 0) commaIdx = raw.Length;

            var slice   = raw.Substring(pos, commaIdx - pos);
            var trimmed = slice.Trim();
            if (string.IsNullOrEmpty(trimmed)) { pos = commaIdx + 1; continue; }

            int leadingWs = slice.Length - slice.TrimStart().Length;
            int startCol  = baseCol + pos + leadingWs;
            int endCol    = startCol + trimmed.Length;

            var m = ParamRx.Match(trimmed);
            string type, paramName;
            if (!m.Success)
            {
                type = trimmed; paramName = trimmed;
            }
            else
            {
                var baseType = m.Groups[1].Value.Trim();
                var stars    = m.Groups[2].Value;
                paramName    = m.Groups[3].Value;
                type         = string.IsNullOrEmpty(stars) ? baseType : $"{baseType} {stars}".Trim();
                if (string.IsNullOrEmpty(type)) type = paramName;
            }

            result.Add(new ParsedParameter(paramName, type, ordinal++, sigIdx + 1, startCol, sigIdx + 1, endCol));
            pos = commaIdx + 1;
        }

        return result;
    }
}
