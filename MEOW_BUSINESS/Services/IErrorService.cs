using System;
using System.Collections.Generic;

namespace MEOW_BUSINESS.Services;

public interface IErrorService
{
    event Action<IEnumerable<Exception>> OnError;

    void Add(Exception exception);
    void Add(IEnumerable<Exception> exceptions);
}