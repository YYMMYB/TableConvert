using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TableConvertor;


public class Line {
    public CellData[] cells;
    public int start;

    public Line(int width) {
        cells = new CellData[width];
        start = 0;
    }

    public Line(Line line) {
        cells = line.cells;
    }

    public Line(CellData[] cells, int start) {
        this.cells = cells;
    }


    public override string ToString() {
        return $"{start}: {cells}";
    }

}


public class Format {
    // HVList 为 true
    // HVTuple 为 false
    public virtual bool ResetChildren { get => true; }
    public int size;

    public Format? template;
    public List<Format>? children = new();
    public int curChildIndex;
    public Format CurChild {
        get {
            if (children == null)
                throw new Exception();
            return children[curChildIndex];
        }
        set {
            if (children == null)
                throw new Exception();
            children[curChildIndex] = value;
        }
    }

    public enum State {
        Invalid,
        Start,
        MustInputValue,
        Breakable,
        Finished,
        EndParent,
    }
    public State state = State.Start;
    public bool Breakable {
        get =>
            state == State.Breakable
            || state == State.Finished
            || state == State.EndParent;
    }
    public bool Finished {
        get => state == State.Finished
            || state == State.EndParent;
    }
    public bool Valid {
        get => state != State.Invalid;
    }

    public bool Empty {
        get; set;
    }

    public Format? history;

    // 一般模式: 深拷贝 value 和 children, 重置 history, savedxxx
    // 历史记录: 保留 value 和 children 的引用, 保留 history, 设置 savedxxx 属性
    // 目前来说 历史记录 模式没有用到. 也没改bug, 肯定是错的(已知的,至少没处理 curChildIndex). 
    public virtual Format Clone() {
        var res = new Format();
        res.ClonedBy(this);
        return res;
    }

    public virtual void ClonedBy(Format origin) {
        size = origin.size;

        state = origin.state;
        Empty = origin.Empty;

        template = origin.template?.Clone();
        if (origin.children != null) {
            var ls = new List<Format>();

            foreach (var ch in origin.children) {
                ls.Add(ch.Clone());
            }
            children = ls;
            curChildIndex = origin.curChildIndex;
        }
        history = origin.history;
    }

    // 不会保留历史记录, 完全的新项.
    public virtual void Reset() {
        // 保持不变的
        //size = size;
        //resetChildren = resetChildren;
        state = State.Start;
        Empty = false;

        template?.Reset();
        if (children != null) {
            if (ResetChildren) {
                children = new List<Format>();
            }
            foreach (var ch in children) {
                ch.Reset();
            }
            curChildIndex = 0;
        } else {
            curChildIndex = -1;
        }


        //history = history;
    }

    public virtual void ForceFinish() {
        if (Breakable) {
            if (!Finished) {
                Trans(State.Finished);
                if (children != null) {
                    foreach (var ch in children) {
                        //if (!ch.Valid) {
                        //    Trans(State.Invalid);
                        //    return;
                        //}
                        if (!ch.Finished) {
                            ch.ForceFinish();
                        }
                    }
                }
            }
        } else {
            Trans(State.Invalid);
            return;
        }
    }

    public virtual void Trans(State newState) {

        state = newState;
    }

    // 目前是子节点和值全都克隆, 可能(肯定)有优化办法, 共用值的List
    public void Save() {
        history = Clone();
    }
    public void Restore() {
        if (history != null) {
            ClonedBy(history);
        } else {
            Trans(State.Invalid);
            return;
        }
    }
    public void DropHistroy() {
        history = history.history;
    }

    public void ReadLine(Line line) {
        ParseValue(line);
    }

    public virtual void ParseValue(Line line) {
        if (Finished) {
            var end = line.start + size;
            ParseFinished(line, end);
        }
    }

    public virtual void ParseOptionValue(Line line) {
        var start = line.start;
        var fromState = state;
        ParseValue(line);
        if (!Valid) {
            if (fromState == State.Start) {
                Reset();
                line.start = start;
                var end = line.start + size;
                ParseIgnore(line, end);
                if (Valid) {
                    Empty = true;
                    Trans(State.Finished);
                }
            }
        }
    }

