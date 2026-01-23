// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.NetworkClient
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SKSSL.Networking.Messages;

#nullable enable
namespace SKSSL
{
  public class NetworkClient
  {
    public Socket Socket { get; private set; }

    public byte[] Buffer { get; set; }

    public event EventHandler<ConnectionAcceptedEventArgs> OnConnectionSuccessful;

    public event EventHandler<MessageSentEventArgs> OnMessageSent;

    public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

    public NetworkClient()
    {
      this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IpTimeToLive, true);
      this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
      this.Buffer = new byte[1024];
    }

    public void Connect(IPAddress host, int port) => this.Task_BeginConnecting(host, port);

    private void Task_BeginConnecting(IPAddress host, int port)
    {
      Task.Factory.FromAsync(this.Socket.BeginConnect(host, port, (AsyncCallback) null, (object) null), new Action<IAsyncResult>(this.Socket.EndConnect)).ContinueWith((Action<Task>) (nextTask => this.Task_OnConnectSuccessful()), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnConnectSuccessful()
    {
      Console.WriteLine(string.Format("Connected to {0}.", (object) this.Socket.RemoteEndPoint));
      this.Task_BeginReceive();
      if (this.OnConnectionSuccessful == null)
        return;
      this.OnConnectionSuccessful((object) this, new ConnectionAcceptedEventArgs()
      {
        Socket = this.Socket
      });
    }

    public void Send(MessageBase messageBase)
    {
      if (!this.Socket.Connected)
        return;
      byte[] data = messageBase.GetBytes();
      Task<int> task = Task.Factory.FromAsync<int>(this.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, (AsyncCallback) null, (object) this.Socket), new Func<IAsyncResult, int>(this.Socket.EndSend));
      task.ContinueWith((Action<Task<int>>) (nextTask => this.Task_OnSendCompleted(task.Result, data.Length, this.Socket.RemoteEndPoint, messageBase.MessageType)), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnSendCompleted(
      int numBytesSent,
      int expectedBytesSent,
      EndPoint to,
      MessageType messageType)
    {
      if (numBytesSent != expectedBytesSent)
        Console.WriteLine(string.Format("Warning: Expected to send {0} bytes but actually sent {1}!", (object) expectedBytesSent, (object) numBytesSent));
      Console.WriteLine(string.Format("Sent a {0} byte {1}Message to {2}.", (object) numBytesSent, (object) messageType, (object) to));
      if (this.OnMessageSent == null)
        return;
      this.OnMessageSent((object) this, new MessageSentEventArgs()
      {
        Length = numBytesSent,
        To = to
      });
    }

    private void Task_BeginReceive()
    {
      Task<int> task = Task.Factory.FromAsync<int>(this.Socket.BeginReceive(this.Buffer, 0, this.Buffer.Length, SocketFlags.None, (AsyncCallback) null, (object) null), new Func<IAsyncResult, int>(this.Socket.EndReceive));
      task.ContinueWith((Action<Task<int>>) (nextTask =>
      {
        try
        {
          this.Task_OnReceiveCompleted(task.Result);
          this.Task_BeginReceive();
        }
        catch (Exception ex)
        {
          string str = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
          Console.WriteLine(ex.StackTrace + Environment.NewLine + str);
          this.ShutdownAndClose();
        }
      }), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnReceiveCompleted(int numBytesRead)
    {
      BufferValueReader reader = new BufferValueReader(this.Buffer);
      Message message = new Message();
      message.ReadPayload((IValueReader) reader);
      reader.Position = 0;
      Console.WriteLine(string.Format("Received a {0} byte {1}Message from {2}.", (object) numBytesRead, (object) message.MessageType, (object) this.Socket.RemoteEndPoint));
      if (this.OnMessageReceived == null)
        return;
      this.OnMessageReceived((object) this, new MessageReceivedEventArgs()
      {
        From = (IPEndPoint) this.Socket.RemoteEndPoint,
        MessageReader = (IValueReader) reader,
        MessageType = message.MessageType
      });
    }

    public void Disconnect() => this.Socket.Disconnect(true);

    public void ShutdownAndClose()
    {
    }
  }
}
