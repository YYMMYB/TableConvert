using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableConvertor;

namespace Test;
public class ProjTest {
    [Test]
    public void TProjLoad() {
        var proj = new Project();
        proj.Load(Global.I.root, C.GetPath("testProj"));
    }
}
