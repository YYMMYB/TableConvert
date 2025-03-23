using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace TableConvertor;

public class Table : Module {

    public Table(string name) : base(name) { }

    public string[,] tableArr;
    public int[] headRange = [-1,-1];
    public int[] valueRange = [-1,-1];

    public int[] ColRange => [1, tableArr.GetLength(1)];

    public RawHead rawHead;
    public Head head;
    public Format format;
    public RawValue rawValue;

    //public string thisname;
    //public override string Name => thisname;

    //public override string FullName => ParentMod.CulcFullName(Name);

    //Module mod;
    //public override Module ParentMod { get => mod; set => mod = value; }

    public static Table CreateByCsv(string path) {
        var name = Path.GetFileNameWithoutExtension(path);
        name = StringUtil.TableName(name);
        var table = new Table(name);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = false
        };
        using (var f = new StreamReader(path))
        using (var csv = new CsvReader(f, config)) {
            List<string[]> tableList = [];
            int colCount = -1;
            while (csv.Read()) {
                colCount = int.Max(colCount, csv.ColumnCount);
                var list = new string[colCount];
                for (int i = 0; i < colCount; i++) {
                    list[i] = csv.GetField(i);
                }
                tableList.Add(list);
                //Console.WriteLine($"{suc}");
            }
            //Console.WriteLine(colCount);
            table.tableArr = new string[tableList.Count, colCount];
            for (int i = 0; i < tableList.Count; i++) {
                for (int j = 0; j < colCount; j++) {
                    table.tableArr[i, j] = tableList[i][j];
                }
            }
        }
        table.ParseRange(0);
        return table;
    }

    public void ParseRange(int col) {
        string[] defaultOrder = [StringUtil.TableHeadPartName, StringUtil.TableValuePartName];
        bool canDefault = true;
        int defaultIndex = 0;
        for (int i = 0; i < tableArr.GetLength(0); i++) {
            var s = tableArr[i, col].Trim();
            if (StringUtil.IsEmptyString(s)) {
                continue;
            }

            if (canDefault && s == StringUtil.KeywordPrefix) {
                s = defaultOrder[defaultIndex];
                defaultIndex += 1;
            } else {
                canDefault = false;
            }

            if (s == StringUtil.TableHeadPartName) {
                headRange[0] = i;
            } else if (s == StringUtil.TableValuePartName) {
                headRange[1] = i;
                valueRange[0] = i;
                valueRange[1] = tableArr.GetLength(0);
                break;
            } else {
                throw new Exception();
            }
        }
    }

    public void LoadRawHead() {
        rawHead = new RawHead(tableArr, headRange, ColRange);
        rawHead.Read();
    }

    public void LoadHead() {
        head = Head.Create(this, null, rawHead);
    }

    public void LoadType() {
        head.CreateType(null);
    }

    public void LoadFormat() {
        format = head.CreateFormat();
        format.SetParam(new Format.InitParam { table = tableArr });
    }

    public void LoadRawValue() {
        format.Read(valueRange[0], valueRange[1]);
        rawValue = format.value;
    }
}