using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;

public record class HeadNode {
    public string[,] table;
    public int[] colRange = [-1, -1];
    public int StartCol => colRange[0];
    public int EndCol => colRange[1];

    public bool isVarient;
    public string fullName;
    public string name;
    public string? type;
    public string? typeName;
    public Dictionary<string, List<string>> attrs = new();

    public HeadNode parent;
    public List<HeadNode> children = new();
    public int hChildrenCount;


    public string typeName2;
    public bool isEnum;

    public HeadNode(string[,] table, int[] colRange) {
        this.table = table;
        this.colRange = colRange;
    }

    public void Read(int startRow, int endRow, string parentFullName) {
        string cell = table[startRow, StartCol];
        ParseCell(cell, parentFullName);
        var startRow2 = NextRow(StartCol, startRow, endRow);
        while (startRow2 < endRow) {
            var end = NextRow(StartCol, startRow2, endRow);
            var cell2 = table[startRow2, StartCol];
            var attr = HeadParser.TryAttr(cell2);
            if (attr != null) {
                var s = attr.Split(' ', 2, StringSplitOptions.TrimEntries);
                attrs.TryAdd(s[0], new List<string>());
                attrs[s[0]].Add(s[1]);
            } else {
                break;
            }
            startRow2 = end;
        }
        var startCol2 = StartCol;
        if (isVarient) {
            startRow2 = startRow;
            startCol2 = NextCol(startRow, StartCol, EndCol);
        }

        if (startRow2 < endRow) {
            ParseChildren(startRow2, startCol2, endRow, EndCol);
        }
    }


    private int ParseChildren(int startRow2, int startCol2, int endRow, int endCol) {
        var meetVarient = false;
        do {
            var cell = table[startRow2, startCol2];
            if (HeadParser.TryVarient(cell) != null) {
                meetVarient = true;
                break;
            }
            var endCol2 = NextCol(startRow2, startCol2, endCol);
            var node = new HeadNode(table, [startCol2, endCol2]);
            node.parent = this;
            children.Add(node);
            node.Read(startRow2, endRow, fullName);
            startCol2 = endCol2;
        } while (startCol2 < endCol);

        hChildrenCount = children.Count;

        if (meetVarient) {
            do {
                var endRow2 = startRow2;
                while (endRow2 < endRow) {
                    endRow2 = NextRow(startCol2, endRow2, endRow);
                    var cell = table[endRow2, startCol2];
                    if (HeadParser.TryAttr(cell) == null) {
                        break;
                    }
                }
                var node = new HeadNode(table, [startCol2, endCol]);
                node.parent = this;
                children.Add(node);
                node.Read(startRow2, endRow2, fullName);
                startRow2 = endRow2;
            } while (startRow2 < endRow);
        }
        return startCol2;
    }

    public int NextRow(int col, int startRow, int endRow) {
        for (int i = startRow + 1; i < endRow; i++) {
            var s = table[i, col];
            if (!HeadParser.IsEmpty(s) && HeadParser.TryComment(s) == null) {
                return i;
            }
        }
        return endRow;
    }

    public int NextCol(int row, int startCol, int endCol) {
        for (int i = startCol + 1; i < endCol; i++) {
            var s = table[row, i];
            if (!HeadParser.IsEmpty(s) && HeadParser.TryComment(s) == null) {
                return i;
            }
        }
        return endCol;
    }

    public void ParseCell(string cell, string parentFullName) {
        string? variant = HeadParser.TryVarient(cell);
        if (variant != null) {
            isVarient = true;
            name = variant;
            fullName = parentFullName + "." + name;
            return;
        }
        var s = cell.Trim();
        var s2 = s.Split(':', 2, StringSplitOptions.TrimEntries);
        name = s2[0];
        fullName = parentFullName + "." + name;
        if (s2.Length > 1) {
            var s3 = s2[1].Split(' ', 2, StringSplitOptions.TrimEntries);
            type = s3[0];
            if (s3.Length > 1) {
                typeName = s3[1];
            } else {
                typeName = null;
            }
        } else {
            type = null;
            typeName = null;
        }
    }

