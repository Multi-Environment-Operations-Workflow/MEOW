# MeowMessage Serialization Specifications

## General

Any MeowMessage shall follow this template for serializing a message.

The serialization of a message should return a byte array where the first 4 bytes a reserved for:

1. byte 1 is the message type
2. byte 2 is the sender uuid
3. byte 3-4 is the senders message id
   - (gives the sender the possibility of sending 65k messages)

The bytes from byte 5 and on are free to be used in any way needed to represent the message.

### Example

The below example shows the serialization of a message with type == 0, the sender has the id 3, and it is there second message. The ellipsis denote the possibility of additional data.

```c#
Byte[] message = [0x00, 0x03, 0x0001, ...]
```

The example is given in hexadecimal.

## MeowMessageText

```c#
public class MeowMessageText(string message, string sender) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Message { get; set; } = message;
}
```

MeowMessageText introduces the Message field of type string, this string is unbounded as of current(31-10/25). This will be encoded as 4 bytes for the length of the string, followed by the byte representation of the string.

This looks like:

```c#

Byte[] message = [..., byte representation of length, byte representation of message string]
```
