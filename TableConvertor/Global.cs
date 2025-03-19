using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;

public class Global {
    public static Global I { get; } = new();
    public Dictionary<string, Item> items = new();

    public string CommonTypeFullName(string type) {
        return $".~Common.{type}";
    }

    public void CreateModules(string path) {

    }

    public T GetItem<T>(string path) where T:Item {
        return (T)items[path];
    }
}

public class Item { 

}


public static class StringUtil {
    public static string Constraint = "builtin.constraint";
    public static Dictionary<string, string> attrs = new Dictionary<string, string>() {
        {"c", Constraint},
    };
    public static string BuiltinAttr(string token) {
        return attrs[token];
    }

    public static bool IsEmptyString(string cell) {
        return cell == null || cell.Trim().Length == 0;
    }

    public static bool IsEmptyValueString(string cell) {
        return IsEmptyString(cell);
    }

    public static string? TryComment(string cell) {
        if (cell.Trim().StartsWith("//")) {
            return cell.Trim().Substring(2);
        }
        return null;
    }
    public static string? TryAttr(string cell) {
        if (cell.Trim().StartsWith("#")) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }
    public static string? TryVarient(string cell) {
        if (cell.Trim().StartsWith("|")) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }

    public static bool IsEmptyIdent(string ident) {
        return IsEmptyString(ident) || ident == "_";
    }


    public static string JoinItem(params string[] comp) {
        return string.Join(".", comp);
    }
    public static bool IsAbsItem(string path) {
        return path.StartsWith(".");
    }
    public static string ItemName(string path) {
        return path.Substring(path.LastIndexOf('.') + 1);
    }
    public static string ParentItem(string path) {
        return path.Substring(0, path.LastIndexOf("."));
    }


    public static string JoinIdent(params string[] comp) {
        return string.Join("_", comp);
    }
}

