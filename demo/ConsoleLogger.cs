using System;
using Microsoft.Extensions.Logging;

namespace AgentOps.Demo
{
    /// <summary>
    /// Minimal console logger used by the demo to avoid needing the full DI stack.
    /// </summary>
    internal sealed class ConsoleLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
        }
    }
}
