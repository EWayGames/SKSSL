// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.StreamValueReader
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.IO;
using System.Text;

#nullable enable
namespace SKSSL
{
  public class StreamValueReader : IValueReader
  {
    private readonly Stream stream;

    public StreamValueReader(Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof (stream));
      this.stream = stream.CanRead ? stream : throw new ArgumentException("Can not read from this stream", nameof (stream));
    }

    public bool ReadBool() => this.ReadByte() == (byte) 1;

    public byte[] ReadBytes() => this.ReadBytes(this.ReadInt32());

    public byte[] ReadBytes(int count)
    {
      byte[] buffer = count >= 0 ? new byte[count] : throw new ArgumentOutOfRangeException(nameof (count), "count must be >= 0");
      int num;
      for (int offset = 0; offset < buffer.Length && (num = this.stream.Read(buffer, offset, count)) > 0; count -= num)
        offset += num;
      return buffer;
    }

    public sbyte ReadSByte() => (sbyte) this.stream.ReadByte();

    public short ReadInt16() => BitConverter.ToInt16(this.ReadBytes(2), 0);

    public int ReadInt32() => BitConverter.ToInt32(this.ReadBytes(4), 0);

    public long ReadInt64() => BitConverter.ToInt64(this.ReadBytes(8), 0);

    public byte ReadByte() => (byte) this.stream.ReadByte();

    public ushort ReadUInt16() => BitConverter.ToUInt16(this.ReadBytes(2), 0);

    public uint ReadUInt32() => BitConverter.ToUInt32(this.ReadBytes(4), 0);

    public ulong ReadUInt64() => BitConverter.ToUInt64(this.ReadBytes(8), 0);

    public string ReadString(Encoding encoding)
    {
      if (encoding == null)
        throw new ArgumentNullException(nameof (encoding));
      byte[] bytes = this.ReadBytes();
      return bytes.Length == 0 ? (string) null : encoding.GetString(bytes, 0, bytes.Length);
    }

    public Decimal ReadDecimal()
    {
      int[] bits = new int[this.ReadInt32()];
      for (int index = 0; index < bits.Length; ++index)
        bits[index] = this.ReadInt32();
      return new Decimal(bits);
    }

    public float ReadSingle() => BitConverter.ToSingle(this.ReadBytes(4), 0);

    public double ReadDouble() => BitConverter.ToDouble(this.ReadBytes(8), 0);

    public void Flush() => this.stream.Flush();
  }
}
