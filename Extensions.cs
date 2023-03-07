// Decompiled with JetBrains decompiler
// Type: Radar.Extensions
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.PoEMemory.Components;
using GameOffsets.Native;
using SharpDX;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;


#nullable enable
namespace Radar
{
  public static class Extensions
  {
    public static Vector3 GridPos(this 
    #nullable disable
    Render render) => Vector3.op_Division(render.Pos, 10.869565f);

    public static Vector2 ToSdx(this Vector2 v) => new Vector2(v.X, v.Y);

    public static Vector2i Truncate(this Vector2 v) => new Vector2i((int) v.X, (int) v.Y);

    public static IEnumerable<T> GetEveryNth<T>(this IEnumerable<T> source, int n)
    {
      int i = 0;
      foreach (T obj in source)
      {
        T item = obj;
        if (i == 0)
          yield return item;
        ++i;
        i %= n;
        item = default (T);
      }
    }

    public static bool Like(this string str, string pattern) => new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(str);
  }
}
