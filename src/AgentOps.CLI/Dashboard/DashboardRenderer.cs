using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Application.Dashboard;

namespace AgentOps.CLI.Dashboard
{
    /// <summary>
    /// Renders a <see cref="DashboardResult"/> as a coloured box-drawing table in the console.
    /// </summary>
    public class DashboardRenderer
    {
        // Total inner width of the box (between the two ║ borders), must be ≥ 62
        private const int BoxWidth = 64;

        public void Render(DashboardResult result)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            WriteFullLine('╔', '╗', '═');

            WriteTitle("AgentOps Console — Agent Dashboard");
            WriteTitle($"Repo: {result.Owner}/{result.Repo}");
            WriteTitle($"Generado: {result.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");

            WriteSeparator('╠', '╣', '═');

            // Summary block
            WritePaddedLine("RESUMEN", ConsoleColor.Cyan);
            WritePaddedLine(
                $"  Total: {result.TotalAgents}  |  ✅ Aprobados: {result.ApprovedCount}" +
                $"  |  ⚠️  En revisión: {result.ReviewCount}" +
                $"  |  ❌ Bloqueados: {result.BlockedCount}");

            if (result.Agents.Count == 0)
            {
                WritePaddedLine("  (sin agentes en data/agent-definitions/)");
                WriteFullLine('╚', '╝', '═');
                return;
            }

            // ── Table header ──
            WriteTableSeparator('╠', '╬', '╣', '═');
            WriteTableRow("Agente", "Versión", "Score", "Estado", "Violaciones", header: true);
            WriteTableSeparator('╠', '╬', '╣', '═');

            // ── Table rows ──
            foreach (var row in result.Agents)
            {
                string statusLabel;
                ConsoleColor statusColor;

                switch (row.Status)
                {
                    case "APPROVED":
                        statusLabel = "✅ APROV";
                        statusColor = ConsoleColor.Green;
                        break;
                    case "REVIEW":
                        statusLabel = "⚠️  REVIS";
                        statusColor = ConsoleColor.Yellow;
                        break;
                    default:
                        statusLabel = "❌ BLOQ";
                        statusColor = ConsoleColor.Red;
                        break;
                }

                string violations = BuildViolationSummary(row);
                // Show ⚡ next to agent name when there are active exceptions
                string displayName = row.HasActiveExceptions ? row.AgentName + " ⚡" : row.AgentName;
                WriteTableRow(displayName, row.Version, row.GovernanceScore.ToString(),
                              statusLabel, violations, rowColor: statusColor);
            }

            // ── Violations detail ──
            var agentsWithViolations = result.Agents
                .Where(a => a.ViolationDetails.Count > 0)
                .ToList();

            WriteTableSeparator('╠', '╩', '╣', '═');
            WritePaddedLine("DETALLE DE VIOLACIONES", ConsoleColor.Cyan);

            if (agentsWithViolations.Count == 0)
            {
                WritePaddedLine("  ✅ Todos los agentes cumplen las reglas de governance.");
            }
            else
            {
                foreach (var agent in agentsWithViolations)
                {
                    string statusIcon = agent.Status == "BLOCKED" ? "❌" : "⚠️ ";
                    string nameDisplay = agent.HasActiveExceptions
                        ? agent.AgentName + " ⚡"
                        : agent.AgentName;
                    WritePaddedLine($"  {statusIcon} {nameDisplay}:",
                        agent.Status == "BLOCKED" ? ConsoleColor.Red : ConsoleColor.Yellow);

                    foreach (var violation in agent.ViolationDetails)
                    {
                        WritePaddedLine($"    - {violation}");
                    }
                    // Show exception notes if any
                    foreach (var note in agent.ExceptionNotes)
                    {
                        WritePaddedLine($"    {note}", ConsoleColor.Yellow);
                    }
                    WritePaddedLine("");
                }
            }

            WriteFullLine('╚', '╝', '═');
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void WriteFullLine(char left, char right, char fill)
        {
            SetCyan();
            Console.Write(left);
            Console.Write(new string(fill, BoxWidth));
            Console.WriteLine(right);
            Console.ResetColor();
        }

        private static void WriteSeparator(char left, char right, char fill)
            => WriteFullLine(left, right, fill);

        private static void WriteTitle(string text)
        {
            SetCyan();
            Console.Write('║');
            Console.ResetColor();
            string centered = CenterText(text, BoxWidth);
            Console.Write(centered);
            SetCyan();
            Console.WriteLine('║');
            Console.ResetColor();
        }

