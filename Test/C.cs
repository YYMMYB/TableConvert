using CsvHelper.Configuration;
using System.Globalization;

namespace Test;

public static class C {
    public static string PROJ_DIR = "D:\\Project\\TableConvertor\\Test";
    public static CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture) {
        HasHeaderRecord = false
    };
    public static string GetPath(string localPath) {
        return Path.Join(PROJ_DIR, localPath);
    }
}
