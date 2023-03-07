// Decompiled with JetBrains decompiler
// Type: Radar.TargetType
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Radar
{
  [JsonConverter(typeof (StringEnumConverter))]
  public enum TargetType
  {
    Tile,
    Entity,
  }
}
