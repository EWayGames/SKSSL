// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.NetworkIntroducer
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SKSSL.Networking;
using SKSSL.Networking.Messages;

#nullable enable
namespace SKSSL
{
  public class NetworkIntroducer
  {
    public Socket Socket { get; private set; }

    public List<UserClient> Clients { get; private set; }

    public List<Registrant> Registrants { get; private set; }

    public event EventHandler<ConnectionAcceptedEventArgs> OnConnectionAccepted;

    public event EventHandler<MessageSentEventArgs> OnMessageSent;

    public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

    public NetworkIntroducer()
    {
      this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
      this.Clients = new List<UserClient>();
      this.Registrants = new List<Registrant>();
    }

    public void Listen(EndPoint on)
    {
      this.Socket.Bind(on);
      this.Socket.Listen(int.MaxValue);
      this.Task_BeginAccepting();
    }

    private void Task_BeginAccepting()
    {
      Task<Socket> task = Task.Factory.FromAsync<Socket>(new Func<AsyncCallback, object, IAsyncResult>(this.Socket.BeginAccept), new Func<IAsyncResult, Socket>(this.Socket.EndAccept), (object) null);
      task.ContinueWith((Action<Task<Socket>>) (nextTask =>
      {
        this.Task_OnConnectionAccepted(task.Result);
        this.Task_BeginAccepting();
      }), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnConnectionAccepted(Socket socket)
    {
      Console.WriteLine(string.Format("Connection to {0} accepted.", (object) socket.RemoteEndPoint));
      if (this.Clients.FindAll((Predicate<UserClient>) (registrant => registrant.RemoteEndPoint == socket.RemoteEndPoint)).Any<UserClient>())
        this.Clients.RemoveAll((Predicate<UserClient>) (registrant => registrant.RemoteEndPoint == socket.RemoteEndPoint));
            UserClient registrant1 = new UserClient(socket);
      this.Clients.Add(registrant1);
      this.Task_BeginReceive(registrant1, socket);
      if (this.OnConnectionAccepted == null)
        return;
      this.OnConnectionAccepted((object) this, new ConnectionAcceptedEventArgs()
      {
        Socket = socket
      });
    }

    public void Send(EndPoint to, MessageBase messageBase)
    {
            UserClient registrant = this.Clients.Find((Predicate<UserClient>) (r => r.RemoteEndPoint == to));
      if (registrant == null || !registrant.Socket.Connected)
        return;
      byte[] data = messageBase.GetBytes();
      Task<int> task = Task.Factory.FromAsync<int>(registrant.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, (AsyncCallback) null, (object) this.Socket), new Func<IAsyncResult, int>(registrant.Socket.EndSend));
      task.ContinueWith((Action<Task<int>>) (nextTask => this.Task_OnSendCompleted(task.Result, data.Length, registrant.RemoteEndPoint, messageBase.MessageType)), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnSendCompleted(
      int numBytesSent,
      int expectedBytesSent,
      EndPoint to,
      MessageType messageType)
    {
      if (numBytesSent != expectedBytesSent)
        Console.WriteLine(string.Format("Warning: Expected to send {0} bytes but actually sent {1}!", (object) expectedBytesSent, (object) numBytesSent));
      Console.WriteLine(string.Format("Sent a {0} byte {1} | Message to {2}.", (object) numBytesSent, (object) messageType, (object) to));
      if (this.OnMessageSent == null)
        return;
      this.OnMessageSent((object) this, new MessageSentEventArgs()
      {
        Length = numBytesSent,
        To = to
      });
    }

    private void Task_BeginReceive(UserClient registrant, Socket socket)
    {
      Task<int> task = Task.Factory.FromAsync<int>(registrant.Socket.BeginReceive(registrant.Buffer, 0, registrant.Buffer.Length, SocketFlags.None, (AsyncCallback) null, (object) null), new Func<IAsyncResult, int>(registrant.Socket.EndReceive));
      task.ContinueWith((Action<Task<int>>) (nextTask =>
      {
        try
        {
          this.Task_OnReceiveCompleted(task.Result, registrant, socket);
          this.Task_BeginReceive(registrant, socket);
        }
        catch (Exception ex)
        {
          string str = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
          Console.WriteLine(ex.StackTrace + Environment.NewLine + str);
          this.ShutdownAndClose();
        }
      }), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private void Task_OnReceiveCompleted(int numBytesRead, UserClient registrant, Socket socket)
    {
      if (numBytesRead > 0)
      {
        BufferValueReader reader = new BufferValueReader(registrant.Buffer);
        Message message = new Message();
        message.ReadPayload((IValueReader) reader);
        reader.Position = 0;
        Console.WriteLine(string.Format("Received a {0} byte {1} | Message from {2}.", (object) numBytesRead, (object) message.MessageType, (object) registrant.Socket.RemoteEndPoint));
        if (this.OnMessageReceived == null)
          return;
        this.OnMessageReceived((object) this, new MessageReceivedEventArgs()
        {
          From = (IPEndPoint) registrant.RemoteEndPoint,
          MessageReader = (IValueReader) reader,
          MessageType = message.MessageType
        });
      }
      else
      {
        Console.WriteLine(string.Format("Dropped the client due to second round byte read was {0} bytes | Message to {1}.", (object) numBytesRead, (object) socket.RemoteEndPoint));
        socket.BeginDisconnect(true, (AsyncCallback) null, (object) socket);
      }
    }

    public void ShutdownAndClose()
    {
    }
  }
}
