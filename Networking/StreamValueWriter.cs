// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.StreamValueWriter
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.IO;
using System.Text;

#nullable enable
namespace SKSSL
{
  public class StreamValueWriter : IValueWriter
  {
    private readonly Stream stream;

    public StreamValueWriter(Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof (stream));
      if (!stream.CanWrite)
        throw new ArgumentException("Can not write to this stream", nameof (stream));
      if (!BitConverter.IsLittleEndian)
        throw new NotSupportedException("Big Endian architecture not supported");
      this.stream = stream;
    }

    public void WriteByte(byte value) => this.stream.WriteByte(value);

    public void WriteSByte(sbyte value) => this.stream.WriteByte((byte) value);

    public bool WriteBool(bool value)
    {
      this.stream.WriteByte(value ? (byte) 1 : (byte) 0);
      return value;
    }

    public void WriteBytes(byte[] value)
    {
      if (value == null)
        throw new ArgumentNullException(nameof (value));
      this.WriteInt32(value.Length);
      this.stream.Write(value, 0, value.Length);
    }

    public void WriteBytes(byte[] value, int offset, int length)
    {
      if (value == null)
        throw new ArgumentNullException(nameof (value));
      if (offset < 0 || offset >= value.Length)
        throw new ArgumentOutOfRangeException(nameof (offset), "offset can not negative or >=data.Length");
      if (length < 0 || offset + length >= value.Length)
        throw new ArgumentOutOfRangeException(nameof (length), "length can not be negative or combined with offset longer than the array");
      this.WriteInt32(length);
      this.stream.Write(value, offset, length);
    }

    public void WriteInt16(short value) => this.Write(BitConverter.GetBytes(value));

    public void WriteInt32(int value) => this.Write(BitConverter.GetBytes(value));

    public void WriteInt64(long value) => this.Write(BitConverter.GetBytes(value));

    public void WriteUInt16(ushort value) => this.Write(BitConverter.GetBytes(value));

    public void WriteUInt32(uint value) => this.Write(BitConverter.GetBytes(value));

    public void WriteUInt64(ulong value) => this.Write(BitConverter.GetBytes(value));

    public void WriteDecimal(Decimal value)
    {
      int[] bits = Decimal.GetBits(value);
      this.WriteInt32(bits.Length);
      for (int index = 0; index < bits.Length; ++index)
        this.WriteInt32(bits[index]);
    }

    public void WriteSingle(float value) => this.Write(BitConverter.GetBytes(value));

    public void WriteDouble(double value) => this.Write(BitConverter.GetBytes(value));

    public void WriteString(Encoding encoding, string value)
    {
      if (encoding == null)
        throw new ArgumentNullException(nameof (encoding));
      this.WriteBytes(!string.IsNullOrEmpty(value) ? encoding.GetBytes(value) : new byte[0]);
    }

    public void Flush() => this.stream.Flush();

    private void Write(byte[] data) => this.stream.Write(data, 0, data.Length);
  }
}
