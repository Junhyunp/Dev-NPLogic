#r "nuget: ClosedXML, 0.102.3"

using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;

var files = Directory.GetFiles(@"C:\Users\pwm89\dev\nplogic\manager\client", "test_*.xlsx");
var testFile = files.First();
Console.WriteLine($"파일: {testFile}");

using var wb = new XLWorkbook(testFile);
var ws = wb.Worksheet(1);

Console.WriteLine("\n=== 헤더 (1행) ===");
for (int c = 1; c <= 15; c++)
{
    var v = ws.Cell(1, c).GetString();
    if (!string.IsNullOrEmpty(v))
        Console.WriteLine($"Col {c}: {v}");
}

Console.WriteLine("\n=== 데이터 (2행) ===");
for (int c = 1; c <= 15; c++)
{
    var v = ws.Cell(2, c).GetString();
    if (!string.IsNullOrEmpty(v))
        Console.WriteLine($"Col {c}: {v}");
}

Console.WriteLine("\n=== 데이터 (3행) ===");
for (int c = 1; c <= 15; c++)
{
    var v = ws.Cell(3, c).GetString();
    if (!string.IsNullOrEmpty(v))
        Console.WriteLine($"Col {c}: {v}");
}

