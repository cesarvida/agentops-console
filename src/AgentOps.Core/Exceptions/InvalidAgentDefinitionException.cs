using System;

namespace AgentOps.Core.Exceptions
{
    public class InvalidAgentDefinitionException : DomainException
    {
        public InvalidAgentDefinitionException(string message) : base(message) { }
    }
}
