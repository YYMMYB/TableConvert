using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json.Nodes;

namespace TableConvertor;
public class Layout {
    public string typeFullName;

    public Type type => Global.I.GetItem<Type>(typeFullName);

    public virtual JsonNode ToJson(RawValue rawValue) {
        throw new Exception();
    }
}

public class LitLayout : Layout {
    public override JsonNode ToJson(RawValue rawValue) {
        return type.Parse((rawValue as LiteralRawValue).lit);
    }
}

public class ObjLayout : Layout {
    public record struct Field {
        public int index;
        public Layout layout;
    }
    public Dictionary<string, Field> fields = new();
    public Dictionary<string, ObjLayout> deriveds = new();


    public override JsonNode ToJson(RawValue rawValue) {
        var rawV = (rawValue as ListRawValue)!;
        var res = new JsonObject();
        var ty = type as ObjectType;
        foreach (var (fnm, fty) in ty.fields) {
            var fnode = children[fnm].ToJson(rawV.list[fieldsIndex[fnm]]);
            res.Add(fnm, fnode);
        }
        return res;
    }
}

public class MapLayout : Layout {
    public bool isRefKey;
    public List<string> refKeyIndex = new();

    public override JsonNode ToJson(RawValue rawValue) {
        var rawV = (rawValue as ListRawValue)!;
        var res = new JsonObject();
        var ty = type as ObjectType;
        foreach (var (fnm, fty) in ty.fields) {
            var fnode = children[fnm].ToJson(rawV.list[fieldsIndex[fnm]]);
            res.Add(fnm, fnode);
        }
        return res;
    }
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