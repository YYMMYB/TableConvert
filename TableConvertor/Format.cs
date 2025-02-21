using System;
using System.Collections;
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
    public int size;

    public Line line;

    // 和 line 一样, 只是个引用, 函数的通用参数.
    // state真正的存储位置, 会根据实际情况有所不同.
    // 可能随时变化. 因为 Format 可以重用.
    // 比如 HFormat 读取列表的时候, 每个新项都会重新设置正确的 state
    public required State state;


    public void ReadLine(Line line) {
        OnNewLine(line);
        var column = 0;
        ParseValue(ref column);
    }

    public virtual void OnNewLine(Line line) {
        this.line = line;
    }

    public virtual void ParseValue(ref int column) {
        if (!state.valid) {
            throw new Exception();
        }
    }

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
    public virtual void Collect() { }

    public virtual void ClearState() { }

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
            state.breakable = true;
        }
        column += 1;
    }
}

public class VTemplate : Format {

    public List<Format> children = new();

    public Format CurTemplate { get => children[state.curFormat]; }


    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
        children[state.cur].OnNewLine(line);
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        var start = column;

        if (!state.finished) {
            if (state.CurChild.finished) {
                state.Next();
                CurTemplate.state = state.CurChild;
            }
            CurTemplate.ParseValue(ref column);
            if (!state.CurChild.valid) {
                state.valid = false;
            }

            state.breakable = state.CurChild.Breackable;

            // 收集. 收集后 state 应该就可以释放掉了(或者重新利用)
            // 这里用了隐藏条件, 在竖模板里, 如果最后一个finished, 那么前面的一定都finished了
            if (children.Count <= state.cur && state.CurChild.finished) {
                Collect();
            }

        }

        AlignColumn(start, ref column);
    }

    public override void Collect() {
        base.Collect();
        var res = new List<Value>();
        foreach (var chState in state.children) {
            if (!chState.valid) {
                throw new Exception();
            }
            if (!chState.finished) {
                throw new Exception();
            }
            if (chState.collected) {
                throw new Exception();
            }
            chState.collected = true;
            res.Add(chState.value);
        }
        var q = from item in state.children
                select item.value;
        state.value = new ListValue(res);
        state.finished = true;
    }
}

public class VList : Format {
    public required Format itemFormat;
    public override void OnNewLine(Line line) {
        base.OnNewLine(line);
        itemFormat.OnNewLine(line);
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        var start = column;

        if (!state.finished) {
            if (state.CurChild.Breackable) {
                itemFormat.ParseValue(ref column);
                if (!state.CurChild.valid) {
                    CollectItem();
                }

            } else {
                itemFormat.ParseValue(ref column);
                if (!state.CurChild.valid) {
                    state.valid = false;
                }
            }

            state.breakable = state.CurChild.Breackable;
        } else {

        }
        AlignColumn(start, ref column);
    }

    public  void CollectItem() {
        base.Collect();
        if (state.value == null) {
            state.value = new ListValue(new List<Value>());
        }
        var list = (state.value as ListValue).list;
        itemFormat.Collect();
        list.Add(state.CurChild.value);
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
