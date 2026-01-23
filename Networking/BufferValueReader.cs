// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.BufferValueReader
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Text;

#nullable enable
namespace SKSSL
{
  public class BufferValueReader : IValueReader
  {
    private readonly byte[] buffer;
    private readonly int length;
    private int position;

    public BufferValueReader(byte[] buffer)
    {
      this.buffer = buffer != null ? buffer : throw new ArgumentNullException(nameof (buffer));
      this.length = buffer.Length;
    }

    public BufferValueReader(byte[] buffer, int offset, int length)
    {
      this.buffer = buffer != null ? buffer : throw new ArgumentNullException(nameof (buffer));
      this.position = offset;
      this.length = length;
    }

    public byte[] Buffer => this.buffer;

    public int Position
    {
      get => this.position;
      set => this.position = value;
    }

    public bool ReadBool() => this.buffer[this.position++] == (byte) 1;

    public byte[] ReadBytes()
    {
      int count = this.ReadInt32();
      byte[] dst = new byte[count];
      System.Buffer.BlockCopy((Array) this.buffer, this.Position, (Array) dst, 0, count);
      this.Position += count;
      return dst;
    }

    public byte[] ReadBytes(int count)
    {
      byte[] dst = count >= 0 ? new byte[count] : throw new ArgumentOutOfRangeException(nameof (count), "count must be >= 0");
      System.Buffer.BlockCopy((Array) this.buffer, this.position, (Array) dst, 0, count);
      this.position += count;
      return dst;
    }

    public sbyte ReadSByte() => (sbyte) this.buffer[this.position++];

    public short ReadInt16()
    {
      short int16 = BitConverter.ToInt16(this.buffer, this.position);
      this.position += 2;
      return int16;
    }

    public int ReadInt32()
    {
      int int32 = BitConverter.ToInt32(this.buffer, this.position);
      this.position += 4;
      return int32;
    }

    public long ReadInt64()
    {
      long int64 = BitConverter.ToInt64(this.buffer, this.position);
      this.position += 8;
      return int64;
    }

    public byte ReadByte() => this.buffer[this.position++];

    public ushort ReadUInt16()
    {
      ushort uint16 = BitConverter.ToUInt16(this.buffer, this.position);
      this.position += 2;
      return uint16;
    }

    public uint ReadUInt32()
    {
      uint uint32 = BitConverter.ToUInt32(this.buffer, this.position);
      this.position += 4;
      return uint32;
    }

    public ulong ReadUInt64()
    {
      ulong uint64 = BitConverter.ToUInt64(this.buffer, this.position);
      this.position += 8;
      return uint64;
    }

    public Decimal ReadDecimal()
    {
      int[] bits = new int[4];
      for (int index = 0; index < bits.Length; ++index)
        bits[index] = this.ReadInt32();
      return new Decimal(bits);
    }

    public float ReadSingle()
    {
      float single = BitConverter.ToSingle(this.buffer, this.position);
      this.position += 4;
      return single;
    }

    public double ReadDouble()
    {
      double num = BitConverter.ToDouble(this.buffer, this.position);
      this.position += 8;
      return num;
    }

    public string ReadString(Encoding encoding)
    {
      if (encoding == null)
        throw new ArgumentNullException(nameof (encoding));
      int count = this.Read7BitEncodedInt();
      if (count == -1)
        return (string) null;
      string str = encoding.GetString(this.buffer, this.position, count);
      this.Position += count;
      return str;
    }

    public void Flush()
    {
    }

    private int Read7BitEncodedInt()
    {
      int num1 = 0;
      int num2 = 0;
      byte num3;
      do
      {
        num3 = this.ReadByte();
        num1 |= ((int) num3 & (int) sbyte.MaxValue) << num2;
        num2 += 7;
      }
      while (((uint) num3 & 128U) > 0U);
      return num1;
    }
  }
}
