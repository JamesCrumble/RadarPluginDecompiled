// Decompiled with JetBrains decompiler
// Type: Radar.Radar
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using GameOffsets;
using GameOffsets.Native;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


#nullable enable
namespace Radar
{
  public class Radar : BaseSettingsPlugin<
  #nullable disable
  RadarSettings>
  {
    private const string TextureName = "radar_minimap";
    private const int TileToGridConversion = 23;
    private const int TileToWorldConversion = 250;
    public const float GridToWorldMultiplier = 10.869565f;
    private const double TileHeightFinalMultiplier = 7.8125;
    private const double CameraAngle = 0.67544242052180559;
    private static readonly float CameraAngleCos = (float) Math.Cos(43.0 * Math.PI / 200.0);
    private static readonly float CameraAngleSin = (float) Math.Sin(43.0 * Math.PI / 200.0);
    private double _mapScale;
    private ConcurrentDictionary<string, List<TargetDescription>> _targetDescriptions = new ConcurrentDictionary<string, List<TargetDescription>>();
    private Vector2i? _areaDimensions;
    private TerrainData _terrainMetadata;
    private float[][] _heightData;
    private byte[] _rawTerrainData;
    private int[][] _processedTerrainData;
    private Dictionary<string, TargetDescription> _targetDescriptionsInArea = new Dictionary<string, TargetDescription>();
    private HashSet<string> _currentZoneTargetEntityPaths = new HashSet<string>();
    private CancellationTokenSource _findPathsCts = new CancellationTokenSource();
    private ConcurrentDictionary<string, TargetLocations> _clusteredTargetLocations = new ConcurrentDictionary<string, TargetLocations>();
    private ConcurrentDictionary<string, List<Vector2i>> _allTargetLocations = new ConcurrentDictionary<string, List<Vector2i>>();
    private ConcurrentDictionary<Vector2, RouteDescription> _routes = new ConcurrentDictionary<Vector2, RouteDescription>();
    private byte[] _rotationSelectorCache;
    private byte[] _rotationHelperCache;
    private static readonly List<Color> RainbowColors;
    private RectangleF _rect;
    private ImDrawListPtr _backGroundWindowPtr;

    private byte[] RotationSelector => this._rotationSelectorCache ?? (this._rotationSelectorCache = this.GetRotationSelector());

    private byte[] RotationHelper => this._rotationHelperCache ?? (this._rotationHelperCache = this.GetRotationHelper());

    public virtual void AreaChange(AreaInstance area)
    {
      this.StopPathFinding();
      if (!this.GameController.Game.IsInGameState && !this.GameController.Game.IsEscapeState)
        return;
      this._targetDescriptionsInArea = this.GetTargetDescriptionsInArea().ToDictionary<TargetDescription, string>((Func<TargetDescription, string>) (x => x.Name));
      this._currentZoneTargetEntityPaths = this._targetDescriptionsInArea.Values.Where<TargetDescription>((Func<TargetDescription, bool>) (x => x.TargetType == TargetType.Entity)).Select<TargetDescription, string>((Func<TargetDescription, string>) (x => x.Name)).ToHashSet<string>();
      this._terrainMetadata = this.GameController.IngameState.Data.DataStruct.Terrain;
      this._rawTerrainData = this.GameController.Memory.ReadStdVector<byte>(Radar.Radar.Cast(this._terrainMetadata.LayerMelee));
      this._heightData = this.GetTerrainHeight();
      this._allTargetLocations = this.GetTargets();
      this._processedTerrainData = this.ParseTerrainPathData();
      this.GenerateMapTexture();
      this._clusteredTargetLocations = this.ClusterTargets();
      this.StartPathFinding();
    }

    public virtual void OnLoad()
    {
      this.LoadTargets();
      this.Settings.Reload.OnPressed = (Action) (() => Core.MainRunner.Run(new Coroutine((Action) (() =>
      {
        this.LoadTargets();
        base.AreaChange(this.GameController.Area.CurrentArea);
      }), (IYieldBase) new WaitTime(0), (IPlugin) this, "RestartPathfinding", false, true)));
      this.Settings.MaximumPathCount.OnValueChanged += (EventHandler<int>) ((_1, _2) => Core.MainRunner.Run(new Coroutine(new Action(this.RestartPathFinding), (IYieldBase) new WaitTime(0), (IPlugin) this, "RestartPathfinding", false, true)));
      this.Settings.TerrainColor.OnValueChanged += (EventHandler<Color>) ((_3, _4) => this.GenerateMapTexture());
      this.Settings.Debug.DrawHeightMap.OnValueChanged += (EventHandler<bool>) ((_5, _6) => this.GenerateMapTexture());
      this.Settings.Debug.SkipEdgeDetector.OnValueChanged += (EventHandler<bool>) ((_7, _8) => this.GenerateMapTexture());
      this.Settings.Debug.SkipNeighborFill.OnValueChanged += (EventHandler<bool>) ((_9, _10) => this.GenerateMapTexture());
      this.Settings.Debug.SkipRecoloring.OnValueChanged += (EventHandler<bool>) ((_11, _12) => this.GenerateMapTexture());
      this.Settings.Debug.DisableHeightAdjust.OnValueChanged += (EventHandler<bool>) ((_13, _14) => this.GenerateMapTexture());
    }

    public virtual void EntityAdded(Entity entity)
    {
      Positioned positioned = entity.GetComponent<Positioned>();
      if (positioned == null)
        return;
      string path = entity.Path;
      if (this._currentZoneTargetEntityPaths.Contains(path))
      {
        bool alreadyContains = false;
        this._allTargetLocations.AddOrUpdate(path, (Func<string, List<Vector2i>>) (_ =>
        {
          List<Vector2i> vector2iList = new List<Vector2i>();
          vector2iList.Add(positioned.GridPos.Truncate());
          return vector2iList;
        }), (Func<string, List<Vector2i>, List<Vector2i>>) ((_, l) => !(alreadyContains = l.Contains(positioned.GridPos.Truncate())) ? ((IEnumerable<Vector2i>) l).Append<Vector2i>(positioned.GridPos.Truncate()).ToList<Vector2i>() : l));
        if (!alreadyContains)
        {
          TargetLocations valueOrDefault = this._clusteredTargetLocations.GetValueOrDefault<string, TargetLocations>(path);
          TargetLocations targetLocations = this._clusteredTargetLocations.AddOrUpdate(path, (Func<string, TargetLocations>) (_ => this.ClusterTarget(this._targetDescriptionsInArea[path])), (Func<string, TargetLocations, TargetLocations>) ((_1, _2) => this.ClusterTarget(this._targetDescriptionsInArea[path])));
          if (valueOrDefault == null || !((IEnumerable<Vector2>) targetLocations.Locations).ToHashSet<Vector2>().SetEquals((IEnumerable<Vector2>) valueOrDefault.Locations))
            this.RestartPathFinding();
        }
      }
    }

    private Vector2 GetPlayerPosition()
    {
      Positioned component = this.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Positioned>();
      if (component == null)
        return new Vector2(0.0f, 0.0f);
      Vector2 playerPosition;
      // ISSUE: explicit constructor call
      ((Vector2) ref playerPosition).\u002Ector((float) component.GridX, (float) component.GridY);
      return playerPosition;
    }

    public virtual void Render()
    {
      IngameUIElements ingameUi = this.GameController.Game.IngameState.IngameUi;
      if (!ToggleNode.op_Implicit(this.Settings.Debug.IgnoreFullscreenPanels) && (((Element) ingameUi.DelveWindow).IsVisible || ((Element) ingameUi.Atlas).IsVisible || ((Element) ingameUi.SellWindow).IsVisible))
        return;
      RectangleF rectangleF = this.GameController.Window.GetWindowRectangle();
      ((RectangleF) ref rectangleF).Location = Vector2.Zero;
      this._rect = rectangleF;
      if (!ToggleNode.op_Implicit(this.Settings.Debug.DisableDrawRegionLimiting))
      {
        if (ingameUi.OpenRightPanel.IsVisible)
          this._rect.Right = ingameUi.OpenRightPanel.GetClientRectCache.Left;
        if (ingameUi.OpenLeftPanel.IsVisible)
          this._rect.Left = ingameUi.OpenLeftPanel.GetClientRectCache.Right;
      }
      ImGui.SetNextWindowSize(new Vector2(((RectangleF) ref this._rect).Width, ((RectangleF) ref this._rect).Height));
      ImGui.SetNextWindowPos(new Vector2(this._rect.Left, this._rect.Top));
      ImGui.Begin("radar_background", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus);
      this._backGroundWindowPtr = ImGui.GetWindowDrawList();
      SubMap largeMap = ((RemoteMemoryObject) ingameUi.Map.LargeMap).AsObject<SubMap>();
      if (((Element) largeMap).IsVisible)
      {
        rectangleF = ((Element) largeMap).GetClientRect();
        Vector2 mapCenter = Vector2.op_Addition(Vector2.op_Addition(Vector2.op_Addition(((RectangleF) ref rectangleF).TopLeft, largeMap.Shift), largeMap.DefaultShift), new Vector2((float) RangeNode<int>.op_Implicit(this.Settings.Debug.MapCenterOffsetX), (float) RangeNode<int>.op_Implicit(this.Settings.Debug.MapCenterOffsetY)));
        this._mapScale = (double) this.GameController.IngameState.Camera.Height / 677.0 * (double) largeMap.Zoom * (double) RangeNode<float>.op_Implicit(this.Settings.CustomScale);
        this.DrawLargeMap(mapCenter);
        this.DrawTargets(mapCenter);
      }
      this.DrawWorldPaths(largeMap);
      ImGui.End();
    }

    private void DrawWorldPaths(SubMap largeMap)
    {
      if (!ToggleNode.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.ShowPathsToTargets) || ((Element) largeMap).IsVisible && ToggleNode.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.ShowPathsToTargetsOnlyWithClosedMap))
        return;
      ExileCore.PoEMemory.Components.Render component = this.GameController.Game.IngameState.Data.LocalPlayer?.GetComponent<ExileCore.PoEMemory.Components.Render>();
      if (component == null)
        return;
      Camera camera = this.GameController.IngameState.Camera;
      Vector3 pos = component.Pos;
      pos.Z = component.RenderStruct.Height;
      Vector3 vector3 = pos;
      Vector2 screen1 = camera.WorldToScreen(vector3);
      foreach ((RouteDescription routeDescription, float num1) in this._routes.Values.GroupBy<RouteDescription, double>((Func<RouteDescription, double>) (x =>
      {
        double num3;
        if (x.Path.Count < 2)
        {
          num3 = 0.0;
        }
        else
        {
          Vector2i vector2i = Vector2i.op_Subtraction(x.Path[1], x.Path[0]);
          if (true)
            ;
          double num4 = Math.Atan2((double) vector2i.Y, (double) vector2i.X);
          if (true)
            ;
          num3 = num4;
        }
        return num3;
      })).SelectMany<IGrouping<double, RouteDescription>, (RouteDescription, float)>((Func<IGrouping<double, RouteDescription>, IEnumerable<(RouteDescription, float)>>) (group => group.Select<RouteDescription, (RouteDescription, float)>((Func<RouteDescription, int, (RouteDescription, float)>) ((route, i) => (route, (float) ((double) i - (double) group.Count<RouteDescription>() / 2.0 + 0.5)))))))
      {
        Vector2 vector2_1 = screen1;
        Vector2 vector2_2 = vector2_1;
        int num2 = 0;
        foreach (Vector2i vector2i in routeDescription.Path)
        {
          Vector2 screen2 = this.GameController.IngameState.Camera.WorldToScreen(new Vector3((float) vector2i.X * 10.869565f, (float) vector2i.Y * 10.869565f, -this._heightData[vector2i.Y][vector2i.X]));
          Vector2 vector2_3;
          if (ToggleNode.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.OffsetPaths))
          {
            Vector2 vector2_4 = Vector2.op_Subtraction(screen2, vector2_1);
            if (true)
              ;
            Vector2 vector2_5 = Vector2.op_Division(new Vector2(vector2_4.Y, -vector2_4.X), ((Vector2) ref vector2_4).Length());
            if (true)
              ;
            vector2_3 = vector2_5;
          }
          else
            vector2_3 = Vector2.Zero;
          Vector2 vector2_6 = Vector2.op_Multiply(Vector2.op_Multiply(vector2_3, num1), RangeNode<float>.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.PathThickness));
          vector2_1 = screen2;
          Vector2 vector2_7 = Vector2.op_Addition(screen2, vector2_6);
          if (++num2 % RangeNode<int>.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.DrawEveryNthSegment) == 0)
          {
            if (((RectangleF) ref this._rect).Contains(vector2_2) || ((RectangleF) ref this._rect).Contains(vector2_7))
              this.Graphics.DrawLine(vector2_2, vector2_7, RangeNode<float>.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.PathThickness), routeDescription.WorldColor());
            else
              break;
          }
          vector2_2 = vector2_7;
        }
      }
    }

    private void DrawBox(Vector2 p0, Vector2 p1, Color color) => this._backGroundWindowPtr.AddRectFilled(Extensions.ToVector2Num(p0), Extensions.ToVector2Num(p1), Extensions.ToImgui(color));

    private void DrawText(string text, Vector2 pos, Color color) => this._backGroundWindowPtr.AddText(Extensions.ToVector2Num(pos), Extensions.ToImgui(color), text);

    private Vector2 TranslateGridDeltaToMapDelta(Vector2 delta, float deltaZ)
    {
      deltaZ /= 10.869565f;
      return Vector2.op_Multiply((float) this._mapScale, new Vector2((delta.X - delta.Y) * Radar.Radar.CameraAngleCos, (deltaZ - (delta.X + delta.Y)) * Radar.Radar.CameraAngleSin));
    }

    private void DrawLargeMap(Vector2 mapCenter)
    {
      if (!ToggleNode.op_Implicit(this.Settings.DrawWalkableMap) || !this.Graphics.LowLevel.HasTexture("radar_minimap") || !this._areaDimensions.HasValue)
        return;
      ExileCore.PoEMemory.Components.Render component = this.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Render>();
      if (component == null)
        return;
      RectangleF rectangleF;
      // ISSUE: explicit constructor call
      ((RectangleF) ref rectangleF).\u002Ector(-component.GridPos().X, -component.GridPos().Y, (float) this._areaDimensions.Value.X, (float) this._areaDimensions.Value.Y);
      float deltaZ = -component.RenderStruct.Height;
      Vector2 vector2_1 = Vector2.op_Addition(mapCenter, this.TranslateGridDeltaToMapDelta(new Vector2(((RectangleF) ref rectangleF).Left, ((RectangleF) ref rectangleF).Top), deltaZ));
      Vector2 vector2_2 = Vector2.op_Addition(mapCenter, this.TranslateGridDeltaToMapDelta(new Vector2(((RectangleF) ref rectangleF).Right, ((RectangleF) ref rectangleF).Top), deltaZ));
      Vector2 vector2_3 = Vector2.op_Addition(mapCenter, this.TranslateGridDeltaToMapDelta(new Vector2(((RectangleF) ref rectangleF).Right, ((RectangleF) ref rectangleF).Bottom), deltaZ));
      Vector2 vector2_4 = Vector2.op_Addition(mapCenter, this.TranslateGridDeltaToMapDelta(new Vector2(((RectangleF) ref rectangleF).Left, ((RectangleF) ref rectangleF).Bottom), deltaZ));
      this._backGroundWindowPtr.AddImageQuad(this.Graphics.LowLevel.GetTexture("radar_minimap"), Extensions.ToVector2Num(vector2_1), Extensions.ToVector2Num(vector2_2), Extensions.ToVector2Num(vector2_3), Extensions.ToVector2Num(vector2_4));
    }

    private void DrawTargets(Vector2 mapCenter)
    {
      Color color = this.Settings.PathfindingSettings.TargetNameColor.Value;
      ExileCore.PoEMemory.Components.Render component = this.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Render>();
      if (component == null)
        return;
      Vector2 vector2;
      // ISSUE: explicit constructor call
      ((Vector2) ref vector2).\u002Ector(component.GridPos().X, component.GridPos().Y);
      float num1 = -component.RenderStruct.Height;
      int count = 0;
      if (ToggleNode.op_Implicit(this.Settings.PathfindingSettings.ShowPathsToTargetsOnMap))
      {
        foreach (RouteDescription routeDescription in (IEnumerable<RouteDescription>) this._routes.Values)
        {
          count = (count + 1) % 5;
          foreach (Vector2i vector2i in ((IEnumerable<Vector2i>) routeDescription.Path).Skip<Vector2i>(count).GetEveryNth<Vector2i>(5))
          {
            Vector2 mapDelta = this.TranslateGridDeltaToMapDelta(Vector2.op_Subtraction(new Vector2((float) vector2i.X, (float) vector2i.Y), vector2), num1 - this._heightData[vector2i.Y][vector2i.X]);
            this.DrawBox(Vector2.op_Subtraction(Vector2.op_Addition(mapCenter, mapDelta), new Vector2(2f, 2f)), Vector2.op_Addition(Vector2.op_Addition(mapCenter, mapDelta), new Vector2(2f, 2f)), routeDescription.MapColor());
          }
        }
      }
      if (ToggleNode.op_Implicit(this.Settings.PathfindingSettings.ShowAllTargets))
      {
        foreach ((string str, List<Vector2i> vector2iList) in this._allTargetLocations)
        {
          Vector2 sdx = (this.Graphics.MeasureText(str) / 2f).ToSdx();
          foreach (Vector2i vector2i in vector2iList)
          {
            Vector2 mapDelta = this.TranslateGridDeltaToMapDelta(Vector2.op_Subtraction(((Vector2i) ref vector2i).ToVector2(), vector2), num1 - this._heightData[vector2i.Y][vector2i.X]);
            if (ToggleNode.op_Implicit(this.Settings.PathfindingSettings.EnableTargetNameBackground))
              this.DrawBox(Vector2.op_Subtraction(Vector2.op_Addition(mapCenter, mapDelta), sdx), Vector2.op_Addition(Vector2.op_Addition(mapCenter, mapDelta), sdx), Color.Black);
            this.DrawText(str, Vector2.op_Subtraction(Vector2.op_Addition(mapCenter, mapDelta), sdx), color);
          }
        }
      }
      else
      {
        if (!ToggleNode.op_Implicit(this.Settings.PathfindingSettings.ShowSelectedTargets))
          return;
        foreach ((string key, TargetLocations targetLocations) in this._clusteredTargetLocations)
        {
          foreach (Vector2 location in targetLocations.Locations)
          {
            float num2 = 0.0f;
            if ((double) location.X < (double) this._heightData[0].Length && (double) location.Y < (double) this._heightData.Length)
              num2 = this._heightData[(int) location.Y][(int) location.X];
            string text = string.IsNullOrWhiteSpace(targetLocations.DisplayName) ? key : targetLocations.DisplayName;
            Vector2 sdx = (this.Graphics.MeasureText(text) / 2f).ToSdx();
            Vector2 mapDelta = this.TranslateGridDeltaToMapDelta(Vector2.op_Subtraction(location, vector2), num1 - num2);
            if (ToggleNode.op_Implicit(this.Settings.PathfindingSettings.EnableTargetNameBackground))
              this.DrawBox(Vector2.op_Subtraction(Vector2.op_Addition(mapCenter, mapDelta), sdx), Vector2.op_Addition(Vector2.op_Addition(mapCenter, mapDelta), sdx), Color.Black);
            this.DrawText(text, Vector2.op_Subtraction(Vector2.op_Addition(mapCenter, mapDelta), sdx), color);
          }
        }
      }
    }

    private byte[] GetRotationSelector()
    {
      Pattern pattern1 = new Pattern("?? 8D ?? ^ ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 8D ?? ?? ?? ?? 8B ?? 0F B6 ?? ?? ?? 8D ?? ?? ?? 88 ?? ?? ??", "Terrain Rotation Selector");
      long pattern2 = this.GameController.Memory.FindPatterns(new IPattern[1]
      {
        (IPattern) pattern1
      })[0];
      return this.GameController.Memory.ReadBytes(this.GameController.Memory.AddressOfProcess + ((long) this.GameController.Memory.Read<int>(this.GameController.Memory.AddressOfProcess + pattern2 + (long) pattern1.PatternOffset) + pattern2 + (long) pattern1.PatternOffset + 4L), 8);
    }

    private byte[] GetRotationHelper()
    {
      Pattern pattern1 = new Pattern("?? 8D ?? ^ ?? ?? ?? ?? ?? 03 ?? 8B ?? ?? 2B ?? 89 ?? ?? ?? 8B ?? ?? ?? FF ??", "Terrain Rotator Helper");
      long pattern2 = this.GameController.Memory.FindPatterns(new IPattern[1]
      {
        (IPattern) pattern1
      })[0];
      return this.GameController.Memory.ReadBytes(this.GameController.Memory.AddressOfProcess + ((long) this.GameController.Memory.Read<int>(this.GameController.Memory.AddressOfProcess + pattern2 + (long) pattern1.PatternOffset) + pattern2 + (long) pattern1.PatternOffset + 4L), 24);
    }

    private static unsafe StdVector Cast(NativePtrArray nativePtrArray)
    {
      // ISSUE: untyped stack allocation
      IntPtr pointer = __untypedstackalloc(checked (new IntPtr(1) * sizeof (NativePtrArray)));
      *(NativePtrArray*) pointer = nativePtrArray;
      return MemoryMarshal.Cast<NativePtrArray, StdVector>(new Span<NativePtrArray>((void*) pointer, 1))[0];
    }

    private float[][] GetTerrainHeight()
    {
      byte[] rotationSelector = this.RotationSelector;
      byte[] rotationHelper = this.RotationHelper;
      TileStructure[] tileData = this.GameController.Memory.ReadStdVector<TileStructure>(Radar.Radar.Cast(this._terrainMetadata.TgtArray));
      Dictionary<long, sbyte[]> tileHeightCache = ((IEnumerable<TileStructure>) tileData).Select<TileStructure, long>((Func<TileStructure, long>) (x => x.SubTileDetailsPtr)).Distinct<long>().AsParallel<long>().Select(addr => new
      {
        addr = addr,
        data = this.GameController.Memory.ReadStdVector<sbyte>(this.GameController.Memory.Read<SubTileStructure>(addr).SubTileHeight)
      }).ToDictionary(x => x.addr, x => x.data);
      int gridSizeX = (int) this._terrainMetadata.NumCols * 23;
      int toExclusive = (int) this._terrainMetadata.NumRows * 23;
      float[][] result = new float[toExclusive][];
      Parallel.For(0, toExclusive, (Action<int>) (y =>
      {
        result[y] = new float[gridSizeX];
        for (int index1 = 0; index1 < gridSizeX; ++index1)
        {
          TileStructure tileStructure = tileData[y / 23 * (int) this._terrainMetadata.NumCols + index1 / 23];
          sbyte[] numArray1 = tileHeightCache[tileStructure.SubTileDetailsPtr];
          int num1 = 0;
          if (numArray1.Length != 0)
          {
            int num2 = index1 % 23;
            int num3 = y % 23;
            int num4 = 22;
            int[] numArray2 = new int[4]
            {
              num4 - num2,
              num2,
              num4 - num3,
              num3
            };
            int index2 = (int) rotationSelector[(int) tileStructure.RotationSelector] * 3;
            int num5 = (int) rotationHelper[index2];
            int num6 = (int) rotationHelper[index2 + 1];
            int num7 = (int) rotationHelper[index2 + 2];
            int num8 = numArray2[num5 * 2 + num6];
            int index3 = numArray2[num7 + (1 - num5) * 2] * 23 + num8;
            num1 = (int) numArray1[index3];
          }
          result[y][index1] = (float) ((int) tileStructure.TileHeight * this._terrainMetadata.TileHeightMultiplier + num1) * (125f / 16f);
        }
      }));
      return result;
    }

    private void GenerateMapTexture()
    {
      float[][] gridHeightData = this._heightData;
      int maxX = this._areaDimensions.Value.X;
      int maxY = this._areaDimensions.Value.Y;
      Configuration configuration = Configuration.Default.Clone();
      configuration.PreferContiguousImageBuffers = true;
      using (Image<Rgba32> image1 = new Image<Rgba32>(configuration, maxX, maxY))
      {
        if (ToggleNode.op_Implicit(this.Settings.Debug.DrawHeightMap))
        {
          float minHeight = ((IEnumerable<float[]>) gridHeightData).Min<float[]>((Func<float[], float>) (x => ((IEnumerable<float>) x).Min()));
          float maxHeight = ((IEnumerable<float[]>) gridHeightData).Max<float[]>((Func<float[], float>) (x => ((IEnumerable<float>) x).Max()));
          PixelRowOperation<Point> pixelRowOperation;
          // ISSUE: method pointer
          ProcessingExtensions.Mutate<Rgba32>(image, configuration, (Action<IImageProcessingContext>) (c => PixelRowDelegateExtensions.ProcessPixelRowsAsVector4(c, pixelRowOperation ?? (pixelRowOperation = new PixelRowOperation<Point>((object) this, __methodptr(\u003CGenerateMapTexture\u003Eb__3))))));
        }
        else
        {
          Vector4 unwalkableMask = Vector4.UnitX + Vector4.UnitW;
          Vector4 walkableMask = Vector4.UnitY + Vector4.UnitW;
          if (ToggleNode.op_Implicit(this.Settings.Debug.DisableHeightAdjust))
            Parallel.For(0, maxY, (Action<int>) (y =>
            {
              for (int index = 0; index < maxX; ++index)
              {
                int num = this._processedTerrainData[y][index];
                image[index, y] = new Rgba32(num == 0 ? unwalkableMask : walkableMask);
              }
            }));
          else
            Parallel.For(0, maxY, (Action<int>) (y =>
            {
              for (int index = 0; index < maxX; ++index)
              {
                int num1 = (int) ((double) gridHeightData[y][index / 2 * 2] / 10.8695650100708 / 2.0);
                int num2 = index + num1;
                int num3 = y + num1;
                int num4 = this._processedTerrainData[y][index];
                if (num2 >= 0 && num2 < maxX && num3 >= 0 && num3 < maxY)
                  image[num2, num3] = new Rgba32(num4 == 0 ? unwalkableMask : walkableMask);
              }
            }));
          if (!ToggleNode.op_Implicit(this.Settings.Debug.SkipNeighborFill))
            Parallel.For(0, maxY, (Action<int>) (y =>
            {
              for (int index1 = 0; index1 < maxX; ++index1)
              {
                Rgba32 rgba32_1 = image[index1, y];
                if (((Rgba32) ref rgba32_1).ToVector4() == Vector4.Zero)
                {
                  int num5 = 0;
                  int num6 = 0;
                  for (int index2 = -1; index2 < 2; ++index2)
                  {
                    for (int index3 = -1; index3 < 2; ++index3)
                    {
                      int num7 = index1 + index2;
                      int num8 = y + index3;
                      if (num7 >= 0 && num7 < maxX && num8 >= 0 && num8 < maxY)
                      {
                        Rgba32 rgba32_2 = image[index1 + index2, y + index3];
                        Vector4 vector4 = ((Rgba32) ref rgba32_2).ToVector4();
                        if (vector4 == walkableMask)
                          ++num5;
                        else if (vector4 == unwalkableMask)
                          ++num6;
                      }
                    }
                  }
                  image[index1, y] = new Rgba32(num5 > num6 ? walkableMask : unwalkableMask);
                }
              }
            }));
          if (!ToggleNode.op_Implicit(this.Settings.Debug.SkipEdgeDetector))
            new EdgeDetectorProcessor(new EdgeDetectorKernel(new DenseMatrix<float>(new float[5, 5]
            {
              {
                -1f,
                -1f,
                -1f,
                -1f,
                -1f
              },
              {
                -1f,
                -1f,
                -1f,
                -1f,
                -1f
              },
              {
                -1f,
                -1f,
                24f,
                -1f,
                -1f
              },
              {
                -1f,
                -1f,
                -1f,
                -1f,
                -1f
              },
              {
                -1f,
                -1f,
                -1f,
                -1f,
                -1f
              }
            })), false).CreatePixelSpecificProcessor<Rgba32>(configuration, image, ImageInfoExtensions.Bounds((IImageInfo) image)).Execute();
          if (!ToggleNode.op_Implicit(this.Settings.Debug.SkipRecoloring))
          {
            PixelRowOperation<Point> pixelRowOperation;
            // ISSUE: method pointer
            ProcessingExtensions.Mutate<Rgba32>(image, configuration, (Action<IImageProcessingContext>) (c => PixelRowDelegateExtensions.ProcessPixelRowsAsVector4(c, pixelRowOperation ?? (pixelRowOperation = new PixelRowOperation<Point>((object) this, __methodptr(\u003CGenerateMapTexture\u003Eb__8))))));
          }
        }
        using (Image<Rgba32> image2 = image1.Clone(configuration))
          this.Graphics.LowLevel.AddOrUpdateTexture("radar_minimap", image2);
      }
    }

    private int[][] ParseTerrainPathData()
    {
      byte[] mapTextureData = this._rawTerrainData;
      int bytesPerRow = this._terrainMetadata.BytesPerRow;
      int toExclusive = mapTextureData.Length / bytesPerRow;
      int[][] processedTerrainData = new int[toExclusive][];
      int xSize = bytesPerRow * 2;
      this._areaDimensions = new Vector2i?(new Vector2i(xSize, toExclusive));
      for (int index = 0; index < toExclusive; ++index)
        processedTerrainData[index] = new int[xSize];
      Parallel.For(0, toExclusive, (Action<int>) (y =>
      {
        for (int index1 = 0; index1 < xSize; index1 += 2)
        {
          byte num1 = mapTextureData[y * bytesPerRow + index1 / 2];
          for (int index2 = 0; index2 < 2; ++index2)
          {
            int num2 = (int) num1 >> 4 * index2 & 15;
            processedTerrainData[y][index1 + index2] = num2;
          }
        }
      }));
      return processedTerrainData;
    }

    private void LoadTargets() => this._targetDescriptions = JsonConvert.DeserializeObject<ConcurrentDictionary<string, List<TargetDescription>>>(File.ReadAllText(Path.Combine(this.DirectoryFullName, "targets.json")));

    private void RestartPathFinding()
    {
      this.StopPathFinding();
      this.StartPathFinding();
    }

    private void StartPathFinding()
    {
      if (!ToggleNode.op_Implicit(this.Settings.PathfindingSettings.ShowPathsToTargetsOnMap))
        return;
      this.FindPaths((IReadOnlyDictionary<string, TargetLocations>) this._clusteredTargetLocations, this._routes, this._findPathsCts.Token);
    }

    private void StopPathFinding()
    {
      this._findPathsCts.Cancel();
      this._findPathsCts = new CancellationTokenSource();
      this._routes = new ConcurrentDictionary<Vector2, RouteDescription>();
    }

    private void FindPaths(
      IReadOnlyDictionary<string, TargetLocations> tiles,
      ConcurrentDictionary<Vector2, RouteDescription> routes,
      CancellationToken cancellationToken)
    {
      List<Vector2> list = tiles.SelectMany<KeyValuePair<string, TargetLocations>, Vector2>((Func<KeyValuePair<string, TargetLocations>, IEnumerable<Vector2>>) (x => (IEnumerable<Vector2>) x.Value.Locations)).Distinct<Vector2>().ToList<Vector2>();
      PathFinder pf = new PathFinder(this._processedTerrainData, new int[5]
      {
        1,
        2,
        3,
        4,
        5
      });
      foreach ((Vector2 vector2, Color color) in ((IEnumerable<Vector2>) list).Take<Vector2>(RangeNode<int>.op_Implicit(this.Settings.MaximumPathCount)).Zip<Vector2, Color>(Enumerable.Repeat<List<Color>>(Radar.Radar.RainbowColors, 100).SelectMany<List<Color>, Color>((Func<List<Color>, IEnumerable<Color>>) (x => (IEnumerable<Color>) x))))
        Task.Run((Func<Task>) (() => this.FindPath(pf, vector2, color, routes, cancellationToken)));
    }

    private async Task WaitUntilPluginEnabled(CancellationToken cancellationToken)
    {
      while (!ToggleNode.op_Implicit(this.Settings.Enable))
        await Task.Delay(TimeSpan.FromSeconds(1.0), cancellationToken);
    }

    private async Task FindPath(
      PathFinder pf,
      Vector2 point,
      Color color,
      ConcurrentDictionary<Vector2, RouteDescription> routes,
      CancellationToken cancellationToken)
    {
      Vector2 playerPosition = this.GetPlayerPosition();
      IEnumerable<List<Vector2i>> pathI = pf.RunFirstScan(new Vector2i((int) playerPosition.X, (int) playerPosition.Y), new Vector2i((int) point.X, (int) point.Y));
      foreach (List<Vector2i> vector2iList in pathI)
      {
        List<Vector2i> elem = vector2iList;
        await this.WaitUntilPluginEnabled(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
          pathI = (IEnumerable<List<Vector2i>>) null;
          return;
        }
        if (((IEnumerable<Vector2i>) elem).Any<Vector2i>())
        {
          RouteDescription rd = new RouteDescription()
          {
            Path = elem,
            MapColor = new Func<Color>(GetMapColor),
            WorldColor = new Func<Color>(GetWorldColor)
          };
          routes.AddOrUpdate(point, rd, (Func<Vector2, RouteDescription, RouteDescription>) ((_1, _2) => rd));
        }
        elem = (List<Vector2i>) null;
      }
      while (true)
      {
        await this.WaitUntilPluginEnabled(cancellationToken);
        Vector2 newPosition = this.GetPlayerPosition();
        if (Vector2.op_Equality(playerPosition, newPosition))
          await Task.Delay(100, cancellationToken);
        else if (!cancellationToken.IsCancellationRequested)
        {
          playerPosition = newPosition;
          List<Vector2i> path = pf.FindPath(new Vector2i((int) playerPosition.X, (int) playerPosition.Y), new Vector2i((int) point.X, (int) point.Y));
          if (path != null)
          {
            RouteDescription rd = new RouteDescription()
            {
              Path = path,
              MapColor = new Func<Color>(GetMapColor),
              WorldColor = new Func<Color>(GetWorldColor)
            };
            routes.AddOrUpdate(point, rd, (Func<Vector2, RouteDescription, RouteDescription>) ((_3, _4) => rd));
          }
          newPosition = new Vector2();
          path = (List<Vector2i>) null;
        }
        else
          break;
      }
      pathI = (IEnumerable<List<Vector2i>>) null;

      Color GetWorldColor() => !ToggleNode.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.UseRainbowColorsForPaths) ? ColorNode.op_Implicit(this.Settings.PathfindingSettings.WorldPathSettings.DefaultPathColor) : color;

      Color GetMapColor() => !ToggleNode.op_Implicit(this.Settings.PathfindingSettings.UseRainbowColorsForMapPaths) ? ColorNode.op_Implicit(this.Settings.PathfindingSettings.DefaultMapPathColor) : color;
    }

    private ConcurrentDictionary<string, List<Vector2i>> GetTargets() => new ConcurrentDictionary<string, List<Vector2i>>((IEnumerable<KeyValuePair<string, List<Vector2i>>>) ((IEnumerable<IGrouping<string, List<Vector2i>>>) ((IEnumerable<KeyValuePair<string, List<Vector2i>>>) this.GetTileTargets()).Concat<KeyValuePair<string, List<Vector2i>>>((IEnumerable<KeyValuePair<string, List<Vector2i>>>) this.GetEntityTargets()).ToLookup<KeyValuePair<string, List<Vector2i>>, string, List<Vector2i>>((Func<KeyValuePair<string, List<Vector2i>>, string>) (x => x.Key), (Func<KeyValuePair<string, List<Vector2i>>, List<Vector2i>>) (x => x.Value))).ToDictionary<IGrouping<string, List<Vector2i>>, string, List<Vector2i>>((Func<IGrouping<string, List<Vector2i>>, string>) (x => x.Key), (Func<IGrouping<string, List<Vector2i>>, List<Vector2i>>) (x => ((IEnumerable<List<Vector2i>>) x).SelectMany<List<Vector2i>, Vector2i>((Func<List<Vector2i>, IEnumerable<Vector2i>>) (v => (IEnumerable<Vector2i>) v)).ToList<Vector2i>())));

    private Dictionary<string, List<Vector2i>> GetEntityTargets() => ((IEnumerable<IGrouping<string, Vector2i>>) ((IEnumerable<Entity>) this.GameController.Entities).Where<Entity>((Func<Entity, bool>) (x => x.HasComponent<Positioned>())).Where<Entity>((Func<Entity, bool>) (x => this._currentZoneTargetEntityPaths.Contains(x.Path))).ToLookup<Entity, string, Vector2i>((Func<Entity, string>) (x => x.Path), (Func<Entity, Vector2i>) (x => x.GetComponent<Positioned>().GridPos.Truncate()))).ToDictionary<IGrouping<string, Vector2i>, string, List<Vector2i>>((Func<IGrouping<string, Vector2i>, string>) (x => x.Key), (Func<IGrouping<string, Vector2i>, List<Vector2i>>) (x => ((IEnumerable<Vector2i>) x).ToList<Vector2i>()));

    private Dictionary<string, List<Vector2i>> GetTileTargets()
    {
      TileStructure[] tileData = this.GameController.Memory.ReadStdVector<TileStructure>(Radar.Radar.Cast(this._terrainMetadata.TgtArray));
      ConcurrentDictionary<string, ConcurrentQueue<Vector2i>> ret = new ConcurrentDictionary<string, ConcurrentQueue<Vector2i>>();
      Parallel.For(0, tileData.Length, (Action<int>) (tileNumber =>
      {
        string key = MiscHelpers.ToString(this.GameController.Memory.Read<TgtDetailStruct>(this.GameController.Memory.Read<TgtTileStruct>(tileData[tileNumber].TgtFilePtr).TgtDetailPtr).name, this.GameController.Memory);
        if (string.IsNullOrEmpty(key))
          return;
        Vector2i vector2i;
        // ISSUE: explicit constructor call
        ((Vector2i) ref vector2i).\u002Ector(tileNumber % (int) this._terrainMetadata.NumCols * 23, tileNumber / (int) this._terrainMetadata.NumCols * 23);
        ret.GetOrAdd(key, (Func<string, ConcurrentQueue<Vector2i>>) (_ => new ConcurrentQueue<Vector2i>())).Enqueue(vector2i);
      }));
      return ((IEnumerable<KeyValuePair<string, ConcurrentQueue<Vector2i>>>) ret).ToDictionary<KeyValuePair<string, ConcurrentQueue<Vector2i>>, string, List<Vector2i>>((Func<KeyValuePair<string, ConcurrentQueue<Vector2i>>, string>) (k => k.Key), (Func<KeyValuePair<string, ConcurrentQueue<Vector2i>>, List<Vector2i>>) (k => ((IEnumerable<Vector2i>) k.Value).ToList<Vector2i>()));
    }

    private bool IsDescriptionInArea(string descriptionAreaPattern) => this.GameController.Area.CurrentArea.Area.RawName.Like(descriptionAreaPattern);

    private IEnumerable<TargetDescription> GetTargetDescriptionsInArea() => this._targetDescriptions.Where<KeyValuePair<string, List<TargetDescription>>>((Func<KeyValuePair<string, List<TargetDescription>>, bool>) (x => this.IsDescriptionInArea(x.Key))).SelectMany<KeyValuePair<string, List<TargetDescription>>, TargetDescription>((Func<KeyValuePair<string, List<TargetDescription>>, IEnumerable<TargetDescription>>) (x => (IEnumerable<TargetDescription>) x.Value));

    private ConcurrentDictionary<string, TargetLocations> ClusterTargets()
    {
      ConcurrentDictionary<string, TargetLocations> tileMap = new ConcurrentDictionary<string, TargetLocations>();
      Dictionary<string, TargetDescription>.ValueCollection values = this._targetDescriptionsInArea.Values;
      ParallelOptions parallelOptions = new ParallelOptions();
      parallelOptions.MaxDegreeOfParallelism = 1;
      Action<TargetDescription> body = (Action<TargetDescription>) (target =>
      {
        TargetLocations targetLocations = this.ClusterTarget(target);
        if (targetLocations == null)
          return;
        tileMap[target.Name] = targetLocations;
      });
      Parallel.ForEach<TargetDescription>((IEnumerable<TargetDescription>) values, parallelOptions, body);
      return tileMap;
    }

    private TargetLocations ClusterTarget(TargetDescription target)
    {
      List<Vector2i> vector2iList;
      if (!this._allTargetLocations.TryGetValue(target.Name, out vector2iList))
        return (TargetLocations) null;
      int[] second = KMeans.Cluster(((IEnumerable<Vector2i>) vector2iList).Select<Vector2i, Vector2d>((Func<Vector2i, Vector2d>) (x => new Vector2d((double) x.X, (double) x.Y))).ToArray<Vector2d>(), target.ExpectedCount);
      List<Vector2> source1 = new List<Vector2>();
      foreach (IGrouping<int, (Vector2i, int)> source2 in ((IEnumerable<Vector2i>) vector2iList).Zip<Vector2i, int>((IEnumerable<int>) second).GroupBy<(Vector2i, int), int>((Func<(Vector2i, int), int>) (x => x.Second)))
      {
        Vector2 v = new Vector2();
        int num1 = 0;
        foreach ((Vector2i tile, int _) in (IEnumerable<(Vector2i, int)>) source2)
        {
          int num2 = this.IsGridWalkable(tile) ? 100 : 1;
          v = Vector2.op_Addition(v, Vector2.op_Multiply((float) num2, ((Vector2i) ref tile).ToVector2()));
          num1 += num2;
        }
        v = Vector2.op_Division(v, (float) num1);
        Vector2i? nullable = ((IEnumerable<Vector2i>) ((IEnumerable<(Vector2i, int)>) source2).Select<(Vector2i, int), Vector2i>((Func<(Vector2i, int), Vector2i>) (tile => new Vector2i(tile.First.X, tile.First.Y))).Where<Vector2i>(new Func<Vector2i, bool>(this.IsGridWalkable)).OrderBy<Vector2i, float>((Func<Vector2i, float>) (x =>
        {
          Vector2 vector2 = Vector2.op_Subtraction(((Vector2i) ref x).ToVector2(), v);
          return ((Vector2) ref vector2).LengthSquared();
        }))).Select<Vector2i, Vector2i?>((Func<Vector2i, Vector2i?>) (x => new Vector2i?(x))).FirstOrDefault<Vector2i?>();
        if (nullable.HasValue)
        {
          Vector2i vector2i = nullable.Value;
          v = ((Vector2i) ref vector2i).ToVector2();
        }
        if (!this.IsGridWalkable(v.Truncate()))
        {
          Vector2i vector2i = this.GetAllNeighborTiles(v.Truncate()).First<Vector2i>(new Func<Vector2i, bool>(this.IsGridWalkable));
          v = ((Vector2i) ref vector2i).ToVector2();
        }
        source1.Add(v);
      }
      return new TargetLocations()
      {
        Locations = ((IEnumerable<Vector2>) source1).Distinct<Vector2>().ToArray<Vector2>(),
        DisplayName = target.DisplayName
      };
    }

    private bool IsGridWalkable(Vector2i tile)
    {
      int num = this._processedTerrainData[tile.Y][tile.X];
      return (num == 5 ? 0 : (num != 4 ? 1 : 0)) == 0;
    }

    private IEnumerable<Vector2i> GetAllNeighborTiles(Vector2i start)
    {
      foreach (int range in Enumerable.Range(1, 100000))
      {
        Vector2i vector2i;
        int xStart = Math.Max(0, vector2i.X - range);
        int yStart = Math.Max(0, vector2i.Y - range);
        int xEnd = Math.Min(this._areaDimensions.Value.X, vector2i.X + range);
        int yEnd = Math.Min(this._areaDimensions.Value.Y, vector2i.Y + range);
        for (int x = xStart; x <= xEnd; ++x)
        {
          yield return new Vector2i(x, yStart);
          yield return new Vector2i(x, yEnd);
        }
        for (int y = yStart + 1; y <= yEnd - 1; ++y)
        {
          yield return new Vector2i(xStart, y);
          yield return new Vector2i(xEnd, y);
        }
        if (xStart == 0 && yStart == 0 && xEnd == this._areaDimensions.Value.X && yEnd == this._areaDimensions.Value.Y)
          break;
      }
    }

    static Radar()
    {
      List<Color> colorList = new List<Color>();
      colorList.Add(Color.Red);
      colorList.Add(Color.Green);
      colorList.Add(Color.Blue);
      colorList.Add(Color.Yellow);
      colorList.Add(Color.Violet);
      colorList.Add(Color.Orange);
      colorList.Add(Color.White);
      colorList.Add(Color.LightBlue);
      colorList.Add(Color.Indigo);
      Radar.Radar.RainbowColors = colorList;
    }
  }
}
