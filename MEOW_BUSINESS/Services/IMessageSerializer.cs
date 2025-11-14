using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IMessageSerializer
{
    byte[] Serialize(MeowMessage message);
    MeowMessage Deserialize(byte[] data);
}
