namespace TableConvertor;

public class State {
    public Value? value;
    public bool valid;
    public bool finished;
    public bool collected;

    // 有多个子节点的会使用 (H,VTemplate,H,VList)
    public int curFormat;

    // emmm 似乎只有并行的节点会用, 就是 HTemplate, HList 才会用多个子状态.
    // 而 V 系列的, 处理完一个子节点, 才会处理下一个, 这样可以及时收集前一个, 所以不需要多个子状态.
    public int curState;

    public bool Breakable { get => finished || field; set; }


    public State? history;
    public int containerValueCount;


    public State() {
        Clear();
        curState = 0;
    }
    public void Clear() {
        value = null;
        finished = false;
        Breakable = false;
        collected = false;
        valid = true;

        curFormat = 0;

        history = null;
        containerValueCount = 0;
    }

    public State Clone() {
        var clone = new State();
        clone.CloneFrom(this);
        return clone;
    }

    public void CloneFrom(State other) {
        this.value = other.value;
        this.finished = other.finished;
        this.collected = other.collected;
        this.valid = other.valid;
        this.Breakable = other.Breakable;
        this.curFormat = other.curFormat;
        this.curState = other.curState;

        this.history = other.history;
        this.containerValueCount = other.containerValueCount;
    }

    public void Save() {
        if (value is ListValue listValue) {
            containerValueCount = listValue.list.Count;
        } else if (value is MapValue mapValue) {
            containerValueCount = mapValue.map.Count;
        }
        history = Clone();
    }

    public void Restore() {
        if (history != null) {
            CloneFrom(history);
            var count = containerValueCount;
            if (value is ListValue listValue) {
                listValue.list.RemoveRange(count, listValue.list.Count);
            }else if (value is MapValue mapValue) {
                mapValue.map.RemoveRange(count, mapValue.map.Count);
            }
        }
    }
}
