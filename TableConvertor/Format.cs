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


public class State {
    public List<State> children = new();
    public Value? value;
    public bool finished;
    public bool collected;
    public bool valid;

    public State() {
        value = null;
        finished = false;
        collected = false;
        valid = true;
    }
}

public class Format {
    public int size;

    public Line line;

    // 和 line 一样, 只是个引用, 函数的通用参数.
    // state真正的存储位置, 会根据实际情况有所不同.
    // 可能随时变化. 因为 Format 可以重用.
    // 比如 HFormat 读取列表的时候, 每个新项都会重新设置正确的 state
    public State state;


    public void ReadLine(Line line) {
        OnNewLine(line);
        var column = 0;
        ParseValue(ref column);
    }

    public virtual void OnNewLine(Line line) {
        this.line = line;
    }

    public virtual void ParseValue(ref int column) { }

    public virtual void AlignColumn(int start, ref int column) {
        if (column < start) {
            throw new Exception();
        } else if (column < start + size) {
            for (var c = column; c < start + size; c++) {
                if (line.cells[c].k == CellData.Kind.Value) {
                    state.valid = false;
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
        if (state.finished) {
            if (this.line.cells[column].k == CellData.Kind.Value) {
                state.valid = false;
            } else {
                state.valid = true;
            }
        } else {
            state.value = this.line.cells[column].val;
            state.finished = true;
            state.valid = true;
        }
        column += 1;
    }
}

public class VTemplate : Format {

    public List<HTemplate> templates = new();
    public int cur;
    public int curState;
    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
        templates[cur].OnNewLine(line);
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        var start = column;
        if (!state.finished) {
            templates[cur].ParseValue(ref column);
            if (templates[cur].state.finished) {
                cur += 1;
            }
        }

        AlignColumn(start, ref column);

        var newState = new State();
        state.children[curState].children.Add(newState);
        templates[cur].state = newState;

        if (cur >= templates.Count) {
            // 收集
            var q = from item in templates
                    select item.state.value;
            state.value = new ListValue(q.ToList());
            state.finished = true;
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

        state.finished = true;
        state.valid = true;
        foreach (var item in items) {
            item.ParseValue(ref column);
            state.finished = state.finished && item.state.finished;
            state.valid = state.valid && item.state.valid;
        }

        AlignColumn(start, ref column);

        if (state.finished) {
            // 收集
            var q = from item in items
                    select item.state.value;
            state.value = new ListValue(q.ToList());
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
        if (!gearFormat.state.finished) {
            gearFormat.ParseValue(ref column);
            if (!gearFormat.state.valid) { state.valid = false; } else {
                if (gearFormat.state.finished) {
                    gear = gearFormat.state.value;
                }
            }
        }

        if (gearFormat.state.finished) {
            branches[gear].ParseValue(ref column);
        }

        AlignColumn(start, ref column);
    }
}
