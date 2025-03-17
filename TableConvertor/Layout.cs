using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json.Nodes;

namespace TableConvertor;
public class Layout
{
}

public class ObjLayout: Layout {
    public Dictionary<string, int> fieldsIndex = new();
}

public class MapLayout : Layout {
    public List<string> keyIndex = new();
}

public class ListLayout : Layout {
    public bool hasKey;
    public ListLayout(bool hasKey) {
        this.hasKey = hasKey;
    }
}

// 用于默认值之类的. 目前没有用到
public class Context {
    
}

public class JsonConverter {
    public virtual JsonNode ToJson(RawValue raw, Type type, Layout layout, Context context) {
        throw  new NotImplementedException();
    }
}

public class LitJsonConverter : JsonConverter{
    public override JsonNode ToJson(RawValue raw, Type type, Layout layout, Context context) {

    }
}