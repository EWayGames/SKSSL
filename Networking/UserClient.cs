// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.UserClient
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System.Net;
using System.Net.Sockets;

namespace SKSSL.Networking;

public class UserClient // IMPL: No networking has been achieved as of 20251215. One day...!
{
    private readonly ISettings _settings;

    public UserClient(ISettings settings)
    {
        _settings = settings;
    }
    
    /// <summary>Updates client information.</summary>
    public void Update()
    {
        
    }

    public Socket Socket { get; set; }

    public byte[] Buffer { get; set; }

    public UserClient(Socket socket)
    {
        this.Socket = socket;
        this.Buffer = new byte[1024];
    }

    public EndPoint RemoteEndPoint
    {
        get => this.Socket == null ? (EndPoint)null : this.Socket.RemoteEndPoint;
    }
}