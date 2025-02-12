namespace TableConvertor;

public interface ReadState
{
    bool Finished { get; }
    bool Breakable { get; }
    Value? Val { get; }
}

public class One : ReadState
{
    public bool Finished => throw new NotImplementedException();

    public bool Breakable => throw new NotImplementedException();

    public Value? Val => throw new NotImplementedException();
}

public class Parallar
{

}