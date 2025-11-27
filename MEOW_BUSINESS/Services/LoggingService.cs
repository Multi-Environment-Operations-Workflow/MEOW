using System;
using System.Collections.Generic;

namespace MEOW_BUSINESS.Services;

public class LoggingService: ILoggingService
{
    List<(string, object?)> Logs { get; } = new();
    
    public event Action<(string, object?)>? OnLog;
    
    public void AddLog((string, object?) message)
    {
        Logs.Add(message);
        OnLog?.Invoke(message);
    }
}