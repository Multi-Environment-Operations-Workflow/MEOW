using System.Collections.Specialized;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IPinService
{
    void SetupReceiveMessages();

    void SetupPinReceivedAction(NotifyCollectionChangedEventHandler onMessage);

    Task<(bool, List<Exception>)> SendMessage(PinItem pin);

    Task<bool> SendPinMetadataAsync(PinItem pin);

    Task OnPinDataReceivedAsync(byte[] data);

    void AddPin(PinItem pin);
    void RemovePin(PinItem pin);
    List<PinItem> GetPins();
}
