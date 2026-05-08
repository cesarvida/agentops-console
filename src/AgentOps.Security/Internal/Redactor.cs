using System.Text.RegularExpressions;

namespace AgentOps.Security.Internal
{
    internal static class Redactor
    {
        private static readonly Regex EmailRx = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
        private static readonly Regex LongTokenRx = new(@"\b[a-fA-F0-9]{20,}\b", RegexOptions.Compiled);

        public static string Redact(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var s = EmailRx.Replace(input, "[REDACTED_EMAIL]");
            s = LongTokenRx.Replace(s, "[REDACTED_TOKEN]");
            return s;
        }
    }
}
