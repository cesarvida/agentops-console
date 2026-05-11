using System;
using System.Collections.Generic;

namespace AgentOps.GitHub.Models
{
    public class PullRequestSnapshot
    {
        public string Owner { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string BaseBranch { get; set; } = string.Empty;
        public string HeadBranch { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public List<DiffFile> Files { get; set; } = new List<DiffFile>();
        public DateTime SnapshotTimestamp { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
