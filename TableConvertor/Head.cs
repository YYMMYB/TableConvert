using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TableConvertor;
public class Head {
    public Module mod;

    public Head? parent;
    public int[] colRange;
    public int[] rowRange;

    public string name;
    public string autoName;
    public string? typeName;
    public string? fullTypeName;

    public Dictionary<string, List<string>> attrs = new();

    public static Head Create(Module mod, Head? parent, RawHead raw) {
        Head head;

        if (raw.isVertical) {
            head = new ObjectHead();
            var s = StringUtil.TryVarient(raw.content);
            var s2 = StringUtil.SplitWhite(s!);
            head.name = s2[0];
            if (s2.Length > 1) {
                head.typeName = s2[1];
            } else {
                head.typeName = s2[0];
            }
        } else {
            string name, t, tN;
            SplitContent(raw, out name, out t, out tN);

            if (t == null) {
                if (raw.children.Count > 0) {
                    head = new ObjectHead();
                } else {
                    head = new SingleHead();
                }
            } else {
                if (IsList(t)) {
                    var head1 = new ListHead();
                    head1.type = StringUtil.TypeName(t);
                    head = head1;
                } else if (IsObject(t)) {
                    var head1 = new ObjectHead();
                    head = head1;
                } else if (IsSingle(t)) {
                    var head1 = new SingleHead();
                    head1.type = StringUtil.TypeName(t);
                    head = head1;
                } else if (StringUtil.Type_Ref.Contains(t)) {
                    var head1 = new RefHead();
                    head1.r = StringUtil.SplitItem(tN);
                    head = head1;
                    tN = null;
                } else {
                    throw new Exception();
                }
            }

            head.name = name;
            head.typeName = tN;
        }

        head.mod = mod;
        head.rowRange = raw.rowRange;
        head.colRange = raw.colRange;
        head.parent = parent;

        if (head is ListHead lh) {
            if (raw.children.Count == 1) {
                lh.valueHead = Create(mod, head, raw.children[0]);
                lh.valueHead.name = StringUtil.ValueFieldName;
            } else if (raw.children.Count == 2) {
                lh.keyHead = Create(mod, head, raw.children[0]);
                lh.keyHead.name = StringUtil.KeyFieldName;
                lh.valueHead = Create(mod, head, raw.children[1]);
                lh.valueHead.name = StringUtil.ValueFieldName;
            } else {
                throw new Exception();
            }
        } else if (head is ObjectHead oh) {
            if (raw.horizontalCount < raw.children.Count) {
                oh.switchCol = raw.children[raw.horizontalCount].StartCol;
            }
            for (int i = 0; i < raw.horizontalCount; i++) {
                oh.fields.Add(Create(mod, head, raw.children[i]));
            }
            for (int i = raw.horizontalCount; i < raw.children.Count; i++) {
                var n = Create(mod, head, raw.children[i]);
                n.colRange[0] += 1;
                oh.deriveds.Add(n.name, n);
            }
        }

        return head;
    }

    private static void SplitContent(RawHead raw, out string name, out string t, out string tN) {
        var s = StringUtil.SplitType(raw.content);
        name = s[0];
        t = null;
        tN = null;
        if (s.Length > 1) {
            var ps = StringUtil.SplitWhite(s[1], 2);
            t = ps[0];
            if (ps.Length > 1) {
                tN = ps[1];
            }
        }
    }

    public static bool IsList(string s) {
        return StringUtil.Type_List.Contains(s) || StringUtil.Type_Map.Contains(s);
    }
    public static bool IsObject(string s) {
        return StringUtil.Type_Object.Contains(s);
    }
    public static bool IsSingle(string s) {
        return StringUtil.Type_Enum.Contains(s)
            || StringUtil.Type_Bool.Contains(s)
            || StringUtil.Type_Int.Contains(s)
            || StringUtil.Type_Float.Contains(s)
            || StringUtil.Type_String.Contains(s)
            ;
    }


    public virtual JsonNode Read(RawValue raw) {
        throw new NotImplementedException();
    }

