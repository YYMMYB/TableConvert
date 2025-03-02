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


namespace TableConvertor;


public class Line {
    public string[] cells;
    public int start;

    public Line(int width) {
        cells = new string[width];
        start = 0;
    }

    public Line(Line line) {
        cells = line.cells;
    }

    public Line(string[] cells, int start) {
        this.cells = cells;
        this.start = start;
    }


    public override string ToString() {
        return $"{start}: {cells}";
    }

}

public enum State {
    Incomplete,
    // 调用 TryFinish 并且必须成功, 返回 true.
    // 该状态调用 TryParse 返回 false 时, 一般就代表了结束,
    // 这时一般就需要外部调用 TryFinish.
    // 没有自动调用, 是因为要保证 任何返回 false 的Try函数, 都不改变任何状态(即对象和调用前相同).
    // 我用了这个条件, 所以还是任何时候都保证比较好, 否则担心出bug.
    Breakable,
    Finished,
}

public enum Flow {
    Next,
    Ok,
    Invalid,
}

public interface Parser {
    bool TryParse(Line line);
    bool TryNextLine();
    bool TryFinish();

    Value? TryCollect();

    Parser NewReset();

    State St { get; }
}

public class OneCell : Parser {
    public string value;
    Func<string, bool> predicate;

    State _st;
    public State St => _st;

    public OneCell(Func<string, bool> predicate) {
        this.predicate = predicate;
        _st = State.Incomplete;
    }

    public Parser NewReset() {
        return new OneCell(predicate);
    }

    public Value? TryCollect() {
        throw new NotImplementedException();
    }

    public bool TryNextLine() {
        return St == State.Finished;
    }

    public bool TryParse(Line line) {
        switch (St) {
            case State.Incomplete:
                var v = line.cells[line.start];
                if (!predicate(v)) {
                    return false;
                }
                value = v;
                line.start++;
                _st = State.Finished;
                return true;
        }
        return false;
    }

    public bool TryFinish() {
        return St == State.Finished;
    }
}

public class VComb : Parser {

    public Parser[] parsers;
    public int curIndex = 0;
    public Parser Cur { get => parsers[curIndex]; }
    public Parser Next { get => parsers[curIndex + 1]; }
    bool IsLast { get => curIndex == parsers.Length - 1; }
    bool FollowEmptyTail { get => curIndex >= parsers.Length + ignoreTail - 1; }

    State _st;
    public State St => _st;

    public int ignoreTail;

    private VComb() { }

    public VComb(params Parser[] parsers) {
        var ignoreTail = 0;
        for (; parsers.Length + ignoreTail > 0; ignoreTail--) {
            var idx = parsers.Length + ignoreTail - 1;
            if (parsers[idx].St == State.Breakable || parsers[idx].St == State.Finished) {
                continue;
            } else {
                break;
            }
        }

        Init(parsers, ignoreTail);
    }

    void Init(Parser[] parsers, int ignoreTail) {
        if (parsers.Length == 0) {
            throw new Exception();
        }
        this.parsers = parsers;
        this.ignoreTail = ignoreTail;
        UpdateState();
    }

    void UpdateState() {
        if (IsLast) {
            _st = Cur.St;
        } else if (FollowEmptyTail) {
            if (Cur.St == State.Incomplete) {
                _st = State.Incomplete;
            } else {
                _st = State.Breakable;
            }
        } else {
            _st = State.Incomplete;
        }
    }

    public bool TryParse(Line line) {
        if (St == State.Finished) {
            return false;
        }
        switch (Cur.St) {
            case State.Incomplete:
                if (Cur.TryParse(line) && Cur.TryNextLine()) {
                    UpdateState();
                    return true;
                }
                break;
            case State.Breakable:
                if (Cur.TryParse(line) && Cur.TryNextLine()) {
                    UpdateState();
                    return true;
                } else {
                    if (IsLast) {
                        return false;
                    } else {
                        var cache = Cur;
                        curIndex += 1;
                        if (TryParse(line)) {
                            cache.TryFinish();
                            return true;
                        } else {
                            curIndex -= 1;
                            return false;
                        }
                    }
                }
                break;
            case State.Finished:
                curIndex++;
                if (TryParse(line)) {
                    return true;
                } else {
                    curIndex--;
                    return false;
                }
                break;
        }
        return false;
    }

