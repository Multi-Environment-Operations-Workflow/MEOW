using System;

namespace MEOW_BUSINESS.Services;

public interface ILoggingService
{
    event Action<(string, object?)>? OnLog;
    
    void AddLog((string, object?) message);
}