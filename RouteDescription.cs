// Decompiled with JetBrains decompiler
// Type: Radar.RouteDescription
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using GameOffsets.Native;
using SharpDX;
using System;
using System.Collections.Generic;

namespace Radar
{
  public class RouteDescription
  {
    public List<Vector2i> Path { get; set; }

    public Func<Color> MapColor { get; set; }

    public Func<Color> WorldColor { get; set; }
  }
}
