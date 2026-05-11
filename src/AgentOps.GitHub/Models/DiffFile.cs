namespace AgentOps.GitHub.Models
{
    public class DiffFile
    {
        public string Path { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Changes { get; set; }
        public string Patch { get; set; } = string.Empty;
        public string? PreviousPath { get; set; }
        public bool IsBinary { get; set; }
    }
}
