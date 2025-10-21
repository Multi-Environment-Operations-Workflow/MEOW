// #if ANDROID
// using Android.Bluetooth;
// using Android.Content;
// using Java.Util;

// namespace MEOW.Components.Services;

// internal sealed class AndroidGattServer
// {
//     private readonly BluetoothManager _btManager;
//     private BluetoothGattServer? _gattServer;
//     private readonly HashSet<BluetoothDevice> _subscribers = new();

//     private static readonly UUID ChatServiceUuid = UUID.FromString(ChatUuids.ChatService.ToString());
//     private static readonly UUID MsgSendUuid     = UUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
//     private static readonly UUID MsgRecvUuid     = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());

//     public event Action<byte[]>? MessageReceived;

//     public AndroidGattServer(Context ctx)
//     {
//         _btManager = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService)!;
//     }

//     public void Start()
//     {
//         _gattServer = _btManager.OpenGattServer(Android.App.Application.Context, new ServerCb(this));

//         var service = new BluetoothGattService(ChatServiceUuid, GattServiceType.Primary);

//         var sendChar = new BluetoothGattCharacteristic(
//             MsgSendUuid, GattProperty.Read | GattProperty.Notify, GattPermission.Read);

//         var recvChar = new BluetoothGattCharacteristic(
//             MsgRecvUuid, GattProperty.Write | GattProperty.WriteNoResponse, GattPermission.Write);

//         service.AddCharacteristic(sendChar);
//         service.AddCharacteristic(recvChar);

//         _gattServer.AddService(service);
//     }

//     public void Stop()
//     {
//         _gattServer?.Close();
//         _subscribers.Clear();
//     }

//     public bool NotifyAll(byte[] data)
//     {
//         var svc = _gattServer?.GetService(ChatServiceUuid);
//         var ch  = svc?.GetCharacteristic(MsgSendUuid);
//         if (ch == null || _gattServer == null) return false;

//         ch.SetValue(data);
//         var ok = false;
//         foreach (var dev in _subscribers)
//             ok |= _gattServer.NotifyCharacteristicChanged(dev, ch, false);
//         return ok;
//     }

//     private sealed class ServerCb : BluetoothGattServerCallback
//     {
//         private readonly AndroidGattServer _o;
//         public ServerCb(AndroidGattServer o) => _o = o;

//         public override void OnConnectionStateChange(BluetoothDevice d, ProfileState s, ProfileState ns)
//         {
//             if (ns == ProfileState.Connected) _o._subscribers.Add(d);
//             else                               _o._subscribers.Remove(d);
//         }

//         public override void OnCharacteristicWriteRequest(
//             BluetoothDevice device, int requestId, BluetoothGattCharacteristic characteristic,
//             bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
//         {
//             if (characteristic.Uuid.Equals(MsgRecvUuid) && value != null)
//                 _o.MessageReceived?.Invoke(value);

//             if (responseNeeded)
//                 _o._gattServer?.SendResponse(device, requestId, GattStatus.Success, offset, value);
//         }

//         public override void OnCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic)
//         {
//             _o._gattServer?.SendResponse(device, requestId, GattStatus.Success, offset, characteristic.GetValue());
//         }
//     }
// }
// #endif
