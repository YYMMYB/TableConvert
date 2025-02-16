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


public abstract class Item {
    public required bool nullable;

    public Line input;

    // 应该换成另一个结构体, 临时先用这个
    public CellData.Kind k;
    public Value? val;
    public abstract bool IsVEnd { get; }

    
    public abstract void OnNewLine();
    public abstract void ReadOn(ref int column);
}

public class SingleCell : Item {
    public override bool IsVEnd { get => true; }
    bool finish;

    public override void OnNewLine() {
        throw new NotImplementedException();
    }

    public override void ReadOn(ref int column) {
        val = input.cells[column].val;
        k = input.cells[column].k;
        column += 1;
    }
}


public partial class Format : Item {
    public Format? parent;

    public Line input;

    public override bool IsVEnd { get { throw new NotImplementedException(); } }

    public void InputLine(Line line) {
        input = line;
        // TODO
    }
}

public partial class Format : Item {
    public HFormat Row { get { throw new NotImplementedException(); } }

    public override void ReadOn(ref int column) {
        Row.ReadOn(ref column);
    }
}

public class HFormat {
    public required Format manager;
    public Line Input { get => manager.input; }

    public required int maxSize;
    public required bool repeatable;
    public required bool itemNullable;


    public bool firstKind;
    public CellData.Kind k;
    public List<Item> items = new();
    public int cur = 0;
    public bool IsHEnd { get => cur >= items.Count; }
    public bool IsVEnd { get; set; }

    public class V {
        public List<Value?>? items = new();
    }
    protected List<V?>? values = new();

    // 每次新的一行都要调用这个
    public void OnNewLine() {
        firstKind = true;
        cur = 0;
        foreach (var item in items) {

        }
    }

    public void ReadOn(ref int column) {
        var startColumn = column;
        while (!IsHEnd) {
            var list = new V();
            for (; cur < items.Count; cur++) {
                var item = items[cur];
                item.ReadOn(ref column);

                list.items.Add(item.val);
                if (item.k != CellData.Kind.Ignore) {
                    if (firstKind) {
                        k = item.k;
                        firstKind = false;
                    } else if (item.k != k) {
                        throw new Exception("kind 应该保持不变");
                    }
                }
            }
            // 优化: 第一次非空时再创建列表
            if (list.items.All((i) => i == null)) {
                list = null;
            }
            values.Add(list);

            if (repeatable) {
                if (column - startColumn >= maxSize || Input.cells[column].k == CellData.Kind.End) {
                    Finish();
                    column += 1;
                } else {
                    cur = 0;
                }
            }
        }
        // 优化: 第一次非空时再创建列表
        if (values.All((i) => i == null)) {
            values = null;
        }
    }

    public void Finish() {
        Debug.Assert(cur == items.Count - 1);
        cur = items.Count;
    }
}


public class VFormat {
    public required Format manager;
    public Line Input { get => manager.input; }

    public List<HFormat> items = new();
    public int cur = 0;
    public bool IsEnd { get { return cur >= items.Count; } }

    public class V {
        public List<HFormat.V> rowGroups = new();
    }
    public List<V> values = new();

    public void OnNewLine() {
        cur += 1;
    }

    public void ReadOn(ref int column) {

    }
}


// 2层
// 根据输入值, 选择使用哪套子级的能力

// 1层 (0层的父级)
// 纵向的能力, 与横向相同

// 0层
// 横向列出子节点的能力
// 横向重复子节点的能力, 固定次数, 任意次数

// -1层
// 读取输入的能力
// 单个节点是否可为空的能力

