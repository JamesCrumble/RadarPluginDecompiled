// Decompiled with JetBrains decompiler
// Type: Radar.RadarSettings
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;

namespace Radar
{
  public class RadarSettings : ISettings
  {
    [JsonIgnore]
    public ButtonNode Reload { get; set; } = new ButtonNode();

    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public RangeNode<float> CustomScale { get; set; } = new RangeNode<float>(1f, 0.1f, 10f);

    public ToggleNode DrawWalkableMap { get; set; } = new ToggleNode(true);

    public ColorNode TerrainColor { get; set; } = new ColorNode(new Color(Vector3.op_Division(new Vector3(150f), (float) byte.MaxValue)));

    public RangeNode<int> MaximumPathCount { get; set; } = new RangeNode<int>(1000, 0, 1000);

    public PathfindingSettings PathfindingSettings { get; set; } = new PathfindingSettings();

    public DebugSettings Debug { get; set; } = new DebugSettings();
  }
}
