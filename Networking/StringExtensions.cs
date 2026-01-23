// Decompiled with JetBrains decompiler
// Type: SKSSL.Networking.StringExtensions
// Assembly: SKSSL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8B98CDD1-E58F-4F79-8D54-EFB305228BC6
// Assembly location: D:\DEVBOOK_6\DECOMP\CS\SKSSL.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

#nullable enable
namespace SKSSL
{
  public static class StringExtensions
  {
    public static IPEndPoint Parse(this string str, int defaultPort = -1)
    {
      if (string.IsNullOrEmpty(str) || str.Trim().Length == 0)
        throw new ArgumentException("Endpoint descriptor may not be empty.");
      if (defaultPort != -1 && (defaultPort < 0 || defaultPort > (int) ushort.MaxValue))
        throw new ArgumentException(string.Format("Invalid default port '{0}'", (object) defaultPort));
      string[] source = str.Split(new char[1]{ ':' });
      int port;
      IPAddress address;
      if (source.Length <= 2)
      {
        port = source.Length != 1 ? source[1].AsPort() : defaultPort;
        if (!IPAddress.TryParse(source[0], out address))
          address = source[0].IpToHost();
      }
      else
      {
        if (source.Length <= 2)
          throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", (object) str));
        if (source[0].StartsWith("[") && source[source.Length - 2].EndsWith("]"))
        {
          address = IPAddress.Parse(string.Join(":", ((IEnumerable<string>) source).Take<string>(source.Length - 1).ToArray<string>()));
          port = source[source.Length - 1].AsPort();
        }
        else
        {
          address = IPAddress.Parse(str);
          port = defaultPort;
        }
      }
      return port != -1 ? new IPEndPoint(address, port) : throw new ArgumentException(string.Format("No port specified: '{0}'", (object) str));
    }

    private static int AsPort(this string str)
    {
      int result;
      return int.TryParse(str, out result) && result >= 0 && result <= (int) ushort.MaxValue ? result : throw new FormatException(string.Format("Invalid end point port '{0}'", (object) str));
    }

    private static IPAddress IpToHost(this string str)
    {
      IPAddress[] hostAddresses = Dns.GetHostAddresses(str);
      return hostAddresses != null && hostAddresses.Length != 0 ? hostAddresses[0] : throw new ArgumentException(string.Format("Host not found: {0}", (object) str));
    }
  }
}
