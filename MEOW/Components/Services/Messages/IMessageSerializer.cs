using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageSerializer
{
    byte[] Serialize(MeowMessage message);
    MeowMessage Deserialize(byte[] data);
}
