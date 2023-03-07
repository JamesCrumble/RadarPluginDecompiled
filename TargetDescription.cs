// Decompiled with JetBrains decompiler
// Type: Radar.TargetDescription
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

namespace Radar
{
  public class TargetDescription
  {
    public string Name { get; set; } = "";

    public string DisplayName { get; set; }

    public int ExpectedCount { get; set; } = 1;

    public TargetType TargetType { get; set; }
  }
}
