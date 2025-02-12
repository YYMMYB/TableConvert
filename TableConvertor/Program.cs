// See https://aka.ms/new-console-template for more information


List<int> list = [1, 2, 3, 4, 5];

foreach(var i in list[1..^0])
{
    Console.WriteLine(i);
}