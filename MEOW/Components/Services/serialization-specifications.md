# MeowMessage Serialization Specifications

## General

Any MeowMessage shall follow this template for serializing a message.

The serialization of a message should return a byte array where the first 4 bytes a reserved for:

1. byte 1 is the message type
2. byte 2 is the sender length n
3. byte 3-n is the sender(name)

The bytes from byte 5 and on are free to be used in any way needed to represent the message.

### Example

The example is an encoding of the abstract base class MeowMEssage:

```cs
Byte[] message = [0x00, 0x01, 0x51, 0x12, ...]
```

The example is given in hexadecimal.

## MeowMessageText

```cs
public class MeowMessageText(string message, string sender) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Message { get; set; } = message;
}
```

MeowMessageText introduces the Message field of type string, this string is unbounded as of current(31-10/25). This will be encoded as 4 bytes for the length of the string, followed by the byte representation of the string.

This looks like:

```cs
byte[] message = [..., byte representation of length, byte representation of message string];
```

## MeowMessageGps

```cs
public class MeowMessageGps(string sender, float longitude, float latitude) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; } = longitude;
    public float Latitude { get; set; } = latitude;
}
```

MeowMessageGps introduces Longitude and Latitude as 2 32-bit floats. This will be encoded as longitude followed by latitude:

```cs
byte[] message = [..., byte representation of longitude, byte representation of latitude];
```
