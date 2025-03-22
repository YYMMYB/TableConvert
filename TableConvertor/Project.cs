using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;
public class Project {
    public string rootPath;
    public string tablePath;

    public void LoadTables(Module parent ,string folder) {
        foreach (var name in Directory.GetFileSystemEntries(folder)) {
            var p = Path.Join(folder, name);
            if (Directory.Exists(p)) {
                var mod = CreateModule(parent, name);
                LoadTables(mod,p);
            } else if (File.Exists(p)) {
                if (Path.GetExtension(p) == ".csv") {
                    var table = Table.CreateByCsv(p);
                    parent.AddItem(table);
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
        parent.AddItem(mod);
        return mod;
    }
}

