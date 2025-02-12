namespace TableConvertor;

public class Value
{
}

public class LiteralValue : Value
{
    string lit;

    public LiteralValue(string lit)
    {
        this.lit = lit;
    }
}

public class ListValue : Value
{
    List<Value> list;
    public ListValue(List<Value> list)
    {
        this.list = list;
    }
}

public class MapValue : Value
{
    List<(Value, Value)> map;
    public MapValue(List<(Value, Value)> map)
    {
        this.map = map;
    }
}


public class CellData
{
    public enum Kind
    {
        // 注释 或 空白
        Ignore,
        End,
        Default,
        Placeholder,
        Value,
    }

    public Kind K;
    public Value? Val;

    public CellData(Kind k, Value? val = null)
    {
        K = k;
        Val = val;
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
            case "$default":
                res = new CellData(Kind.Default); break;
            default:
                res = new CellData(Kind.Value, new LiteralValue(s));
                break;
        }
        return res;
    }
}



