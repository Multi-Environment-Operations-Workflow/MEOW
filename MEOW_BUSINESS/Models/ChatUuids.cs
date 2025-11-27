using System;

namespace MEOW_BUSINESS.Models;

public static class ChatUuids
{
    public static readonly Guid ChatService = Guid.Parse("0000c0de-0000-1000-8000-00805f9b34fb");
    public static readonly Guid MessageSendCharacteristic = Guid.Parse("0000c0d1-0000-1000-8000-00805f9b34fb");
    public static readonly Guid MessageReceiveCharacteristic = Guid.Parse("0000c0d2-0000-1000-8000-00805f9b34fb");
}
