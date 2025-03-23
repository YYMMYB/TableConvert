using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace TableConvertor;

public class Global {
    public static Global I { get; } = new();
    public Dictionary<string, Item> items = new();
    public Module root;
    static Global() {
        I = new();
        I.root = Module.CreateRootModule();
        I.AddItem(I.root.FullName, I.root);
    }

    public Module GetOrCreateParentModules(string absPath) {
        if (!StringUtil.IsAbsItem(absPath)) {
            throw new Exception();
        }
        var pathComp = StringUtil.SplitItem(absPath);
        var mod = Global.I.GetItem<Module>(StringUtil.RootPath(absPath));
        for (int i = 2; i < pathComp.Length - 1; i++) {
            var name = pathComp[i];
            if (mod.GetItem<Module>(name) == null) {
                var ch = new Module();
                ch.thisname = name;
                mod.AddItem(ch);
            }
            mod = mod.GetItem<Module>(name);
        }
        return mod;
    }

    public T? GetItem<T>(string path) where T : Item {
        if (!items.ContainsKey(path)) {
            return null;
        } else {
            return (T)items[path];
        }
    }

    // 只给Module的AddItem用, 别的不要用
    public void AddItem(string path, Item item) {
        items.Add(path, item);
    }

    public bool ExistItem(string path) {
        return items.ContainsKey(path);
    }
}


// Module
// Type
// Table
// 的父类, 用于统一存储, 防止重名
public abstract class Item {
    public abstract string Name { get; }
    public abstract string FullName { get; }
    public abstract Module? ParentMod { get; set; }
}


public static class StringUtil {
    public static JsonSerializerOptions JsonOpt = new JsonSerializerOptions() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Constraint = "builtin.constraint";
    public static Dictionary<string, string> attrs = new Dictionary<string, string>() {
        {"c", Constraint},
    };
    public static string BuiltinAttr(string token) {
        return attrs[token];
    }

    public static string CommentPrefix = "//";
    public static string AttrPrefix = "#";
    public static string VarientPrefix = "|";
    public static string ItemSplitor = ".";
    public static string IdentConnector = "_";
    public static string AutoInfer = "_";
    public static string KeywordPrefix = "$";

    public static string TypeSplitor = ":";



    public static bool IsEmptyString(string cell) {
        return cell == null || cell.Trim().Length == 0;
    }

    public static bool IsEmptyValueString(string cell) {
        return IsEmptyString(cell);
    }

    public static string? TryComment(string cell) {
        if (cell.Trim().StartsWith(CommentPrefix)) {
            return cell.Trim().Substring(2);
        }
        return null;
    }
    public static string? TryAttr(string cell) {
        if (cell.Trim().StartsWith(AttrPrefix)) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }
    public static string? TryVarient(string cell) {
        if (cell.Trim().StartsWith(VarientPrefix)) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }

    public static bool IsEmptyIdent(string ident) {
        return IsEmptyString(ident) || ident == AutoInfer;
    }


    public static string JoinItem(params string[] comp) {
        return string.Join(ItemSplitor, comp);
    }
    public static string[] SplitItem(string s) {
        return s.Split(ItemSplitor);
    }
    public static string RootPath(string s) {
        var len = s.IndexOf('.', 1);
        if (len == -1) {
            len = s.Length;
        }
        return s.Substring(0, len);
    }
    public static bool IsAbsItem(string path) {
        return path.StartsWith(ItemSplitor);
    }
    public static string ItemName(string path) {
        return path.Substring(path.LastIndexOf(ItemSplitor) + 1);
    }
    public static string ParentItem(string path) {
        return path.Substring(0, path.LastIndexOf(ItemSplitor));
    }

    public static string JoinIdent(params string[] comp) {
        return string.Join(IdentConnector, comp);
    }

    public static string[] SplitType(string s) {
        return s.Split(TypeSplitor, StringSplitOptions.TrimEntries);
    }
    public static string[] SplitWhite(string s) {
        return s.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
    public static string[] SplitWhite(string s, int count) {
        return s.Split(' ', count, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] Type_String = ["s", "str", "string"];
    public static string[] Type_Bool = ["b", "bool"];
    public static string[] Type_Int = ["i", "int"];
    public static string[] Type_Float = ["f", "float"];
    public static string[] Type_Object = ["o", "obj", "object"];
    public static string[] Type_Enum = ["e", "enum"];
    public static string[] Type_List = ["l", "list"];
    public static string[] Type_Map = ["m", "map"];
    public static string[][] Types = [
        Type_Bool,
        Type_String,
        Type_Int,
        Type_Float,
        Type_Object,
        Type_Enum,
        Type_List,
        Type_Map
    ];

    public static string[] Type_Ref = ["r", "ref"];

    public static string TypeName(string ty) {
        foreach (var t in Types) {
            if (t.Contains(ty))
                return t.Last();
        }
        throw new Exception();
    }


    public static string TypeFieldName = KeywordPrefix + "type";
    public static string FirstFieldName = KeywordPrefix + "first";

    public static string KeyFieldName = KeywordPrefix + "key";
    public static string ValueFieldName = KeywordPrefix + "value";

    public static string TableHeadPartName = KeywordPrefix + "head";
    public static string TableValuePartName = KeywordPrefix + "value";
    public static string TablePartEndName = KeywordPrefix + "end";


    //public static string TableNamePrefix = "_t_";
    public static string TableName(string name) {
        //return TableNamePrefix + name;
        return name;
    }

    public static string RootModuleName = KeywordPrefix;
}