    public static void RemoveHelperField(JsonNode node) {
        if (node is JsonObject onode) {
            onode.Remove(StringUtil.FirstFieldName);
        }
    }

    public virtual Format CreateFormat() { throw new NotImplementedException(); }

    public virtual void CreateType(string? mid) {
        CalcFullTypeName(mid);
        if (Global.I.GetAbsItem<Type>(fullTypeName) != null) {
            // todo 检验
            return;
        }
        var typeMod = Global.I.GetOrCreateParentModules(fullTypeName);
        CreateType2(mid, typeMod);
    }

    protected virtual void CreateType2(string? mid, Module typeMod) { throw new NotImplementedException(); }

    public void CalcAutoName(string? mid) {
        string p = parent?.autoName ?? "";
        if (mid == null) {
            autoName = StringUtil.JoinIdent(p, name);
        } else {
            autoName = StringUtil.JoinIdent(p, mid, name);
        }
    }

    public virtual void CalcFullTypeName(string? mid) {
        CalcAutoName(mid);
        if (typeName != null && StringUtil.IsAbsItem(typeName)) {
            fullTypeName = typeName;
            typeName = StringUtil.ItemName(fullTypeName);
        } else {
            var tn = typeName;
            if (tn == null) {
                tn = autoName;
            }
            typeName = tn;
            fullTypeName = mod.CulcFullName(tn);
        }
    }

}

public class ListHead : Head {
    public Head? keyHead;
    public Head valueHead;
    public string type;

    public override JsonNode Read(RawValue raw) {
        var lraw = (raw as ListRawValue)!;

        var count = lraw.list.Count;

        JsonObject map = new JsonObject();
        JsonArray arr = new JsonArray();

        for (int i = 0; i < count; i++) {
            JsonNode v;
            JsonNode k = null;
            if (keyHead == null || keyHead is RefHead) {
                v = valueHead.Read(lraw.Get(i).Get(0));
            } else {
                k = keyHead.Read(lraw.Get(i).Get(0));
                v = valueHead.Read(lraw.Get(i).Get(1));
            }

            if (keyHead is RefHead rkeyHead) {
                JsonNode obj = v;
                foreach (var key in rkeyHead.r) {
                    obj = obj[key!]!;
                }
                k = obj;
            }

            if (type == "map") {
                RemoveHelperField(k!);
                RemoveHelperField(v);
                map.Add(ConvertToKey(k!), v);
            } else if (type == "list") {
                RemoveHelperField(v);
                arr.Add(v);
            }
        }

        if (type == "map") {
            return map;
        } else if (type == "list") {
            return arr;
        } else {
            throw new Exception();
        }
    }

    public string ConvertToKey(JsonNode node) {
        var s = node.ToJsonString(StringUtil.JsonOpt);
        return s;
    }

    public override Format CreateFormat() {
        HFormat h;
        if (keyHead == null || keyHead is RefHead) {
            h = new HFormat(valueHead.CreateFormat());
        } else {
            h = new HFormat(keyHead.CreateFormat(), valueHead.CreateFormat());
        }
        h.colRange = colRange;
        var res = new ListFormat(h);
        res.colRange = colRange;
        return res;
    }

    protected override void CreateType2(string? mid, Module typeMod) {
        // 必须先算value, 因为key可能会引用value的东西
        valueHead.CreateType(null);
        var vt = valueHead.fullTypeName;
        if (type == "map") {
            string kt;
            if (keyHead is RefHead rkh) {
                var k = rkh.Target(valueHead);
                kt = k.fullTypeName;
            } else {
                Debug.Assert(keyHead != null);
                keyHead.CreateType(null);
                kt = keyHead.fullTypeName;
            }
            var t = new MapType(typeName, kt, vt);
            typeMod.AddItem(t);
        } else if (type == "list") {
            var t = new ListType(typeName, vt);
            typeMod.AddItem(t);
        } else {
            throw new Exception();
        }
    }
}

public class ObjectHead : Head {
    public List<Head> fields = new();

    public Head? baseHead;
    public Dictionary<string, Head> deriveds = new();
    public int switchCol = -1;

    public Head? FieldByName(string name) {
        foreach (Head head in fields) {
            if (head.name == name) {
                return head;
            }
        }
        return null;
    }