    public string? GetEnumVarientValue() {
        if (isVarient) {
            return name;
        } else {
            return null;
        }
    }

    public string? GetDerivedClassName() {
        if (isVarient) {
            return name;
        } else {
            return null;
        }
    }

    public bool NoConstraint() {
        return !attrs.ContainsKey(HeadAttr.Constraint) || attrs[HeadAttr.Constraint].Count <= 0;
    }

    public bool UseCommonType() {
        return NoConstraint() && HeadParser.IsEmptyIdent(typeName);
    }

    public string CalcTypeName() {
        if (HeadParser.IsEmptyIdent(typeName)) {
            return fullName;
        } else {
            return typeName;
        }
    }

    public HeadNode GetChildByIndex(int index) {
        return children[index];
    }

    public HeadNode? GetChildByName(string name) {
        foreach (var child in children) {
            if (child.name == name) {
                return child;
            }
        }
        return null;
    }

    public HeadNode? GetValueChild() {
        if (hChildrenCount == 1) {
            return children[0];
        } else if (hChildrenCount == 2) {
            return children[1];
        } else {
            return null;
        }
    }
    public HeadNode? GetKeyChild() {
        if (hChildrenCount == 2) {
            return children[0];
        } else {
            return null;
        }
    }

    public Type? ThisType(Module mod) {
        return Global.I.types[typeName2];
    }

