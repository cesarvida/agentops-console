namespace AgentOps.Core.ValueObjects
{
    public readonly struct AgentId
    {
        public string Value { get; }
        public AgentId(string value) => Value = value;
        public override string ToString() => Value;
    }
}
