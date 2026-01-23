// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.Messages.MessageBase
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

#nullable enable
namespace SKSSL.Networking.Messages
{
  public abstract class MessageBase
  {
    public MessageType MessageType { get; set; }

    public MessageBase(MessageType messageType) => this.MessageType = messageType;

    public virtual void WritePayload(IValueWriter writer)
    {
      writer.WriteInt32((int) this.MessageType);
    }

    public virtual void ReadPayload(IValueReader reader)
    {
      this.MessageType = (MessageType) reader.ReadInt32();
    }
  }
}
