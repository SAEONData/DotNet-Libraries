using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SAEON.Core;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Data;
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

        private static void WriteCellValue(OpenXmlWriter writer, object value, int row, int col)
        {
            if (value == null) return;
            var attributes = new List<OpenXmlAttribute>
            {
                new OpenXmlAttribute("r", null, $"{ExcelHelper.GetColumnName(col)}{row}")
            };
            if (value is string aString)
            {
                WriteString(aString);
            }
            else if ((value is int) || value is long || (value is double) || (value is float) || (value is decimal))
            {
                WriteNumber(value);
            }
            else if (((value is int?) && ((int?)value).HasValue) ||
                     ((value is long?) && ((long?)value).HasValue) ||
                     ((value is double?) && ((double?)value).HasValue) ||
                     ((value is float?) && ((float?)value).HasValue) ||
                     ((value is decimal?) && ((decimal?)value).HasValue))
            {
                WriteNumber(value);
            }
            else if (value is bool aBool)
            {
                WriteBoolean(aBool);
            }
            else if (value is DateTime aDateTime)
            {
                WriteDateTime(aDateTime);
            }
            else if ((value is DateTime?) && ((DateTime?)value).HasValue)
            {
                WriteDateTime(((DateTime?)value).Value);
            }
            else if (value is DateTimeOffset aDateTimeOffset)
            {
                WriteDateTimeOffset(aDateTimeOffset);
            }
            else if ((value is DateTimeOffset?) && ((DateTimeOffset?)value).HasValue)
            {
                WriteDateTimeOffset(((DateTimeOffset?)value).Value);
            }
            else if (value is Enum aEnum)
            {
                WriteString(aEnum.ToString());
            }
            else // Ignore unknown types
            {
                WriteString($"Unknown type {value.GetType().Name} {value}");
            }

            void WriteString(string aValue, bool forceInline = false)
            {
                if (!UseSharedStrings || forceInline)
                {
                    attributes.Add(new OpenXmlAttribute("t", null, "inlineStr"));
                    writer.WriteStartElement(new Cell(), attributes);
                    writer.WriteElement(new InlineString(new Text(aValue)));
                    writer.WriteEndElement();
                }
                else
                {
                    attributes.Add(new OpenXmlAttribute("t", null, "s"));
                    writer.WriteStartElement(new Cell(), attributes);
                    if (!sharedStrings.ContainsKey(aValue))
                    {
                        sharedStrings.Add(aValue, sharedStringsIndex++);
                    }
                    writer.WriteElement(new CellValue($"{sharedStrings[aValue]}"));
                    writer.WriteEndElement();
                }
            }

            void WriteNumber(object aValue)
            {
                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new CellValue(aValue.ToString()));
                writer.WriteEndElement();
            }

            void WriteBoolean(bool aValue)
            {
                attributes.Add(new OpenXmlAttribute("t", null, "b"));
                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new CellValue(aValue ? "1" : "0"));
                writer.WriteEndElement();
            }

            void WriteDateTime(DateTime aValue)
            {
                attributes.Add(new OpenXmlAttribute("s", null, "1"));
                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new CellValue(aValue.ToOADate().ToString()));
                writer.WriteEndElement();
            }

            void WriteDateTimeOffset(DateTimeOffset aValue)
            {
                attributes.Add(new OpenXmlAttribute("s", null, "1"));
                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new CellValue(aValue.UtcDateTime.ToOADate().ToString()));
                writer.WriteEndElement();
            }
        }

        private static void WriteRowStart(OpenXmlWriter writer, int row)
        {
            writer.WriteStartElement(new Row(), new List<OpenXmlAttribute> { new OpenXmlAttribute("r", null, row.ToString()) });
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
                        var sw = new Stopwatch();
                        sw.Start();
                        var last = new TimeSpan();
                        var lastLog = last;

                        writer.WriteStartElement(new Worksheet());
                        writer.WriteStartElement(new SheetData());

                        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

                        int r = 1;
                        int rMax = list.Count + 1;
                        WriteRowStart(writer, r);
                        int c = 1;
                        foreach (var prop in props)
                        {
                            WriteCellValue(writer, prop.Name, r, c++);
                        }
                        writer.WriteEndElement(); //end of Row tag

                        foreach (var item in list)
                        {
                            r++;
                            var rPerc = 1.0 * r / rMax;
                            var elapsed = sw.Elapsed;
                            var timeMax = new TimeSpan((long)(elapsed.Ticks / rPerc));
                            if ((elapsed - lastLog).TotalSeconds >= 60)
                            {
                                SAEONLogs.Information("Row {0} of {1}, {2:F2}% complete, in {3}, {4} of {5}, {6} left", r, rMax, rPerc * 100.0, (elapsed - last).TimeStr(), elapsed.TimeStr(), timeMax, (timeMax - elapsed).TimeStr());
                                lastLog = elapsed;
                            }
                            last = elapsed;

                            WriteRowStart(writer, r);
                            c = 1;
                            foreach (var prop in props)
                            {
                                WriteCellValue(writer, prop.GetValue(item), r, c++);
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

        private static SpreadsheetDocument CreateSpreadsheetDocument(SpreadsheetDocument document, DataTable table)
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
                        var sw = new Stopwatch();
                        sw.Start();
                        var last = new TimeSpan();
                        var lastLog = last;

                        writer.WriteStartElement(new Worksheet());
                        writer.WriteStartElement(new SheetData());

                        int r = 1;
                        int rMax = table.Rows.Count + 1;
                        WriteRowStart(writer, r);
                        int c = 1;
                        foreach (DataColumn column in table.Columns)
                        {
                            WriteCellValue(writer, column.ColumnName, r, c++);
                        }
                        writer.WriteEndElement(); //end of Row tag

                        foreach (DataRow row in table.Rows)
                        {
                            r++;
                            var rPerc = 1.0 * r / rMax;
                            var elapsed = sw.Elapsed;
                            var timeMax = new TimeSpan((long)(elapsed.Ticks / rPerc));
                            if ((elapsed - lastLog).TotalSeconds >= 60)
                            {
                                SAEONLogs.Information("Row {0} of {1}, {2:F2}% complete, in {3}, {4} of {5}, {6} left", r, rMax, rPerc * 100.0, (elapsed - last).TimeStr(), elapsed.TimeStr(), timeMax, (timeMax - elapsed).TimeStr());
                                lastLog = elapsed;
                            }
                            last = elapsed;

                            WriteRowStart(writer, r);
                            c = 1;
                            foreach (DataColumn column in table.Columns)
                            {
                                if (row.IsNull(column))
                                {
                                    WriteCellValue(writer, null, r, c++);

                                }
                                else
                                {
                                    WriteCellValue(writer, row[column], r, c++);
                                }
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

        public static SpreadsheetDocument CreateSpreadsheet<T>(string fileName, DataTable table) where T : class
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return CreateSpreadsheetDocument(SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook), table);
        }

    }
}