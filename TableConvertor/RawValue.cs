using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TableConvertor;

public record class RawValue  {
    //public virtual Value Clone() {
    //    return new Value();
    //}
    public virtual int Count { get { return 0; } }
    public virtual void Truncate(int end) { }

    public virtual JsonNode ToJson() {
        return null;
    }

    public override string ToString() {
        var s = JsonSerializer.Serialize(ToJson());
        return s;
    }

    public virtual RawValue Get(int i) {
        throw new NotImplementedException();
    }

    public virtual string Lit() {
        throw new NotImplementedException();
    }

}

public record class LiteralRawValue : RawValue {
    public string lit;

    public LiteralRawValue(string lit) {
        this.lit = lit;
    }
    //public override Value Clone() {
    //    return new LiteralValue(lit);
    //}

    public override string Lit() {
        return lit;
    }

    public override JsonNode ToJson() {
        return JsonValue.Create(lit);
    }


}

public record class ListRawValue : RawValue {
    public List<RawValue> list = new();
    public override int Count => list.Count;
    public ListRawValue() { }
    public ListRawValue(List<RawValue> list) {
        this.list = list;
    }
    public void Add(RawValue v) {
        list.Add(v);
    }
    public override RawValue Get(int i) {
        return list[i];
    }
    //public override Value Clone() {
    //    var ls = new List<Value>();
    //    foreach (Value v in list) {
    //        ls.Add(v.Clone());
    //    }
    //    return new ListValue(ls);
    //}
    public override void Truncate(int end) {
        list.RemoveRange(end, list.Count);
    }


    public override JsonNode ToJson() {
        var arr = new JsonNode[list.Count];
        var i = 0;
        foreach (var ch in list) {
            var v = ch.ToJson();
            arr[i] = v;
            i++;
        }
        var j = new JsonArray(arr);
        return j;
    }


}

public class ItemEqList<T> : List<T>, IEquatable<ItemEqList<T>>
    where T : IEquatable<T> {

    public ItemEqList() : base() { }
    public ItemEqList(IEnumerable<T> collection) : base(collection) { }
    public ItemEqList(int capacity) : base(capacity) { }

    public override bool Equals(object? obj) {
        return base.Equals(obj);
    }

    public bool Equals(ItemEqList<T> other) {
        for (int i = 0; i < this.Count; i++) {
            if (!this[i].Equals(other[i])) {
                return false;
            }
        }
        return true;
    }
    public override int GetHashCode() {
        var res = 0;
        foreach(var ch in  this) {
            res ^= ch.GetHashCode();
        }
        return res;
    }
}