// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.ObjectSerializer
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

#nullable enable
namespace SKSSL
{
  internal class ObjectSerializer
  {
    private readonly Type type;
    private Func<IValueReader, bool, object> deserializer;
    private Action<IValueWriter, object, bool> serializer;
    private ConstructorInfo ctor;
    private Dictionary<MemberInfo, ObjectSerializer.SerializationPair> members;
    private bool deserializingConstructor;
    private static readonly Dictionary<Type, ObjectSerializer> Serializers = new Dictionary<Type, ObjectSerializer>();
    private static readonly ObjectSerializer baseSerializer = new ObjectSerializer(typeof (object));

    public ObjectSerializer(Type type)
    {
      this.type = !(type == (Type) null) ? type : throw new ArgumentNullException(nameof (type));
      this.GenerateSerialization();
    }

    public void Serialize(IValueWriter writer, object obj)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      this.serializer(writer, obj, false);
    }

    public T Deserialize<T>(IValueReader reader)
    {
      if (typeof (T) != this.type)
        throw new ArgumentException("Type does not match serializer type");
      return (T) this.Deserialize(reader);
    }

    public object Deserialize(IValueReader reader)
    {
      if (reader == null)
        throw new ArgumentNullException(nameof (reader));
      return this.deserializer(reader, false);
    }

    private void GenerateSerialization()
    {
      this.deserializer = ObjectSerializer.GetDeserializer(this.type, this);
      this.serializer = this.GetSerializer();
    }

