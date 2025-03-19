using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace TableConvertor;

public class Type : Item {
    public Module module;
    public string name;
    public List<Constraint> constraints = new();

    public Type(string name) {
        this.module = module;
        this.name = name;
    }

    public void AddConstraint(Constraint constraint) {
        constraints.Add(constraint);
    }

    public JsonNode Parse(string str) {
        throw new Exception();
    }
}

public class StringType : Type {
    public StringType(string name) : base(name) {
    }
}

public class FloatType : Type {
    public FloatType(string name) : base(name) {
    }
}

public class BoolType : Type {
    public BoolType(string name) : base(name) {
    }
}

public class IntType : Type {
    public IntType(string name) : base(name) {
    }
}


public class ListType : Type {
    public Type valueType;

    public ListType(string name, Type valueType) : base(name) {
        this.valueType = valueType;
    }


}

public class MapType : Type {
    public Type keyType;
    public Type valueType;
    public MapType(string name, Type keyType, Type valueType) : base(name) {
        this.keyType = keyType;
        this.valueType = valueType;
    }
}

public class ObjectType : Type {
    public Dictionary<string, Type> fields = new();
    public Type? baseType;

    public ObjectType(string name, Type? baseType) : base(name) {
        this.baseType = baseType;
    }

    public void AddField(string name, Type type) {
        fields.Add(name, type);
    }
}

public class EnumType : Type {
    public List<string> variants = new();

    public EnumType(string name) : base(name) {
    }

    public void AddVariant(string name) {
        variants.Add(name);
    }
}

public class Constraint {

}

// todo 优化
public class Value {
    public Type? type;
    public object value;
}