    public void ParseFinished(Line line, int end) {
        for (; line.start < end; line.start++) {
            switch (line.cells[line.start].k) {
                case CellData.Kind.Value:
                    Trans(State.Invalid);
                    return;
                case CellData.Kind.End:
                    Trans(State.EndParent);
                    break;
                case CellData.Kind.Ignore:
                    break;
            }
        }
        line.start = end;
    }

    public void ParseIgnore(Line line, int end) {
        for (; line.start < end; line.start++) {
            switch (line.cells[line.start].k) {
                case CellData.Kind.Value:
                case CellData.Kind.End:
                    Trans(State.Invalid);
                    return;
                    break;
                case CellData.Kind.Ignore:
                    break;
            }
        }
        line.start = end;
        Console.WriteLine($"{line.start}");
    }

    public void HorizontalFormatUnfinishedStateTransition() {
        var allBreakable = true;
        foreach (var ch in children) {
            if (!ch.Breakable) {
                allBreakable = false;
                break;
            }
        }
        if (allBreakable) {
            Trans(State.Breakable);
            var allFinished = true;
            foreach (var ch in children) {
                if (!ch.Finished) {
                    allFinished = false;
                    break;
                }
            }
            if (allFinished) {
                Trans(State.Finished);
                // 因为现在是没完成状态, 所以永远不会(不应该)接受到 $end

                //var endP = false;
                //foreach (var ch in children) {
                //    if (ch.state == State.EndParent) {
                //        endP = true;
                //        break;
                //    }
                //}
                //if (endP) {
                //    Trans(State.EndParent);
                //}
            } else {
                var existEndParent = false;
                foreach (var ch in children) {
                    if (ch.state == State.EndParent) {
                        existEndParent = true;
                        break;
                    }
                }
                if (existEndParent) {
                    Trans(State.Invalid);
                    return;
                }
            }
        } else {
            Trans(State.MustInputValue);
        }
    }



    public virtual Value RawCollect() {
        if (children != null) {
            var ls = new List<Value>();
            foreach (var ch in children) {
                ls.Add(ch.RawCollect());
            }
            return new ListValue(ls);
        } else {
            var c = this as OneCell;
            return c.value ?? new LiteralValue("");
        }

    }
}

public class OneCell : Format {
    public Value? value;

    public OneCell() {
        children = null;
        size = 1;
    }

    public override Format Clone() {
        var res = new OneCell();
        res.ClonedBy(this);
        return res;
    }

    public override void ClonedBy(Format origin) {
        base.ClonedBy(origin);
        value = (origin as OneCell).value?.Clone();
    }

    public override void Reset() {
        base.Reset();
        value = null;
    }

    public override void ParseValue(Line line) {
        base.ParseValue(line);
        if (!Valid) return;

        var end = line.start + size;

        if (!Finished) {
            var cell = line.cells[line.start];
            switch (cell.k) {
                case CellData.Kind.Value:
                    value = cell.val;
                    break;
                case CellData.Kind.End:
                case CellData.Kind.Ignore:
                    Trans(State.Invalid);
                    return;
                    break;
            }
            line.start += 1;
            ParseIgnore(line, end);
            if (!Valid) return;

            Trans(State.Finished);
        }
    }
}

public class HList : Format {

    //public List<Format> defaultChildren = new();

    public override Format Clone() {
        var res = new HList();
        res.ClonedBy(this);
        return res;
    }

    public override void ParseValue(Line line) {
        base.ParseValue(line);
        if (!Valid) return;


        if (!Finished) {
            var end = line.start + size;
            if (template == null || children == null) {
                throw new Exception();
            }
            curChildIndex = 0;
            while (line.start + template.size <= size) {
                if (state == State.Start) {
                    if (curChildIndex == children.Count) {
                        var ch = template.Clone();
                        children.Add(ch);
                    } else {
                        throw new Exception();
                    }
                }
                CurChild.ParseOptionValue(line);
                if (!CurChild.Valid) {
                    Trans(State.Invalid);
                    return;
                }

                curChildIndex++;
            }

            ParseIgnore(line, end);
            if (!Valid) { return; }

            var allEmpty = true;
            foreach (var ch in children) {
                if (!ch.Empty) {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty) Empty = true;

            HorizontalFormatUnfinishedStateTransition();
            if (!Valid) { return; }
        }
    }
}

public class VList : Format {
    public override Format Clone() {
        var res = new VList();
        res.ClonedBy(this);
        return res;
    }

