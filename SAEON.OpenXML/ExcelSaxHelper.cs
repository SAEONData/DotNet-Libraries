using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SAEON.Core;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SAEON.OpenXML
{
    public static class ExcelSaxHelper
    {
        public static bool UseSharedStrings { get; set; } = true;

        private static Dictionary<string, int> sharedStrings = new Dictionary<string, int>();
        private static int sharedStringsIndex;

        private static void WriteCellValue(OpenXmlWriter writer, object value, List<OpenXmlAttribute> attributes = null)
        {
            if (value is string aString)
            {
                if (attributes == null)
                {
                    attributes = new List<OpenXmlAttribute>();
                }
                if (!UseSharedStrings)
                {
                    attributes.Add(new OpenXmlAttribute("t", null, "inlineStr"));
                    writer.WriteStartElement(new Cell(), attributes);
                    writer.WriteElement(new InlineString(new Text(aString)));
                    writer.WriteEndElement();
                }
                else
                {
                    attributes.Add(new OpenXmlAttribute("t", null, "s"));
                    writer.WriteStartElement(new Cell(), attributes);
                    if (!sharedStrings.ContainsKey(aString))
                    {
                        sharedStrings.Add(aString, sharedStringsIndex++);
                    }
                    writer.WriteElement(new CellValue(sharedStrings[aString].ToString()));
                    writer.WriteEndElement();
                }
            }
            else if (((value is int?) && ((int?)value).HasValue) ||
                     ((value is long?) && ((long?)value).HasValue) ||
                     ((value is double?) && ((double?)value).HasValue) ||
                     ((value is float?) && ((float?)value).HasValue) ||
                     ((value is decimal?) && ((decimal?)value).HasValue))
            {
                if (attributes == null)
                {
                    writer.WriteStartElement(new Cell() { DataType = CellValues.Number });
                }
                else
                {
                    writer.WriteStartElement(new Cell() { DataType = CellValues.Number }, attributes);
                }
                writer.WriteElement(new CellValue(value.ToString()));
                writer.WriteEndElement();
            }
            else if (value is bool aBool)
            {
                if (attributes == null)
                {
                    attributes = new List<OpenXmlAttribute>();
                }
                attributes.Add(new OpenXmlAttribute("t", null, "b"));
                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new CellValue(aBool ? "1" : "0"));
                writer.WriteEndElement();
            }
            else if (value is DateTime aDateTime)
            {
                if (attributes == null)
                {
                    attributes = new List<OpenXmlAttribute>();
                }
                attributes.Add(new OpenXmlAttribute("s", null, "1"));
                writer.WriteStartElement(new Cell() { DataType = CellValues.Number }, attributes);
                writer.WriteElement(new CellValue(aDateTime.ToOADate().ToString()));
                writer.WriteEndElement();
            }
            else if ((value is DateTime?) && ((DateTime?)value).HasValue)
            {
                if (attributes == null)
                {
                    attributes = new List<OpenXmlAttribute>();
                }
                attributes.Add(new OpenXmlAttribute("s", null, "1"));
                writer.WriteStartElement(new Cell() { DataType = CellValues.Number }, attributes);
                writer.WriteElement(new CellValue(((DateTime?)value).Value.ToOADate().ToString()));
                writer.WriteEndElement();
            }
            else
            {
                if (attributes == null)
                {
                    writer.WriteStartElement(new Cell() { DataType = CellValues.Number });
                }
                else
                {
                    writer.WriteStartElement(new Cell() { DataType = CellValues.Number }, attributes);
                }
                writer.WriteElement(new CellValue(value.ToString()));
                writer.WriteEndElement();
            }
        }

        private static SpreadsheetDocument CreateSpreadsheetDocument<T>(SpreadsheetDocument document, List<T> list) where T : class
        {
            using (SAEONLogs.MethodCall(typeof(SpreadsheetDocument)))
            {
                try
                {
                    // Add a WorkbookPart to the document.
                    var workbookPart = document.AddWorkbookPart();

                    // Add default styles
                    var workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    var style = workbookStylesPart.Stylesheet = ExcelHelper.CreateStylesheet();
                    style.Save();

                    var workbook = workbookPart.Workbook = new Workbook();
                    var sheets = workbook.AppendChild<Sheets>(new Sheets());

                    // create worksheet 1
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
                    sheets.Append(sheet);

                    sharedStrings = new Dictionary<string, int>();
                    sharedStringsIndex = 0;

                    using (var writer = OpenXmlWriter.Create(worksheetPart))
                    {
                        writer.WriteStartElement(new Worksheet());
                        writer.WriteStartElement(new SheetData());

                        // Header
                        writer.WriteStartElement(new Row());
                        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                        foreach (var prop in props)
                        {
                            WriteCellValue(writer, prop.Name);
                        }
                        writer.WriteEndElement(); //end of Row tag

                        var sw = new Stopwatch();
                        sw.Start();
                        var last = new TimeSpan();
                        var lastLog = new TimeSpan(last.Ticks);
                        int r = 1;
                        int rMax = list.Count;
                        foreach (var item in list)
                        {
                            var rPerc = 1.0 * r / rMax;
                            var elapsed = sw.Elapsed;
                            var timeMax = new TimeSpan((long)(elapsed.Ticks / rPerc));
                            if ((elapsed - lastLog).TotalSeconds >= 60)
                            {
                                SAEONLogs.Information("Row {0} of {1}, {2:F2}% complete, in {3}, {4} of {5}, {6} left", r, rMax, rPerc * 100.0, (elapsed - last).TimeStr(), elapsed.TimeStr(), timeMax, (timeMax - elapsed).TimeStr());
                                lastLog = elapsed;
                            }
                            last = elapsed;
                            r++;
                            writer.WriteStartElement(new Row());
                            foreach (var prop in props)
                            {
                                WriteCellValue(writer, prop.GetValue(item));
                            }
                            writer.WriteEndElement(); //end of Row tag
                        }

                        writer.WriteEndElement(); //end of SheetData
                        writer.WriteEndElement(); //end of worksheet
                        writer.Close();
                        SAEONLogs.Information("Processed {0} rows in {1}", rMax, sw.Elapsed.TimeStr());
                    }
                    if (sharedStrings.Any())
                    {
                        var sharedStringPart = workbookPart.AddNewPart<SharedStringTablePart>();
                        using (var writer = OpenXmlWriter.Create(sharedStringPart))
                        {
                            writer.WriteStartElement(new SharedStringTable());
                            foreach (var item in sharedStrings)
                            {
                                writer.WriteStartElement(new SharedStringItem());
                                writer.WriteElement(new Text(item.Key));
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                        }
                    }
                    return document;
                }
                catch (Exception ex)
                {
                    SAEONLogs.Exception(ex);
                    throw;
                }
            }
        }

        public static SpreadsheetDocument CreateSpreadsheet<T>(string fileName, List<T> list) where T : class
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            return CreateSpreadsheetDocument(SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook), list);
        }

    }
}