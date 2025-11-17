using MEOW_BUSINESS.Enums;

namespace MEOW_BUSINESS.Models;

public class QuickChat (float longitude, float latitude, QuickChatMessageType type)
{
        public readonly float Longitude = longitude;
        public readonly float Latitude = latitude;
        public readonly QuickChatMessageType Type = type;
}