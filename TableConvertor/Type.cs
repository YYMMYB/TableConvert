using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace TableConvertor;

public class Type : Item {
    public override string Name => thisname;
    public override string FullName => module.CulcFullName(Name);
    public override Module ParentMod { get => module; set => module = value; }

    public Module module;
    public string thisname;
    public List<Constraint> constraints = new();

    public Type(string name) {
        this.module = module;
        thisname = name;
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
    public string valueType;

    public ListType(string name, string valueType) : base(name) {
        this.valueType = valueType;
    }
}

public class MapType : Type {
    public string keyType;
    public string valueType;
    public MapType(string name, string keyType, string valueType) : base(name) {
        this.keyType = keyType;
        this.valueType = valueType;
    }
}

public class ObjectType : Type {
    public Dictionary<string, string> fields = new();
    public string? baseType;

    public ObjectType(string name, string? baseType) : base(name) {
        this.baseType = baseType;
    }

    public void AddField(string name, string type) {
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

