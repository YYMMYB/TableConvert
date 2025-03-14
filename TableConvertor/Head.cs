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
    public string name;
    public string? type;
    public string? typeName;
    public Dictionary<string, string> attrs = new();

    public HeadNode parent;
    public List<HeadNode> children = new();
    public int hChildrenCount;

    public HeadNode(string[,] table, int[] colRange) {
        this.table = table;
        this.colRange = colRange;
    }

    public void Read(int startRow, int endRow) {
        string cell = table[startRow, StartCol];
        ParseCell(cell);
        var startRow2 = NextRow(StartCol, startRow, endRow);
        while (startRow2 < endRow) {
            var end = NextRow(StartCol, startRow2, endRow);
            var cell2 = table[startRow2, StartCol];
            var attr = HeadParser.TryAttr(cell2);
            if (attr != null) {
                var s = attr.Split(' ', 2, StringSplitOptions.TrimEntries);
                attrs[s[0]] = s[1];
            } else {
                break;
            }
            startRow2 = end;
        }

        if (isVarient) {
            var startCol2 = NextCol(startRow, StartCol, EndCol);
        }

        if (startRow2 < endRow) {
            ParseChildren(startRow2, StartCol, endRow, EndCol);
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
            node.Read(startRow2, endRow);
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
                var node = new HeadNode(table, [startCol2, endRow2]);
                node.parent = this;
                children.Add(node);
                node.Read(startRow2, endRow2);
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

    public void ParseCell(string cell) {
        string? variant = HeadParser.TryVarient(cell);
        if (variant != null) {
            isVarient = true;
            name = variant;
            return;
        }
        var s = cell.Trim();
        var s2 = s.Split(':', 2, StringSplitOptions.TrimEntries);
        name = s2[0];
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
}