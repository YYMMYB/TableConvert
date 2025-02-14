using System;
using System.Collections.Generic;
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
    public int end;

    public Line(Line line) {
        cells = line.cells;
        start = line.start;
        end = line.end;
    }

    public Line(CellData[] cells, int start, int end) {
        this.cells = cells;
        this.start = start;
        this.end = end;
    }
}

public abstract class Format {
    // 不变信息, 根据表头确定.
    public Format? parent;

    // $end 可以控制的东西.
    // 目前只有 VList (竖向一维字典和数组). 以后的 HVList (同质双索引二维字典, 同质齐次二维数组) 应该也会用
    public bool beAbleToCreateNewItem;
    public bool CanCreateNewItem() {
        return beAbleToCreateNewItem && !allFinished;
    }

    // #default 的逻辑类似,
    // 用于有键的字典或列表. 只能写在键里.
    public bool beAbleToProvideDefault;
    public bool CanProvideDefault() {
        return beAbleToProvideDefault;
    }

    // 参数 需要每行重写设置.
    public Line input;

    // 最后收集的值.
    public Value? value;



    // 依赖上次结果, 和新输入的信息. 每次输入都会更新.

    // 所有子节点为空, 包括本次输入, 和历史未被收集的输入, 都为空.
    // 用于 HList 跳过空项目
    public bool allEmpty;


    // 在不该出现值的地方出现了值.
    // 用于判断是否要开启新项目.
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

    // 新建项的时候的子节点, 就是竖排格式的第一行. 横排格式与Children相同
    protected virtual IEnumerable<Format> ChildrenOfFirstRow() { yield break; }

    // 所有子节点, 不管是否用到.
    // 主要是更新 Input. 防止用到的时候 input 是旧的.
    protected virtual IEnumerable<Format> AllChildren() { yield break; }

    // 一般是连续的, 但是防止出现不连续的情况, 所以用这个.
    // 与输入无关, 固定的. 这是为了保证第一行和当前行, 节点控制的列的范围是一样的.
    // 否则不好判断是否需要新建项.
    protected virtual IEnumerable<int> ControledColumn() { yield break; }

    public enum Controled {
        None,
        Child,
        Value,
        Empty,
    }

    public struct InputColumnInfo {
        public Format? child;
        public Controled controled;
    }

    public abstract InputColumnInfo LookupColumn(int i);

    public abstract InputColumnInfo LookupColumnOfFirstRow(int i);

    // XXX 效率很低, 可以缓存一下.
    public virtual IEnumerable<int> SelfColumn() {
        return from col in ControledColumn()
               let info = LookupColumn(col)
               where info.controled != Controled.None && info.controled != Controled.Child
               select col;
    }


    public virtual void InitInput(Line new_input) {
        input = new_input;
        foreach (var ch in AllChildren()) {
            ch.InitInput(new_input);
        }
    }

    public virtual void CollectInfoOfFirstRow() {

        foreach (var ch in ChildrenOfFirstRow()) {
            ch.CollectInfoOfFirstRow();
        }
        // TODO
        throw new NotImplementedException();
    }

    public virtual void CollectInfo() {
        foreach (var ch in Children()) {
            ch.CollectInfo();
        }

        allEmpty = allEmpty && Children().All((fmt) => { return fmt.allEmpty; });

        var q1 = from col in SelfColumn()
                 where LookupColumn(col).controled == Controled.Empty
                 && input.cells[col].K == CellData.Kind.Value
                 select true;
        existFreeInputValue = q1.Any();
        existFreeInputValue = existFreeInputValue || Children().Any((fmt) => {
            return fmt.existFreeInputValue
            && !fmt.CanCreateNewItem();
        });

        var q2 = from col in SelfColumn()
                 where input.cells[col].K == CellData.Kind.End
                 select true;
        existEndKeyword = q2.Any();
        existEndKeyword = existEndKeyword || Children().Any((fmt) => {
            return fmt.existEndKeyword
            && !fmt.CanCreateNewItem();
        });

        var q3 = from col in SelfColumn()
                 where input.cells[col].K == CellData.Kind.Default
                 select true;
        existDefaultKeyword = q3.Any();
        existDefaultKeyword = existDefaultKeyword || Children().Any((fmt) => {
            return fmt.existDefaultKeyword
            && !fmt.CanProvideDefault();
        });
    }

    public virtual void CollectValue() {
        if (CanCreateNewItem() && existFreeInputValue) {
            ForceBuildItem();
            NewItem();
        }

        foreach (var ch in Children()) {
            ch.CollectValue();
        }

        if (CanCreateNewItem() && existEndKeyword) {
            ForceBuildItem();
            BuildValue();
        } else {
            ReadLine();
            if (NeedAutoBuild()) {
                BuildValue();
            }
        }

    }

    public virtual void NewItem() {
        throw new NotImplementedException();
    }

    public virtual void ForceBuildItem() {
        throw new NotImplementedException();
    }

    // 读取自身控制的值.
    public abstract void ReadLine();

    // 当前子节点, 与自身值控制的值的状态, 是否要自动执行 BuildValue.
    // 对于 VList 永远为 false, 不会自动完成.
    public virtual bool NeedAutoBuild() {
        var q1 = from col in SelfColumn()
                 where LookupColumn(col).controled != Controled.Empty
                 select true;
        var noSelfValue = !q1.Any();
        var q2 = from ch in Children()
                 where !ch.allFinished
                 select true;
        var allChildFinished = !q2.Any();
        var noNewItem = !CanCreateNewItem();
        return noSelfValue && allChildFinished && noNewItem;
    }

    // 通过被自身控制的值, 和子节点的值, 构建自身的值, 并切换到准备接受下一行输入的状态.
    public void BuildValue() {
        // TODO 占位符与读取默认值
        if (allEmpty) {
            value = null;
        } else {
            _BuildValue();
        }
        allFinished = true;
    }

    protected abstract void _BuildValue();

}
