using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Project {
    public void Load(Module parent, string folder) {
        foreach (var p in Directory.GetFileSystemEntries(folder)) {
            Console.WriteLine(p);
            if (Directory.Exists(p)) {
                var name = Path.GetFileName(p);
                Console.WriteLine(name);

                var mod = CreateModule(parent, name);
                Load(mod, p);
            } else if (File.Exists(p)) {
                if (Path.GetExtension(p) == ".csv") {
                    var table = Table.CreateByCsv(p);
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
    }

    public Module CreateModule(Module parent, string name) {
        var mod = new Module();
        mod.thisname = name;
        parent.AddItem(mod);
        return mod;
    }
}

