using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class PinService(IMessageService messageService, IUserStateService userStateService) : IPinService
{
    private readonly ObservableCollection<PinItem> _pins = new();

    public void SetupReceiveMessages()
    {
        messageService.SetupMessageReceivedAction<MeowMessageTask>(PinMessageReceivedAction);
    }

    private void PinMessageReceivedAction(MeowMessageTask pin)
    {
        _pins.Add(new PinItem(pin.Title, pin.TextContext, pin.FileData));
    }

    public void SetupPinReceivedAction(NotifyCollectionChangedEventHandler onMessage)
    {
        _pins.CollectionChanged -= onMessage;
        _pins.CollectionChanged += onMessage;
    }

    public Task<(bool, List<Exception>)> SendMessage(PinItem pin)
    {
        var meowMessage = new MeowMessageTask(userStateService.GetName(), pin.Title, pin.TextContext, pin.FileData);

        return messageService.SendMessage(meowMessage);
    }

    public async void AddPin(PinItem pin)
    {
        _pins.Add(pin);
        var result = await SendMessage(pin);
        if (!result.Item1)
        {

            var errorText = !result.Item2.Any() ? "unknown error"
                : string.Join("; ", result.Item2.Select(ex => ex.Message ?? ex.ToString()));

            _pins.Add(new PinItem("Error", errorText, ""));
        }
    }

    public void RemovePin(PinItem pin)
    {
        _pins.Remove(pin);
    }

    public List<PinItem> GetPins()
    {
        return _pins.ToList();
    }

    public Task<bool> SendPinMetadataAsync(PinItem pin) => throw new NotImplementedException();
    public Task OnPinDataReceivedAsync(byte[] data) => throw new NotImplementedException();
}
