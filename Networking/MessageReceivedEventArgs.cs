// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.MessageReceivedEventArgs
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Net;
using SKSSL.Networking.Messages;

#nullable enable
namespace SKSSL
{
  public class MessageReceivedEventArgs : EventArgs
  {
    public IPEndPoint From { get; set; }

    public MessageType MessageType { get; set; }

    public IValueReader MessageReader { get; set; }
  }
}
