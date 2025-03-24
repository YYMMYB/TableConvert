// See https://aka.ms/new-console-template for more information


using TableConvertor;

string outPath;

string dataPath;
string codePath;


string root;
if (args.Length == 1) {
    root = args[0];
    outPath = Path.Join(Environment.CurrentDirectory, "out");
} else if (args.Length == 2) {
    root = args[0];
    outPath = args[1];
} else {
    root = @"D:\Project\TableConvertor\Test\testProj\";
    outPath = @"D:\Project\TableConvertor\GenTest\";
}

dataPath = Path.Join(outPath, "data");
codePath = Path.Join(outPath, "code");

if (Directory.Exists(dataPath))
    Directory.Delete(dataPath, true);
if (Directory.Exists(codePath))
    Directory.Delete(codePath, true);

var proj = new Project();
proj.Load(Global.I.root, root);
var dg = new DataGen(Global.I.root, dataPath);
dg.Gen(dg.rootMod, dg.rootFolder);
var cg = new CodeGen(Global.I.root, codePath);
cg.Gen();
Console.ReadLine();