using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TableConvertor;


namespace TableConvertor;

public record class Token {
    public string content;
}

public interface IInput<Tk> {
    Finished Finished { get; }
    Tk Peer(int n);
    void Advance(int n);
}

public abstract class Input : IInput<Token>, IHistory {
    public abstract Finished Finished { get; }
    public abstract void Advance(int n);
    public abstract Token Peer(int n);


    public abstract IHistoryState CaulculateHistoryState();
    public abstract void RestoreHistoryState(IHistoryState state);
}


public interface IParser {
    Finished Finished { get; }
    bool Parse(Input input);
    IParser CloneNew();
}

public abstract class Parser : IParser, IHistory {
    protected Finished _finished;
    public Finished Finished { get => _finished; }
    public abstract bool Parse(Input input);
    public abstract IParser CloneNew();
    public abstract void CloneFrom(IParser origin);

    // 历史记录功能具体实现.
    public IHistoryState? history;
    public abstract IHistoryState CaulculateHistoryState();
    public abstract void RestoreHistoryState(IHistoryState state);

    // 历史记录功能的使用.
    public void SaveToHistory() {
        history = CaulculateHistoryState();
    }
    public void RestoreHistory() {
        if (history != null) {
            RestoreHistoryState(history);
        }
    }
    public void DropHistory() {
        history = history?.PrevHistoryState();
    }
}

public interface IHistoryState {
    IHistoryState? PrevHistoryState();
}

public interface IHistory {
    IHistoryState CaulculateHistoryState();
    void RestoreHistoryState(IHistoryState state);
}

public record struct Finished {
    private int _id;
    public static Finished Incomplete = new Finished { _id = 0 };
    public static Finished Line = new Finished { _id = 1 };
    public static Finished All = new Finished { _id = 2 };

    public bool IsIncomplete() => _id == 0;
    public bool LineIsFinished() => _id >= 1;
    public bool AllFinished() => _id == 2;
}

public class HRepeat : Parser {

    public Parser template;
    public List<Parser> children = [];
    public int curIdx;
    public Parser Cur { get => children[curIdx]; }

    bool IncreaseIndex() {
        curIdx++;
        return true;
    }

    void ResetIndex() {
        curIdx = 0;
    }

    void Update() {

    }

    public override bool Parse(Input input) {
        if (Finished.AllFinished()) {
            return false;
        }

        while (input.Finished.IsIncomplete()) {
            if (Cur.Finished.LineIsFinished()) {
                if (!IncreaseIndex()) {
                    throw new Exception();
                }
            }
            if (!Cur.Parse(input)) {
                return false;
            }
            Update();
            if (Finished.LineIsFinished()) {
                break;
            }
        }

        return true;

    }

    public override IHistoryState CaulculateHistoryState() {
        throw new NotImplementedException();
    }


    public override void RestoreHistoryState(IHistoryState state) {
        throw new NotImplementedException();
    }

    public override IParser CloneNew() {
        throw new NotImplementedException();
    }

    public override void CloneFrom(IParser origin) {
        throw new NotImplementedException();
    }
}

public class HRepeatState : IHistoryState {
    public IHistoryState? prevHistory;
    public IHistoryState? PrevHistoryState() {
        return prevHistory;
    }

    public List<IHistoryState> childrenState = [];
}