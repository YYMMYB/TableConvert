// See https://aka.ms/new-console-template for more information
using LoadJson;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;


var d = "\"";
var s = JsonSerializer.Serialize(d);
Console.WriteLine(s);
