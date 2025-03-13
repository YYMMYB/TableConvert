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

    void ParseFile(string file, Format parser) {
        using (var reader = new StreamReader(Path.Join(PROJ_DIR, file)))
        using (var csv = new CsvReader(reader, config)) {
            List<string[]> table = [];
            int colCount = -1;
            while (csv.Read()) {
                colCount = csv.ColumnCount;
                var list = new string[colCount];
                for (int i = 0; i < colCount; i++) {
                    list[i] = csv.GetField(i);
                }
                table.Add(list);
                //Console.WriteLine($"{suc}");
            }
            Console.WriteLine(colCount);
            string[,] tableArr = new string[table.Count, colCount];
            for (int i = 0; i < table.Count; i++) {
                for (int j = 0; j < colCount; j++) {
                    tableArr[i, j] = table[i][j];
                }
            }
            parser.SetParam(new InitParam { table = tableArr, startColumn = 0 });
            parser.Read(0, tableArr.GetLength(0));
        }
    }
    [Test]
    public void Test() {
        var parser = new ListFormat(
            new HFormat(
                new SingleFormat(),
                new ListFormat(
                    new SingleFormat()
                    ),
                new SingleFormat(),
                new SingleFormat(),
                new SwitchFormat(new Dictionary<string, Format> {
                    ["x"] = new ListFormat(new SingleFormat()),
                    ["y"] = new HFormat(new SingleFormat(), new SingleFormat())
                })
            )
        );
        ParseFile("b1.csv", parser);
        var json = parser.value.ToJson();
        Console.WriteLine(json);
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
