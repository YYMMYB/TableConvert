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

public abstract class Format
{
    // 不变信息, 根据表头确定.
    public Format? parent;
    public required int size;
    public int start;

    // 参数 需要每行重写设置.
    public Line input;



    // 子节点, 叶节点的信息

    // 依赖上次结果, 和新输入的信息

    // 编译格式正确的输入所需的信息

    // 所有子节点为空, 包括本次输入, 和历史未被收集的输入, 都为空.
    // 用于 HList 跳过空项目
    public bool allEmpty;

    public enum Controled
    {
        None,
        Child,
        Value,
        Empty,
    }



    // 被子节点控制的列, 希望接受输入值.
    public List<bool>? controledColumn;

    // 在范围内, 但不属于任何子节点的某个格子, 存在值.
    // 区别于 existInputValue, 这个属性考虑范围内 不属于任何叶节点的 输入,
    // 与 existInputValue 的输入 可以做不交并, 成为该节点的整个范围.
    // 有值的格子没有被用到. 要报错, 而不只是警告.
    // 因为所有可能有值的格子都会访问一次, 而没被访问的就一定不能有值.
    // (但是没被访问的可以有关键字)
    public bool existFreeInputValue;

    // 所有已完成的子节点的所有范围内输入(不止是叶节点) 存在 $end, (value和其他关键字视为空, 辅助错误检查).
    public bool existEndKeyword;
    // 所有已完成的子节点的所有范围内输入(不止是叶节点) 要么是$default, 要么为空.
    public bool existDefaultKeyword;



    // 收集值后, 再更新的信息. 也是第一行输入前, 要设定初始状态的信息.

    // 所有子节点都完成了, 包括本次和历史输入.
    // 收集时赋值, 用于自动开启新项
    public bool allFinished = true;
    // 所有子节点都可以打断
    public bool allBreakable = true;




    // 不依赖子节点的属性, 可以删掉, 这里只是在设计算法的时候帮助梳理思路

    // 所有冲突的值, 是否都能输入到新Item的第一行.
    // 用于自动判断 VList 是否结束当前项, 并开启新的一项.
    public bool allConflictInItemFirstLine;




    // 当前状态, 所有会接受输入的子节点.
    // 注意, 完成状态没有子节点.
    protected virtual IEnumerable<Format> Children() { yield break; }


    public virtual void InitInput(Line new_input)
    {
        input = new_input;
    }

    public virtual void CollectInfo()
    {
        foreach (var ch in Children())
        {
            ch.CollectInfo();
        }

        allEmpty = allEmpty && Children().All((fmt) => { return fmt.allEmpty; });

    }

    public virtual void CollectValue() { }

    public virtual void Read()
    {
        foreach (var ch in Children())
        {
            ch.Read();
        }
        CollectValue();
    }

}

public class Literal : Format
{
    public Value? Val { get; protected set; }

    public void Read(Line input)
    {
        var i = 0;
        foreach (var cell in input.cells[input.start..input.end])
        {
            if (i == 0)
            {
                //if (!Finished)
                //{
                //    if (cell.K != CellData.Kind.Value)
                //    { }
                //    else
                //    {
                //        Val = cell.Val;
                //    }
                //}
                //else
                //{
                //    switch (cell.K)
                //    {
                //        case CellData.Kind.Value:
                //            throw new Exception("重复.已经有值.");
                //            break;
                //        default: break;
                //    }
                //}
            }
            i++;
        }
    }

}
