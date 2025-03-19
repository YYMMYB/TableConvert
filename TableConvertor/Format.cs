using System.Diagnostics;

namespace TableConvertor;



public class Format {
    public string[,] table;
    public int[] colRange = [-1, -1];
    public int StartCol => colRange[0];
    public int EndCol => colRange[1];
    public RawValue? value;

    public virtual bool ExistSingleRow { get => false; }
    public virtual bool AllEmpty(int row) {
        for (var c = StartCol; c < EndCol; c++) {
            if (!StringUtil.IsEmptyValueString(table[row, c])) {
                return false;
            }
        }
        return true;
    }

    public virtual IEnumerable<int> SingleCol() {
        throw new NotImplementedException();
    }
    public record struct InitParam {
        public string[,] table;
        public int startColumn;
        public bool calculateRange;
    }
    public virtual void SetParam(InitParam param) {
        this.table = param.table;
    }

    public virtual void CalculateRange(int startColumn) {
        this.colRange[0] = startColumn;
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
        if (param.calculateRange) {
            CalculateRange(param.startColumn);
        }
    }

    public override void CalculateRange(int startColumn) {
        base.CalculateRange(startColumn);
        colRange[1] = colRange[0] + 1;
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(value == null);
        value = new LiteralRawValue(table[startRow, StartCol]);
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
        var start = param.startColumn;
        if (param.calculateRange) {
            param.startColumn += 1;
        }
        foreach (var ch in cases.Values) {
            ch.SetParam(param);
        }
        if (param.calculateRange) {
            CalculateRange(start);
        }
    }

    public override void CalculateRange(int startColumn) {
        base.CalculateRange(startColumn);
        var end = StartCol + 1;
        foreach (var c in cases.Values) {
            if (c.EndCol > end)
                end = c.EndCol;
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
        value = new ListRawValue([new LiteralRawValue(curCase), CurFormat.value]);
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
        var start = param.startColumn;
        foreach (var ch in children) {
            ch.SetParam(param);
            if (param.calculateRange) {
                param.startColumn = ch.EndCol;
            }
        }
        if (param.calculateRange) {
            CalculateRange(start);
        }
    }

    public override void CalculateRange(int startColumn) {
        base.CalculateRange(startColumn);
        colRange[1] = children.Last().EndCol;
    }

    public override void Reset() {
        base.Reset();
        foreach (var c in children) {
            c.Reset();
        }
    }
    public override void Read(int startRow, int endRow) {
        var listValue = new ListRawValue();
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
        if (param.calculateRange) {
            CalculateRange(param.startColumn);
        }
    }

    public override void CalculateRange(int startColumn) {
        base.CalculateRange(startColumn);
        colRange = template.colRange;
    }

    public override void Reset() {
        base.Reset();
        template.Reset();
    }

    public override void Read(int startRow, int endRow) {
        Debug.Assert(value == null);
        var listValue = new ListRawValue();
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
                if (!StringUtil.IsEmptyValueString(table[row, col])) {
                    return row;
                }
            }
        }
        return endRow;
    }
}