    public Parser NewReset() {
        var newCh = new Parser[parsers.Length];
        for (int i = 0; i < parsers.Length; i++) {
            newCh[i] = parsers[i].NewReset();
        }
        var res = new VComb();
        res.Init(newCh, this.ignoreTail);
        return res;
    }


    public Value? TryCollect() {
        throw new NotImplementedException();
    }

    public bool TryFinish() {
        if (St != State.Breakable) {
            return false;
        }
        if (Cur.TryFinish()) {
            _st = State.Finished;
            return true;
        } else {
            return false;
        }
    }

    public bool TryNextLine() {
        return true;
    }
}

public class VList : Parser {
    public Parser template;
    public List<Parser> parsers = new();
    public int maxLen;

    public bool IsLast => (maxLen >= 0) && (parsers.Count >= maxLen - 1);

    State _st = State.Breakable;
    public State St => _st;

    public VList(Parser template, int upLimit = -1) {
        this.template = template;
        this.maxLen = upLimit;
        //UpdateState();
        _st = State.Breakable;
    }

    void UpdateState() {
        if (template.St == State.Finished) {
            if (IsLast) {
                _st = State.Finished;
            } else {
                _st = State.Breakable;
            }
        } else {
            _st = template.St;
        }
    }

    void PushTemplate() {
        if (IsLast) {
            throw new Exception();
        }
        parsers.Add(template);
        template = template.NewReset();
    }
    void PopTemplate() {
        template = parsers.Last();
        parsers.RemoveAt(parsers.Count - 1);
    }

    public bool TryParse(Line line) {
        if (St == State.Finished) return false;

        switch (template.St) {
            case State.Incomplete:
                if (template.TryParse(line) && template.TryNextLine()) {
                    UpdateState();
                    return true;
                }
                break;
            case State.Breakable:
                if (template.TryParse(line) && template.TryNextLine()) {
                    UpdateState();
                    return true;
                } else if (IsLast) {
                    return false;
                } else {
                    PushTemplate();
                    // 这里没有递归调用, 防止 template 可空时(即初始状态是Breakable), 出现无限递归.
                    // 并且由于每项都一样, 所以再检测一次就够了(因此在有maxLen的时候, 这样也是对的).
                    if (template.TryParse(line) && template.TryNextLine()) {
                        UpdateState();
                        if (parsers.Last().TryFinish()) {
                            return true;
                        } else {
                            // 原因同 VComb 的这种情况
                            throw new Exception();
                        }
                    } else {
                        PopTemplate();
                    }
                }
                break;
            case State.Finished:
                PushTemplate();
                // 这里不会无限递归, 要求初始状态永远不能是 Finish
                if (TryParse(line)) {
                    return true;
                } else {
                    PopTemplate();
                    return false;
                }
                break;
        }
        return false;
    }

    public Value? TryCollect() {
        throw new NotImplementedException();
    }

    public bool TryFinish() {
        if (St != State.Breakable) return false;

        if (template.TryFinish()) {
            PushTemplate();
            return true;
        } else {
            return false;
        }
    }

    public bool TryNextLine() {
        return true;
    }

    public Parser NewReset() {
        var newT = template.NewReset();
        return new VList(newT, maxLen);
    }

}


public class HComb : Parser {

    State _st;
    public State St => _st;

    public Parser NewReset() {
        throw new NotImplementedException();
    }

    public Value? TryCollect() {
        throw new NotImplementedException();
    }

    public bool TryFinish() {
        throw new NotImplementedException();
    }

    public bool TryNextLine() {
        throw new NotImplementedException();
    }

    public bool TryParse(Line line) {
        throw new NotImplementedException();
    }
}

