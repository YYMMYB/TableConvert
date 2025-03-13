using System.Diagnostics;

namespace TableConvertor;

public static class CellUtil {
    public static bool IsEmpty(string cell) {
        return cell == null || cell.Length == 0;
    }
}

public record struct InitParam {
    public string[,] table;
    public int startColumn;
}

public class Format {
    public string[,] table;
    public int[] colRange = [-1, -1];
    public int StartCol => colRange[0];
    public int EndCol => colRange[1];
    public Value? value;

    public virtual bool ExistSingleRow { get => false; }
    public virtual bool AllEmpty(int row) {
        for (var c = StartCol; c < EndCol; c++) {
            if (!CellUtil.IsEmpty(table[row, c])) {
                return false;
            }
        }
        return true;
    }

    public virtual IEnumerable<int> SingleCol() {
        throw new NotImplementedException();
    }

    public virtual void SetParam(InitParam param) {
        this.colRange[0] = param.startColumn;
        this.table = param.table;
    }

    public virtual void Reset() {
        value = null;
    }

    public virtual void Read(int startRow, int endRow) {

    }

}

public class SingleFormat : Format {
    public override bool ExistSingleRow => true;

    public override IEnumerable<int> SingleCol() {
        yield return StartCol;
    }

    public override void SetParam(InitParam param) {
        base.SetParam(param);
        colRange = [param.startColumn, param.startColumn + 1];
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

    public SwitchFormat(Dictionary<string, Format> cases) : base() {
        this.cases = cases;
    }

    public override IEnumerable<int> SingleCol() {
        yield return StartCol;
    }

    public override void SetParam(InitParam param) {
        base.SetParam(param);
        param.startColumn += 1;
        var end = param.startColumn;
        foreach (var ch in cases.Values) {
            ch.SetParam(param);
            if (ch.EndCol > end) {
                end = ch.EndCol;
            }
        }
        colRange[1] = end;
    }

    public override void Reset() {
        base.Reset();
        curCase = null;
        foreach (var c in cases.Values) {
            c.Reset();
        }
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(curCase == null);
        curCase = table[startRow, StartCol];
        CurFormat.Read(startRow, endRow);
        value = new ListValue([new LiteralValue(curCase), CurFormat.value]);
    }
}

public class HFormat : Format {

    public HFormat(params List<Format> children) : base() {
        this.children = children;
    }

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

    public List<Format> children;

    public override IEnumerable<int> SingleCol() {
        foreach (var c in children) {
            foreach (var col in c.SingleCol()) {
                yield return col;
            }
        }
    }

    public override void SetParam(InitParam param) {
        base.SetParam(param);
        foreach (var ch in children) {
            ch.SetParam(param);
            param.startColumn = ch.EndCol;
        }
        colRange[1] = children.Last().EndCol;
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
        }
        value = listValue;
    }
}

public class ListFormat : Format {
    public override bool ExistSingleRow => false;

    public Format template;

    public ListFormat(Format template) : base() {
        this.template = template;
    }

    public override IEnumerable<int> SingleCol() {
        yield break;
    }

    public override void SetParam(InitParam param) {
        base.SetParam(param);
        template.SetParam(param);
        colRange = template.colRange;
    }

    public override void Reset() {
        base.Reset();
        template.Reset();
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(value == null);
        var listValue = new ListValue();
        if (template.AllEmpty(startRow)) {
            value = listValue;
            return;
        }
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