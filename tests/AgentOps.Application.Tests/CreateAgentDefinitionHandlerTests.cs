using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using Moq;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class CreateAgentDefinitionHandlerTests
    {
        [Fact]
        public async Task Handler_Success_Calls_Repos()
        {
            var repo = new Mock<IAgentDefinitionRepository>();
            var audit = new Mock<IAuditRepository>();
            var clock = new Mock<IClock>();
            clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
            repo.Setup(x => x.AddAsync(It.IsAny<AgentDefinition>())).ReturnsAsync("/tmp/agent.json");
            audit.Setup(x => x.AppendAsync(It.IsAny<AuditEntry>())).ReturnsAsync("audit-id");
            var handler = new CreateAgentDefinitionHandler(repo.Object, audit.Object, clock.Object);
            var req = new CreateAgentDefinitionRequest
            {
                Name = "AgentName",
                Description = "A valid description for agent.",
                Purpose = "Purpose",
                Rules = new List<string>{"rule1"},
                Tools = new List<string>{"tool1"}
            };
            var resp = await handler.HandleAsync(req);
            Assert.Equal("Success", resp.Status);
            Assert.Equal("/tmp/agent.json", resp.SavedPath);
            Assert.Equal("audit-id", resp.AuditId);
            repo.Verify(x => x.AddAsync(It.IsAny<AgentDefinition>()), Times.Once);
            audit.Verify(x => x.AppendAsync(It.IsAny<AuditEntry>()), Times.Once);
        }

        [Fact]
        public async Task Handler_Fail_InvalidInput_DoesNotPersist()
        {
            var repo = new Mock<IAgentDefinitionRepository>();
            var audit = new Mock<IAuditRepository>();
            var clock = new Mock<IClock>();
            var handler = new CreateAgentDefinitionHandler(repo.Object, audit.Object, clock.Object);
            var req = new CreateAgentDefinitionRequest
            {
                Name = "",
                Description = "",
                Purpose = "",
                Rules = new List<string>(),
                Tools = new List<string>()
            };
            var resp = await handler.HandleAsync(req);
            Assert.Equal("Error", resp.Status);
            repo.Verify(x => x.AddAsync(It.IsAny<AgentDefinition>()), Times.Never);
            audit.Verify(x => x.AppendAsync(It.IsAny<AuditEntry>()), Times.Never);
        }
    }
}
