// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.xISerializer
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

#nullable enable
namespace SKSSL
{
  public interface xISerializer
  {
    void Serialize(IValueWriter writer, object element);

    object Deserialize(IValueReader reader);
  }
}
