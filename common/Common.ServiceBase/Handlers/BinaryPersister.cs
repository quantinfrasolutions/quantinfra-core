// using System;
// using System.Buffers.Binary;
// using System.IO;
// using System.Text;
// using System.Threading.Tasks;
// using Disruptor;
// using QuantInfra.Common.Messaging.Sockets;
//
// namespace QuantInfra.Common.Messaging.Patterns.Handlers;
//
// public class BinaryPersister : 
//     IEventHandler<IncomingDisruptorMessage<Message>>,
//     IDisposable, IAsyncDisposable
// {
//     private readonly byte[] _lenBuf = new byte[4];
//     private readonly byte[] _seqBuf = new byte[4];
//     public readonly string FilePath;
//         
//     private FileStream _fs;
//
//     public BinaryPersister(PersisterOptions options)
//     {
//         FilePath = options.FilePath;
//     }
//
//     public void Start()
//     {
//         _fs = File.Open(FilePath, FileMode.Append);
//     }
//     
//     public void OnEvent(IncomingDisruptorMessage<Message> data, long sequence, bool endOfBatch)
//     {
//         var senderCompId = Encoding.UTF8.GetBytes(data.TransportMessage.Header.Value.SenderCompId);
//         BinaryPrimitives.WriteUInt32LittleEndian(_lenBuf, (uint)senderCompId.Length);
//         _fs.Write(_lenBuf, 0, 4);
//         _fs.Write(senderCompId);
//         
//         BinaryPrimitives.WriteInt64LittleEndian(_seqBuf, data.TransportMessage.Header.Value.MsgSeqNum);
//         _fs.Write(_seqBuf);
//         
//         var payload = data.TransportMessage.Payload;
//         if ((uint)payload.Length > uint.MaxValue)
//             throw new ArgumentOutOfRangeException(nameof(payload), "Payload too large for u32 length.");
//
//         BinaryPrimitives.WriteUInt32LittleEndian(_lenBuf, (uint)payload.Length);
//
//         // Two writes are fine; if you want "single syscall", use Writev via OS APIs.
//         _fs.Write(_lenBuf, 0, 4);
//         _fs.Write(payload);
//         _fs.Flush();
//     }
//
//     public void Dispose()
//     {
//         _fs.Flush();
//         _fs.Close();
//         _fs.Dispose();
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         await _fs.DisposeAsync();
//     }
// }
//
// public class PersisterOptions
// {
//     public string FilePath { get; init; }
// }