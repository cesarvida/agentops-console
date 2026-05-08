namespace AgentOps.CLI
{
    public interface IConsoleWriter
    {
        void WriteLine(string message);
        void WriteSuccess(string message);
        void WriteError(string message);
        void WriteWarning(string message);
    }
}
