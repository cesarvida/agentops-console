using System.Text;
using System.Text.RegularExpressions;

namespace AgentOps.Application.Analysis.Pipeline;

/// <summary>
/// Layer 2 — Normalizes content to detect obfuscated attacks:
/// base64, unicode escapes, URL encoding, string concatenation, ROT13.
/// NEVER executes code — purely text transformation.
/// </summary>
public class ContentNormalizer
{
    private static readonly Regex Base64Pattern =
        new(@"[A-Za-z0-9+/]{20,}={0,2}", RegexOptions.Compiled);

    private static readonly Regex UnicodeEscape =
        new(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

    private static readonly Regex UrlEncoding =
        new(@"%([0-9a-fA-F]{2})", RegexOptions.Compiled);

    // "os.re" + "move" → capture: string + "+" + string
    private static readonly Regex StringConcat =
        new(@"""([^""]{1,20})""\s*\+\s*""([^""]{1,20})""", RegexOptions.Compiled);

    // os/*comment*/.remove style
    private static readonly Regex PythonCommentObfuscation =
        new(@"(\w+)/\*[^*]*\*+(?:[^/*][^*]*\*+)*/\.(\w+)", RegexOptions.Compiled);

    // Known dangerous keywords to check after ROT13 decode
    private static readonly string[] DangerousKeywords =
        ["os.remove", "os.unlink", "eval(", "exec(", "subprocess", "requests.post", "pickle"];

    public string Normalize(string content, out bool obfuscationFound)
    {
        bool localObfuscated = false;
        var result = content;

        // 1. Resolve string concatenations
        result = StringConcat.Replace(result, m =>
        {
            var combined = m.Groups[1].Value + m.Groups[2].Value;
            if (ContainsDangerousKeyword(combined))
                localObfuscated = true;
            return $"\"{combined}\"";
        });

        // 2. Remove Python comment obfuscation (os/*comment*/.remove)
        result = PythonCommentObfuscation.Replace(result, m =>
        {
            var combined = m.Groups[1].Value + "." + m.Groups[2].Value;
            if (ContainsDangerousKeyword(combined))
                localObfuscated = true;
            return combined;
        });

        // 3. Decode unicode escapes
        result = UnicodeEscape.Replace(result, m =>
        {
            var ch = (char)Convert.ToInt32(m.Groups[1].Value, 16);
            return ch.ToString();
        });

        // 4. Decode URL encoding
        result = UrlEncoding.Replace(result, m =>
        {
            var ch = (char)Convert.ToInt32(m.Groups[1].Value, 16);
            return ch.ToString();
        });

        // 5. Attempt base64 decode
        result = Base64Pattern.Replace(result, m =>
        {
            try
            {
                var bytes = Convert.FromBase64String(PadBase64(m.Value));
                var decoded = Encoding.UTF8.GetString(bytes);
                // Only replace if it looks like readable ASCII
                if (decoded.All(c => c >= 0x20 && c < 0x7F))
                {
                    if (ContainsDangerousKeyword(decoded))
                        localObfuscated = true;
                    return decoded;
                }
            }
            catch { /* not valid base64 */ }
            return m.Value;
        });

        // 6. Check ROT13 on the whole content
        var rot13 = ApplyRot13(result);
        if (DangerousKeywords.Any(kw => rot13.Contains(kw, StringComparison.OrdinalIgnoreCase)))
        {
            localObfuscated = true;
            result = rot13; // replace with decoded
        }

        obfuscationFound = localObfuscated;
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static bool ContainsDangerousKeyword(string text) =>
        DangerousKeywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase));

    private static string PadBase64(string input)
    {
        int mod = input.Length % 4;
        return mod == 0 ? input : input + new string('=', 4 - mod);
    }

    private static string ApplyRot13(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c >= 'A' && c <= 'Z') sb.Append((char)(((c - 'A' + 13) % 26) + 'A'));
            else if (c >= 'a' && c <= 'z') sb.Append((char)(((c - 'a' + 13) % 26) + 'a'));
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
