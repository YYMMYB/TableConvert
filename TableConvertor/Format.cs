using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TableConvertor;


public class Line
{
    public CellData[] cells;
    public int start;
    public int end;

    public Line(Line line)
    {
        cells = line.cells;
        start = line.start;
        end = line.end;
    }

    public Line(CellData[] cells, int start, int end)
    {
        this.cells = cells;
        this.start = start;
        this.end = end;
    }
}

public enum EmptyMode
{
    MustEmpty,
    Mixed,
    MustHasValue,
}

public abstract class Format
{
    public Format? parent;
    public required int size;

    public abstract bool Finished { get; }

    public abstract bool Breakable { get; }

    public bool InDefault
    {
        get
        {
            if (parent == null) return field;
            else
            {
                if (field) return true;
                else return parent.InDefault;
            }
        }
        protected set => field = value;
    }

    public abstract bool IsAllChildrenEmpty { get; }

    public abstract void Read(Line input);

    public abstract void CheckDefault();
    public abstract void CheckBreak();

    public abstract Value Collect(Line input);
    public abstract void Break();
}

public class Literal : Format
{
    public override bool Finished => Val != null;

    public override bool Breakable => Finished;

    public Value? Val { get; protected set; }

    public override bool IsAllChildrenEmpty => throw new NotImplementedException();

    public override void Read(Line input)
    {
        var i = 0;
        foreach (var cell in input.cells[input.start..input.end])
        {
            if (i == 0)
            {
                if (!Finished)
                {
                    if (cell.K != CellData.Kind.Value)
                    { }
                    else
                    {
                        Val = cell.Val;
                    }
                }
                else
                {
                    switch (cell.K)
                    {
                        case CellData.Kind.Value:
                            throw new Exception("重复.已经有值.");
                            break;
                        default: break;
                    }
                }
            }
            i++;
        }
    }

    public override void CheckDefault()
    {
        throw new NotImplementedException();
    }

    public override void CheckBreak()
    {
        throw new NotImplementedException();
    }

    public override void Break()
    {
        throw new NotImplementedException();
    }

    public override Value Collect(Line input)
    {
        throw new NotImplementedException();
    }
}

public class HList : Format
{
    public required Format[] Item;

    public override sealed bool  Finished { get => _finished; }
    bool _finished;

    public override bool Breakable => Finished;

    public override bool IsAllChildrenEmpty => throw new NotImplementedException();

    public override void Break()
    {
        throw new NotImplementedException();
    }

    public override void CheckBreak()
    {
        throw new NotImplementedException();
    }

    public override void CheckDefault()
    {
        throw new NotImplementedException();
    }

    public override Value Collect(Line input)
    {
        throw new NotImplementedException();
    }

    public override void Read(Line input)
    {

        var end = input.start;
        foreach (var item in Item)
        {
            var start = end;
            end = item.size + end;
            if (end > input.end)
            {
                throw new Exception("BUG HList 的Item过多");
            }

            var chInput = new Line(input.cells, start, end);
            item.Read(chInput);
        }

        // TODO end < input.end 时 检测剩余列不能有输入

        var finished = true;
        foreach (var item in Item)
        {
            if (!item.Finished)
            {
                finished = false;
                break;
            }
        }

        if (finished)
        {
            _finished = true;

        }
    }
}