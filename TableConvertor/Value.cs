using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TableConvertor;

public record class Value {
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
}

public record class LiteralValue : Value {
    string lit;

    public LiteralValue(string lit) {
        this.lit = lit;
    }
    //public override Value Clone() {
    //    return new LiteralValue(lit);
    //}


    public override JsonNode ToJson() {
        return JsonValue.Create(lit);
    }


}

public record class ListValue : Value {
    public ItemEqList<Value> list;
    public override int Count => list.Count;
    public ListValue(ItemEqList<Value> list) {
        this.list = list;
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

public record class MapValue : Value {
    public ItemEqList<(Value, Value)> map;
    public override int Count => map.Count;
    public MapValue(ItemEqList<(Value, Value)> map) {
        this.map = map;
    }
    //public override Value Clone() {
    //    var ls = new List<(Value, Value)> ();
    //    foreach (var (k, v) in map) {
    //        ls.Add((k.Clone(), v.Clone()));
    //    }
    //    return new MapValue(ls);
    //}
    public override void Truncate(int end) {
        map.RemoveRange(end, map.Count);
    }
}


public class CellData {
    public enum Kind {
        // 注释 或 空白
        Ignore,
        End,
        Value,
    }

    public Kind k;
    public Value? val;

    public CellData(Kind k, Value? val = null) {
        this.k = k;
        this.val = val;
    }

    public static CellData FromString(string s) {
        CellData res;
        switch (s) {
            case "":
                res = new CellData(Kind.Ignore); break;
            case "$end":
                res = new CellData(Kind.End); break;
            default:
                res = new CellData(Kind.Value, new LiteralValue(s));
                break;
        }
        return res;
    }

    public override string ToString() {
        return $"{k} {val}";
    }
}



public class ItemEqList<T> : List<T>, IEquatable<ItemEqList<T>>
    where T : IEquatable<T> {

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