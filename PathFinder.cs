// Decompiled with JetBrains decompiler
// Type: Radar.PathFinder
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using GameOffsets.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


#nullable enable
namespace Radar
{
  public class PathFinder
  {
    private readonly 
    #nullable disable
    bool[][] _grid;
    private readonly ConcurrentDictionary<Vector2i, Dictionary<Vector2i, float>> ExactDistanceField = new ConcurrentDictionary<Vector2i, Dictionary<Vector2i, float>>();
    private readonly int _dimension2;
    private readonly int _dimension1;
    private static readonly IReadOnlyList<Vector2i> NeighborOffsets;

    public PathFinder(int[][] grid, int[] pathableValues)
    {
      HashSet<int> pv = ((IEnumerable<int>) pathableValues).ToHashSet<int>();
      this._grid = ((IEnumerable<int[]>) grid).Select<int[], bool[]>((Func<int[], bool[]>) (x => ((IEnumerable<int>) x).Select<int, bool>((Func<int, bool>) (y => pv.Contains(y))).ToArray<bool>())).ToArray<bool[]>();
      this._dimension1 = this._grid.Length;
      this._dimension2 = this._grid[0].Length;
    }

    private bool IsTilePathable(Vector2i tile) => tile.X >= 0 && tile.X < this._dimension2 && tile.Y >= 0 && tile.Y < this._dimension1 && this._grid[tile.Y][tile.X];

    private static IEnumerable<Vector2i> GetNeighbors(Vector2i tile) => ((IEnumerable<Vector2i>) PathFinder.NeighborOffsets).Select<Vector2i, Vector2i>((Func<Vector2i, Vector2i>) (offset => Vector2i.op_Addition(tile, offset)));

    private static float GetExactDistance(Vector2i tile, Dictionary<Vector2i, float> dict) => ((IReadOnlyDictionary<Vector2i, float>) dict).GetValueOrDefault<Vector2i, float>(tile, float.PositiveInfinity);

    public IEnumerable<List<Vector2i>> RunFirstScan(Vector2i start, Vector2i target)
    {
      Vector2i key;
      this.ExactDistanceField.TryAdd(key, new Dictionary<Vector2i, float>());
      Dictionary<Vector2i, float> exactDistanceField = this.ExactDistanceField[key];
      exactDistanceField[key] = 0.0f;
      Dictionary<Vector2i, Vector2i> localBacktrackDictionary = new Dictionary<Vector2i, Vector2i>();
      BinaryHeap<float, Vector2i> queue = new BinaryHeap<float, Vector2i>();
      queue.Add(0.0f, key);
      Stopwatch sw = Stopwatch.StartNew();
      localBacktrackDictionary.Add(key, key);
      List<Vector2i> reversePath = new List<Vector2i>();
      KeyValuePair<float, Vector2i> top;
      while (queue.TryRemoveTop(out top))
      {
        Vector2i current = top.Value;
        float currentDistance = top.Key;
        Vector2i vector2i;
        if (reversePath.Count == 0 && ((Vector2i) ref current).Equals(vector2i))
        {
          reversePath.Add(current);
          Vector2i it;
          Vector2i previous;
          for (it = current; Vector2i.op_Inequality(it, key) && localBacktrackDictionary.TryGetValue(it, out previous); it = previous)
            reversePath.Add(previous);
          yield return reversePath;
          it = new Vector2i();
        }
        foreach (Vector2i neighbor1 in PathFinder.GetNeighbors(current))
        {
          Vector2i neighbor = neighbor1;
          TryEnqueueTile(neighbor, current, currentDistance);
          neighbor = new Vector2i();
        }
        if (sw.ElapsedMilliseconds > 100L)
        {
          yield return reversePath;
          sw.Restart();
        }
        current = new Vector2i();
      }

      void TryEnqueueTile(Vector2i coord, Vector2i previous, float previousScore)
      {
        if (!this.IsTilePathable(coord) || localBacktrackDictionary.ContainsKey(coord))
          return;
        localBacktrackDictionary.Add(coord, previous);
        float key = previousScore + ((Vector2i) ref coord).DistanceF(previous);
        exactDistanceField.TryAdd(coord, key);
        queue.Add(key, coord);
      }
    }

    public List<Vector2i> FindPath(Vector2i start, Vector2i target)
    {
      Dictionary<Vector2i, float> exactDistanceField = this.ExactDistanceField[target];
      if (float.IsPositiveInfinity(PathFinder.GetExactDistance(start, exactDistanceField)))
        return (List<Vector2i>) null;
      List<Vector2i> path = new List<Vector2i>();
      Vector2i vector2i;
      for (Vector2i tile = start; Vector2i.op_Inequality(tile, target); tile = vector2i)
      {
        vector2i = PathFinder.GetNeighbors(tile).MinBy<Vector2i, float>((Func<Vector2i, float>) (x => PathFinder.GetExactDistance(x, exactDistanceField)));
        Debug.Assert(!path.Contains(vector2i));
        path.Add(vector2i);
      }
      return path;
    }

    static PathFinder()
    {
      List<Vector2i> vector2iList = new List<Vector2i>();
      vector2iList.Add(new Vector2i(0, 1));
      vector2iList.Add(new Vector2i(1, 1));
      vector2iList.Add(new Vector2i(1, 0));
      vector2iList.Add(new Vector2i(1, -1));
      vector2iList.Add(new Vector2i(0, -1));
      vector2iList.Add(new Vector2i(-1, -1));
      vector2iList.Add(new Vector2i(-1, 0));
      vector2iList.Add(new Vector2i(-1, 1));
      PathFinder.NeighborOffsets = (IReadOnlyList<Vector2i>) vector2iList;
    }
  }
}
