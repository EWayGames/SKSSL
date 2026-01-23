// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.Messages.RequestIntroducerRegistrationMessage
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System.Net;

#nullable enable
namespace SKSSL.Networking.Messages
{
  public class RequestIntroducerRegistrationMessage : MessageBase
  {
    public IPEndPoint InternalClientEndPoint { get; set; }

    public RequestIntroducerRegistrationMessage()
      : base(MessageType.RequestIntroducerRegistration)
    {
    }

    public override void WritePayload(IValueWriter writer)
    {
      base.WritePayload(writer);
      writer.WriteBytes(this.InternalClientEndPoint.Address.GetAddressBytes());
      writer.WriteInt32(this.InternalClientEndPoint.Port);
    }

    public override void ReadPayload(IValueReader reader)
    {
      base.ReadPayload(reader);
      this.InternalClientEndPoint = new IPEndPoint(new IPAddress(reader.ReadBytes()), reader.ReadInt32());
    }
  }
}
