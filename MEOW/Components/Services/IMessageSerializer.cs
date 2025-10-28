using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageSerializer
{
    byte[] Serialize(IMessage message);
    IMessage Deserialize(byte[] data);
}
