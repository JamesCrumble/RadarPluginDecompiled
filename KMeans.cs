// Decompiled with JetBrains decompiler
// Type: Radar.KMeans
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Radar
{
  public static class KMeans
  {
    public static int[] Cluster(Vector2d[] rawData, int numClusters)
    {
      if (numClusters >= rawData.Length)
        return Enumerable.Range(0, rawData.Length).ToArray<int>();
      bool flag1 = true;
      bool flag2 = true;
      int[] clustering = KMeans.InitClustering(rawData, numClusters);
      Vector2d[] means = new Vector2d[numClusters];
      int num = rawData.Length * 10;
      for (int index = 0; flag1 & flag2 && index < num; ++index)
      {
        flag2 = KMeans.UpdateMeans(rawData, clustering, means);
        flag1 = KMeans.UpdateClustering(rawData, clustering, means);
      }
      return clustering;
    }

    private static int[] InitClustering(Vector2d[] data, int numClusters)
    {
      HashSet<int> selectedClusters = new HashSet<int>()
      {
        0
      };
      while (selectedClusters.Count < numClusters)
      {
        (Vector2d, int) tuple1 = ((IEnumerable<Vector2d>) data).Select<Vector2d, (Vector2d, int)>((Func<Vector2d, int, (Vector2d, int)>) ((tuple, index) => (tuple, index))).Where<(Vector2d, int)>((Func<(Vector2d, int), bool>) (x => !selectedClusters.Contains(x.index))).MaxBy<(Vector2d, int), double>((Func<(Vector2d, int), double>) (c => selectedClusters.Min<int>((Func<int, double>) (x => KMeans.Distance(c.tuple, data[x])))));
        selectedClusters.Add(tuple1.Item2);
      }
      Dictionary<int, int> clusterNumbers = selectedClusters.Select<int, (int, int)>((Func<int, int, (int, int)>) ((x, i) => (x, i))).ToDictionary<(int, int), int, int>((Func<(int, int), int>) (x => x.x), (Func<(int, int), int>) (x => x.i));
      return ((IEnumerable<Vector2d>) data).Select<Vector2d, int>((Func<Vector2d, int>) (x => clusterNumbers[selectedClusters.MinBy<int, double>((Func<int, double>) (y => KMeans.Distance(x, data[y])))])).ToArray<int>();
    }

    private static bool UpdateMeans(Vector2d[] data, int[] clustering, Vector2d[] means)
    {
      int length = means.Length;
      int[] numArray = new int[length];
      for (int index1 = 0; index1 < data.Length; ++index1)
      {
        int index2 = clustering[index1];
        ++numArray[index2];
      }
      for (int index = 0; index < length; ++index)
      {
        if (numArray[index] == 0)
          return false;
      }
      for (int index = 0; index < means.Length; ++index)
        means[index] = new Vector2d();
      for (int index = 0; index < data.Length; ++index)
        means[clustering[index]] += data[index];
      for (int index = 0; index < means.Length; ++index)
        means[index] /= (double) numArray[index];
      return true;
    }

    private static bool UpdateClustering(Vector2d[] data, int[] clustering, Vector2d[] means)
    {
      int length = means.Length;
      bool flag = false;
      int[] numArray1 = new int[clustering.Length];
      Array.Copy((Array) clustering, (Array) numArray1, clustering.Length);
      Dictionary<int, int> dictionary = ((IEnumerable<int>) numArray1).GroupBy<int, int>((Func<int, int>) (x => x)).ToDictionary<IGrouping<int, int>, int, int>((Func<IGrouping<int, int>, int>) (x => x.Key), (Func<IGrouping<int, int>, int>) (x => x.Count<int>()));
      double[] source = new double[length];
      for (int index1 = 0; index1 < data.Length; ++index1)
      {
        for (int index2 = 0; index2 < length; ++index2)
          source[index2] = KMeans.Distance(data[index1], means[index2]);
        int key = ((IEnumerable<double>) source).Select<double, (double, int)>((Func<double, int, (double, int)>) ((distance, index) => (distance, index))).MinBy<(double, int), double>((Func<(double, int), double>) (x => x.distance)).Item2;
        ref int local = ref numArray1[index1];
        if (key != local)
        {
          flag = true;
          if (dictionary[local] > 1)
          {
            dictionary[local]--;
            dictionary[key]++;
            local = key;
          }
        }
      }
      if (!flag)
        return false;
      int[] numArray2 = new int[length];
      for (int index3 = 0; index3 < data.Length; ++index3)
      {
        int index4 = numArray1[index3];
        ++numArray2[index4];
      }
      for (int index = 0; index < length; ++index)
      {
        if (numArray2[index] == 0)
          return false;
      }
      Array.Copy((Array) numArray1, (Array) clustering, numArray1.Length);
      return true;
    }

    private static double Distance(Vector2d v1, Vector2d v2) => (v1 - v2).Length;
  }
}
