using System;
using System.Collections.Generic;

namespace MEOW_BUSINESS.Services;

public class MeowErrorService : IErrorService
{
    public event Action<IEnumerable<Exception>>? OnError;

    public void Add(Exception exception)
    {
        OnError?.Invoke([exception]);
    }

    public void Add(IEnumerable<Exception> exceptions)
    {
        OnError?.Invoke(exceptions);
    }
}