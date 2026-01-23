// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.Registrant
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System.Net;

#nullable enable
namespace SKSSL
{
  public class Registrant
  {
    public IPEndPoint InternalEndPoint { get; set; }

    public IPEndPoint ExternalEndPoint { get; set; }
  }
}
