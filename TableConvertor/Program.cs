// See https://aka.ms/new-console-template for more information


using TableConvertor;

string outPath;

string dataPath;
string codePath;

string projPath;



if (args.Length == 1) {
    projPath = args[0];
    outPath = Path.Join(Environment.CurrentDirectory, "out");
} else if (args.Length == 2) {
    projPath = args[0];
    outPath = args[1];
} else {
    projPath = @"D:\Project\gd_balatroxx3\data\tables";
    outPath = @"D:\Project\TableConvertor\GenTest\";
}

dataPath = Path.Join(outPath, "data");
codePath = Path.Join(outPath, "code");

if (Directory.Exists(dataPath))
    Directory.Delete(dataPath, true);
if (Directory.Exists(codePath))
    Directory.Delete(codePath, true);

var proj = new Project();
proj.Load(Global.I.root, projPath);
var dg = new DataGen(Global.I.root, dataPath);
dg.Gen(dg.rootMod, dg.rootFolder);
var cg = new CodeGen(Global.I.root, codePath);
cg.Gen();

