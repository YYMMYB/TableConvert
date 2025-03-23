using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace test {
    public static class T {
        public static void t() {
            var t = new cfg.tables();
            var someValue = t.综合[1001].自动结构体;
            var sV2 = t.命名空间.表2[666].id;
        }
    }

    namespace cfg {
        public class tables {
            public Dictionary<int, 综合.值> 综合;
            public 命名空间.tables 命名空间;

            public static tables load(string folder) {
                var tables = new tables();

                // 表的读取, 可以有多个
                using (var r = File.OpenRead(Path.Join(folder, "综合.json"))) {
                    tables.综合 = JsonSerializer.Deserialize<Dictionary<int, 综合.值>>(r);
                }

                // 命名空间的读取, 可以有多个
                tables.命名空间 = cfg.命名空间.tables.load(Path.Join(folder, "命名空间"));

                return tables;
            }
        }

        namespace 命名空间 {
            public class tables {
                public Dictionary<int, 表2.值> 表2;

                public static tables load(string folder) {
                    throw new NotImplementedException();
                }
            }

            namespace 表2 {

                public class 值 {
                    public int id;
                    public string name;
                    public _表__value_自动结构体 自动结构体;
                }

                public class _表__value_自动结构体 {

                }
            }
        }

        namespace 综合 {

            public class 值 {
                public int id;
                public string name;
                public _表__value_自动结构体 自动结构体;
            }

            public class _表__value_自动结构体 {

            }
        }
    }
}
