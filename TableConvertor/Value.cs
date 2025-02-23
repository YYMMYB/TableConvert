using System.Collections.Generic;

namespace TableConvertor;

public class Value
{
    public virtual Value Clone() {
        return new Value();
    }
    public virtual int Count { get { return 0; } }
    public virtual void Truncate(int end) { }
}

public class LiteralValue : Value
{
    string lit;

    public LiteralValue(string lit)
    {
        this.lit = lit;
    }
    public override Value Clone() {
        return new LiteralValue(lit);
    }
}

public class ListValue : Value
{
    public List<Value> list;
    public override int Count => list.Count;
    public ListValue(List<Value> list)
    {
        this.list = list;
    }
    public override Value Clone() {
        var ls = new List<Value>();
        foreach (Value v in list) {
            ls.Add(v.Clone());
        }
        return new ListValue(ls);
    }
    public override void Truncate(int end) {
        list.RemoveRange(end, list.Count);
    }
}

public class MapValue : Value
{
    public List<(Value, Value)> map;
    public override int Count => map.Count;
    public MapValue(List<(Value, Value)> map)
    {
        this.map = map;
    }
    public override Value Clone() {
        var ls = new List<(Value, Value)> ();
        foreach (var (k, v) in map) {
            ls.Add((k.Clone(), v.Clone()));
        }
        return new MapValue(ls);
    }
    public override void Truncate(int end) {
        map.RemoveRange(end, map.Count);
    }
}


public class CellData
{
    public enum Kind
    {
        // 注释 或 空白
        Ignore,
        End,
        Value,
    }

    public Kind k;
    public Value? val;

    public CellData(Kind k, Value? val = null)
    {
        this.k = k;
        this.val = val;
    }

    public static CellData FromString(string s)
    {
        CellData res;
        switch (s)
        {
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
}



