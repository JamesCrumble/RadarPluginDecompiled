// Decompiled with JetBrains decompiler
// Type: Radar.BinaryHeap`2
// Assembly: Radar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A3E1D317-687D-415B-BA8A-9F47E8A90E24
// Assembly location: C:\Users\namst\Desktop\Radar\Radar\Radar.dll

using System.Collections.Generic;

namespace Radar
{
  public class BinaryHeap<TKey, TValue>
  {
    private readonly List<KeyValuePair<TKey, TValue>> _storage = new List<KeyValuePair<TKey, TValue>>();

    private void SieveUp(int startIndex)
    {
      int i1 = startIndex;
      for (int i2 = (i1 - 1) / 2; i1 != i2 && this.Compare(i1, i2) < 0; i2 = (i1 - 1) / 2)
      {
        this.Swap(i1, i2);
        i1 = i2;
      }
    }

    private void SieveDown(int startIndex)
    {
      int i2_1;
      for (int i1 = startIndex; i1 * 2 + 1 < this._storage.Count; i1 = i2_1)
      {
        int num = i1 * 2 + 1;
        int i2_2 = i1 * 2 + 2;
        i2_1 = i2_2 >= this._storage.Count ? (this.Compare(i1, num) > 0 ? num : i1) : (this.Compare(i1, num) > 0 ? (this.Compare(i1, i2_2) > 0 ? (this.Compare(num, i2_2) > 0 ? i2_2 : num) : num) : (this.Compare(i1, i2_2) > 0 ? i2_2 : i1));
        if (i2_1 == i1)
          break;
        this.Swap(i1, i2_1);
      }
    }

    private int Compare(int i1, int i2)
    {
      Comparer<TKey> comparer = Comparer<TKey>.Default;
      KeyValuePair<TKey, TValue> keyValuePair = this._storage[i1];
      TKey key1 = keyValuePair.Key;
      keyValuePair = this._storage[i2];
      TKey key2 = keyValuePair.Key;
      return comparer.Compare(key1, key2);
    }

    private void Swap(int i1, int i2)
    {
      List<KeyValuePair<TKey, TValue>> storage1 = this._storage;
      int num = i1;
      List<KeyValuePair<TKey, TValue>> storage2 = this._storage;
      int index1 = i2;
      KeyValuePair<TKey, TValue> keyValuePair1 = this._storage[i2];
      KeyValuePair<TKey, TValue> keyValuePair2 = this._storage[i1];
      int index2 = num;
      KeyValuePair<TKey, TValue> keyValuePair3;
      KeyValuePair<TKey, TValue> keyValuePair4 = keyValuePair3 = keyValuePair1;
      storage1[index2] = keyValuePair3;
      storage2[index1] = keyValuePair4 = keyValuePair2;
    }

    public void Add(TKey key, TValue value)
    {
      this._storage.Add(new KeyValuePair<TKey, TValue>(key, value));
      this.SieveUp(this._storage.Count - 1);
    }

    public bool TryRemoveTop(out KeyValuePair<TKey, TValue> value)
    {
      if (this._storage.Count == 0)
      {
        value = new KeyValuePair<TKey, TValue>();
        return false;
      }
      value = this._storage[0];
      List<KeyValuePair<TKey, TValue>> storage1 = this._storage;
      List<KeyValuePair<TKey, TValue>> storage2 = this._storage;
      KeyValuePair<TKey, TValue> keyValuePair = storage2[storage2.Count - 1];
      storage1[0] = keyValuePair;
      this._storage.RemoveAt(this._storage.Count - 1);
      this.SieveDown(0);
      return true;
    }
  }
}
