using System.Collections.Generic;

namespace AgentOps.Application.Parsing
{
    /// <summary>
    /// Metadata produced by <see cref="FlexibleAgentMapper"/> describing how an external
    /// agent definition was translated to an <see cref="AgentOps.Core.Entities.AgentDefinition"/>.
    /// Consumed by the CLI <c>--external</c> flag to display format analysis.
    /// </summary>
    public class AgentMappingContext
    {
        /// <summary>Auto-detected format of the source file: "JSON" or "YAML".</summary>
        public string DetectedFormat { get; set; } = "Unknown";

        /// <summary>True when the flexible mapper was used as a fallback.</summary>
        public bool UsedFlexibleMapper { get; set; }

        /// <summary>Original file path that was loaded.</summary>
        public string SourceFile { get; set; } = "";

        /// <summary>Top-level keys from the source document that matched a known alias.</summary>
        public List<string> RecognizedFields { get; set; } = new();

        /// <summary>Top-level keys from the source document that had no alias match.</summary>
        public List<string> UnrecognizedFields { get; set; } = new();

        /// <summary>
        /// Human-readable notes describing how each mapped field was resolved.
        /// E.g. "capabilities → actions (3 elements detected)", "author → owner".
        /// </summary>
        public List<string> MappingNotes { get; set; } = new();

        /// <summary>Total number of fields found after flattening the document.</summary>
        public int TotalFieldCount { get; set; }

        /// <summary>Count of recognized (alias-matched) top-level fields.</summary>
        public int RecognizedFieldCount => RecognizedFields.Count;
    }
}
