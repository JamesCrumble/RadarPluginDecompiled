// Decompiled with JetBrains decompiler
// Type: Radar.Vector2d
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Radar
{
  public readonly record struct Vector2d
  {
    public readonly double X;
    public readonly double Y;

    public Vector2d(double X, double Y)
    {
      this.X = X;
      this.Y = Y;
    }

    public double Length => Math.Sqrt(this.X * this.X + this.Y * this.Y);

    public static Vector2d operator -(Vector2d v1, Vector2d v2) => new Vector2d(v1.X - v2.X, v1.Y - v2.Y);

    public static Vector2d operator +(Vector2d v1, Vector2d v2) => new Vector2d(v1.X + v2.X, v1.Y + v2.Y);

    public static Vector2d operator /(Vector2d v, double d) => new Vector2d(v.X / d, v.Y / d);

    [CompilerGenerated]
    public override int GetHashCode() => EqualityComparer<double>.Default.GetHashCode(this.X) * -1521134295 + EqualityComparer<double>.Default.GetHashCode(this.Y);

    [CompilerGenerated]
    public bool Equals(Vector2d other) => EqualityComparer<double>.Default.Equals(this.X, other.X) && EqualityComparer<double>.Default.Equals(this.Y, other.Y);

    [CompilerGenerated]
    public void Deconstruct(out double X, out double Y)
    {
      X = this.X;
      Y = this.Y;
    }
  }
}
