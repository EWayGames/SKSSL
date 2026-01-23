// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.MessageSentEventArgs
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Net;

#nullable enable
namespace SKSSL
{
  public class MessageSentEventArgs : EventArgs
  {
    public EndPoint To { get; set; }

    public int Length { get; set; }
  }
}
