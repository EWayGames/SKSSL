// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.IValueReader
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Text;

#nullable enable
namespace SKSSL
{
  public interface IValueReader
  {
    bool ReadBool();

    byte[] ReadBytes();

    byte[] ReadBytes(int count);

    sbyte ReadSByte();

    short ReadInt16();

    int ReadInt32();

    long ReadInt64();

    byte ReadByte();

    ushort ReadUInt16();

    uint ReadUInt32();

    ulong ReadUInt64();

    Decimal ReadDecimal();

    float ReadSingle();

    double ReadDouble();

    string ReadString(Encoding encoding);

    void Flush();
  }
}
