

using Dumpify;

DumpConfig.Default.MembersConfig.IncludeFields = true;

var t = cfg.Tables.load(@"D:\Project\TableConvertor\GenTest\data\");

t.简单.Dump();

