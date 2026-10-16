using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Parses the profile's "Additional Environment Variables" text: whitespace-separated <c>NAME=VALUE</c>
    /// assignments with shell-like quoting. A value is taken literally outside quotes (backslashes are kept, so
    /// Windows paths need no escaping), literally inside single quotes, and inside double quotes with only
    /// <c>\"</c> and <c>\\</c> as escapes. Quoted and unquoted parts may be adjacent. There is no <c>$VAR</c>
    /// expansion. A repeated name keeps its last value.
    /// </summary>
    public static class EnvironmentVariableParser
    {
        private static readonly Regex NameRegex = new("^[A-Za-z_][A-Za-z0-9_]*$");

        /// <summary>
        /// Returns the assignments in the order they first appear. Throws <see cref="ArgumentException"/> with a
        /// user-facing message on malformed input.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Parse(string? text)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var i = 0;
            while (true)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                if (i >= text.Length) break;

                var tokenStart = i;
                while (i < text.Length && text[i] != '=' && !char.IsWhiteSpace(text[i])) i++;
                var name = text[tokenStart..i];

                if (i >= text.Length || text[i] != '=')
                    throw new ArgumentException(string.Format(Strings.Validation_EnvVarMissingEquals, RawToken(text, tokenStart)));
                if (!NameRegex.IsMatch(name))
                    throw new ArgumentException(string.Format(Strings.Validation_EnvVarInvalidName, name.Length > 0 ? name : RawToken(text, tokenStart)));

                i++; // the '='
                var value = ReadValue(text, ref i, tokenStart);

                var existing = result.FindIndex(kvp => kvp.Key == name);
                if (existing >= 0) result[existing] = new(name, value);
                else result.Add(new(name, value));
            }

            return result;
        }

        private static string ReadValue(string text, ref int i, int tokenStart)
        {
            var value = new StringBuilder();
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
            {
                var c = text[i];
                if (c == '\'')
                {
                    var close = text.IndexOf('\'', i + 1);
                    if (close < 0) throw Unterminated(text, tokenStart);
                    value.Append(text, i + 1, close - i - 1);
                    i = close + 1;
                }
                else if (c == '"')
                {
                    i++;
                    while (true)
                    {
                        if (i >= text.Length) throw Unterminated(text, tokenStart);
                        var q = text[i];
                        if (q == '"') { i++; break; }
                        if (q == '\\' && i + 1 < text.Length && (text[i + 1] == '"' || text[i + 1] == '\\'))
                        {
                            value.Append(text[i + 1]);
                            i += 2;
                            continue;
                        }
                        value.Append(q);
                        i++;
                    }
                }
                else
                {
                    value.Append(c);
                    i++;
                }
            }
            return value.ToString();
        }

        private static ArgumentException Unterminated(string text, int tokenStart)
            => new(string.Format(Strings.Validation_EnvVarUnterminatedQuote, text[tokenStart..].TrimEnd()));

        // The token as typed, up to the next whitespace, for naming it in an error message.
        private static string RawToken(string text, int start)
        {
            var end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
            return text[start..end];
        }
    }
}
