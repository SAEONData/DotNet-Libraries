using DocumentFormat.OpenXml.Packaging;
using System;

namespace SAEON.OpenXML.ConsoleTests
{
    class Program
    {
        private static void Dump(object[,] array, bool showTypes = false)
        {
            Console.WriteLine($"Rows: {array.GetUpperBound(0) + 1} Cols: {array.GetUpperBound(1) + 1}");
            for (var r = 0; r <= array.GetUpperBound(0); r++)
            {
                Console.Write($"R: {r}");
                for (var c = 0; c <= array.GetUpperBound(1); c++)
                {
                    Console.Write($" {c}={array[r, c]}");
                    if (showTypes) Console.Write($" {array[r, c].GetType().Name}");
                }
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(@"G:\My Drive\Elwandle\Node Drive\Data Store\Observations\Observations Database Setup Template.xlsx", false))
            {
                ExcelHelper.Validate(doc);
                var definedNames = ExcelHelper.GetDefinedNames(doc);
                foreach (var kv in definedNames)
                {
                    Console.WriteLine($"Key: {kv.Key} Value: {kv.Value}");
                }
                // Programmes
                var programmes = "Programmes!A3:F102";
                Console.WriteLine(programmes);
                var (sheetName, colLeft, rowTop, colRight, rowBottom) = ExcelHelper.SplitRange(programmes);
                Console.WriteLine($"{nameof(sheetName)}: {sheetName} {nameof(colLeft)}: {colLeft} {nameof(rowTop)}: {rowTop} {nameof(colRight)}: {colRight} {nameof(rowBottom)}: {rowBottom}");
                Dump(ExcelHelper.GetRangeValues(doc, programmes));
                Dump(ExcelHelper.GetRangeValues(doc, "Programmes!H3:I102"));
                Dump(ExcelHelper.GetRangeValues(doc, "Stations!G3:O102"), true);
            }
        }
    }
}
