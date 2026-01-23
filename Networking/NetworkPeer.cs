// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.NetworkPeer
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#nullable enable
namespace SKSSL
{
  public class NetworkPeer : NetworkClient
  {
    public Socket PeerSocket { get; private set; }

    public byte[] PeerBuffer { get; private set; }

    public event EventHandler<ConnectionAcceptedEventArgs> OnConnectionAccepted;

    public NetworkPeer()
    {
      this.PeerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IpTimeToLive, true);
      this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
      this.PeerBuffer = new byte[1024];
    }

    public void Bind(EndPoint on) => this.Socket.Bind(on);

    public void Listen()
    {
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
      this.PeerSocket = socket;
      if (this.OnConnectionAccepted == null)
        return;
      this.OnConnectionAccepted((object) this, new ConnectionAcceptedEventArgs()
      {
        Socket = socket
      });
    }
  }
}
