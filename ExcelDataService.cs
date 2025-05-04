using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CounterPlg {
    public class ExcelDataService {
        private readonly Dictionary<string, string> _accountToAddressMap;
        private static readonly Dictionary<int, string> MonthNames = new Dictionary<int, string>
        {
            { 1, "Січень" }, { 2, "Лютий" }, { 3, "Березень" }, { 4, "Квітень" },
            { 5, "Травень" }, { 6, "Червень" }, { 7, "Липень" }, { 8, "Серпень" },
            { 9, "Вересень" }, { 10, "Жовтень" }, { 11, "Листопад" }, { 12, "Грудень" }
        };

        public ExcelDataService(string excelFilePath)
        {
            _accountToAddressMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Встановлення некомерційної ліцензії для EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            LoadExcelData(excelFilePath);
        }

        private void LoadExcelData(string excelFilePath)
        {
            if (!File.Exists(excelFilePath))
            {
                throw new FileNotFoundException($"Excel-файл не знайдено за шляхом: {excelFilePath}");
            }

            using var package = new ExcelPackage(new FileInfo(excelFilePath));
            var worksheet = package.Workbook.Worksheets[0]; // Перший аркуш
            if (worksheet.Dimension == null || worksheet.Dimension.Columns < 2)
            {
                throw new InvalidOperationException("Excel-файл має містити щонайменше два стовпці: особовий рахунок і адреса.");
            }

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++) // Починаємо з 2, якщо є заголовок
            {
                string personalAccount = worksheet.Cells[row, 1].Text?.Trim();
                string address = worksheet.Cells[row, 2].Text?.Trim();

                if (!string.IsNullOrWhiteSpace(personalAccount) && !string.IsNullOrWhiteSpace(address))
                {
                    _accountToAddressMap[personalAccount] = address;
                }
            }
        }

        public string GetAddressByPersonalAccount(string personalAccount)
        {
            return _accountToAddressMap.TryGetValue(personalAccount?.Trim() ?? "", out var address) ? address : null;
        }

        public void GeneratePlgReport(List<Counter> counters, string outputFilePath, int month, int year)
        {
            using var package = new ExcelPackage(new FileInfo(outputFilePath));
            var worksheet = package.Workbook.Worksheets.Add("Звіт ПЛГ");

            // Заголовки
            worksheet.Cells[1, 1].Value = "Ном. №";
            worksheet.Cells[1, 2].Value = "Марка лічильника";
            worksheet.Cells[1, 3].Value = "КМЧ";
            worksheet.Cells[1, 4].Value = "№ лічильника";
            worksheet.Cells[1, 5].Value = "Знаходження";
            worksheet.Cells[1, 6].Value = "Особовий рахунок";
            worksheet.Cells[1, 7].Value = "Адреса";
            worksheet.Cells[1, 8].Value = "Дата списання";
            worksheet.Cells[1, 9].Value = "Списано";

            // Форматування заголовків
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Завантаження даних
            int row = 2;

            string monthName = MonthNames.TryGetValue(month, out var name) ? name : throw new ArgumentException("Невалідний місяць");

            foreach (var counter in counters)
            {
                bool isFirstNumber = true;
                foreach (var number in counter.CounterNumbers
                    .Where(n => n.IsWrittenOff &&
                                n.SelectedMonth == monthName &&
                                int.TryParse(n.SelectedYear, out int parsedYear) && parsedYear == year))
                {
                    // Заповнення даних лічильника тільки для першого номера
                    if (isFirstNumber)
                    {
                        worksheet.Cells[row, 1].Value = counter.CounterNumber;
                        worksheet.Cells[row, 2].Value = counter.Brand;
                        worksheet.Cells[row, 3].Value = counter.CounterNumber;
                        isFirstNumber = false;
                    }

                    // Заповнення даних номера
                    worksheet.Cells[row, 4].Value = number.Number;
                    worksheet.Cells[row, 5].Value = number.Location;
                    worksheet.Cells[row, 6].Value = number.PersonalAccount;
                    worksheet.Cells[row, 7].Value = number.Address;
                    worksheet.Cells[row, 8].Value = number.WriteOffDisplay;
                    worksheet.Cells[row, 9].Value = "Так";

                    row++;
                }
            }

            // Автоформатування колонок
            worksheet.Cells.AutoFitColumns();

            // Збереження файлу
            package.Save();
        }
    }
}