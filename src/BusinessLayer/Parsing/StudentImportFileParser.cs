using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BusinessLayer.Parsing;

public static class StudentImportFileParser
{
    // Import only needs the first couple of columns; this also bounds the padding below.
    private const int MaxColumns = 64;

    public static IReadOnlyList<string[]> ReadRows(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => ReadCsvRows(fileStream),
            ".xlsx" => ReadExcelRows(fileStream),
            _ => throw new InvalidOperationException(
                "Only .csv and .xlsx files are supported for batch student import.")
        };
    }

    private static List<string[]> ReadCsvRows(Stream fileStream)
    {
        var rows = new List<string[]>();
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(line.Split(',', ';'));
        }

        return rows;
    }

    private static List<string[]> ReadExcelRows(Stream fileStream)
    {
        // The OpenXml reader needs a seekable stream; an upload stream is not.
        using var buffer = new MemoryStream();
        fileStream.CopyTo(buffer);
        buffer.Position = 0;

        using var document = SpreadsheetDocument.Open(buffer, false);
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("The Excel file does not contain a workbook.");
        var sheet = workbookPart.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault()
            ?? throw new InvalidOperationException("The Excel file does not contain any sheet.");
        var sheetPartId = sheet.Id?.Value
            ?? throw new InvalidOperationException("The Excel sheet has no readable content.");
        var worksheet = ((WorksheetPart)workbookPart.GetPartById(sheetPartId)).Worksheet
            ?? throw new InvalidOperationException("The Excel sheet has no readable content.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

        var rows = new List<string[]>();
        foreach (var row in worksheet.Descendants<Row>())
        {
            var values = new List<string>();
            foreach (var cell in row.Elements<Cell>())
            {
                // Empty cells are omitted from the file, so pad by column reference to
                // keep "Full name" and "Email" in their original columns. A cell without a
                // usable reference is simply appended.
                var columnIndex = GetColumnIndex(cell.CellReference?.Value);
                if (columnIndex >= 0 && columnIndex <= MaxColumns)
                {
                    while (values.Count < columnIndex)
                    {
                        values.Add("");
                    }
                }

                values.Add(GetCellText(cell, sharedStrings));
            }

            if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(values.ToArray());
        }

        return rows;
    }

    private static string GetCellText(Cell cell, SharedStringTable? sharedStrings)
    {
        var raw = cell.CellValue?.InnerText ?? "";
        var dataType = cell.DataType?.Value;

        if (dataType == CellValues.SharedString
            && sharedStrings is not null
            && int.TryParse(raw, out var sharedStringIndex)
            && sharedStringIndex >= 0
            && sharedStringIndex < sharedStrings.ChildElements.Count)
        {
            return sharedStrings.ChildElements[sharedStringIndex].InnerText;
        }

        return dataType == CellValues.InlineString ? cell.InnerText : raw;
    }

    // "B7" -> 1. Returns -1 when the cell has no usable reference so the caller appends.
    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return -1;
        }

        var columnIndex = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            columnIndex = (columnIndex * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return columnIndex - 1;
    }
}
