using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using TableConvertor;

namespace Test;

public class Tests {
    public Format t1;
    public Format t2;

    public static string PROJ_DIR = "D:\\Project\\TableConvertor\\Test";

    [SetUp]
    public void SetupT1() {
        Directory.SetCurrentDirectory(PROJ_DIR);

        var a = new VList();
        a.template = new OneCell();
        a.size = 1;
        a.Reset();

        var b = new VTuple();
        b.children = [new OneCell(), new OneCell()];
        b.size = 1;
        b.Reset();

        var c = new HTuple();
        c.children = [a, b];
        c.size = 2;
        c.Reset();

        var d = new HList();
        d.template = c;
        d.size = 4;
        d.Reset();

        t1 = new VList();
        t1.template = d;
        t1.size = 4;
        t1.Reset();
    }

    [SetUp]
    public void SetupT2() {
        var a = new HTuple();
        a.children = [new OneCell(), new OneCell()];
        a.size = 2;
        a.Reset();

        var b = new HList();
        b.template = a;
        b.size = 7;
        b.Reset();

        var c = new HTuple();
        c.children = [b, new OneCell()];
        c.size = 8;
        c.Reset();

        var d = new VList();
        d.template = c;
        d.size = c.size;
        d.Reset();

        t2 = d;
    }

    public void TestFile(Format f, String path) {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = false
        };
        var row = 0;
        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config)) {
            while (csv.Read()) {
                var line = new Line(csv.ColumnCount);
                for (int i = 0; i < csv.ColumnCount; i++) {
                    var s = csv.GetField(i);
                    line.cells[i] = CellData.FromString(s);
                }

                f.ReadLine(line);
                var v = f.RawCollect();
                Console.WriteLine($"{row}: {f.Valid}: {v}");

                row += 1;
            }
        }

        //var v = root.RawCollect();
        //Console.WriteLine(v);
    }

    [Test]
    public void Test1() {
        TestFile(t1, "Test1.csv");
    }

    [Test]
    public void Test2() {
        TestFile(t2, "Test2.csv");
    }
}
