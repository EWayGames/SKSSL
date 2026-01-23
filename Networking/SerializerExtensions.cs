// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.SerializerExtensions
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable
namespace SKSSL
{
  public static class SerializerExtensions
  {
    public static void WriteUniversalDate(this IValueWriter writer, DateTime date)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      if (date.Kind != DateTimeKind.Utc)
        date = date.ToUniversalTime();
      writer.WriteInt64(date.Ticks);
    }

    public static DateTime ReadUniversalDate(this IValueReader reader)
    {
      return reader != null ? new DateTime(reader.ReadInt64(), DateTimeKind.Utc) : throw new ArgumentNullException(nameof (reader));
    }

    public static void WriteDate(this IValueWriter writer, DateTime date)
    {
      writer.WriteLocalDate(date);
    }

    public static DateTime ReadDate(this IValueReader reader) => reader.ReadLocalDate().Item2;

    public static void WriteLocalDate(this IValueWriter writer, DateTime date)
    {
      writer.WriteLocalDate(date, TimeZoneInfo.Local);
    }

    public static void WriteLocalDate(
      this IValueWriter writer,
      DateTime date,
      TimeZoneInfo timeZone)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      writer.WriteString(timeZone.ToSerializedString());
      writer.WriteInt64(date.ToLocalTime().Ticks);
    }

    public static Tuple<TimeZoneInfo, DateTime> ReadLocalDate(this IValueReader reader)
    {
      return reader != null ? new Tuple<TimeZoneInfo, DateTime>(TimeZoneInfo.FromSerializedString(reader.ReadString()), new DateTime(reader.ReadInt64(), DateTimeKind.Unspecified)) : throw new ArgumentNullException(nameof (reader));
    }

    public static void WriteString(this IValueWriter writer, string value)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      writer.WriteString(Encoding.UTF8, value);
    }

    public static string ReadString(this IValueReader reader)
    {
      return reader != null ? reader.ReadString(Encoding.UTF8) : throw new ArgumentNullException(nameof (reader));
    }

    public static void WriteEnumerable<T>(this IValueWriter writer, IEnumerable<T> enumerable) where T : ISerializable
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      T[] objArray = enumerable != null ? enumerable.ToArray<T>() : throw new ArgumentNullException(nameof (enumerable));
      writer.WriteInt32(objArray.Length);
      for (int index = 0; index < objArray.Length; ++index)
        objArray[index].Serialize(writer);
    }

    public static void WriteEnumerable<T>(
      this IValueWriter writer,
      ISerializer<T> serializer,
      IEnumerable<T> enumerable)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      if (serializer == null)
        throw new ArgumentNullException(nameof (serializer));
      T[] objArray = enumerable != null ? enumerable.ToArray<T>() : throw new ArgumentNullException(nameof (enumerable));
      writer.WriteInt32(objArray.Length);
      for (int index = 0; index < objArray.Length; ++index)
        serializer.Serialize(writer, objArray[index]);
    }

    public static IEnumerable<T> ReadEnumerable<T>(this IValueReader reader, Func<T> elementFactory) where T : ISerializable
    {
      if (reader == null)
        throw new ArgumentNullException(nameof (reader));
      if (elementFactory == null)
        throw new ArgumentNullException(nameof (elementFactory));
      T[] objArray = new T[reader.ReadInt32()];
      for (int index = 0; index < objArray.Length; ++index)
      {
        T obj;
        objArray[index] = obj = elementFactory();
        obj = obj;
        obj.Deserialize(reader);
      }
      return (IEnumerable<T>) objArray;
    }

    public static IEnumerable<T> ReadEnumerable<T>(
      this IValueReader reader,
      ISerializer<T> serializer)
    {
      if (reader == null)
        throw new ArgumentNullException(nameof (reader));
      if (serializer == null)
        throw new ArgumentNullException(nameof (serializer));
      T[] objArray = new T[reader.ReadInt32()];
      for (int index = 0; index < objArray.Length; ++index)
        objArray[index] = serializer.Deserialize(reader);
      return (IEnumerable<T>) objArray;
    }

    public static IEnumerable<T> ReadEnumerable<T>(
      this IValueReader reader,
      Func<IValueReader, T> elementFactory)
    {
      if (reader == null)
        throw new ArgumentNullException(nameof (reader));
      if (elementFactory == null)
        throw new ArgumentNullException(nameof (elementFactory));
      T[] objArray = new T[reader.ReadInt32()];
      for (int index = 0; index < objArray.Length; ++index)
        objArray[index] = elementFactory(reader);
      return (IEnumerable<T>) objArray;
    }

    public static void Write(this IValueWriter writer, object element, Type serializeAs)
    {
      writer.Write(element, (xISerializer) new FixedSerializer(serializeAs));
    }

    public static void Write(this IValueWriter writer, object element, xISerializer serializer)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      if (element == null)
        throw new ArgumentNullException(nameof (element));
      if (serializer == null)
        throw new ArgumentNullException(nameof (serializer));
      serializer.Serialize(writer, element);
    }

    public static void Write<T>(this IValueWriter writer, T element)
    {
      writer.Write<T>(element, Serializer<T>.Default);
    }

    public static void Write<T>(this IValueWriter writer, T element, ISerializer<T> serializer)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      if (serializer == null)
        throw new ArgumentNullException(nameof (serializer));
      serializer.Serialize(writer, element);
    }

    public static T Read<T>(this IValueReader reader)
    {
      return reader != null ? reader.Read<T>(Serializer<T>.Default) : throw new ArgumentNullException(nameof (reader));
    }

    public static T Read<T>(this IValueReader reader, ISerializer<T> serializer)
    {
      if (reader == null)
        throw new ArgumentNullException(nameof (reader));
      return serializer != null ? serializer.Deserialize(reader) : throw new ArgumentNullException(nameof (serializer));
    }

    public static object Read(this IValueReader reader) => reader.Read<object>();

    public static object Read(this IValueReader reader, Type type)
    {
      return reader != null ? ObjectSerializer.GetSerializer(type).Deserialize(reader) : throw new ArgumentNullException(nameof (reader));
    }
  }
}
