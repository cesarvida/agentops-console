using System.Collections.Generic;
using AgentOps.Core.Entities;
using AgentOps.Security.Models;

namespace AgentOps.Security.Interfaces
{
    public interface ISecurityRule
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// Evaluate the rule against an AgentDefinition and return 0..N findings.
        /// </summary>
        IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent);
    }
}
