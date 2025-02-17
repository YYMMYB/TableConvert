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

public class Formate {
    public Line line;
    public Value? value;

    // 跨行状态
    public bool finished;

    // 每行状态
    public bool valid;

    public void ReadLine(Line line) {
        Reset(line);
        var column = 0;
        ParseValue(ref column);
    }

    public virtual void Reset(Line line) {
        valid = true;
    }

    public virtual void ParseValue(ref int column) { }
}

public class OneCell : Formate {
    public override void Reset(Line line) {
        base.Reset(line);
        this.line = line;
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

public class VTemplate : Formate {

    public List<HTemplate> templates = new();
    public int cur;
    public override void Reset(Line line) {
        base.Reset(line);
        this.line = line;
        templates[cur].Reset(line);
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        if (!finished) {
            templates[cur].ParseValue(ref column);
            if (templates[cur].finished) {
                cur += 1;
            }
        }
        if (cur >= templates.Count) {
            finished = true;
        }
    }
}

public class HTemplate : Formate {
    public Line line;
    public Value value;


    public List<Formate> items = new();

    public override void Reset(Line line) {
        this.line = line;
        foreach (var item in items) {
            item.Reset(line);
        }
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        finished = true;
        valid = true;
        foreach (var item in items) {
            item.ParseValue(ref column);
            finished = finished && item.finished;
            valid = valid && item.valid;
        }
    }
}
