// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.IValueWriter
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Text;

#nullable enable
namespace SKSSL
{
  public interface IValueWriter
  {
    void WriteByte(byte value);

    void WriteSByte(sbyte value);

    bool WriteBool(bool value);

    void WriteBytes(byte[] value);

    void WriteBytes(byte[] value, int offset, int length);

    void WriteInt16(short value);

    void WriteInt32(int value);

    void WriteInt64(long value);

    void WriteUInt16(ushort value);

    void WriteUInt32(uint value);

    void WriteUInt64(ulong value);

    void WriteDecimal(Decimal value);

    void WriteSingle(float value);

    void WriteDouble(double value);

    void WriteString(Encoding encoding, string value);

    void Flush();
  }
}