    public override JsonNode Read(RawValue raw) {
        var lraw = (raw as ListRawValue)!;
        JsonObject node;
        if (deriveds.Count != 0) {
            var sw = (lraw.list.Last() as ListRawValue)!;
            string derivedName = (sw.list[0] as LiteralRawValue)!.lit;
            node = (deriveds[derivedName].Read(sw.list[1]) as JsonObject)!;
            if (node.ContainsKey(StringUtil.FirstFieldName)) {
                node[StringUtil.TypeFieldName] = derivedName;
                node.Remove(StringUtil.FirstFieldName);
            }
        } else {
            node = new JsonObject();
            node.Add(StringUtil.FirstFieldName, null);
        }

        for (int i = 0; i < fields.Count; i++) {
            var v = fields[i].Read(lraw.Get(i));
            RemoveHelperField(v);
            node.Add(fields[i].name, v);
        }
        return node;
    }

    public override Format CreateFormat() {
        var l = new List<Format>();
        foreach (var f in fields) {
            l.Add(f.CreateFormat());
        }
        if (deriveds.Count != 0) {
            var d = new Dictionary<string, Format>();
            foreach (var (k, h) in deriveds) {
                var f = h.CreateFormat();
                d.Add(k, f);
            }
            var sw = new SwitchFormat(d);
            sw.colRange = [switchCol, colRange[1]];
            l.Add(sw);
        }
        var res = new HFormat(l);
        res.colRange = colRange;
        return res;
    }

    protected override void CreateType2(string? mid, Module typeMod) {
        var ty = new ObjectType(typeName, null);
        foreach (var f in fields) {
            f.CreateType(null);
            ty.AddField(f.name, f.fullTypeName);
        }
        typeMod.AddItem(ty);

        foreach (var (dn, d) in deriveds) {
            d.CreateType(dn);
            var t = Global.I.GetAbsItem<ObjectType>(d.fullTypeName);
            if (t.baseType != null && t.baseType != fullTypeName) {
                throw new Exception();
            }
            t.baseType = fullTypeName;
        }
    }
}

public class SingleHead : Head {
    public string type;

    public override JsonNode Read(RawValue raw) {
        string lit = (raw as LiteralRawValue)!.lit;
        if (type == "string" || type == "enum") {
            return JsonValue.Create(lit);
        } else if (type == "float") {
            return JsonValue.Create(double.Parse(lit));
        } else if (type == "int") {
            return JsonValue.Create(int.Parse(lit));
        } else if (type == "bool") {
            return JsonValue.Create(bool.Parse(lit));
        } else if (type == null) {
            try {
                var j = JsonSerializer.Deserialize<JsonNode>(lit);
                return j;
            } catch {
                return JsonValue.Create(lit);
            }
        } else {
            throw new Exception();
        }
    }

    public override Format CreateFormat() {
        SingleFormat res = new();
        res.colRange = colRange;
        return res;
    }

    public override void CalcFullTypeName(string? mid) {
        // todo 约束判断 无约束用全局的, 有约束用特殊的
        // 目前都没有约束, 所以先一直用全局的
        fullTypeName = StringUtil.JoinItem(StringUtil.EngineModuleAbsPath, type);
        typeName = StringUtil.ItemName(fullTypeName);
    }
    protected override void CreateType2(string? mid, Module typeMod) {
        Type t;
        if (type == "string") {
            t = new StringType(typeName);
        } else if (type == "float") {
            t = new FloatType(typeName);
        } else if (type == "int") {
            t = new IntType(typeName);
        } else if (type == "bool") {
            t = new BoolType(typeName);
        } else if (type == "enum") {
            // todo enum
            t = new EnumType(typeName);
            //throw new Exception();
        } else if (type == null) {
            throw new Exception();
        } else {
            throw new Exception();
        }
        typeMod.AddItem(t);
    }
}

public class RefHead : Head {
    public string[] r;

    public Head Target(Head root) {
        foreach (var n in r) {
            root = (root as ObjectHead).FieldByName(n);
        }
        return root;
    }
}
