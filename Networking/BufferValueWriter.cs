// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.BufferValueWriter
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Text;

#nullable enable
namespace SKSSL
{
  public class BufferValueWriter : IValueWriter
  {
    private byte[] buffer;
    private int position;

    public BufferValueWriter(byte[] buffer)
    {
      this.buffer = buffer != null ? buffer : throw new ArgumentNullException(nameof (buffer));
    }

    public int Length
    {
      get => this.position;
      set
      {
        if (value > this.position)
        {
          int num = value - this.position;
          if (this.position + num >= this.buffer.Length)
          {
            int length1 = this.buffer.Length;
            int length2 = length1 * 2;
            while (length2 <= length1 + num)
              length2 *= 2;
            byte[] dst = new byte[length2];
            System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
            this.buffer = dst;
          }
        }
        this.position = value;
      }
    }

    public byte[] Buffer => this.buffer;

    public void WriteByte(byte value)
    {
      if (this.position + 1 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 1)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      this.buffer[this.position++] = value;
    }

    public void WriteSByte(sbyte value)
    {
      if (this.position + 1 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 1)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      this.buffer[this.position++] = (byte) value;
    }

    public bool WriteBool(bool value)
    {
      if (this.position + 1 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 1)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      this.buffer[this.position++] = value ? (byte) 1 : (byte) 0;
      return value;
    }

    public void WriteBytes(byte[] value)
    {
      if (value == null)
        throw new ArgumentNullException(nameof (value));
      int num = 4 + value.Length;
      if (this.position + num >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + num)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      System.Buffer.BlockCopy((Array) BitConverter.GetBytes(value.Length), 0, (Array) this.buffer, this.position, 4);
      this.position += 4;
      System.Buffer.BlockCopy((Array) value, 0, (Array) this.buffer, this.position, value.Length);
      this.position += value.Length;
    }

    public void WriteBytes(byte[] value, int offset, int length)
    {
      if (value == null)
        throw new ArgumentNullException(nameof (value));
      if (offset < 0 || offset >= value.Length)
        throw new ArgumentOutOfRangeException(nameof (offset), "offset can not negative or >=data.Length");
      if (length < 0 || offset + length > value.Length)
        throw new ArgumentOutOfRangeException(nameof (length), "length can not be negative or combined with offset longer than the array");
      int num = 4 + length;
      if (this.position + num >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + num)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      System.Buffer.BlockCopy((Array) BitConverter.GetBytes(length), 0, (Array) this.buffer, this.position, 4);
      this.position += 4;
      System.Buffer.BlockCopy((Array) value, offset, (Array) this.buffer, this.position, length);
      this.position += length;
    }

    public void InsertBytes(int offset, byte[] value, int valueOffset, int length)
    {
      if (value == null)
        throw new ArgumentNullException(nameof (value));
      if (valueOffset < 0 || valueOffset >= value.Length)
        throw new ArgumentOutOfRangeException(nameof (offset), "offset can not negative or >=data.Length");
      if (length < 0 || valueOffset + length > value.Length)
        throw new ArgumentOutOfRangeException(nameof (length), "length can not be negative or combined with offset longer than the array");
      if (this.position + length >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + length)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      if (offset != this.position)
        System.Buffer.BlockCopy((Array) this.buffer, offset, (Array) this.buffer, offset + length, this.position - offset);
      System.Buffer.BlockCopy((Array) value, valueOffset, (Array) this.buffer, offset, length);
      this.position += length;
    }

    public unsafe void WriteInt16(short value)
    {
      if (this.position + 2 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 2)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(short*) (numPtr + this.position) = value;
      this.position += 2;
    }

    public unsafe void WriteInt32(int value)
    {
      if (this.position + 4 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 4)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(int*) (numPtr + this.position) = value;
      this.position += 4;
    }

    public unsafe void WriteInt64(long value)
    {
      if (this.position + 8 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 8)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(long*) (numPtr + this.position) = value;
      this.position += 8;
    }

    public unsafe void WriteUInt16(ushort value)
    {
      if (this.position + 2 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 2)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(short*) (numPtr + this.position) = (short) value;
      this.position += 2;
    }

    public unsafe void WriteUInt32(uint value)
    {
      if (this.position + 4 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 4)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(int*) (numPtr + this.position) = (int) value;
      this.position += 4;
    }

    public unsafe void WriteUInt64(ulong value)
    {
      if (this.position + 8 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 8)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(long*) (numPtr + this.position) = (long) value;
      this.position += 8;
    }

    public void WriteDecimal(Decimal value)
    {
      foreach (int bit in Decimal.GetBits(value))
        this.WriteInt32(bit);
    }

    public unsafe void WriteSingle(float value)
    {
      if (this.position + 4 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 4)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(float*) (numPtr + this.position) = value;
      this.position += 4;
    }

    public unsafe void WriteDouble(double value)
    {
      if (this.position + 8 >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + 8)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      fixed (byte* numPtr = this.buffer)
        *(double*) (numPtr + this.position) = value;
      this.position += 8;
    }

    public void WriteString(Encoding encoding, string value)
    {
      if (encoding == null)
        throw new ArgumentNullException(nameof (encoding));
      if (value == null)
      {
        this.Write7BitEncodedInt(-1);
      }
      else
      {
        byte[] bytes = encoding.GetBytes(value);
        int num = 4 + bytes.Length;
        if (this.position + num >= this.buffer.Length)
        {
          int length1 = this.buffer.Length;
          int length2 = length1 * 2;
          while (length2 <= length1 + num)
            length2 *= 2;
          byte[] dst = new byte[length2];
          System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
          this.buffer = dst;
        }
        this.Write7BitEncodedInt(bytes.Length);
        System.Buffer.BlockCopy((Array) bytes, 0, (Array) this.buffer, this.position, bytes.Length);
        this.position += bytes.Length;
      }
    }

    public void Flush() => this.position = 0;

    public void Pad(int count)
    {
      if (this.position + count >= this.buffer.Length)
      {
        int length1 = this.buffer.Length;
        int length2 = length1 * 2;
        while (length2 <= length1 + count)
          length2 *= 2;
        byte[] dst = new byte[length2];
        System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
        this.buffer = dst;
      }
      this.position += count;
    }

    public byte[] ToArray()
    {
      byte[] dst = new byte[this.Length];
      System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
      return dst;
    }

    private void Write7BitEncodedInt(int value)
    {
      uint num;
      for (num = (uint) value; num >= 128U; num >>= 7)
        this.WriteByte((byte) (num | 128U));
      this.WriteByte((byte) num);
    }

    private void EnsureAdditionalCapacity(int additionalCapacity)
    {
      if (this.position + additionalCapacity < this.buffer.Length)
        return;
      int length1 = this.buffer.Length;
      int length2 = length1 * 2;
      while (length2 <= length1 + additionalCapacity)
        length2 *= 2;
      byte[] dst = new byte[length2];
      System.Buffer.BlockCopy((Array) this.buffer, 0, (Array) dst, 0, this.Length);
      this.buffer = dst;
    }
  }
}
