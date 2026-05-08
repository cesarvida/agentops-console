using System;
using System.Collections.Generic;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Core.Exceptions;
using Xunit;

namespace AgentOps.Core.Tests
{
    public class AgentDefinitionTests
    {
        [Theory]
        [InlineData("", "desc", "purpose")]
        [InlineData("ab", "desc", "purpose")]
        public void Throws_On_Invalid_Name(string name, string desc, string purpose)
        {
            Assert.Throws<InvalidAgentDefinitionException>(() =>
                new AgentDefinition(
                    new AgentId(Guid.NewGuid().ToString()),
                    name,
                    desc,
                    purpose,
                    new List<string>{"rule"},
                    new List<string>{"tool"},
                    new AgentConfiguration(),
                    DateTime.UtcNow,
                    "1.0"
                ));
        }

        [Fact]
        public void Throws_On_Short_Description()
        {
            Assert.Throws<InvalidAgentDefinitionException>(() =>
                new AgentDefinition(
                    new AgentId(Guid.NewGuid().ToString()),
                    "ValidName",
                    "short",
                    "purpose",
                    new List<string>{"rule"},
                    new List<string>{"tool"},
                    new AgentConfiguration(),
                    DateTime.UtcNow,
                    "1.0"
                ));
        }
    }
}
