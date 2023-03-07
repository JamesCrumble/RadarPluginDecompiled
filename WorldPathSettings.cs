// Decompiled with JetBrains decompiler
// Type: Radar.WorldPathSettings
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Radar
{
  [Submenu]
  public class WorldPathSettings
  {
    public ToggleNode ShowPathsToTargets { get; set; } = new ToggleNode(true);

    public ToggleNode ShowPathsToTargetsOnlyWithClosedMap { get; set; } = new ToggleNode(true);

    public ToggleNode UseRainbowColorsForPaths { get; set; } = new ToggleNode(true);

    public ColorNode DefaultPathColor { get; set; } = new ColorNode(Color.Red);

    public ToggleNode OffsetPaths { get; set; } = new ToggleNode(true);

    public RangeNode<float> PathThickness { get; set; } = new RangeNode<float>(1f, 1f, 20f);

    public RangeNode<int> DrawEveryNthSegment { get; set; } = new RangeNode<int>(1, 1, 10);
  }
}
