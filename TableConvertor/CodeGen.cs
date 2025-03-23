using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TableConvertor;
public class CodeGen {
    public string rootFolder;
    public Module rootMod;

    public CodeGen(Module rootMod, string rootFolder) {
        this.rootFolder = rootFolder;
        this.rootMod = rootMod;
    }

    public void Gen(Module mod, string folder) {
        foreach (var (name, i) in mod.items) {
            Console.WriteLine(name, i);
            if (i is Table t) { // Table 继承了 Module 所以必须先判断Table 
                Directory.CreateDirectory(folder);
                var path = Path.Join(folder, $"{name}.json");
                var jn = t.head.Read(t.rawValue);
                using (var f = new StreamWriter(path)) {
                    f.Write(JsonSerializer.Serialize(jn, StringUtil.JsonOpt));
                }
            } else if (i is Module m) {
                var path = Path.Join(folder, name);
                Gen(m, path);
            } else {
            }
        }
    }
}

