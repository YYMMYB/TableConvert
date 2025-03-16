using CsvHelper;
using CsvHelper.Configuration;
using Shouldly;
using System.Globalization;
using System.Text.Json.Nodes;
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

    void ParseDataFile(string file, Format format) {
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
            //Console.WriteLine(colCount);
            string[,] tableArr = new string[table.Count, colCount];
            for (int i = 0; i < table.Count; i++) {
                for (int j = 0; j < colCount; j++) {
                    tableArr[i, j] = table[i][j];
                }
            }
            format.SetParam(new Format.InitParam { table = tableArr, startColumn = 0 , calculateRange = true});
            format.Read(0, tableArr.GetLength(0));
        }
    }

    [Test]
    public void TestFormatData() {
        var fileName = "b1";
        var format = new ListFormat(
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
                }),
                new ListFormat(
                    new HFormat(
                        new SingleFormat(),
                        new ListFormat(new SingleFormat())
                    )
                )
            )
        );
        ParseDataFile(fileName + ".csv", format);
        var json = format.value.ToJson();
        using (var writer = new StreamWriter(Path.Join(PROJ_DIR, fileName + ".json"))) {
            writer.Write(json);
        }
        var res = """
[
  [
    "a1",
    [
      "b1",
      "bb1",
      "bbb1"
    ],
    "c1",
    "d1",
    [
      "x",
      [
        "x1",
        "xx1"
      ]
    ],
    [
      [
        "_",
        [
          "l1",
          "l2"
        ]
      ],
      [
        "_",
        [
          "l1"
        ]
      ]
    ]
  ],
  [
    "a2",
    [
      "b2",
      "bb2",
      "bbb2"
    ],
    "c2",
    "d2",
    [
      "y",
      [
        "y1",
        "yy1"
      ]
    ],
    [
      [
        "_",
        [
          "l1",
          "l2"
        ]
      ],
      [
        "_",
        [
          "l1"
        ]
      ],
      [
        "_",
        [
          "l1"
        ]
      ]
    ]
  ]
]
""";
        json.ToString().ShouldBe(res);
    }

    public HeadNode ParseHead(string file, int startRow, int endRow) {
        HeadNode head;
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

            head = new HeadNode(tableArr, [0, colCount]);

            head.Read(startRow, endRow);
        }
        return head;
    }

    public void ShowHead(HeadNode head, int level) {
        Console.WriteLine($"{level} {head.isVarient} {head.name}");
        var i = 0;
        var vh = "h";
        foreach (var child in head.children) {
            if (i == head.hChildrenCount) {
                vh = "v";
            }
            ShowHead(child, level + 1);
            i++;
        }
    }

    [Test]
    public void TestHead() {
        var fileName = "h1";
        var head = ParseHead(fileName + ".csv", 0, 7);
        ShowHead(head,0);
    }

    [Test]
    public void TestValueEq() {
        RawValue v1 = new ListRawValue([
            new LiteralRawValue("a"),
            new LiteralRawValue("a"),
            new LiteralRawValue("a"),
            ]);

        ListRawValue v2 = new ListRawValue([
            new LiteralRawValue("a"),
            new LiteralRawValue("a"),
            new LiteralRawValue("a"),
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