    public override void ParseValue(Line line) {
        base.ParseValue(line);
        if (!Valid) return;

        if (!Finished) {
            var end = line.start + size;

            if (state == State.Start) {
                children.Add(template.Clone());
                curChildIndex = 0;
                CurChild.ParseOptionValue(line);

                if (!CurChild.Valid) {
                    Trans(State.Invalid); return;
                }

                if (CurChild.Empty) {
                    Empty = true;
                    Trans(State.Finished); return;
                }

                // 复制的后面 Must 的逻辑
                if (!CurChild.Breakable) {
                    Trans(State.MustInputValue);
                } else {
                    Trans(State.Breakable);
                }

                return;
            } else if (Breakable) {
                var vld = false;
                var forceFinish = false;
                CurChild.Save();
                var start = line.start;
                CurChild.ParseValue(line);
                if (CurChild.Valid) {
                    vld = true;
                    if (CurChild.state == State.EndParent) {
                        CurChild.ForceFinish();
                        if (CurChild.Valid) {
                            forceFinish = true;
                        } else {
                            vld = false;
                        }
                    }
                }
                if (vld) {
                    CurChild.DropHistroy();
                    if (CurChild.Breakable) {
                        Trans(State.Breakable);
                        if (forceFinish) {
                            Trans(State.Finished);
                        }
                    } else {
                        Trans(State.MustInputValue);
                    }
                    return;
                } else {
                    CurChild.Restore();
                    line.start = start;
                    CurChild.ForceFinish();
                    if (!CurChild.Valid) {
                        Trans(State.Invalid); return;
                    }

                    children.Add(template.Clone());
                    curChildIndex += 1;

                    // 后面逻辑 和 Must 一样
                }
            }

            // 这里是 Must 的逻辑, 但是 Start 和 Breakable 是通用的, 所以没写if

            CurChild.ParseValue(line);
            if (!CurChild.Valid) {
                Trans(State.Invalid);
                return;
            }

            if (!CurChild.Breakable) {
                Trans(State.MustInputValue);
            } else {
                Trans(State.Breakable);
            }
        }
    }
}

public class HTuple : Format {
    public override bool ResetChildren => false;

    public override Format Clone() {
        var res = new HTuple();
        res.ClonedBy(this);
        return res;
    }

    public override void ParseValue(Line line) {
        base.ParseValue(line);
        if (!Valid) return;

        if (!Finished) {
            var end = line.start + size;

            curChildIndex = 0;
            while (curChildIndex < children.Count) {
                CurChild.ParseValue(line);
                if (!CurChild.Valid) {
                    Trans(State.Invalid);
                    return;
                }

                curChildIndex++;
            }

            ParseIgnore(line, end);
            if (!Valid) { return; }

            HorizontalFormatUnfinishedStateTransition();
            if (!Valid) { return; }
        }
    }
}

public class VTuple : Format {
    public override bool ResetChildren => false;

    public override Format Clone() {
        var res = new VTuple();
        res.ClonedBy(this);
        return res;
    }

    public override void ParseValue(Line line) {
        base.ParseValue(line);
        if (!Valid) return;

        if (!Finished) {
            if (state == State.Start) {
                curChildIndex = 0;
            }

            CurChild.ParseValue(line);
            if (!CurChild.Valid) {
                Trans(State.Invalid);
                return;
            }

            if (CurChild.Breakable) {
                Trans(State.Breakable);
                if (CurChild.Finished) {
                    curChildIndex += 1;
                    if (curChildIndex >= children.Count) {
                        Trans(State.Finished);
                    }
                }
            } else {
                Trans(State.MustInputValue);
            }
        }
    }
}

//public class Switch: Format {
//    public override bool ResetChildren => false;

//    public override void ParseValue(Line line) {
//        base.ParseValue(line);
//        if (!Valid) return;

//    }
//}