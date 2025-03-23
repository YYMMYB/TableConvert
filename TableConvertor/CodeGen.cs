using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace TableConvertor;
public class CodeGen {
    public string rootFolder;
    public Module rootMod;
    public string rootNmspace = "cfg";
    public string tablesName = "Tables";

    public CodeGen(Module rootMod, string rootFolder) {
        this.rootFolder = rootFolder;
        this.rootMod = rootMod;
    }

    public void Gen(Module mod, string folder) {


        var nmspace_m = mod.FullName;

        var s_fields_m = new StringBuilder();
        var s_tableLoadCode_m = new StringBuilder();
        var s_nmspaceLoadCode_m = new StringBuilder();

        foreach (var (name, i) in mod.items) {
            {
                if (i is Module m && m.IsEngineModule) {
                    continue;
                }
            }

            if (!(mod is Table)) {
                if (i is Table t) {
                    var typeCode = TypeToCode(rootNmspace, t.RootType.FullName);
                    s_tableLoadCode_m.AppendLine($$"""
using (var r = File.OpenRead(Path.Join(folder, "{{name}}.json"))) {
    tables.{{name}} = JsonSerializer.Deserialize<{{typeCode}}>(r, JsonOpt);
}
""");

                    s_fields_m.AppendLine($$"""
public {{typeCode}} {{name}};
""");
                } else if (i is Module m) {
                    var chTables = StringUtil.JoinItem(NameToCode(rootNmspace, m.FullName), tablesName);
                    s_nmspaceLoadCode_m.AppendLine($$"""
tables.{{name}} = {{chTables}}.load(Path.Join(folder, "{{name}}"));
""");

                    s_fields_m.AppendLine($$"""
public {{name}}.{{tablesName}} {{name}};
""");
                }
            }

            var nmspace = i.ParentMod.FullName;

            if (i is Type ty) {
                // 类型 -> 类型.cs

                if (ty is ObjectType oty) {
                    var s_name = NameToCode(rootNmspace, name);
                    Directory.CreateDirectory(folder);
                    var path = Path.Join(folder, $"{s_name}.cs");
                    using (var f = new StreamWriter(path)) {
                        {
                            string s_baseType;
                            if (oty.baseType == null) {
                                s_baseType = null;
                            } else {
                                s_baseType = $$""": {{rootNmspace}}{{oty.baseType}}""";
                            }

                            var s_fields = new StringBuilder();
                            foreach (var (fname, fty) in oty.fields) {
                                var s_fty = TypeToCode(rootNmspace, fty);
                                s_fields.AppendLine($$"""public {{s_fty}} {{fname}};""");
                            }

                            f.Write($$"""
namespace {{rootNmspace}}{{nmspace}};

public class {{s_name}} {{s_baseType}} {

{{s_fields}}

}

""");
                        }
                    }
                }
            } else if (i is Module m) {
                // 模块 -> 文件夹
                // 模块 -> 文件夹/tables.cs

                var path = Path.Join(folder, name);
                Gen(m, path);
            }
        }


        var tablePath = Path.Join(folder, tablesName + ".cs");
        using (var f = new StreamWriter(tablePath)) {


            f.Write($$"""
using System.Text.Json;
using System.Text.Encodings.Web;


namespace {{rootNmspace}}{{nmspace_m}};

public class {{tablesName}} {

public static JsonSerializerOptions JsonOpt = new JsonSerializerOptions() {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    IncludeFields = true,
};

{{s_fields_m}}

    public static {{tablesName}} load(string folder) {
        var tables = new {{tablesName}}();

        // 命名空间的读取
{{s_nmspaceLoadCode_m}}

        // 表的读取
{{s_tableLoadCode_m}}

        return tables;
    }
}
""");
        }
    }

    public string NameToCode(string rootNmspace, string name) {
        if (StringUtil.IsAbsItem(name)) {

            if (name.StartsWith(StringUtil.EngineModuleAbsPath)) {
                name = name.Substring(StringUtil.EngineModuleAbsPath.Length + 1);
            } else {
                name = $$"""{{rootNmspace}}{{name}}""";
            }
        }
        name = name.Replace(StringUtil.KeywordPrefix, StringUtil.CodeKeywordPrefix);

        return name;
    }

    public string TypeToCode(string rootNmspace, string typeFullName) {
        var t = Global.I.GetAbsItem<Type>(typeFullName);
        if (t is ListType lt) {
            var v = TypeToCode(rootNmspace, lt.valueType);
            return $"List<{v}>";
        } else if (t is MapType mt) {
            var k = TypeToCode(rootNmspace, mt.keyType);
            var v = TypeToCode(rootNmspace, mt.valueType);
            return $"Dictionary<{k}, {v}>";
        } else {
            return NameToCode(rootNmspace, typeFullName);
        }
    }
}

