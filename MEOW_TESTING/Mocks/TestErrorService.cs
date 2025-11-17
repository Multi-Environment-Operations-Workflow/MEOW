using MEOW_BUSINESS.Services;

namespace MEOW_TESTING.Mocks;

public class TestErrorService: IErrorService
{
    public event Action<IEnumerable<Exception>>? OnError;
    public void Add(Exception exception)
    {
        throw new Exception("AN ERROR WAS ADDED TO THE ERROR SERVICE DURING A TEST!", exception);
    }

    public void Add(IEnumerable<Exception> exceptions)
    {
        throw new AggregateException("MULTIPLE ERRORS WERE ADDED TO THE ERROR SERVICE DURING A TEST!", exceptions);
    }
}