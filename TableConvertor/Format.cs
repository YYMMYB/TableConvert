using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    // HVList 为 true
    // HVTuple 为 false
    public bool ResetChildren { get; set; }

    public Line line;
    public Value? value;

    public Format? keyTemplate;
    public Format? template;
    public List<Format>? children;
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

    public bool firstParse;
    public bool valid;
    public bool finished;
    public bool breakable;
    public bool isEmpty;
    public bool allValue;

    public bool meetEnd;
    public bool meetDefault;

    public bool defaultMode;
    public Value? defaultValue;

    public Format? history;

    // 一般模式: 深拷贝 value 和 children, 重置 history, savedxxx
    // 历史记录: 保留 value 和 children 的引用, 保留 history, 设置 savedxxx 属性
    // 目前来说 历史记录 模式没有用到. 也没改bug, 肯定是错的(已知的,至少没处理 curChildIndex). 
    public virtual Format Clone(bool useForHistory = false) {
        var res = new Format();
        res.ClonedBy(this, useForHistory);
        return res;
    }

    public virtual void ClonedBy(Format origin, bool useForHistory = false) {
        size = origin.size;
        ResetChildren = origin.ResetChildren;

        line = origin.line;
        if (useForHistory) {
            value = origin.value;
        } else {
            value = origin.value?.Clone();
        }

        keyTemplate = origin.keyTemplate?.Clone();
        template = origin.template?.Clone();
        if (origin.children != null) {
            var ls = new List<Format>();
            if (useForHistory) {
                children = origin.children;
            } else {
                foreach (var ch in origin.children) {
                    ls.Add(ch.Clone());
                }
                children = ls;
                curChildIndex = origin.curChildIndex;
            }
        }

        firstParse = origin.firstParse;
        valid = origin.valid;
        finished = origin.finished;
        breakable = origin.breakable;
        isEmpty = origin.isEmpty;
        allValue = origin.allValue;

        meetEnd = origin.meetEnd;
        meetDefault = origin.meetDefault;

        defaultMode = origin.defaultMode;
        defaultValue = origin.defaultValue;

        if (useForHistory) {
            history = origin.history;
        } else {
            history = origin.history;
        }
    }

    // 不会保留历史记录, 完全的新项.
    public virtual void Reset() {
        // 保持不变的
        //size = size;
        //resetChildren = resetChildren;

        //line = line;
        value = null;

        //template = template;
        if (children != null) {
            if (ResetChildren) {
                children = new List<Format>();
            }
            curChildIndex = 0;
        } else {
            curChildIndex = -1;
        }

        firstParse = true;
        valid = true;
        finished = false;
        breakable = false;
        isEmpty = false;
        allValue = false;

        meetEnd = false;
        meetDefault = false;

        defaultMode = false;
        defaultValue = null;

        history = null;

        keyTemplate?.Reset();
        template?.Reset();
        foreach (var ch in children) {
            ch.Reset();
        }
    }

    public virtual void ParseValue(ref int column) {
        if (finished) {
            ParseNonValue(ref column);
        }
    }

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

    public virtual void ForceFinish() {
        if (breakable) {
            finished = true;
            if (children != null) {
                foreach (var ch in children) {
                    if (!ch.finished) {
                        ch.ForceFinish();
                    }
                    if (!ch.valid) {
                        valid = false;
                        return;
                    }
                }
            }
        } else {
            valid = false;
            return;
        }
    }

    public virtual void Collect() { }

    // 目前是子节点和值全都克隆, 可能(肯定)有优化办法, 共用值的List
    public void Save() {
        var h = Clone();
    }
    public void Restore() {
        if (history != null) {
            ClonedBy(history);
        }
    }
    public void DropHistroy() {
        history = null;
    }

    public void ReadLine(Line line) {
        OnNewLine(line);
        var column = 0;
        ParseValue(ref column);
    }

    public void ParseNonValue(ref int column) {
        for (; column < column + size; column++) {
            switch (line.cells[column].k) {
                case CellData.Kind.Value:
                    valid = false;
                    break;
                case CellData.Kind.End:
                    meetEnd = true;
                    break;
                case CellData.Kind.Default:
                    valid = false;
                    break;
                case CellData.Kind.Placeholder:
                    valid = false;
                    break;
                case CellData.Kind.Ignore:
                    break;
            }
        }
        valid = valid && !(meetEnd && meetDefault);
    }

    public virtual void OnNewLine(Line line) {
        this.line = line;
        template?.OnNewLine(line);
        if (children != null) {
            foreach (var child in children) {
                child?.OnNewLine(line);
            }
        }
    }

    public void UpdateValueNullability() {
        if (!defaultMode) {
            if (firstParse) {
                var allE = true;
                var allV = true;
                foreach (var ch in children) {
                    allE = allE && ch.isEmpty;
                    allV = allV && !ch.isEmpty;
                }

                if (!allE && !allV) {
                    valid = false;
                    return;
                }

                isEmpty = allE;
                //allValue = allV;
            } else {
                foreach (var ch in children) {
                    if (isEmpty != ch.isEmpty) {
                        valid = false;
                        return;
                    }
                }
            }
        }
    }
}

