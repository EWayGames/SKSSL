// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.FixedSerializer
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;

#nullable enable
namespace SKSSL
{
  internal class FixedSerializer : xISerializer
  {
    private readonly ObjectSerializer serializer;

    public FixedSerializer(Type type)
    {
      this.serializer = !(type == (Type) null) ? ObjectSerializer.GetSerializer(type) : throw new ArgumentNullException(nameof (type));
    }

    public void Serialize(IValueWriter writer, object element)
    {
      this.serializer.Serialize(writer, element);
    }

    public object Deserialize(IValueReader reader) => this.serializer.Deserialize(reader);
  }
}
