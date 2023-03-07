// Decompiled with JetBrains decompiler
// Type: Radar.DebugSettings
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace Radar
{
  [Submenu]
  public class DebugSettings
  {
    public ToggleNode DrawHeightMap { get; set; } = new ToggleNode(false);

    public ToggleNode DisableHeightAdjust { get; set; } = new ToggleNode(false);

    public ToggleNode SkipNeighborFill { get; set; } = new ToggleNode(false);

    public ToggleNode SkipEdgeDetector { get; set; } = new ToggleNode(false);

    public ToggleNode SkipRecoloring { get; set; } = new ToggleNode(false);

    public ToggleNode DisableDrawRegionLimiting { get; set; } = new ToggleNode(false);

    public ToggleNode IgnoreFullscreenPanels { get; set; } = new ToggleNode(false);

    public RangeNode<int> MapCenterOffsetX { get; set; } = new RangeNode<int>(0, -1000, 1000);

    public RangeNode<int> MapCenterOffsetY { get; set; } = new RangeNode<int>(0, -1000, 1000);
  }
}