    public void LoadType(Module mod) {
        for (int i = 0; i < hChildrenCount; i++) {
            HeadNode ch = GetChildByIndex(i);
            ch.LoadType(mod);
        }

        if (isVarient) {
            typeName2 = GetDerivedClassName();
            var objType = new ObjectType(typeName2, parent.ThisType(mod));
            for (int i = 0; i < hChildrenCount; i++) {
                HeadNode ch = GetChildByIndex(i);
                var chType = ch.ThisType(mod);
                var name = ch.name;
                objType.AddField(name, chType!);
            }
            mod.AddType(objType);
        } else {

            var locTypeName = CalcTypeName();
            if (ModuleUtil.IsFullName(locTypeName)) {
                typeName2 = locTypeName;
                locTypeName = ModuleUtil.GetLocalName(locTypeName);
            } else {
                typeName2 = ModuleUtil.FullName(mod.fullName, locTypeName);
            }

            var modName = ModuleUtil.GetNamespace(typeName2);
            if (modName != mod.fullName) {

            }


            switch (type) {
                case "string":
                case "s": {
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("string");
                        } else {
                            mod.AddType(new StringType(locTypeName));
                        }
                        break;
                    }
                case "float":
                case "f": {
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("float");
                        } else {
                            mod.AddType(new FloatType(locTypeName));
                        }
                        break;
                    }
                case "bool":
                case "b": {
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("bool");
                        } else {
                            mod.AddType(new BoolType(locTypeName));

                        }
                        break;
                    }
                case "int":
                case "i": {
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("int");
                        } else {
                            mod.AddType(new IntType(locTypeName));

                        }
                        break;
                    }
                case "list":
                case "l": {
                        var valueTypeList = GetValueChild().ThisType(mod);
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("int");
                        } else {
                            mod.AddType(new ListType(locTypeName, valueTypeList));
                        }
                        break;
                    }
                case "map":
                case "m": {
                        var keyType = GetKeyChild().ThisType(mod);
                        var valueTypeMap = GetValueChild().ThisType(mod);
                        if (UseCommonType()) {
                            typeName2 = Global.I.CommonTypeFullName("int");
                        } else {
                            mod.AddType(new MapType(locTypeName, keyType, valueTypeMap));
                        }
                        break;
                    }
                case "object":
                case "o": {
                        var objType = new ObjectType(locTypeName, null);
                        for (int i = 0; i < hChildrenCount; i++) {
                            HeadNode ch = GetChildByIndex(i);
                            var chType = ch.ThisType(mod);
                            var name = ch.name;
                            objType.AddField(name, chType!);
                        }
                        mod.AddType(objType);


                        break;
                    }
                case "enum":
                case "e": {
                        isEnum = true;
                        var enumType = new EnumType(locTypeName);
                        for (int i = hChildrenCount; i < children.Count; i++) {
                            HeadNode ch = GetChildByIndex(i);
                            var name = GetEnumVarientValue();
                            enumType.AddVariant(name);
                        }
                        mod.AddType(enumType);
                        break;
                    }
                case "ref": {
                        break;
                    }
                default: {
                        throw new Exception("unknown type");
                    }
            }
        }

        if (!isEnum) {
            for (int i = hChildrenCount; i < children.Count; i++) {
                HeadNode ch = GetChildByIndex(i);
                ch.LoadType(mod);
            }
        }
    }

    public Format LoadFormat(Module mod) {
        if (isEnum || children.Count == 0) {
            var f = new SingleFormat();
            f.colRange = colRange;
            return f;
        } else {
            var chFs = new List<Format>();
            for (int i = 0; i < hChildrenCount; i++) {
                HeadNode ch = GetChildByIndex(i);
                var chF = ch.LoadFormat(mod);
                chFs.Add(chF);
            }

            SwitchFormat? switchF = null;
            if (hChildrenCount < children.Count) {
                var dict = new Dictionary<string, Format>();
                for (int i = hChildrenCount; i < children.Count; i++) {
                    var ch = GetChildByIndex(i);
                    var chF = ch.LoadFormat(mod);
                    dict.Add(ch.GetDerivedClassName(), chF);
                }
                switchF = new SwitchFormat(dict);
                var startCol = children[hChildrenCount].StartCol;
                switchF.colRange = [startCol, colRange[1]];
            }

            if (switchF != null) {
                chFs.Add(switchF);
            }
            var f = new HFormat(chFs);
            if (isVarient) {
                f.colRange = [colRange[0] + 1, colRange[1]];
            } else {
                f.colRange = colRange;
            }
            return f;
        }
    }

    public void LoadLayout(Module mod, Table table) {
        if (isEnum || children.Count == 0) {
            return;
        } else {
            var ty = ThisType(mod);
            Layout lay;
            if (ty is ListType) {
                lay = new ListLayout(hChildrenCount == 2);
            } else if (ty is MapType) {
                lay = new MapLayout();
            }
            for (int i = 0; i < hChildrenCount; i++) {
                HeadNode ch = GetChildByIndex(i);
                ch.LoadLayout(mod, table);
            }
            if (hChildrenCount < children.Count) {
                for (int i = hChildrenCount; i < children.Count; i++) {
                    var ch = GetChildByIndex(i);
                    ch.LoadLayout(mod, table);
                }
            }
        }
    }
}

public static class HeadAttr {
    public static string Constraint = "builtin.constraint";
    public static Dictionary<string, string> tokens = new Dictionary<string, string>() {
        {"c", Constraint},
    };
    public static string Parse(string token) {
        return tokens[token];
    }
}

public static class HeadParser {
    public static bool IsEmpty(string cell) {
        return cell == null || cell.Length == 0;
    }

    public static string? TryComment(string cell) {
        if (cell.Trim().StartsWith("//")) {
            return cell.Trim().Substring(2);
        }
        return null;
    }
    public static string? TryAttr(string cell) {
        if (cell.Trim().StartsWith("#")) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }
    public static string? TryVarient(string cell) {
        if (cell.Trim().StartsWith("|")) {
            return cell.Trim().Substring(1).Trim();
        }
        return null;
    }

    public static bool IsEmptyIdent(string ident) {
        return IsEmpty(ident) || ident == "_";
    }
}