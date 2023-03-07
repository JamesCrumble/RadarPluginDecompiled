// Decompiled with JetBrains decompiler
// Type: Radar.PathfindingSettings
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Radar
{
  [Submenu]
  public class PathfindingSettings
  {
    public ToggleNode ShowPathsToTargetsOnMap { get; set; } = new ToggleNode(true);

    public ColorNode DefaultMapPathColor { get; set; } = new ColorNode(Color.Green);

    public ToggleNode UseRainbowColorsForMapPaths { get; set; } = new ToggleNode(true);

    public ToggleNode ShowAllTargets { get; set; } = new ToggleNode(true);

    public ToggleNode ShowSelectedTargets { get; set; } = new ToggleNode(true);

    public ToggleNode EnableTargetNameBackground { get; set; } = new ToggleNode(true);

    public ColorNode TargetNameColor { get; set; } = new ColorNode(Color.Violet);

    public WorldPathSettings WorldPathSettings { get; set; } = new WorldPathSettings();
  }
}
