using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TableConvertor;


public class Line {
    public CellData[] cells;
    public int start;

    public Line(Line line) {
        cells = line.cells;
    }

    public Line(CellData[] cells, int start) {
        this.cells = cells;
    }
}

public class Format {
    public Line line;
    public Value? value;
    public int size;

    // 跨行状态
    public bool finished;

    // 每行状态
    public bool valid;

    public void ReadLine(Line line) {
        OnNewLine(line);
        var column = 0;
        ParseValue(ref column);
    }

    public virtual void OnNewLine(Line line) {
        this.line = line;
        valid = true;
    }

    public virtual void ParseValue(ref int column) { }

    public virtual void AlignColumn(int start, ref int column) {
        if (column < start) {
            throw new Exception();
        } else if (column < start + size) {
            for (var c = column; c < start + size; c++) {
                if (line.cells[c].k == CellData.Kind.Value) {
                    valid = false;
                    break;
                }
            }
            column = start + size;
        } else if (column == start + size) {

        } else if (column > start + size) {
            throw new Exception();
        }

    }

}

public class OneCell : Format {
    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
    }
    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        if (finished) {
            if (this.line.cells[column].k == CellData.Kind.Value) {
                valid = false;
            } else {
                valid = true;
            }
        } else {
            this.value = this.line.cells[column].val;
            finished = true;
            valid = true;
        }
        column += 1;
    }
}

public class VTemplate : Format {

    public List<HTemplate> templates = new();
    public int cur;
    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
        templates[cur].OnNewLine(line);
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        var start = column;
        if (!finished) {
            templates[cur].ParseValue(ref column);
            if (templates[cur].finished) {
                cur += 1;
            }
        }

        AlignColumn(start, ref column);

        if (cur >= templates.Count) {
            // 收集
            var q = from item in templates
                    select item.value;
            value = new ListValue(q.ToList());
            finished = true;
        }
    }
}

public class HTemplate : Format {

    public List<Format> items = new();

    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
        foreach (var item in items) {
            item.OnNewLine(line);
        }
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        var start = column;

        finished = true;
        valid = true;
        foreach (var item in items) {
            item.ParseValue(ref column);
            finished = finished && item.finished;
            valid = valid && item.valid;
        }

        AlignColumn(start, ref column);

        if (finished) {
            // 收集
            var q = from item in items
                    select item.value;
            value = new ListValue(q.ToList());
        }

    }
}

public class Switch : Format {
    public bool known;
    // 挡位. 表示当前是走的哪个分支.
    public Value? gear;
    public Format gearFormat;
    public Dictionary<Value, Format> branches = new();

    public override void OnNewLine(Line line) {
        base.OnNewLine(line);

        gearFormat.OnNewLine(line);
        // 不知道用谁, 就都设置
        foreach (var (g, b) in branches) {
            b.OnNewLine(line);
        }
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);

        var start = column;
        if (!gearFormat.finished) {
            gearFormat.ParseValue(ref column);
            if (!gearFormat.valid) { valid = false; } else {
                if (gearFormat.finished) {
                    gear = gearFormat.value;
                }
            }
        }

        if (gearFormat.finished) {
            branches[gear].ParseValue(ref column);
        }

        AlignColumn(start, ref column);
    }
}
