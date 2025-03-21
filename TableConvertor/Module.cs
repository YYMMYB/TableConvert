using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Module : Item {
    public Module parent;
    public string thisname;
    public override string Name => thisname;
    public Dictionary<string, string> items = new();

    string _path;
    public string GetPath() {
        if (_path == null) {
            _path = StringUtil.JoinItem(parent.GetPath(), thisname);
        }
        return _path;
    }
    public string GetFullName(string name) {
        return GetPath() + name;
    }

    public T? GetItem<T>(string name) where T: Item {
        return Global.I.GetItem<T>(items[name]);
    }

    public void AddItem<T>(T item) where T : Item {
        var name = item.Name;
        string fullName = GetFullName(name);
        items.Add(name, fullName);
        Global.I.AddItem(fullName, item);
    }
}
