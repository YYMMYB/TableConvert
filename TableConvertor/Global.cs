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

public class PathUtil {
    public static string Join(params string[] comp) {
        return string.Join(".", comp);
    }
    public static bool IsAbs(string path) {
        return path.StartsWith(".");
    }
    public static string Name(string path) {
        return path.Substring(path.LastIndexOf('.') + 1);
    }
    public static string Parent(string path) {
        return path.Substring(0, path.LastIndexOf("."));
    }
}