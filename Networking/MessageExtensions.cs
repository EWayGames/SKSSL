// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.MessageExtensions
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using SKSSL.Networking.Messages;

#nullable enable
namespace SKSSL
{
  public static class MessageExtensions
  {
    public static byte[] GetBytes(this MessageBase messageBase)
    {
      BufferValueWriter writer = new BufferValueWriter(new byte[1024]);
      messageBase.WritePayload((IValueWriter) writer);
      byte[] dst = new byte[writer.Length];
      Buffer.BlockCopy((Array) writer.Buffer, 0, (Array) dst, 0, dst.Length);
      return dst;
    }
  }
}
