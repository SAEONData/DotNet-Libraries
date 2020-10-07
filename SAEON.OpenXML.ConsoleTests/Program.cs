using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAEON.OpenXML.ConsoleTests
{
    public class Test
    {
        public int ANumber { get; set; }
        public DateTime ADate { get; set; } = DateTime.Now;
        public bool ABool { get; set; }
        public string AString { get; set; }
    }
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
            //using (SpreadsheetDocument doc = SpreadsheetDocument.Open(@"G:\My Drive\Elwandle\Node Drive\Data Store\Observations\Observations Database Setup Template.xlsx", false))
            //{
            //    ExcelHelper.Validate(doc);
            //    var definedNames = ExcelHelper.GetDefinedNames(doc);
            //    foreach (var kv in definedNames)
            //    {
            //        Console.WriteLine($"Key: {kv.Key} Value: {kv.Value}");
            //    }
            //    // Programmes
            //    var programmes = "Programmes!A3:F102";
            //    Console.WriteLine(programmes);
            //    var (sheetName, colLeft, rowTop, colRight, rowBottom) = ExcelHelper.SplitRange(programmes);
            //    Console.WriteLine($"{nameof(sheetName)}: {sheetName} {nameof(colLeft)}: {colLeft} {nameof(rowTop)}: {rowTop} {nameof(colRight)}: {colRight} {nameof(rowBottom)}: {rowBottom}");
            //    Dump(ExcelHelper.GetRangeValues(doc, programmes));
            //    Dump(ExcelHelper.GetRangeValues(doc, "Programmes!H3:I102"));
            //    Dump(ExcelHelper.GetRangeValues(doc, "Stations!G3:O102"), true);
            //}

            using (SAEONLogs.MethodCall(typeof(Program)))
            {
                try
                {
                    SAEONLogs.Information("Deleting spreadsheet if exists");
                    if (File.Exists("LargeFile.xlsx")) File.Delete("LargeFile.xlsx");
                    SAEONLogs.Information("Creating spreadsheet");
                    //using (ExcelSaxHelper.CreateSpreadsheet("LargeFile.xlsx", CreateList()))
                    //{

                    //}
                    CreateSpreadsheet();
                    SAEONLogs.Information("Done");

                }
                catch (Exception ex)
                {
                    SAEONLogs.Exception(ex);
                    throw;
                }
            }

            List<Test> CreateList()
            {
                var result = new List<Test>();
                result.Add(new Test { ANumber = 1, AString = "Test" });
                return result;
            }

            void CreateSpreadsheet()
            {
                using (SpreadsheetDocument xl = SpreadsheetDocument.Create("LargeFile.xlsx", SpreadsheetDocumentType.Workbook))
                {
                    List<OpenXmlAttribute> oxa;
                    OpenXmlWriter oxw;

                    xl.AddWorkbookPart();
                    WorksheetPart wsp = xl.WorkbookPart.AddNewPart<WorksheetPart>();

                    oxw = OpenXmlWriter.Create(wsp);
                    oxw.WriteStartElement(new Worksheet());
                    oxw.WriteStartElement(new SheetData());

                    //for (int i = 1; i <= 50000; ++i)
                    for (int i = 1; i <= 50; ++i)
                    {
                        oxa = new List<OpenXmlAttribute>();
                        // this is the row index
                        oxa.Add(new OpenXmlAttribute("r", null, i.ToString()));

                        oxw.WriteStartElement(new Row(), oxa);

                        //for (int j = 1; j <= 100; ++j)
                        for (int j = 1; j <= 10; ++j)
                        {
                            oxa = new List<OpenXmlAttribute>();
                            // this is the data type ("t"), with CellValues.String ("str")
                            oxa.Add(new OpenXmlAttribute("t", null, "str"));

                            // it's suggested you also have the cell reference, but
                            // you'll have to calculate the correct cell reference yourself.
                            // Here's an example:
                            //oxa.Add(new OpenXmlAttribute("r", null, "A1"));

                            oxw.WriteStartElement(new Cell(), oxa);

                            oxw.WriteElement(new CellValue(string.Format("R{0}C{1}", i, j)));

                            // this is for Cell
                            oxw.WriteEndElement();
                        }

                        // this is for Row
                        oxw.WriteEndElement();
                    }

                    // this is for SheetData
                    oxw.WriteEndElement();
                    // this is for Worksheet
                    oxw.WriteEndElement();
                    oxw.Close();

                    oxw = OpenXmlWriter.Create(xl.WorkbookPart);
                    oxw.WriteStartElement(new Workbook());
                    oxw.WriteStartElement(new Sheets());

                    // you can use object initialisers like this only when the properties
                    // are actual properties. SDK classes sometimes have property-like properties
                    // but are actually classes. For example, the Cell class has the CellValue
                    // "property" but is actually a child class internally.
                    // If the properties correspond to actual XML attributes, then you're fine.
                    oxw.WriteElement(new Sheet()
                    {
                        Name = "Sheet1",
                        SheetId = 1,
                        Id = xl.WorkbookPart.GetIdOfPart(wsp)
                    });

                    // this is for Sheets
                    oxw.WriteEndElement();
                    // this is for Workbook
                    oxw.WriteEndElement();
                    oxw.Close();

                    xl.Close();
                }
            }

        }

    }
}
