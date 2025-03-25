using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Project {
    public void Load(Module parent, string folder) {
        foreach (var p in Directory.GetFileSystemEntries(folder)) {
            if (Directory.Exists(p)) {
                var name = Path.GetFileName(p);
                var mod = CreateModule(parent, name);
                Load(mod, p);
            } else if (File.Exists(p)) {
                if (Path.GetExtension(p) == ".csv") {
                    var table = Table.CreateByCsv(p);
                    table.AfterCreate();
                    table.ParseRange(0);

                    parent.AddItem(table);
                    table.LoadRawHead();
                    table.LoadHead();
                    table.LoadType();
                    table.LoadFormat();
                    table.LoadRawValue();
                } else {
                    Console.WriteLine($"未处理文件 {p}");
                }
            } else {
                throw new Exception();
            }
        }

        //PostLoad(Global.I.root);
    }

    public void PostLoad(Module m) {
        foreach (var (name, i) in m.items) {
            if (i is ObjectType ot) {
                if (ot.baseType != null) {
                    var bt = Global.I.GetAbsItem<ObjectType>(ot.baseType);
                }
            } else if (i is Module) {
                PostLoad(i as Module);
            }
        }
    }

    public Module CreateModule(Module parent, string name) {
        var mod = new Module(name);
        parent.AddItem(mod);
        return mod;
    }
}

