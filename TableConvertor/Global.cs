using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;

public class Global {
    public static Global I { get; } = new();
    public Dictionary<string, Module> modules = new();
    public Dictionary<string, Table> tables = new();
    public Dictionary<string, Type> types = new();

    public string CommonTypeFullName(string type) {
        return $".~Common.{type}";
    }

    public void TryAddNewModule(string fullName) {
        if (!modules.ContainsKey(fullName)) {
            modules.Add(fullName, module);
        }
    }
}

