using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Module : Item {
    public override string Name => thisname;
    public override string FullName => GetPath();
    public override Module? ParentMod { get => parent; set => parent = value; }

    Module? parent;
    public string thisname;
    public Dictionary<string, string> items = new();

    string _path;
    public string GetPath() {
        if (_path == null) {
            //if (this == Global.I.root) {
            //    _path = StringUtil.RootModuleName;
            //} else if (ParentMod == Global.I.root) {
            //    _path = StringUtil.JoinItem("", thisname);
            //} else {
            //    _path = StringUtil.JoinItem(ParentMod.GetPath(), thisname);
            //}
            _path = StringUtil.JoinItem(ParentMod?.GetPath()??"", thisname);
        }
        return _path;
    }
    public string CulcFullName(string name) {
        return StringUtil.JoinItem(GetPath(), name);
    }

    public T? GetItem<T>(string name) where T : Item {
        return Global.I.GetItem<T>(items[name]);
    }

    public void AddItem(Item item) {
        item.ParentMod = this; // 必须先设置这个 FullName 依赖于这个属性
        var name = item.Name;
        string fullName = item.FullName;
        items.Add(name, fullName);
        Global.I.AddItem(fullName, item);
    }

    public static Module CreateRootModule() {
        var mod = new Module();

        mod.thisname = StringUtil.RootModuleName;
        mod.ParentMod = null;

        return mod;
    }
}