    private static Func<IValueReader, bool, object> GetDeserializer(
      Type t,
      ObjectSerializer oserializer)
    {
      if (t.IsPrimitive)
      {
        if (t == typeof (bool))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadBool());
        if (t == typeof (byte))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadByte());
        if (t == typeof (sbyte))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadSByte());
        if (t == typeof (short))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadInt16());
        if (t == typeof (ushort))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadUInt16());
        if (t == typeof (int))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadInt32());
        if (t == typeof (uint))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadUInt32());
        if (t == typeof (long))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadInt64());
        if (t == typeof (ulong))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadUInt64());
        if (t == typeof (float))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadSingle());
        if (t == typeof (double))
          return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadDouble());
        throw new ArgumentOutOfRangeException("type");
      }
      if (t == typeof (Decimal))
        return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadDecimal());
      if (t == typeof (DateTime))
        return (Func<IValueReader, bool, object>) ((r, sh) => (object) r.ReadDate());
      if (t == typeof (string))
        return (Func<IValueReader, bool, object>) ((r, sh) => r.ReadBool() ? (object) r.ReadString(Encoding.UTF8) : (object) null);
      if (t.IsEnum)
        return ObjectSerializer.GetDeserializer(Enum.GetUnderlyingType(t), oserializer);
      if (!t.IsArray && !(t == typeof (Array)))
        return (Func<IValueReader, bool, object>) ((r, skipHeader) =>
        {
          if (!skipHeader && ((int) r.ReadUInt16() & 1) == 0)
            return (object) null;
          if (typeof (ISerializable).IsAssignableFrom(t))
          {
            oserializer.LoadCtor(t);
            object deserializer;
            if (!oserializer.deserializingConstructor)
            {
              deserializer = oserializer.ctor.Invoke((object[]) null);
              ((ISerializable) deserializer).Deserialize(r);
            }
            else
              deserializer = oserializer.ctor.Invoke(new object[1]
              {
                (object) r
              });
            return deserializer;
          }
          if (t.GetCustomAttributes(true).OfType<SerializableAttribute>().Any<SerializableAttribute>())
            return oserializer.SerializableDeserializer(r);
          throw new ArgumentException("No serializer found for type " + t?.ToString());
        });
      Type etype = t.GetElementType();
      return (Func<IValueReader, bool, object>) ((r, sh) =>
      {
        if (!r.ReadBool())
          return (object) null;
        Array instance = Array.CreateInstance(etype, r.ReadInt32());
        for (int index = 0; index < instance.Length; ++index)
          instance.SetValue(r.Read(etype), index);
        return (object) instance;
      });
    }

    private Action<IValueWriter, object, bool> GetSerializer()
    {
      return this.GetSerializerAction(this.type);
    }

    private Action<IValueWriter, object, bool> GetSerializerAction(Type t)
    {
      if (t.IsPrimitive)
      {
        if (t == typeof (bool))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteBool((bool) v));
        if (t == typeof (byte))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteByte((byte) v));
        if (t == typeof (sbyte))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteSByte((sbyte) v));
        if (t == typeof (short))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteInt16((short) v));
        if (t == typeof (ushort))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteUInt16((ushort) v));
        if (t == typeof (int))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteInt32((int) v));
        if (t == typeof (uint))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteUInt32((uint) v));
        if (t == typeof (long))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteInt64((long) v));
        if (t == typeof (ulong))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteUInt64((ulong) v));
        if (t == typeof (float))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteSingle((float) v));
        if (t == typeof (double))
          return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteDouble((double) v));
        throw new ArgumentOutOfRangeException("type");
      }
      if (t == typeof (Decimal))
        return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteDecimal((Decimal) v));
      if (t == typeof (DateTime))
        return (Action<IValueWriter, object, bool>) ((w, v, sh) => w.WriteDate((DateTime) v));
      if (t == typeof (string))
        return (Action<IValueWriter, object, bool>) ((w, v, sh) =>
        {
          w.WriteBool(v != null);
          if (v == null)
            return;
          w.WriteString(Encoding.UTF8, (string) v);
        });
      if (t.IsEnum)
        return this.GetSerializerAction(Enum.GetUnderlyingType(t));
      return t.IsArray || t == typeof (Array) ? (Action<IValueWriter, object, bool>) ((w, v, sh) =>
      {
        if (v == null)
        {
          w.WriteBool(false);
        }
        else
        {
          w.WriteBool(true);
          Array array = (Array) v;
          w.WriteInt32(array.Length);
          if (t.IsPrimitive)
          {
            for (int index = 0; index < array.Length; ++index)
              w.Write<object>(array.GetValue(index));
          }
          else
          {
            Type elementType = t.GetElementType();
            for (int index = 0; index < array.Length; ++index)
              w.Write(array.GetValue(index), elementType);
          }
        }
      }) : (Action<IValueWriter, object, bool>) ((w, v, sh) =>
      {
        if (!sh)
        {
          ushort num = 0;
          Type type = (Type) null;
          if (v != null)
          {
            type = v.GetType();
            num = (ushort) ((uint) (ushort) ((uint) num << 1) | 1U);
          }
          w.WriteUInt16(num);
          if (v == null)
            return;
          if (!t.IsAssignableFrom(type))
            throw new ArgumentException();
          if (type != t)
          {
            ObjectSerializer.GetSerializer(type).serializer(w, v, true);
            return;
          }
        }
        if (v is ISerializable serializable2)
        {
          serializable2.Serialize(w);
        }
        else
        {
          if (!(t != typeof (object)) || !t.GetCustomAttributes(true).OfType<SerializableAttribute>().Any<SerializableAttribute>())
            throw new ArgumentException("No serializer found or specified for type " + t?.ToString(), "value");
          this.SerializableSerializer(w, v);
        }
      });
    }

    private void LoadMembers(Type t)
    {
      this.LoadCtor(t);
      if (this.members != null)
        return;
      this.members = ((IEnumerable<MemberInfo>) t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty)).Where<MemberInfo>((Func<MemberInfo, bool>) (mi =>
      {
        if (mi.MemberType == MemberTypes.Field)
        {
          FieldInfo fieldInfo = (FieldInfo) mi;
          return !typeof (Delegate).IsAssignableFrom(fieldInfo.FieldType.BaseType) && !fieldInfo.IsInitOnly;
        }
        if (mi.MemberType != MemberTypes.Property)
          return false;
        PropertyInfo propertyInfo = (PropertyInfo) mi;
        return propertyInfo.GetSetMethod() != (MethodInfo) null && propertyInfo.GetIndexParameters().Length == 0;
      })).ToDictionary<MemberInfo, MemberInfo, ObjectSerializer.SerializationPair>((Func<MemberInfo, MemberInfo>) (mi => mi), (Func<MemberInfo, ObjectSerializer.SerializationPair>) (mi =>
      {
        ObjectSerializer objectSerializer = (ObjectSerializer) null;
        if (mi.MemberType == MemberTypes.Field)
          objectSerializer = this.GetSerializerInternal(((FieldInfo) mi).FieldType);
        else if (mi.MemberType == MemberTypes.Property)
          objectSerializer = this.GetSerializerInternal(((PropertyInfo) mi).PropertyType);
        return new ObjectSerializer.SerializationPair(new Func<IValueReader, bool, object>(objectSerializer.Deserialize), new Action<IValueWriter, object, bool>(objectSerializer.Serialize));
      }));
    }

    private void LoadCtor(Type t)
    {
      if (this.ctor != (ConstructorInfo) null)
        return;
      ConstructorInfo constructor = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, new Type[1]
      {
        typeof (IValueReader)
      }, (ParameterModifier[]) null);
      if ((object) constructor == null)
        constructor = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, Type.EmptyTypes, (ParameterModifier[]) null);
      this.ctor = constructor;
      if (this.ctor == (ConstructorInfo) null)
        throw new ArgumentException("No empty or (ISerializationContext,IValueReader) constructor found for " + t.Name);
      this.deserializingConstructor = this.ctor.GetParameters().Length == 2;
    }

    private object SerializableDeserializer(IValueReader reader)
    {
      bool flag = false;
      if (this.type.IsClass)
        flag = reader.ReadBool();
      if (flag)
        return (object) null;
      using (MemoryStream serializationStream = new MemoryStream(reader.ReadBytes()))
        return new BinaryFormatter().Deserialize((Stream) serializationStream);
    }

    private void SerializableSerializer(IValueWriter writer, object value)
    {
      if (this.type.IsClass)
        writer.WriteBool(value == null);
      using (MemoryStream serializationStream = new MemoryStream())
      {
        new BinaryFormatter().Serialize((Stream) serializationStream, value);
        writer.WriteBytes(serializationStream.ToArray());
      }
    }

    private void Serialize(IValueWriter writer, object value, bool skipHeader)
    {
      this.serializer(writer, value, skipHeader);
    }

    private object Deserialize(IValueReader reader, bool skipHeader)
    {
      return this.deserializer(reader, skipHeader);
    }

    internal ObjectSerializer GetSerializerInternal(Type stype)
    {
      return this.type == stype ? this : ObjectSerializer.GetSerializer(stype);
    }

    internal static ObjectSerializer GetSerializer(Type type)
    {
      if (type == typeof (object) || type.IsInterface || type.IsAbstract)
        return ObjectSerializer.baseSerializer;
      ObjectSerializer serializer;
      bool flag;
      lock (ObjectSerializer.Serializers)
        flag = ObjectSerializer.Serializers.TryGetValue(type, out serializer);
      if (!flag)
      {
        serializer = new ObjectSerializer(type);
        lock (ObjectSerializer.Serializers)
        {
          if (!ObjectSerializer.Serializers.ContainsKey(type))
            ObjectSerializer.Serializers.Add(type, serializer);
        }
      }
      return serializer;
    }

    private class SerializationPair
    {
      public readonly Func<IValueReader, bool, object> Deserializer;
      public readonly Action<IValueWriter, object, bool> Serializer;

      public SerializationPair(
        Func<IValueReader, bool, object> des,
        Action<IValueWriter, object, bool> ser)
      {
        this.Deserializer = des;
        this.Serializer = ser;
      }
    }
  }
}
