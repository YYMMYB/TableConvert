// See https://aka.ms/new-console-template for more information
using LoadJson;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

var cfg = new JsonSerializerOptions() {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
cfg.Converters.Add(new JsonStringEnumConverter());

var d = A.Aaa中文Aaa中文;
var s = JsonSerializer.Serialize(d, cfg);
Console.WriteLine(s);


//[JsonConverter(typeof(JsonStringEnumConverter<A>))]
public enum A {
    XxxAaa, 
    YyyAaa,
    ZzzAaa, 
    // 这个不管用
    //[EnumMember(Value = "233aaa")]
    Aaa中文Aaa中文
}

