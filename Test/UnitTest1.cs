using CsvHelper;
using CsvHelper.Configuration;
using Shouldly;
using System.Globalization;
using TableConvertor;

namespace Test;

public class Tests {
    public static string PROJ_DIR = "D:\\Project\\TableConvertor\\Test";

    [SetUp]
    public void Setup() {

    }

    bool IsValue(string cell) {
        return !(IsEmpty(cell) || IsEnd(cell));
    }

    bool IsEnd(string cell) {
        return cell == "$end";
    }

    bool IsEmpty(string cell) {
        return cell.Trim().Length == 0;
    }

    [Test]
    public void Test() {
        var parse = new VList(
            new VComb(
                new VList(new OneCell(IsValue)),
                new VList(new OneCell(IsEnd), 1)
                )
            );
    }

    [Test]
    public void TestValueEq() {
        Value v1 = new ListValue([
            new LiteralValue("a"),
            new LiteralValue("a"),
            new LiteralValue("a"),
            ]);

        ListValue v2 = new ListValue([
            new LiteralValue("a"),
            new LiteralValue("a"),
            new LiteralValue("a"),
            ]);

        ItemEqList<int> l1 = [1, 2, 3];
        ItemEqList<int> l2 = [1, 2, 3];

        Console.WriteLine(l1.Equals(l2));
        Console.WriteLine(l1 == l2);


        Console.WriteLine(v1.Equals(v2));
        Console.WriteLine(v1 == v2);
        //v1.ShouldBe(v2);
    }

}
