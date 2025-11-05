
namespace MEOW.Components.Services;

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