using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LoadJson;

[JsonDerivedType(typeof(DataDD1), typeDiscriminator: "dd1")]
[JsonDerivedType(typeof(DataDD2), typeDiscriminator: "dd2")]
class DataBase {
}

class DataD : DataBase { }
class DataDD1 : DataD { }
class DataDD2 : DataD { }


