# 简介

把 一系列csv文件变成 json 和 c#.

- 会自动根据表头生成c#的类型声明.
- 会根据目录结构生成命名空间.

表头支持丰富的写法:

- 多层嵌套map和list
- 自定义类型
- 多层继承
- enum
- 当然还有基本类型, int 这些

应该是所有类型都可以处理.
更详细的格式说明看下面的 [这一节](#表格格式) .

# 缺陷

目前没有校验.

如果你输入的是错的, 也可能成功产生错误的结果, 或者死循环, 或者任何行为. 所以你必须保证输入的格式正确.
(当然了, 小游戏其实不用这么在意, 而且一般应该也都会报错, 主要是我没有特别处理, 主要是这个处理起来比较难, 很久之后才会加, 我要先开始做游戏)

# 使用方式

将csv文件夹 拖动到 exe 上, 就能生成个 out 目录

案例:
把 Test/testProj 拖动到生成的 .exe 上(在bin\Release\net9.0里(Release也可能是Debug, 都行)), 就会自动生成 out 文件夹, 里面是 c# 代码和 json 数据. c# 代码拷贝到随便一个工程里应该就可以运行.

# 表格格式

## 总览

emmm 有点懒得写, 有空再写.
看看 testProj 里的 "综合.csv" 吧, 不会的直接提issue问吧.

# Godot 使用指南

终端调用:
```
TableConvertor.exe的路径 csv的文件夹路径 代码和json的父文件夹路径
```

比如我自己在用的bat脚本:
```
"%~dp0/TableConvertor/TableConvertor.exe" "%~dp0/../data/tables" "%~dp0/../balatroxx/gen/cfg"
```
(注:里面`%~dp0`是 .bat 文件所在的目录)

- ../data/tables 是我csv的路径
- ../balatroxx/ 是我的godot工程
- ../balatroxx/gen/cfg 是生成的 代码和json

最终会生成2个文件夹:
- ../balatroxx/gen/cfg/code
- ../balatroxx/gen/cfg/data

把下面的代码复制到一个文件里.
其他的地方用 `Cfg.Tables` 访问生成的表.

```cs
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileAccess = Godot.FileAccess;

public class Cfg {
    public static Cfg I;
    public static cfg.Tables Tables => I.tb;

    static Cfg() {
        I = new Cfg();
        var access = new DataAccess();
        // 这里的路径换成你的路径
        var path = new DataPath("res://gen/cfg/data");
        I.tb = cfg.Tables.load(access, path);
    }

    public cfg.Tables tb;
}

public class DataAccess : IDataAccess {
    public System.IO.Stream GetString(IDataPath path) {
        var p = (DataPath)path;
        GD.Print(p.path);
        var f = FileAccess.Open(p.path, FileAccess.ModeFlags.Read);
        var s = f.GetAsText();
        byte[] byteArray = Encoding.UTF8.GetBytes(s);
        MemoryStream stream = new MemoryStream(byteArray);
        return stream;
    }

    public IDataPath JoinPath(IDataPath path, string item) {
        return (path as DataPath).Join(item);
    }
}

public record class DataPath : IDataPath {
    public string path;

    public DataPath(string p) { path = p; }

    public DataPath Join(string s) {
        var npath = Path.Join(path, s);
        return new DataPath(npath);
    }
}
```

