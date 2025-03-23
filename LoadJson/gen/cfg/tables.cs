using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoadJson.gen.cfg;

public class tables {
    public 命名空间.tables 命名空间;
    public Dictionary<int, 综合.值> 综合;

    public static tables load(string folder) {
        var tables = new tables();

        // 命名空间的读取, 可以有多个
        tables.命名空间 = cfg.命名空间.tables.load(Path.Join(folder, "命名空间"));

        // 表的读取, 可以有多个
        using (var r = File.OpenRead(Path.Join(folder, "综合.json"))) {
            tables.综合 = JsonSerializer.Deserialize<Dictionary<int, 综合.值>>(r);
        }

        return tables;
    }
}