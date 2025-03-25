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

    public void Gen() {
        GenUtils(rootNmspace);
        GenRec(rootMod, rootFolder);
    }

    public void GenUtils(string rootNmspace) {
        var utilsPath = Path.Join(rootFolder, StringUtil.ItemNameToSystemPath(StringUtil.CodeUtilsModuleAbsPath));
        Directory.CreateDirectory(utilsPath);

        var s_DataAccess = $$"""
            using System.IO;
            
            public interface IDataAccess {
                Stream GetString(IDataPath path);
                IDataPath JoinPath(IDataPath path, string item);
            }

            public interface IDataPath {

            }
            """;

        var s_util = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Reflection;
            using System.Text;
            using System.Text.Encodings.Web;
            using System.Text.Json;
            using System.Text.Json.Serialization;
            using System.Threading.Tasks;

            namespace {{rootNmspace}}{{StringUtil.CodeUtilsModuleAbsPath}};

            public static class Util {
                public static JsonSerializerOptions Options = new JsonSerializerOptions() {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    IncludeFields = true,
                    WriteIndented = true,
                    // 似乎 .net 9 才支持这个选项, godot 还没法用. 
                    // 不过我已经调整过 $type 的位置, 到最开头了
                    // 所以没关系, 之前加是为了预防意外
                    // AllowOutOfOrderMetadataProperties = true,
                    Converters = {
                        new JsonStringEnumConverter(),
                        new DictionaryTKeyObjectTValueConverter()
                    }
                };
            }

            public class DictionaryTKeyObjectTValueConverter : JsonConverterFactory {
                public override bool CanConvert(Type typeToConvert) {
                    if (!typeToConvert.IsGenericType) {
                        return false;
                    }

                    if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>)) {
                        return false;
                    }

                    return true;
                }

                public override JsonConverter CreateConverter(
                    Type type,
                    JsonSerializerOptions options) {
                    Type[] typeArguments = type.GetGenericArguments();
                    Type keyType = typeArguments[0];
                    Type valueType = typeArguments[1];

                    JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                        typeof(DictionaryConverterInner<,>).MakeGenericType(
                            [keyType, valueType]),
                        BindingFlags.Instance | BindingFlags.Public,
                        binder: null,
                        args: [options],
                        culture: null)!;

                    return converter;
                }

                private class DictionaryConverterInner<TKey, TValue> :
                    JsonConverter<Dictionary<TKey, TValue>> {
                    private readonly JsonConverter<TValue> _valueConverter;
                    private readonly Type _keyType;
                    private readonly Type _valueType;


                    public DictionaryConverterInner(JsonSerializerOptions options) {
                        // For performance, use the existing converter.
                        _valueConverter = (JsonConverter<TValue>)options
                            .GetConverter(typeof(TValue));

                        // Cache the key and value types.
                        _keyType = typeof(TKey);
                        _valueType = typeof(TValue);
                    }

                    public override Dictionary<TKey, TValue> Read(
                        ref Utf8JsonReader reader,
                        Type typeToConvert,
                        JsonSerializerOptions options) {
                        if (reader.TokenType != JsonTokenType.StartObject) {
                            throw new JsonException();
                        }

                        var dictionary = new Dictionary<TKey, TValue>();

                        while (reader.Read()) {
                            if (reader.TokenType == JsonTokenType.EndObject) {
                                return dictionary;
                            }

                            // Get the key.
                            if (reader.TokenType != JsonTokenType.PropertyName) {
                                throw new JsonException();
                            }

                            string? propertyName = reader.GetString();
                            TKey key = JsonSerializer.Deserialize<TKey>(propertyName, options);
                            // For performance, parse with ignoreCase:false first.
                            if (key == null) {
                                throw new JsonException(
                                    $"Unable to parse \"{propertyName}\" to \"{_keyType}\".");
                            }

                            // Get the value.
                            reader.Read();
                            TValue value = _valueConverter.Read(ref reader, _valueType, options)!;

                            // Add to dictionary.
                            dictionary.Add(key, value);
                        }

                        throw new JsonException();
                    }

                    public override void Write(
                        Utf8JsonWriter writer,
                        Dictionary<TKey, TValue> dictionary,
                        JsonSerializerOptions options) {
                        writer.WriteStartObject();

                        foreach ((TKey key, TValue value) in dictionary) {
                            string propertyName = JsonSerializer.Serialize(key, options);
                            writer.WritePropertyName(propertyName);

                            _valueConverter.Write(writer, value, options);
                        }

                        writer.WriteEndObject();
                    }
                }
            }
            """;

        using (var w = new StreamWriter(Path.Join(utilsPath, "Util.cs"))) {
            WriteToFile(w, s_util);
        }
        using (var w = new StreamWriter(Path.Join(utilsPath, "IDataAccess.cs"))) {
            WriteToFile(w, s_DataAccess);
        }
    }

    protected void GenRec(Module mod, string folder) {
        if (mod.FullName == StringUtil.CodeUtilsModuleAbsPath) {
            throw new Exception();
        }

        Directory.CreateDirectory(folder);

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
                                {
                                var s = access.GetString(access.JoinPath(folder, "{{name}}.json"));
                                tables.{{name}} = JsonSerializer.Deserialize<{{typeCode}}>(s, {{rootNmspace}}{{StringUtil.CodeUtilsModuleAbsPath}}.Util.Options);
                                }
                        """);

                    s_fields_m.AppendLine($$"""
                            public {{typeCode}} {{name}};
                        """);
                } else if (i is Module m) {
                    var chTables = StringUtil.JoinItem(NameToCode(rootNmspace, m.FullName), tablesName);
                    s_nmspaceLoadCode_m.AppendLine($$"""
                                tables.{{name}} = {{chTables}}.load(access, access.JoinPath(folder, "{{name}}"));
                        """);

                    s_fields_m.AppendLine($$"""
                            public {{chTables}} {{name}};
                        """);
                }
            }

            var nmspace = i.ParentMod.FullName;

            if (i is Type ty) {
                // 类型 -> 类型.cs

                if (ty is ObjectType oty) {
                    var s_name = NameToCode(rootNmspace, name);
                    var path = Path.Join(folder, $"{s_name}.cs");

                    string s_baseType;
                    StringBuilder s_baseTypeAttr = new StringBuilder();
                    if (oty.baseType == null) {
                        s_baseType = null;
                        TypeDiscriminatorAttr(s_baseTypeAttr, oty, null);
                    } else {
                        s_baseType = $$""": {{rootNmspace}}{{oty.baseType}}""";

                    }

                    var s_fields = new StringBuilder();
                    foreach (var (fname, fty) in oty.fields) {
                        var s_fty = TypeToCode(rootNmspace, fty);
                        s_fields.AppendLine($$"""
                                    public {{s_fty}} {{fname}};
                                """);
                    }

                    using (var f = new StreamWriter(path)) {
                        var s = $$"""
                            using System.Text.Json.Serialization;

                            namespace {{rootNmspace}}{{nmspace}};

                            {{s_baseTypeAttr}}
                            public class {{s_name}} {{s_baseType}} {

                            {{s_fields}}

                            }
                            """;
                        WriteToFile(f, s);
                    }
                } else if (ty is EnumType ety) {
                    var s_name = NameToCode(rootNmspace, name);
                    var path = Path.Join(folder, $"{s_name}.cs");
                    var s_variant = new StringBuilder();
                    foreach (var v in ety.variants) {
                        s_variant.AppendLine($"""
                                {v},
                            """);
                    }
                    using (var f = new StreamWriter(path)) {
                        var s = $$"""
                            using System.Text.Json.Serialization;

                            namespace {{rootNmspace}}{{nmspace}};

                            public enum {{s_name}} {
                            {{s_variant}}
                            }
                            """;
                        WriteToFile(f, s);
                    }
                }
            } else if (i is Module m) {
                // 模块 -> 文件夹
                // 模块 -> 文件夹/tables.cs

                var path = Path.Join(folder, name);
                GenRec(m, path);
            }
        }

        if (!(mod is Table)) {

            var tablePath = Path.Join(folder, tablesName + ".cs");
            using (var f = new StreamWriter(tablePath)) {
                var s = $$"""
                    using System.Text.Json;
                    using System.Text.Encodings.Web;
                    using System.Collections.Generic;
                    using {{rootNmspace}}{{StringUtil.CodeUtilsModuleAbsPath}};


                    namespace {{rootNmspace}}{{nmspace_m}};

                    public class {{tablesName}} {

                    {{s_fields_m}}

                        public static {{tablesName}} load(IDataAccess access,IDataPath folder) {
                            var tables = new {{tablesName}}();

                            // 命名空间的读取
                    {{s_nmspaceLoadCode_m}}

                            // 表的读取
                    {{s_tableLoadCode_m}}

                            return tables;
                        }
                    }
                    """;
                WriteToFile(f, s);
            }
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

    public void TypeDiscriminatorAttr(StringBuilder sb, ObjectType ty, string? parent) {
        var derivedDis = new List<string>();
        var derivedName = new List<string>();
        foreach (var (dn, d) in ty.derivedType) {
            string dis = StringUtil.JoinDiscriminator(parent, dn);
            var attr = $"""
                [JsonDerivedType(typeof({NameToCode(rootNmspace, d)}), typeDiscriminator: "{dis}")]
                """;
            sb.AppendLine(attr);
            TypeDiscriminatorAttr(sb, Global.I.GetAbsItem<ObjectType>(d), dis);
        }
    }

    public void WriteToFile(StreamWriter f, string s) {
        s = s.ReplaceLineEndings("\n");
        f.Write(s);
    }
}

