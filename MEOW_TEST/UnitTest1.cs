using MEOW_BUSINESS.Services;
using Moq;
namespace MEOW_TEST;

public class UnitTest1
{
    [Fact]
    public void PassingTest()
    {
        var mockPrefs = new Mock<IAppPreferences>();
        mockPrefs.Setup(p => p.Get("username", It.IsAny<string>())).Returns("MYUSERNAME");
        var userStateService = new UserStateService(mockPrefs.Object);
        Assert.Equal("MYUSERNAME", userStateService.GetName());
    }

    [Fact]
    public void FailingTest()
    {
        Assert.Equal(4, 2+2);
    }
}