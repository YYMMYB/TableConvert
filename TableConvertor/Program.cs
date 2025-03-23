// See https://aka.ms/new-console-template for more information


using TableConvertor;

var dataPath = Path.Join(Environment.CurrentDirectory, "out", "data");
var codePath = Path.Join(Environment.CurrentDirectory, "out", "code");


string root;
if (args.Length == 1) {
    root = args[0];
    var proj = new Project();
    proj.Load(Global.I.root, root);
    var dg = new DataGen(Global.I.root, dataPath);
    dg.Gen(dg.rootMod, dg.rootFolder);
    var cg = new CodeGen(Global.I.root, codePath);
    cg.Gen(cg.rootMod, cg.rootFolder);
    Console.ReadLine();
}