        private static void WritePaddedLine(string text, ConsoleColor? color = null)
        {
            SetCyan();
            Console.Write("║ ");
            Console.ResetColor();

            if (color.HasValue) Console.ForegroundColor = color.Value;
            // visible length for padding
            int visibleLen = VisibleLength(text);
            Console.Write(text);
            Console.ResetColor();

            int padding = BoxWidth - 2 - visibleLen;
            if (padding > 0) Console.Write(new string(' ', padding));

            SetCyan();
            Console.WriteLine('║');
            Console.ResetColor();
        }

        // Column widths for the 5-column table
        private const int Col1 = 18;   // Agente
        private const int Col2 =  8;   // Versión
        private const int Col3 =  7;   // Score
        private const int Col4 = 10;   // Estado
        private const int Col5 = 17;   // Violaciones

        private static void WriteTableSeparator(char left, char mid, char right, char fill)
        {
            SetCyan();
            Console.Write(left);
            Console.Write(new string(fill, Col1 + 2));
            Console.Write(mid);
            Console.Write(new string(fill, Col2 + 2));
            Console.Write(mid);
            Console.Write(new string(fill, Col3 + 2));
            Console.Write(mid);
            Console.Write(new string(fill, Col4 + 2));
            Console.Write(mid);
            Console.Write(new string(fill, Col5 + 2));
            Console.WriteLine(right);
            Console.ResetColor();
        }

        private static void WriteTableRow(
            string c1, string c2, string c3, string c4, string c5,
            bool header = false, ConsoleColor? rowColor = null)
        {
            SetCyan();
            Console.Write("║ ");
            Console.ResetColor();

            if (header) SetCyan();
            else if (rowColor.HasValue) Console.ForegroundColor = rowColor.Value;

            Console.Write(Pad(c1, Col1));
            SetCyan(); Console.Write(" ║ "); Console.ResetColor();
            if (header) SetCyan(); else if (rowColor.HasValue) Console.ForegroundColor = rowColor.Value;
            Console.Write(Pad(c2, Col2));
            SetCyan(); Console.Write(" ║ "); Console.ResetColor();
            if (header) SetCyan(); else if (rowColor.HasValue) Console.ForegroundColor = rowColor.Value;
            Console.Write(Pad(c3, Col3));
            SetCyan(); Console.Write(" ║ "); Console.ResetColor();
            if (header) SetCyan(); else if (rowColor.HasValue) Console.ForegroundColor = rowColor.Value;
            Console.Write(Pad(c4, Col4));
            SetCyan(); Console.Write(" ║ "); Console.ResetColor();
            if (header) SetCyan(); else if (rowColor.HasValue) Console.ForegroundColor = rowColor.Value;
            Console.Write(Pad(c5, Col5));
            Console.ResetColor();
            SetCyan(); Console.WriteLine(" ║"); Console.ResetColor();
        }

        private static string BuildViolationSummary(AgentDashboardRow row)
        {
            var parts = new List<string>();
            if (row.CriticalViolations > 0) parts.Add($"{row.CriticalViolations} críticas");
            if (row.WarningViolations > 0)  parts.Add($"{row.WarningViolations} warnings");
            return parts.Count > 0 ? string.Join(", ", parts) : "0";
        }

        private static void SetCyan() => Console.ForegroundColor = ConsoleColor.Cyan;

        private static string CenterText(string text, int width)
        {
            int visLen = VisibleLength(text);
            if (visLen >= width) return text[..Math.Min(text.Length, width)];
            int leftPad  = (width - visLen) / 2;
            int rightPad = width - visLen - leftPad;
            return new string(' ', leftPad) + text + new string(' ', rightPad);
        }

        private static string Pad(string text, int width)
        {
            int visLen = VisibleLength(text);
            if (visLen >= width) return text[..Math.Min(text.Length, width)];
            return text + new string(' ', width - visLen);
        }

        /// <summary>
        /// Returns approximate visible character width, treating emoji as 2 characters
        /// and ANSI escape sequences as 0.
        /// </summary>
        private static int VisibleLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int len = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                // Surrogate pairs (most emoji) count as 2
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    len += 2;
                    i++;
                }
                else
                {
                    len++;
                }
            }
            return len;
        }
    }
}
