using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SAEON.OpenXML
{
    public static class ExcelHelper
    {

        #region Sheets
        public static Sheet GetSheet(SpreadsheetDocument document, int sheetId)
        {
            return document.WorkbookPart.Workbook.Descendants<Sheet>().Where(s => s.SheetId.Value == sheetId).FirstOrDefault();
        }

        public static Sheet GetSheet(SpreadsheetDocument document, string sheetName)
        {
            return document.WorkbookPart.Workbook.Descendants<Sheet>().Where(s => s.Name.Value == sheetName).FirstOrDefault();
        }

        public static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, int sheetId)
        {
            var sheet = GetSheet(document, sheetId);
            return (WorksheetPart)(document.WorkbookPart.GetPartById(sheet.Id));
        }

        public static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string sheetName)
        {
            var sheet = GetSheet(document, sheetName);
            return (WorksheetPart)(document.WorkbookPart.GetPartById(sheet.Id));
        }

        public static List<Sheet> GetAllSheets(SpreadsheetDocument document)
        {
            return document.WorkbookPart.Workbook.Descendants<Sheet>().ToList();
        }

        public static void DeleteSheet(SpreadsheetDocument document, Sheet sheet)
        {
            WorkbookPart workbookPart = document.WorkbookPart;
            WorksheetPart worksheetPart = (WorksheetPart)(workbookPart.GetPartById(sheet.Id));
            sheet.Remove();
            workbookPart.DeletePart(worksheetPart);
        }

        public static void DeleteSheet(SpreadsheetDocument document, string sheetName)
        {
            DeleteSheet(document, GetSheet(document, sheetName));
        }

        public static void DeleteAllSheets(SpreadsheetDocument document)
        {
            GetAllSheets(document).ForEach(s => DeleteSheet(document, s));
        }

        public static WorksheetPart InsertSheet(SpreadsheetDocument document, string sheetName = "")
        {
            using (Logging.MethodCall(typeof(WorksheetPart)))
            {
                try 
                {
                    // Add a blank WorksheetPart.
                    WorksheetPart worksheetPart = document.WorkbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());

                    Sheets sheets = document.WorkbookPart.Workbook.GetFirstChild<Sheets>();
                    if (sheets == null)
                    {
                        sheets = document.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
                    }
                    string relationshipId = document.WorkbookPart.GetIdOfPart(worksheetPart); 
                    Logging.Verbose("relationshipId: {relationshipId}", relationshipId);

                    // Get a unique ID for the new worksheet.
                    uint sheetId = 1; 
                    if (sheets.Elements<Sheet>().Count() > 0)
                    {
                        sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
                    }
                    Logging.Verbose("sheetId: {sheetId}", sheetId);

                    // Give the new worksheet a name. 
                    if (string.IsNullOrEmpty(sheetName))
                        sheetName = "Sheet" + sheetId;
                    Logging.Verbose("sheetName: {sheetName}", sheetName);

                    // Append the new worksheet and associate it with the workbook.
                    Sheet sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };
                    sheets.Append(sheet);
                    return worksheetPart;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex); 
                    throw;
                }
            }
        }
         
        #endregion 

        #region Columns
        public static Column AddColumn(WorksheetPart worksheetPart, UInt32 index, double width)
        {
            Columns columns = worksheetPart.Worksheet.GetFirstChild<Columns>();
            if (columns == null)
            {
                columns = new Columns();
                worksheetPart.Worksheet.Append(columns);
            }
            Column column = new Column();
            column.Min = index;
            column.Max = index;
            column.Width = width;
            columns.Append(column);
            worksheetPart.Worksheet.Save();
            return column;
        }

        public static Column GetColumn(WorksheetPart worksheetPart, UInt32 index)
        {
            Columns columns = worksheetPart.Worksheet.GetFirstChild<Columns>();
            if (columns == null)
            {
                columns = new Columns();
                for (int i = 1; i <= index; i++)
                {
                    Column column = new Column();
                    column.Min = UInt32Value.FromUInt32(index);
                    column.Max = UInt32Value.FromUInt32(index);
                    columns.Append(column);
                }
                worksheetPart.Worksheet.Append(columns);
                worksheetPart.Worksheet.Save();
            }
            return worksheetPart.Worksheet.Descendants<Column>().ElementAt(Convert.ToInt32(index) - 1);
        }

        public static string GetColumnName(uint column)
        {
            uint value = 0;
            uint remainder = 0;
            string result = string.Empty;
            value = column;
            while (value > 0)
            {
                remainder = (value - 1) % 26;
                result = (char)(65 + remainder) + result;
                value = (uint)(Math.Floor((double)((value - remainder) / 26)));
            }
            return result;
        }

        public static string GetColumnName(int column)
        {
            return GetColumnName(Convert.ToUInt32(column));
        }

        public static int GetColumnIndex(string columnName)
        {
            // Remove row numbers
            int r = columnName.IndexOfAny("0123456789".ToCharArray());
            if (r > 0) columnName = columnName.Substring(0, r);
            int result = 0;
            int[] digits = new int[columnName.Length];
            for (int i = 0; i < columnName.Length; ++i)
            {
                digits[i] = Convert.ToInt32(columnName[i]) - 64;
            }
            int mul = 1;
            for (int pos = digits.Length - 1; pos >= 0; --pos)
            {
                result += digits[pos] * mul;
                mul *= 26;
            }
            return result;

        }

        #endregion

        #region Cells

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(WorksheetPart worksheetPart, string columnName, uint rowIndex)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
            }
            else
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            // If there is not a cell with the specified column name, insert one.  
            if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
            {
                return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
            }
            else
            {
                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    //if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                    if (GetColumnIndex(cell.CellReference.Value) > GetColumnIndex(cellReference))
                    {
                        refCell = cell;
                        break;
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                //worksheet.Save();
                return newCell;
            }
        }

        // Given text and a SharedStringTablePart, creates a SharedStringItem with the specified text 
        // and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }

                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
            shareStringPart.SharedStringTable.Save();

            return i;
        }

        public static void SetCellValue(SpreadsheetDocument document, WorksheetPart worksheetPart, string columnName, uint rowIndex, object value)
        {
            if (value == null) return;
            Cell cell = InsertCellInWorksheet(worksheetPart, columnName, rowIndex);
            if (value is string)
            {
                // Get the SharedStringTablePart. If it does not exist, create a new one.
                SharedStringTablePart shareStringPart = document.WorkbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                if (shareStringPart == null) shareStringPart = document.WorkbookPart.AddNewPart<SharedStringTablePart>();

                // Insert the text into the SharedStringTablePart.
                int index = InsertSharedStringItem((string)value, shareStringPart);
                // Set the value of cell
                cell.CellValue = new CellValue(index.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
            }
            else if ((value is int) || (value is double) || (value is float) || (value is decimal))
            {
                cell.CellValue = new CellValue(value.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
            }
            else if (((value is int?) && ((int?)value).HasValue) ||
                     ((value is double?) && ((double?)value).HasValue) ||
                     ((value is float?) && ((float?)value).HasValue) ||
                     ((value is decimal?) && ((decimal?)value).HasValue))
            {
                cell.CellValue = new CellValue(value.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
            }
            else if (value is bool)
            {
                cell.CellValue = new CellValue((bool)value ? "1" : "0");
                cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
            }
            else if (value is DateTime)
            {
                DateTime bv = (DateTime)value;
                cell.CellValue = new CellValue(bv.ToOADate().ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                cell.StyleIndex = 1;
            }
            else if ((value is DateTime?) && ((DateTime?)value).HasValue)
            {
                DateTime? bv = ((DateTime?)value).Value;
                cell.CellValue = new CellValue(bv.Value.ToOADate().ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                cell.StyleIndex = 1;
            }
        }

        public static void SetCellValue(SpreadsheetDocument document, WorksheetPart worksheetPart, string columnName, int rowIndex, object value)
        {
            SetCellValue(document, worksheetPart, columnName, Convert.ToUInt32(rowIndex), value);
        }

        public static void SetCellValue(SpreadsheetDocument document, WorksheetPart worksheetPart, int colIndex, int rowIndex, object value)
        {
            SetCellValue(document, worksheetPart, GetColumnName(colIndex), Convert.ToUInt32(rowIndex), value);
        }

        public static object GetCellValue(SpreadsheetDocument document, WorksheetPart worksheetPart, string columnName, int rowIndex)
        {
            object result = null;
            Cell cell = worksheetPart.Worksheet.Descendants<Cell>().
              Where(c => c.CellReference == columnName + rowIndex).FirstOrDefault();
            if (cell != null) result = GetCellValue(document, cell);
            return result;
        }

        public static object GetCellValue(SpreadsheetDocument document, WorksheetPart worksheetPart, int colIndex, int rowIndex)
        {
            return GetCellValue(document, worksheetPart, GetColumnName(colIndex), rowIndex);
        }

        private static object GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            object result = cell.InnerText;
            if (cell.DataType != null)
                switch (cell.DataType.Value)
                {
                    case CellValues.Boolean:
                        result = cell.InnerText != "0";
                        break;
                    case CellValues.Date:
                        result = DateTime.FromOADate(double.Parse(cell.InnerText));
                        break;
                    case CellValues.Number:
                        int i;
                        double d;
                        if (int.TryParse(cell.InnerText, out i))
                            result = i;
                        else if (double.TryParse(cell.InnerText, out d))
                            result = d;
                        else
                            result = cell.InnerText;
                        break;
                    case CellValues.SharedString:
                        // For shared strings, look up the value in the
                        // shared strings table.
                        var stringTable = document.WorkbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

                        // If the shared string table is missing, something 
                        // is wrong. Return the index that is in
                        // the cell. Otherwise, look up the correct text in 
                        // the table.
                        if (stringTable != null)
                        {
                            result = stringTable.SharedStringTable.ElementAt(int.Parse(cell.InnerText)).InnerText;
                        }
                        break;
                }
            return result;
        }

        #endregion

        #region Document

        private static Stylesheet CreateStylesheet()
        {
            Stylesheet ss = new Stylesheet();

            Fonts fts = new Fonts();
            Font ft = new Font();
            FontName ftn = new FontName();
            ftn.Val = "Calibri";
            FontSize ftsz = new FontSize();
            ftsz.Val = 11;
            ft.FontName = ftn;
            ft.FontSize = ftsz;
            fts.Append(ft);
            fts.Count = (uint)fts.ChildElements.Count;

            Fills fills = new Fills();
            Fill fill;
            PatternFill patternFill;
            fill = new Fill();
            patternFill = new PatternFill();
            patternFill.PatternType = PatternValues.None;
            fill.PatternFill = patternFill;
            fills.Append(fill);
            fill = new Fill();
            patternFill = new PatternFill();
            patternFill.PatternType = PatternValues.Gray125;
            fill.PatternFill = patternFill;
            fills.Append(fill);
            fills.Count = (uint)fills.ChildElements.Count;

            Borders borders = new Borders();
            Border border = new Border();
            border.LeftBorder = new LeftBorder();
            border.RightBorder = new RightBorder();
            border.TopBorder = new TopBorder();
            border.BottomBorder = new BottomBorder();
            border.DiagonalBorder = new DiagonalBorder();
            borders.Append(border);
            borders.Count = (uint)borders.ChildElements.Count;

            CellStyleFormats csfs = new CellStyleFormats();
            CellFormat cf = new CellFormat();
            cf.NumberFormatId = 0;
            cf.FontId = 0;
            cf.FillId = 0;
            cf.BorderId = 0;
            csfs.Append(cf);
            csfs.Count = (uint)csfs.ChildElements.Count;

            uint iExcelIndex = 164;
            NumberingFormats nfs = new NumberingFormats();
            CellFormats cfs = new CellFormats();

            cf = new CellFormat();
            cf.NumberFormatId = 0;
            cf.FontId = 0;
            cf.FillId = 0;
            cf.BorderId = 0;
            cf.FormatId = 0;
            cfs.Append(cf);

            NumberingFormat nf;
            nf = new NumberingFormat();
            nf.NumberFormatId = iExcelIndex++;
            nf.FormatCode = "dd mmm yyyy";
            nfs.Append(nf);
            cf = new CellFormat();
            cf.NumberFormatId = nf.NumberFormatId;
            cf.FontId = 0;
            cf.FillId = 0;
            cf.BorderId = 0;
            cf.FormatId = 0;
            cf.ApplyNumberFormat = true;
            cfs.Append(cf);

            nfs.Count = (uint)nfs.ChildElements.Count;
            cfs.Count = (uint)cfs.ChildElements.Count;

            ss.Append(nfs);
            ss.Append(fts);
            ss.Append(fills);
            ss.Append(borders);
            ss.Append(csfs);
            ss.Append(cfs);

            CellStyles css = new CellStyles();
            CellStyle cs = new CellStyle();
            cs.Name = "Normal";
            cs.FormatId = 0;
            cs.BuiltinId = 0;
            css.Append(cs);
            css.Count = (uint)css.ChildElements.Count;
            ss.Append(css);

            //DifferentialFormats dfs = new DifferentialFormats();
            //dfs.Count = 0;
            //ss.Append(dfs);

            //TableStyles tss = new TableStyles();
            //tss.Count = 0;
            //tss.DefaultTableStyle = "TableStyleMedium9";
            //tss.DefaultPivotStyle = "PivotStyleLight16";
            //ss.Append(tss);

            return ss;
        }

        private static SpreadsheetDocument CreateSpreadsheetDocument(SpreadsheetDocument document)
        {
            using (Logging.MethodCall(typeof(SpreadsheetDocument)))
            {
                try
                {
                    // Add a WorkbookPart to the document.
                    WorkbookPart workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    // Shared string table
                    SharedStringTablePart sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
                    sharedStringTablePart.SharedStringTable = new SharedStringTable();
                    sharedStringTablePart.SharedStringTable.Save();

                    // Stylesheet
                    WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    workbookStylesPart.Stylesheet = CreateStylesheet();
                    workbookStylesPart.Stylesheet.Save();

                    InsertSheet(document);
                    Save(document);
                    return document;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        public static SpreadsheetDocument CreateSpreadsheet(string fileName)
        {
            // Create a spreadsheet document by supplying the filepath.
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            SpreadsheetDocument document = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook);
            return CreateSpreadsheetDocument(document);
        }

        public static SpreadsheetDocument CreateSpreadsheet(Stream stream)
        {
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            SpreadsheetDocument document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            return CreateSpreadsheetDocument(document);
        }

        public static string Validate(SpreadsheetDocument document)
        {
            OpenXmlValidator validator = new OpenXmlValidator();
            var errors = validator.Validate(document);
            StringBuilder sb = new StringBuilder();
            if (errors.Count() > 0)
            {
                sb.AppendLine("Spreadsheet is not valid!");
                foreach (ValidationErrorInfo error in errors)
                {
                    sb.AppendLine("Description: " + error.Description);
                    sb.AppendLine("ErrorType: " + error.ErrorType);
                    sb.AppendLine("Node: " + error.Node);
                    sb.AppendLine("Path: " + error.Path.XPath);
                    sb.AppendLine("Part: " + error.Part.Uri);

                }
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        public static void Save(SpreadsheetDocument document)
        {
            document.WorkbookPart.Workbook.Save();
        }

        public static void Close(SpreadsheetDocument document)
        {
            document.Close(); ;
        }

        public static void SaveAndClose(SpreadsheetDocument document)
        {
            Save(document);
            Close(document);
        }

        #endregion

        #region Utilities

        public static object[,] LoadSpreadsheet(string fileName, string sheetName = "")
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(fileName, false))
            {
                Sheet sheet =
                    string.IsNullOrEmpty(sheetName) ?
                        (Sheet)document.WorkbookPart.Workbook.Sheets.FirstOrDefault() :
                        (Sheet)document.WorkbookPart.Workbook.Descendants<Sheet>().Where(s => s.Name == sheetName).FirstOrDefault();
                WorksheetPart worksheetPart = (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id.Value);
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                int nR = sheetData.Elements<Row>().Count();
                int nC = sheetData.Elements<Row>().Max(r => r.Elements<Cell>().Count());
                object[,] result = new object[nR, nC];
                foreach (Row r in sheetData.Elements<Row>())
                {
                    foreach (Cell c in r.Elements<Cell>())
                    {
                        string cRef = c.CellReference.Value;
                        int p = cRef.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                        int col = GetColumnIndex(cRef.Substring(0, p));
                        int row = int.Parse(cRef.Substring(p));
                        result[row - 1, col - 1] = GetCellValue(document, c);
                    }
                }
                return result;
            }
        }


        public static void WriteList<T>(SpreadsheetDocument document, WorksheetPart worksheetPart, List<T> data)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            int c = 1;
            foreach (PropertyInfo propertyInfo in properties)
            {
                BrowsableAttribute attr = (BrowsableAttribute)propertyInfo.GetCustomAttributes(true).Where(a => a is BrowsableAttribute).FirstOrDefault();
                if ((attr == null) || attr.Browsable)
                {
                    SetCellValue(document, worksheetPart, GetColumnName(c), 1, propertyInfo.Name);
                    c++;
                }
            }
            uint r = 2;
            foreach (T d in data)
            {
                c = 1;
                foreach (PropertyInfo propertyInfo in properties)
                {
                    BrowsableAttribute attr = (BrowsableAttribute)propertyInfo.GetCustomAttributes(true).Where(a => a is BrowsableAttribute).FirstOrDefault();
                    if ((attr == null) || attr.Browsable)
                    {
                        SetCellValue(document, worksheetPart, GetColumnName(c), r, propertyInfo.GetValue(d, null));
                        c++;
                    }
                }
                r++;
            }
        }

        #endregion
    }
}