using CsvHelper;
using Shouldly;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using TableConvertor;

namespace Test;

public class TableTest {
    [SetUp]
    public void Setup() {
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

    string[,] LoadTable(string file) {
        string[,] tableArr;
        using (var reader = new StreamReader(Path.Join(C.PROJ_DIR, file)))
        using (var csv = new CsvReader(reader, C.config)) {
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
            tableArr = new string[table.Count, colCount];
            for (int i = 0; i < table.Count; i++) {
                for (int j = 0; j < colCount; j++) {
                    tableArr[i, j] = table[i][j];
                }
            }
        }
        return tableArr;
    }

    void ParseDataFile(string file, Format format) {
        var tableArr = LoadTable(file);
        format.SetParam(new Format.InitParam { table = tableArr, startColumn = 0, calculateRange = true });
        format.Read(0, tableArr.GetLength(0));
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
        using (var writer = new StreamWriter(Path.Join(C.PROJ_DIR, fileName + ".json"))) {
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

    public RawHead ParseRawHead(string file, int startRow, int endRow) {
        var tableArr = LoadTable(file);
        var head = new RawHead(tableArr, [startRow, endRow], [0, tableArr.GetLength(1)]);
        head.Read();
        return head;
    }

    public void ShowRawHead(RawHead head, int level) {
        Console.WriteLine($"{level} {head.isVertical} {head.content}");
        var i = 0;
        var vh = "h";
        foreach (var child in head.children) {
            if (i == head.horizontalCount) {
                vh = "v";
            }
            ShowRawHead(child, level + 1);
            i++;
        }
    }

    [Test]
    public void TestRawHead() {
        var fileName = "h1";
        var head = ParseRawHead(fileName + ".csv", 0, 7);
        ShowRawHead(head, 0);
    }

    public void ShowHead(Head head, int level) {
        Console.WriteLine($"{level} {head.name} {head.typeName} {head.GetType()}");
        if (head is ListHead lh) {
            if (lh.keyHead != null) {
                ShowHead(lh.keyHead, level + 1);
            }
            ShowHead(lh.valueHead, level + 1);
        } else if (head is ObjectHead oh) {
            foreach (var h in oh.fields) {
                ShowHead(h, level + 1);
            }
            foreach (var (k, h) in oh.deriveds) {
                Console.Write($"{k} ");
                ShowHead(h, level + 1);
            }
        }
    }

    RawHead F1() {
        var fileName = "h1";
        var rawHead = ParseRawHead(fileName + ".csv", 0, 8);
        return rawHead;
    }

    [Test]
    public void TestHead() {
        var rawHead = F1();
        var head = Head.Create(null, null, rawHead);

        ShowHead(head, 0);
    }

    [Test]
    public void TestHeadCreateFormat() {
        var rawHead = F1();
        var head = Head.Create(null, null, rawHead);
        var format = head.CreateFormat();
        format.SetParam(new Format.InitParam { calculateRange = false, table = rawHead.table });
        format.Read(9, 24);
        var rawValue = format.value.ToJson();
        Console.WriteLine(rawValue);
    }

    [Test]
    public void TestHeadRead() {
        var rawHead = F1();
        var head = Head.Create(null, null, rawHead);
        var format = head.CreateFormat();
        format.SetParam(new Format.InitParam { calculateRange = false, table = rawHead.table });
        format.Read(9, 29);
        var rawValue = format.value;
        var json = head.Read(rawValue);
        var ser = JsonSerializer.Serialize(json, StringUtil.JsonOpt);
        using (var w = new StreamWriter(Path.Join(C.PROJ_DIR, "ttt.json"))) {
            w.Write(ser);
        }
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
    class D {
        public string S { get; set; }
    }
    [Test]
    public void TTT() {
        var s = "\"";

        var ser = JsonSerializer.Serialize(s, StringUtil.JsonOpt);
        using (var w = new StreamWriter(Path.Join(C.PROJ_DIR, "ttt.json"))) {
            w.Write(ser);
        }
        Console.WriteLine(ser);
    }
}
