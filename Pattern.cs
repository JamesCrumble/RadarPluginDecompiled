// Decompiled with JetBrains decompiler
// Type: Radar.Pattern
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radar
{
  public class Pattern : IPattern
  {
    private string _mask;

    public Pattern(string pattern, string name)
    {
      List<string> list = ((IEnumerable<string>) pattern.Split(new char[1]
      {
        ' '
      }, StringSplitOptions.RemoveEmptyEntries)).ToList<string>();
      int index = list.FindIndex((Predicate<string>) (x => x == "^"));
      if (index == -1)
        index = 0;
      else
        list.RemoveAt(index);
      this.PatternOffset = index;
      this.Bytes = list.Select<string, byte>((Func<string, byte>) (x => !(x == "??") ? byte.Parse(x, NumberStyles.HexNumber) : (byte) 0)).ToArray<byte>();
      this.Mask = list.Select<string, bool>((Func<string, bool>) (x => x != "??")).ToArray<bool>();
      this.Name = name;
      while (!this.Mask[0])
      {
        this.PatternOffset = this.PatternOffset - 1;
        this.Mask = ((IEnumerable<bool>) this.Mask).Skip<bool>(1).ToArray<bool>();
        this.Bytes = ((IEnumerable<byte>) this.Bytes).Skip<byte>(1).ToArray<byte>();
      }
      while (true)
      {
        bool[] mask = this.Mask;
        if (!mask[mask.Length - 1])
        {
          this.Mask = ((IEnumerable<bool>) this.Mask).SkipLast<bool>(1).ToArray<bool>();
          this.Bytes = ((IEnumerable<byte>) this.Bytes).SkipLast<byte>(1).ToArray<byte>();
        }
        else
          break;
      }
    }

    public string Name { get; }

    public byte[] Bytes { get; }

    public bool[] Mask { get; }

    public int StartOffset => 0;

    public int PatternOffset { get; }

    string IPattern.Mask => this._mask ?? (this._mask = new string(((IEnumerable<bool>) this.Mask).Select<bool, char>((Func<bool, char>) (x => !x ? '?' : 'x')).ToArray<char>()));
  }
}
