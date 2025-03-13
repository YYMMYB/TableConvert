using System.Diagnostics;

namespace TableConvertor;

public static class CellUtil {
    public static bool IsEmpty(string cell) {
        return cell == null || cell.Length == 0;
    }
}

public class Format {
    public string[,] table;
    public int[] colRange;
    public int StartCol => colRange[0];
    public int EndCol => colRange[1];
    public Value? value;

    public virtual bool ExistSingleRow { get => false; }
    public virtual bool AllEmpty(int row) {
        for (var c = StartCol; c <= EndCol; c++) {
            if (!CellUtil.IsEmpty(table[row, c])) {
                return false;
            }
        }
        return true;
    }

    public virtual IEnumerable<int> SingleCol() {
        throw new NotImplementedException();
    }

    public virtual void Read(int startRow, int endRow) {

    }

    public virtual void Reset() {
        value = null;
    }
}

public class SingleFormat : Format {
    public override bool ExistSingleRow => true;

    public override IEnumerable<int> SingleCol() {
        yield return StartCol;
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(value == null);
        value = new LiteralValue(table[startRow, StartCol]);
    }

}

public class SwitchFormat : Format {
    public override bool ExistSingleRow => true;

    public string? curCase;
    public Dictionary<string, Format> cases = new();
    public Format CurFormat => cases[curCase];

    public override IEnumerable<int> SingleCol() {
        yield return StartCol;
    }

    public override void Reset() {
        base.Reset();
        foreach (var c in cases.Values) {
            c.Reset();
        }
    }

    override public void Read(int startRow, int endRow) {
        curCase = table[startRow, StartCol];
        CurFormat.Read(startRow, endRow);
    }
}

public class HFormat : Format {
    public override bool ExistSingleRow {
        get {
            foreach (var ch in children) {
                if (ch.ExistSingleRow) {
                    return true;
                }
            }
            return false;
        }
    }

    public Format[] children;

    public override IEnumerable<int> SingleCol() {
        foreach (var c in children) {
            foreach (var col in c.SingleCol()) {
                yield return col;
            }
        }
    }
    public override void Reset() {
        base.Reset();
        foreach (var c in children) {
            c.Reset();
        }
    }
    public override void Read(int startRow, int endRow) {
        var listValue = new ListValue();
        foreach (var c in children) {
            c.Read(startRow, endRow);
            listValue.Add(c.value);
            c.Reset();
        }
    }
}

public class ListFormat : Format {
    public override bool ExistSingleRow => false;

    public Format template;

    public override IEnumerable<int> SingleCol() {
        yield break;
    }

    public override void Reset() {
        base.Reset();
        template.Reset();
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(value == null);
        if (template.AllEmpty(startRow)) {
            return;
        }
        var listValue = new ListValue();
        if (!template.ExistSingleRow) {
            throw new Exception();
        }
        var start = startRow;
        do {
            var end = NextRow(start, endRow);
            template.Read(start, end);
            listValue.Add(template.value);
            template.Reset();
            start = end;
        }
        while (start < endRow);
        value = listValue;
    }


    public int NextRow(int startRow, int endRow) {
        for (int row = startRow + 1; row < endRow; row++) {
            foreach (var col in template.SingleCol()) {
                if (!CellUtil.IsEmpty(table[row, col])) {
                    return row;
                }
            }
        }
        return endRow;
    }
}