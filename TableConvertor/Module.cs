using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Module : Item {
    public string fullName;
    public string name;
    public Module? parent;

    // 名称 -> 全局路径 也就是 Global 里作为键的路径
    public Dictionary<string, string> items = new();

    public Module(Module? parent, string path) {
        var name = Path.GetDirectoryName(path);
        fullName = ModuleUtil.FullName(parent?.fullName, name);
        this.name = name;
        this.parent = parent;
    }

    public void AddSubModule(string path) {
        var m = new Module(this, path);
        m.LoadDirctory(path);
        m.parent = this;
        subModule.Add(m.name, m.fullName);
        Global.I.modules.Add(m.fullName, m);
    }

    public void LoadDirctory(string path, bool useName = true) {
        if (useName) {
            name = Path.GetDirectoryName(path);
        }

        foreach (var p in Directory.EnumerateFileSystemEntries(path)) {
            if (Directory.Exists(p)) {
                AddSubModule(p);
            } else {
                var ext = Path.GetExtension(p);
                if (ext == ".csv") {
                    // todo
                }
            }
        }
    }

    public void AddType(Type type) {
        type.module = this;
        string typeFullName = ModuleUtil.FullName(fullName, type.name);
        subType.Add(type.name, typeFullName);
        Global.I.types.Add(typeFullName, type);
    }
}

public static class ModuleUtil {
    public static string FullName(string? parent, string name) {
        return parent ?? "" + "." + name;
    }

    public static bool IsFullName(string name) {
        return name.StartsWith(".");
    }

    public static string GetLocalName(string fullName) {
        return fullName.Substring(fullName.LastIndexOf('.') + 1);
    }
    public static string GetNamespace(string fullName) {
        return fullName.Substring(0, fullName.LastIndexOf('.'));
    }
}