public class OneCell : Format {
    public override Format Clone(bool useForHistory = false) {
        var res = new OneCell();
        res.ClonedBy(this, useForHistory);
        return res;
    }

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        if (!finished) {
            var cell = line.cells[column];
            switch (cell.k) {
                case CellData.Kind.Value:
                    if (defaultMode) {
                        value = cell.val;
                    } else {
                        value = cell.val;
                        isEmpty = false;
                    }
                    break;
                case CellData.Kind.Default:
                    if (defaultMode) {
                        valid = false;
                    } else {
                        meetDefault = true;
                    }
                    break;
                case CellData.Kind.End:
                    meetEnd = true; break;
                case CellData.Kind.Ignore:
                case CellData.Kind.Placeholder:
                    if (defaultMode) {
                        value = null;
                    } else {
                        value ??= cell.val;
                        isEmpty = value == null;
                    }
                    break;
            }
            finished = true;
            breakable = true;
            column += 1;
            ParseNonValue(ref column);
        }
        //if (!valid) { return; }
    }
}

public class HList : Format {

    //public List<Format> defaultChildren = new();

    public override Format Clone(bool useForHistory = false) {
        var res = new HList();
        res.ClonedBy(this, useForHistory);
        return res;
    }

    //public override void ClonedBy(Format origin, bool useForHistory = false) {
    //    base.ClonedBy(origin, useForHistory);
    //    var ls = new List<Format>();
    //    foreach (var child in defaultChildren) {
    //        ls.Add(child.Clone());
    //    }
    //    defaultChildren = ls;
    //}

    //public override void Reset() {
    //    base.Reset();
    //    defaultChildren = new();
    //}

    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        if (!finished) {
            if (template == null || children == null) {
                throw new Exception();
            }
            curChildIndex = 0;
            while (column + template.size <= size) {
                if (CurChild == null) {
                    var ch = template.Clone();
                    if (curChildIndex == children.Count) {
                        children.Add(ch);
                    } else {
                        CurChild = ch;
                    }
                }
                CurChild.ParseValue(ref column);
                if (!CurChild.valid) {
                    valid = false;
                    if (!valid) { return; }
                    break;
                }
                curChildIndex++;
            }


            finished = true;
            foreach (var ch in children) {
                if (!ch.finished) {
                    finished = false;
                    break;
                }
            }
            breakable = true;
            foreach (var ch in children) {
                if (!ch.breakable) {
                    breakable = false;
                    break;
                }
            }
            meetEnd = false;
            foreach (var ch in children) {
                if (ch.meetEnd) {
                    meetEnd = true;
                    break;
                }
            }
            meetDefault = false;
            foreach (var ch in children) {
                if (ch.meetDefault) {
                    meetDefault = true;
                    break;
                }
            }

            firstParse = false;
        }
    }
}

// TODO 还要支持列引用 $key : ref value.a 这种
public class VMap : Format {
    public override void ParseValue(ref int column) {
        base.ParseValue(ref column);
        if (!finished) {
            var keyCh = children[curChildIndex - 1];
            var valCh = children[curChildIndex];
            if (breakable) {
                var vld = false;
                Save();
                keyCh.ParseValue(ref column);
                if (keyCh.valid) {
                    valCh.ParseValue(ref column);
                    if (valCh.valid) {
                        if (keyCh.meetEnd || valCh.meetEnd) {
                            ForceFinish();
                            if (valid) {
                                vld = true;
                                DropHistroy();
                            } else {
                                vld = false;
                            }
                        }
                    }
                }
                if (!vld) {
                    Restore();
                    // TODO 处理 defaultMode
                    keyTemplate.Reset();
                    keyTemplate.ParseValue(ref column);
                    if (!keyTemplate.valid) {
                        valid = false;
                        return;
                    }
                    template.Reset();
                    template.ParseValue(ref column);
                    if (!template.valid) {
                        valid = false;
                        return;
                    }
                    children.Add(keyTemplate.Clone());
                    children.Add(template.Clone());
                    curChildIndex += 2;
                }
            } else {
                keyCh.ParseValue(ref column);
                if (!keyCh.valid) {
                    valid = false;
                    return;
                }
                valCh.ParseValue(ref column);
                if (!valCh.valid) {
                    valid = false;
                    return;
                }

                if (keyCh.meetEnd || valCh.meetEnd) {
                    valid = false;
                    return;
                }

            }

            var keyCh2 = children[curChildIndex - 1];
            var valCh2 = children[curChildIndex];
            if (keyCh2.breakable && valCh2.breakable) {
                breakable = true;
            }
        }
    }
}

