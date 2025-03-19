using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableConvertor;

public record class RawHead {
    public string[,] table;

    public int[] rowRange = [-1, -1];
    public int StartRow => rowRange[0];
    public int EndRow => rowRange[1];

    public int[] colRange = [-1, -1];
    public int StartCol => colRange[0];
    public int EndCol => colRange[1];

    public string Cell => table[StartRow, StartCol];

    public bool isVertical;
    public string content;
    public List<string> attrs = new();

    public RawHead parent;
    public List<RawHead> children = new();
    public int horizontalCount;


    public string typeName2;
    public bool isEnum;

    public RawHead(string[,] table, int[] rowRange, int[] colRange) {
        this.table = table;
        this.colRange = colRange;
        this.rowRange = rowRange;
    }

    public void Read() {
        content = Cell;

        var aRow = NextRow(StartCol, StartRow, EndRow);
        while (aRow < EndRow) {
            var aCell = table[aRow, StartCol];
            var attr = StringUtil.TryAttr(aCell);
            if (attr != null) {
                attrs.Add(attr);
            } else {
                break;
            }
            aRow = NextRow(StartCol, aRow, EndRow);
        }

        var chStartCol = StartCol;
        var chStartRow = StartRow;
        if (isVertical) {
            chStartRow = StartRow;
            chStartCol = NextCol(StartRow, StartCol, EndCol);
        } else {
            chStartRow = aRow;
            chStartCol = StartCol;
        }

        if (chStartRow < EndRow) {
            ParseChildren(chStartRow, chStartCol, EndRow, EndCol);
        }
    }

    private RawHead AddChild(int[] chRowRange, int[] chColRange) {
        var node = new RawHead(table, chRowRange, chColRange);
        node.parent = this;
        children.Add(node);
        return node;
    }

    private int ParseChildren(int areaStartRow, int areaStartCol, int areaEndRow, int areaEndCol) {
        var meetVarient = false;
        var chRow = areaStartRow;
        var chCol = areaStartCol;
        do {
            var cell = table[chRow, chCol];
            // todo 优化 根据格式要求 不需要每次都检查, 只检查最后一列即可
            if (StringUtil.TryVarient(cell) != null) {
                meetVarient = true;
                break;
            }
            var chEndCol = NextCol(chRow, chCol, areaEndCol);
            var node = AddChild([chRow, areaEndRow], [chCol, chEndCol]);
            node.Read();
            chCol = chEndCol;
        } while (chCol < areaEndCol);

        horizontalCount = children.Count;

        if (meetVarient) {
            do {
                var chEndRow = NextNonAttr(chCol, chRow, areaEndRow);
                var node = AddChild([chRow, chEndRow], [chCol, areaEndCol]);
                node.isVertical = true;
                node.Read();
                chRow = chEndRow;
            } while (chRow < areaEndRow);
        }

        return areaStartCol;
    }

    public int NextNonAttr(int col, int startRow, int endRow) {
        var row = NextRow(col, startRow, endRow);
        while (row < endRow) {
            var cell = table[row, col];
            if (StringUtil.TryAttr(cell) == null) {
                break;
            }
            row = NextRow(col, row, endRow);
        }
        return row;
    }

    public int NextRow(int col, int startRow, int endRow) {
        for (int i = startRow + 1; i < endRow; i++) {
            var s = table[i, col];
            if (!StringUtil.IsEmptyString(s) && StringUtil.TryComment(s) == null) {
                return i;
            }
        }
        return endRow;
    }

    public int NextCol(int row, int startCol, int endCol) {
        for (int i = startCol + 1; i < endCol; i++) {
            var s = table[row, i];
            if (!StringUtil.IsEmptyString(s) && StringUtil.TryComment(s) == null) {
                return i;
            }
        }
        return endCol;
    }
}


