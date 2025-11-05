namespace MEOW.Components.Services;

public interface IErrorService
{
    event Action<IEnumerable<Exception>> OnError;

    void Add(Exception exception);
    void Add(IEnumerable<Exception> exceptions);
}