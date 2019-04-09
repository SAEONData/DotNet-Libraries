using DocumentFormat.OpenXml.Packaging;
using SAEON.OpenXML;
using System;
using System.Text.RegularExpressions;

namespace SAEON.OpenXML.ConsoleTests
{
    class Program
    {
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
                var programmes = definedNames["ProgrammeNames"];
                Console.WriteLine(programmes);
                var (sheetName, colLeft, rowTop, colRight, rowBottom) = ExcelHelper.SplitRange(programmes);
                Console.WriteLine($"{nameof(sheetName)}: {sheetName} {nameof(colLeft)}: {colLeft} {nameof(rowTop)}: {rowTop} {nameof(colRight)}: {colRight} {nameof(rowBottom)}: {rowBottom}");
                var rangeValues = ExcelHelper.GetRangeValues(doc, programmes);
                Console.WriteLine($"Rows: {rangeValues.GetUpperBound(0)+1} Cols: {rangeValues.GetUpperBound(1)+1}");
                for (var r = 0; r <= rangeValues.GetUpperBound(0); r++)
                {
                    Console.Write($"R: {r}");
                    for (var c = 0; c <= rangeValues.GetUpperBound(1); c++)
                    {
                        Console.Write($" C: {c} {rangeValues[r, c]}");
                    }
                    Console.WriteLine();
                }
                var programmeList = ExcelHelper.GetRangeValues(doc, "Projects!J3:K102");
                for (var r = 0; r <programmeList.GetUpperBound(0); r++)
                {
                    Console.Write($"R: {r}");
                    for (var c = 0; c <= programmeList.GetUpperBound(1); c++)
                    {
                        Console.Write($" C: {c} {programmeList[r, c]}");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
