using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class ReportService
    {
        // Legacy export methods for deprecated MainForm.
        public static string ExportDataTableToCsv(DataTable table, string reportName, string exportDirectory)
        {
            Directory.CreateDirectory(exportDirectory);
            var path = Path.Combine(exportDirectory, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => "\"" + c.ColumnName.Replace("\"", "\"\"") + "\"")));
            foreach (DataRow row in table.Rows)
            {
                sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => "\"" + Convert.ToString(row[c])?.Replace("\"", "\"\"") + "\"")));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            SaveReportEntry(reportName, "CSV", path, "{\"format\":\"csv\",\"legacy\":true}");
            return path;
        }

        public static string ExportDataTableToHtml(DataTable table, string reportName, string exportDirectory, string reportTitle, string reportDescription)
        {
            Directory.CreateDirectory(exportDirectory);
            var path = Path.Combine(exportDirectory, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/></head><body>");
            sb.AppendLine("<h2>" + System.Net.WebUtility.HtmlEncode(reportTitle) + "</h2>");
            sb.AppendLine("<div>" + System.Net.WebUtility.HtmlEncode(reportDescription) + "</div><hr/>");
            sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5'><tr>");
            foreach (DataColumn c in table.Columns) sb.AppendLine("<th>" + System.Net.WebUtility.HtmlEncode(c.ColumnName) + "</th>");
            sb.AppendLine("</tr>");
            foreach (DataRow row in table.Rows)
            {
                sb.AppendLine("<tr>");
                foreach (DataColumn c in table.Columns) sb.AppendLine("<td>" + System.Net.WebUtility.HtmlEncode(Convert.ToString(row[c]) ?? string.Empty) + "</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            SaveReportEntry(reportName, "HTML", path, "{\"format\":\"html\",\"legacy\":true}");
            return path;
        }

        public static string ExportDataTableToExcelCompatible(DataTable table, string reportName, string exportDirectory)
        {
            Directory.CreateDirectory(exportDirectory);
            var path = Path.Combine(exportDirectory, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.xls");
            File.WriteAllText(path, BuildExcelXml(table, reportName, "Экспорт из устаревшего модуля"), new UTF8Encoding(true));
            SaveReportEntry(reportName, "XLS", path, "{\"format\":\"xls\",\"legacy\":true}");
            return path;
        }

        public static string ExportDataTableToWordCompatible(DataTable table, string reportName, string exportDirectory)
        {
            Directory.CreateDirectory(exportDirectory);
            var path = Path.Combine(exportDirectory, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.doc");
            var html = ExportDataTableToHtml(table, reportName, exportDirectory, reportName, "Экспорт из устаревшего модуля");
            if (File.Exists(html))
            {
                File.Copy(html, path, true);
            }
            SaveReportEntry(reportName, "DOC", path, "{\"format\":\"doc\",\"legacy\":true}");
            return path;
        }

        public static DataTable GetSavedReports()
        {
            const string sql = @"
SELECT TOP 1000
    r.Id,
    r.ReportName,
    r.ReportType,
    r.CreatedAt,
    u.Email AS CreatedBy,
    r.FilePath
FROM dbo.Reports r
LEFT JOIN dbo.Users u ON u.Id = r.CreatedByUserID
ORDER BY r.CreatedAt DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static string ExportFormalExcel(DataTable table, string filePath, string reportTitle, string reportSubtitle, string parametersJson)
        {
            EnsureDirectory(filePath);
            var content = BuildExcelXml(table, reportTitle, reportSubtitle);
            File.WriteAllText(filePath, content, new UTF8Encoding(true));
            SaveReportEntry(reportTitle, "EXCEL", filePath, parametersJson);
            return filePath;
        }

        public static string ExportFormalPdf(DataTable table, string filePath, string reportTitle, string reportSubtitle, string parametersJson)
        {
            EnsureDirectory(filePath);
            var pdfBytes = BuildSimplePdf(table, reportTitle, reportSubtitle);
            File.WriteAllBytes(filePath, pdfBytes);
            SaveReportEntry(reportTitle, "PDF", filePath, parametersJson);
            return filePath;
        }

        private static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string BuildExcelXml(DataTable table, string reportTitle, string reportSubtitle)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine("<Styles>");
            sb.AppendLine("<Style ss:ID=\"title\"><Font ss:Bold=\"1\" ss:Size=\"14\"/><Alignment ss:Horizontal=\"Center\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"subtitle\"><Font ss:Bold=\"1\"/><Alignment ss:Horizontal=\"Left\"/></Style>");
            sb.AppendLine("<Style ss:ID=\"header\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#DCE6F1\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders></Style>");
            sb.AppendLine("<Style ss:ID=\"cell\"><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders></Style>");
            sb.AppendLine("</Styles>");
            sb.AppendLine("<Worksheet ss:Name=\"Отчет\">");
            sb.AppendLine("<Table>");
            sb.AppendLine($"<Row><Cell ss:MergeAcross=\"{Math.Max(1, table.Columns.Count - 1)}\" ss:StyleID=\"title\"><Data ss:Type=\"String\">{Xml(reportTitle)}</Data></Cell></Row>");
            sb.AppendLine($"<Row><Cell ss:MergeAcross=\"{Math.Max(1, table.Columns.Count - 1)}\" ss:StyleID=\"subtitle\"><Data ss:Type=\"String\">{Xml(reportSubtitle)}</Data></Cell></Row>");
            sb.AppendLine($"<Row><Cell ss:MergeAcross=\"{Math.Max(1, table.Columns.Count - 1)}\"><Data ss:Type=\"String\">Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}</Data></Cell></Row>");
            sb.AppendLine("<Row/>");
            sb.AppendLine("<Row>");
            foreach (DataColumn c in table.Columns)
            {
                sb.AppendLine($"<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">{Xml(c.ColumnName)}</Data></Cell>");
            }
            sb.AppendLine("</Row>");
            foreach (DataRow row in table.Rows)
            {
                sb.AppendLine("<Row>");
                foreach (DataColumn c in table.Columns)
                {
                    var value = row[c] == DBNull.Value ? string.Empty : Convert.ToString(row[c], CultureInfo.CurrentCulture);
                    sb.AppendLine($"<Cell ss:StyleID=\"cell\"><Data ss:Type=\"String\">{Xml(value)}</Data></Cell>");
                }
                sb.AppendLine("</Row>");
            }
            sb.AppendLine("</Table></Worksheet></Workbook>");
            return sb.ToString();
        }

        private static byte[] BuildSimplePdf(DataTable table, string reportTitle, string reportSubtitle)
        {
            var images = RenderReportPagesAsJpeg(table, reportTitle, reportSubtitle);
            return BuildPdfFromJpegPages(images);
        }

        private static List<byte[]> RenderReportPagesAsJpeg(DataTable table, string reportTitle, string reportSubtitle)
        {
            const int pageWidth = 1754;   // A4 landscape @150dpi
            const int pageHeight = 1240;
            const int margin = 56;
            const int footerReserve = 88;
            const int minColWidth = 52;

            var result = new List<byte[]>();
            var columns = table.Columns.Cast<DataColumn>().ToList();
            var totalColumns = Math.Max(1, columns.Count);
            var tableWidth = pageWidth - 2 * margin;
            var colWidth = Math.Max(minColWidth, tableWidth / totalColumns);
            if (colWidth * totalColumns > tableWidth)
            {
                colWidth = tableWidth / totalColumns;
            }

            var useSmallFont = colWidth < 72;
            var generationDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var rowIndex = 0;
            var pageNum = 0;

            while (rowIndex < table.Rows.Count || (table.Rows.Count == 0 && rowIndex == 0))
            {
                pageNum++;
                using (var bmp = new Bitmap(pageWidth, pageHeight))
                using (var g = Graphics.FromImage(bmp))
                using (var fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
                using (var fSub = new Font("Segoe UI", 10f, FontStyle.Regular))
                using (var fHeader = new Font("Segoe UI", useSmallFont ? 8f : 9f, FontStyle.Bold))
                using (var fCell = new Font("Segoe UI", useSmallFont ? 7f : 8.5f, FontStyle.Regular))
                using (var borderPen = new Pen(Color.FromArgb(100, 116, 139), 1))
                using (var headerBack = new SolidBrush(Color.FromArgb(226, 232, 240)))
                using (var cellBack = new SolidBrush(Color.White))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    g.Clear(Color.White);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    var y = margin;
                    g.DrawString("ГБПОУ \"Брянский строительный колледж им. Н.Е. Жуковского\"", fTitle, textBrush, margin, y);
                    y += 34;
                    g.DrawString(reportTitle, fSub, textBrush, margin, y);
                    y += 22;
                    g.DrawString(reportSubtitle, fSub, textBrush, margin, y);
                    y += 20;
                    g.DrawString("Дата формирования: " + generationDate, fSub, textBrush, margin, y);
                    y += 20;
                    g.DrawString("Страница: " + pageNum, fSub, textBrush, margin, y);
                    y += 28;

                    var rowHeight = useSmallFont ? 30 : 28;

                    for (var c = 0; c < columns.Count; c++)
                    {
                        var x = margin + c * colWidth;
                        var rect = new Rectangle(x, y, colWidth, rowHeight);
                        g.FillRectangle(headerBack, rect);
                        g.DrawRectangle(borderPen, rect);
                        DrawCellText(g, columns[c].ColumnName, fHeader, textBrush, rect);
                    }

                    y += rowHeight;

                    if (table.Rows.Count == 0)
                    {
                        var emptyRect = new Rectangle(margin, y, tableWidth, rowHeight);
                        g.FillRectangle(cellBack, emptyRect);
                        g.DrawRectangle(borderPen, emptyRect);
                        DrawCellText(g, "Нет данных за выбранный период", fCell, textBrush, emptyRect);
                        y += rowHeight;
                        rowIndex++;
                    }
                    else
                    {
                        while (rowIndex < table.Rows.Count && y + rowHeight + footerReserve <= pageHeight)
                        {
                            for (var c = 0; c < columns.Count; c++)
                            {
                                var x = margin + c * colWidth;
                                var rect = new Rectangle(x, y, colWidth, rowHeight);
                                g.FillRectangle(cellBack, rect);
                                g.DrawRectangle(borderPen, rect);
                                var value = Convert.ToString(table.Rows[rowIndex][columns[c].ColumnName], CultureInfo.CurrentCulture) ?? string.Empty;
                                DrawCellText(g, value, fCell, textBrush, rect);
                            }

                            y += rowHeight;
                            rowIndex++;
                        }
                    }

                    y += 26;
                    g.DrawString("Ответственный за формирование отчета: ____________________", fSub, textBrush, margin, y);
                    y += 22;
                    g.DrawString("Подпись: ____________________", fSub, textBrush, margin, y);

                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Jpeg);
                        result.Add(ms.ToArray());
                    }
                }
            }

            return result;
        }

        private static string Xml(string text)
        {
            return System.Security.SecurityElement.Escape(text ?? string.Empty);
        }

        private static byte[] BuildPdfFromJpegPages(List<byte[]> jpegPages)
        {
            var objects = new Dictionary<int, byte[]>();
            var pageIds = new List<int>();
            const int pageWidthPt = 842;
            const int pageHeightPt = 595;

            var objectId = 3;
            foreach (var image in jpegPages)
            {
                var pageId = objectId++;
                var contentId = objectId++;
                var imageId = objectId++;
                pageIds.Add(pageId);

                objects[contentId] = BuildAsciiBytes("<< /Length 37 >>\nstream\nq 842 0 0 595 0 0 cm /Im1 Do Q\nendstream");
                objects[imageId] = BuildImageObject(image, 1754, 1240);
                objects[pageId] = BuildAsciiBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + pageWidthPt + " " + pageHeightPt + "] /Resources << /XObject << /Im1 " + imageId + " 0 R >> >> /Contents " + contentId + " 0 R >>");
            }

            objects[1] = BuildAsciiBytes("<< /Type /Catalog /Pages 2 0 R >>");
            objects[2] = BuildAsciiBytes("<< /Type /Pages /Kids [" + string.Join(" ", pageIds.Select(id => id + " 0 R")) + "] /Count " + pageIds.Count + " >>");

            using (var ms = new MemoryStream())
            {
                WriteAscii(ms, "%PDF-1.4\n");
                var offsets = new Dictionary<int, int>();
                var maxId = objects.Keys.Max();
                for (var id = 1; id <= maxId; id++)
                {
                    offsets[id] = (int)ms.Position;
                    WriteAscii(ms, id.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    ms.Write(objects[id], 0, objects[id].Length);
                    WriteAscii(ms, "\nendobj\n");
                }

                var xrefPos = (int)ms.Position;
                WriteAscii(ms, "xref\n");
                WriteAscii(ms, "0 " + (maxId + 1) + "\n");
                WriteAscii(ms, "0000000000 65535 f \n");
                for (var id = 1; id <= maxId; id++)
                {
                    WriteAscii(ms, offsets[id].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
                }
                WriteAscii(ms, "trailer\n");
                WriteAscii(ms, "<< /Size " + (maxId + 1) + " /Root 1 0 R >>\n");
                WriteAscii(ms, "startxref\n");
                WriteAscii(ms, xrefPos.ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(ms, "%%EOF");
                return ms.ToArray();
            }
        }

        private static void DrawCellText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            var padded = new RectangleF(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height - 4);
            g.DrawString(text ?? string.Empty, font, brush, padded, format);
        }

        private static byte[] BuildImageObject(byte[] jpegBytes, int pixelWidth, int pixelHeight)
        {
            using (var ms = new MemoryStream())
            {
                WriteAscii(ms, "<< /Type /XObject /Subtype /Image /Width " + pixelWidth + " /Height " + pixelHeight + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + jpegBytes.Length + " >>\nstream\n");
                ms.Write(jpegBytes, 0, jpegBytes.Length);
                WriteAscii(ms, "\nendstream");
                return ms.ToArray();
            }
        }

        private static void WriteAscii(Stream stream, string content)
        {
            var bytes = Encoding.ASCII.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte[] BuildAsciiBytes(string content)
        {
            return Encoding.ASCII.GetBytes(content);
        }

        private static void SaveReportEntry(string reportName, string reportType, string filePath, string parametersJson)
        {
            const string sql = @"
INSERT INTO dbo.Reports (ReportName, ReportType, ParametersJSON, CreatedByUserID, FilePath)
VALUES (@ReportName, @ReportType, @ParametersJSON, @CreatedByUserID, @FilePath);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@ReportName", reportName),
                new SqlParameter("@ReportType", reportType),
                new SqlParameter("@ParametersJSON", (object)parametersJson ?? DBNull.Value),
                new SqlParameter("@CreatedByUserID", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                new SqlParameter("@FilePath", (object)filePath ?? DBNull.Value));
        }
    }
}
