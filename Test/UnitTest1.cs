using CsvHelper;
using CsvHelper.Configuration;
using Shouldly;
using System.Globalization;
using TableConvertor;

namespace Test;

public class Tests {
    public static string PROJ_DIR = "D:\\Project\\TableConvertor\\Test";
    CsvConfiguration config;

    [SetUp]
    public void Setup() {
        config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = false
        };
    }

    bool IsValue(string cell) {
        return !(IsEmpty(cell) || IsEnd(cell));
    }

    bool IsEnd(string cell) {
        return cell.StartsWith("$end");
    }

    bool IsEmpty(string cell) {
        return cell.Trim().Length == 0;
    }

    bool Is1(string cell) {
        return cell.StartsWith("1");
    }

    bool Is2(string cell) {
        return cell.StartsWith("2");
    }

    void ParseFile(string file, Parser parser) {
        using (var reader = new StreamReader(Path.Join(PROJ_DIR, file)))
        using (var csv = new CsvReader(reader, config)) {
            while (csv.Read()) {
                var list = new string[csv.ColumnCount];
                for (int i = 0; i < csv.ColumnCount; i++) {
                    list[i] = csv.GetField(i);
                }
                var line = new Line(list, 0);
                var suc = parser.TryParse(line);
                //Console.WriteLine($"{suc}");
            }
        }
    }

    [Test]
    public void Test_有End() {
        var parser = new VList(
            new VComb(
                new VList(new OneCell(IsValue)),
                new VList(new OneCell(IsEnd), 1)
                )
            );

        ParseFile("a2.csv", parser);
    }

    [Test]
    public void Test_无End() {
        var parser = new VList(
            new VComb(
                new OneCell(Is1),
                new OneCell(Is2)
                )
            );
        ParseFile("a3.csv", parser);
        foreach (var ch in parser.parsers) {
            var v1 = ((ch as VComb).parsers[0] as OneCell).value;
            var v2 = ((ch as VComb).parsers[1] as OneCell).value;
            Console.WriteLine($"{v1},{v2}");
        }
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
