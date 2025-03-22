using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Module : Item {
    public override string Name => thisname;
    public override string FullName => GetPath();
    public override Module ParentMod { get => parent; set => parent = value; }

    public Module parent;
    public string thisname;
    public Dictionary<string, string> items = new();

    string _path;
    public string GetPath() {
        if (_path == null) {
            _path = StringUtil.JoinItem(parent.GetPath(), thisname);
        }
        return _path;
    }
    public string CulcFullName(string name) {
        return GetPath() + name;
    }

    public T? GetItem<T>(string name) where T : Item {
        return Global.I.GetItem<T>(items[name]);
    }

    public void AddItem(Item item) {
        var name = item.Name;
        string fullName = item.FullName;
        items.Add(name, fullName);
        item.ParentMod = this;
        Global.I.AddItem(fullName, item);
    }
}
