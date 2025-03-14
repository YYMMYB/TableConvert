using System.Runtime.InteropServices;

namespace TableConvertor;

public class Type {
    public string Name { get; set; }
    public List<Constraint> Constraint { get; set; }

    public virtual bool Parse(string s, out Value? v) {
        v = null;
        return false;
    }
}

public class StringType : Type {
}

public class FloatType : Type {
}

public class BoolType : Type {
}

public class IntType : Type {
}


public class ListType : Type {
    public Type? ValueType { get; set; }
}

public class DictType : Type {
    public Type? KeyType { get; set; }
    public Type? ValueType { get; set; }
}

public class ObjectType : Type {
    public Dictionary<string, Type> Fields { get; set; } = new();
    public Type? BaseType { get; set; }
}

public class EnumType : Type {
    public List<string> Values { get; set; } = new();
}

public class Constraint {

}

// todo 优化
public class Value {
    public Type? type;
    public object value;
}

