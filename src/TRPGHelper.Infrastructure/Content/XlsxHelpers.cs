using System.Globalization;
using ClosedXML.Excel;

namespace TRPGHelper.Infrastructure.Content;

/// <summary>读 xlsx 的小工具：固定表头行、忽略空行、按列名取值。</summary>
internal static class XlsxHelpers
{
    public static IXLWorksheet RequireSheet(XLWorkbook workbook, string filePath, string sheetName)
    {
        if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var sheet))
        {
            throw new ContentException(
                $"{Path.GetFileName(filePath)} 缺少工作表「{sheetName}」，现有工作表：{string.Join("、", workbook.Worksheets.Select(item => item.Name))}。");
        }

        return sheet;
    }

    public static List<SheetRow> ReadRows(
        IXLWorksheet sheet,
        string filePath,
        IReadOnlyCollection<string> requiredColumns)
    {
        var rows = new List<SheetRow>();
        var firstRow = sheet.FirstRowUsed();
        if (firstRow is null)
        {
            return rows;
        }

        var headerRowNumber = firstRow.RowNumber();
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in sheet.Row(headerRowNumber).CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (header.Length > 0)
            {
                headers[header] = cell.Address.ColumnNumber;
            }
        }

        foreach (var required in requiredColumns)
        {
            if (!headers.ContainsKey(required))
            {
                throw new ContentException(
                    $"{Path.GetFileName(filePath)} / 工作表「{sheet.Name}」缺少列「{required}」，现有列：{string.Join("、", headers.Keys)}。");
            }
        }

        foreach (var row in sheet.RowsUsed())
        {
            if (row.RowNumber() <= headerRowNumber)
            {
                continue;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var empty = true;

            foreach (var (header, column) in headers)
            {
                var text = ReadCell(row.Cell(column));
                values[header] = text;
                if (text.Length > 0)
                {
                    empty = false;
                }
            }

            if (empty)
            {
                continue;
            }

            rows.Add(new SheetRow(sheet.Name, row.RowNumber(), values));
        }

        return rows;
    }

    public static string ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => cell.GetString().Trim(),
        };
    }

    public static void WriteSheet(
        XLWorkbook workbook,
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        IReadOnlyDictionary<string, double>? columnWidths = null,
        ISet<string>? wrapColumns = null)
    {
        var sheet = workbook.AddWorksheet(sheetName);

        for (var column = 0; column < headers.Count; column += 1)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        var header = sheet.Row(1);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3E7D4");
        header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        sheet.SheetView.FreezeRows(1);

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < headers.Count; column += 1)
            {
                var value = column < row.Count ? row[column] : null;
                var cell = sheet.Cell(rowIndex, column + 1);

                switch (value)
                {
                    case null:
                        break;
                    case bool boolean:
                        cell.Value = boolean ? "是" : "否";
                        break;
                    case int number:
                        cell.Value = number;
                        break;
                    case double number:
                        cell.Value = number;
                        break;
                    default:
                        cell.Value = value.ToString();
                        break;
                }
            }

            rowIndex += 1;
        }

        if (rowIndex > 2)
        {
            sheet.Range(1, 1, rowIndex - 1, headers.Count).SetAutoFilter();
        }

        if (columnWidths is not null)
        {
            foreach (var (name, width) in columnWidths)
            {
                var index = headers.ToList().FindIndex(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    sheet.Column(index + 1).Width = width;
                }
            }
        }

        if (wrapColumns is not null)
        {
            foreach (var name in wrapColumns)
            {
                var index = headers.ToList().FindIndex(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    sheet.Column(index + 1).Style.Alignment.WrapText = true;
                    sheet.Column(index + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                }
            }
        }
    }
}

internal readonly record struct SheetRow(string Sheet, int RowNumber, IReadOnlyDictionary<string, string> Values)
{
    public string this[string column]
        => Values.TryGetValue(column, out var value) ? value : string.Empty;

    public string Required(string column, string filePath)
    {
        var value = this[column].Trim();
        if (value.Length == 0)
        {
            throw new ContentException(
                $"{Path.GetFileName(filePath)} / 工作表「{Sheet}」第 {RowNumber} 行：列「{column}」不能为空。");
        }

        return value;
    }

    public int Int(string column, int fallback = 0)
        => int.TryParse(this[column], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    public bool Bool(string column, bool fallback = true)
    {
        var value = this[column].Trim();
        if (value.Length == 0)
        {
            return fallback;
        }

        return value.ToLowerInvariant() switch
        {
            "是" or "true" or "1" or "y" or "yes" => true,
            "否" or "false" or "0" or "n" or "no" => false,
            _ => fallback,
        };
    }

    public string Fail(string filePath, string message)
        => throw new ContentException($"{Path.GetFileName(filePath)} / 工作表「{Sheet}」第 {RowNumber} 行：{message}");
}
