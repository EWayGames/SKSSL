// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.Serializer`1
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;

#nullable enable
namespace SKSSL
{
  public static class Serializer<T>
  {
    public static readonly ISerializer<T> Default = (ISerializer<T>) new Serializer<T>.DefaultSerializer();

    private class DefaultSerializer : ISerializer<T>
    {
      public void Serialize(IValueWriter writer, T element)
      {
        Type type;
        if ((object) element != null)
        {
          type = element.GetType();
          if (type.IsValueType && typeof (T) == typeof (object))
            type = typeof (object);
        }
        else
          type = typeof (object);
        ObjectSerializer.GetSerializer(type).Serialize(writer, (object) element);
      }

      public T Deserialize(IValueReader reader)
      {
        return (T) ObjectSerializer.GetSerializer(typeof (T)).Deserialize(reader);
      }
    }
  }
}
