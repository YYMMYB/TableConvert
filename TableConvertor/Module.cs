using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Module : Item {

    public Module(string name) : base(name) { }

    public Dictionary<string, Item> items = new();


    public string CulcFullName(string name) {
        return StringUtil.JoinItem(GetPath(), name);
    }

    public T? GetItem<T>(string path) where T : Item {
        if (StringUtil.IsAbsItem(path)) {
            throw new Exception();
        }
        var p2 = StringUtil.SplitItem(path, 2);
        if (p2.Length == 2) {
            return (items.GetValueOrDefault(p2[0], null) as Module)?.GetItem<T>(p2[1]);
        } else {
            return items.GetValueOrDefault(p2[0],null) as T;
        }
    }

    public void AddItem(Item item) {
        item.ParentMod = this; // 必须先设置这个 FullName 依赖于这个属性
        var name = item.Name;
        items.Add(name, item);
    }

    public static Module CreateRootModule() {
        var mod = new Module(null);
        mod.ParentMod = null;

        return mod;
    }
}
