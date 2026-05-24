using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace ExcelParser
{
    public partial class MainWindow : Window
    {
        private string? _selectedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "Выберите Excel файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtFilePath.Text = _selectedFilePath;
                LblStatus.Content = "Файл выбран";
                LblStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private async void BtnParseAndGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("Сначала выберите Excel файл!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TxtConnectionString.Text))
            {
                MessageBox.Show("Введите строку подключения к SQL Server!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnParseAndGenerate.IsEnabled = false;
            LblStatus.Content = "Обработка...";
            LblStatus.Foreground = System.Windows.Media.Brushes.Orange;

            try
            {
                var records = ParseExcel(_selectedFilePath!);
                
                if (records.Count == 0)
                {
                    MessageBox.Show("Данные не найдены в файле!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LblStatus.Content = "Нет данных";
                    return;
                }

                int successCount = 0;
                int errorCount = 0;
                var errors = new StringBuilder();

                foreach (var record in records)
                {
                    try
                    {
                        await InsertIntoDatabaseAsync(record, TxtConnectionString.Text);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.AppendLine($"Квартира {record.ApartmentNumber}: {ex.Message}");
                    }
                }

                TxtSqlOutput.Text = $"Успешно: {successCount}\nОшибок: {errorCount}\n\n" + 
                                   (errors.Length > 0 ? errors.ToString() : "Все записи обработаны успешно!");
                
                LblStatus.Content = $"Готово! Записей: {successCount}";
                LblStatus.Foreground = System.Windows.Media.Brushes.Green;
                MessageBox.Show($"Обработка завершена!\nУспешно: {successCount}\nОшибок: {errorCount}", 
                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LblStatus.Content = "Ошибка";
                LblStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                BtnParseAndGenerate.IsEnabled = true;
            }
        }

        private List<AccrualChargeDto> ParseExcel(string filePath)
        {
            var records = new List<AccrualChargeDto>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // Первый лист

            var range = worksheet.RangeUsed();
            if (range == null) return records;

            // Поиск колонок по заголовкам
            var headersRow = 1;
            var hotWaterPrevCol = FindColumn(worksheet, headersRow, "ГВС преды");
            var hotWaterCurrentCol = FindColumn(worksheet, headersRow, "ГВС конеч");
            var coldWaterPrevCol = FindColumn(worksheet, headersRow, "ХВС преды");

            if (hotWaterPrevCol == 0 || hotWaterCurrentCol == 0 || coldWaterPrevCol == 0)
            {
                throw new Exception("Не найдены необходимые колонки в Excel файле.\n" +
                    "Ожидаемые колонки: 'ГВС преды', 'ГВС конеч', 'ХВС преды'");
            }

            // Чтение данных начиная со второй строки
            for (int row = 2; row <= worksheet.LastRowUsed()?.RowNumber() ?? 1; row++)
            {
                var apartmentCell = worksheet.Cell(row, 1); // Предполагаем, что номер квартиры в первой колонке
                
                if (apartmentCell.IsEmpty()) continue;

                var record = new AccrualChargeDto
                {
                    ApartmentNumber = (int)apartmentCell.GetValue<double>(),
                    HotWaterPrevious = GetDoubleValue(worksheet, row, hotWaterPrevCol),
                    HotWaterCurrent = GetDoubleValue(worksheet, row, hotWaterCurrentCol),
                    ColdWaterPrevious = GetDoubleValue(worksheet, row, coldWaterPrevCol)
                };

                // Вычисляем потребление
                if (record.HotWaterPrevious.HasValue && record.HotWaterCurrent.HasValue)
                {
                    record.HotWaterConsumption = record.HotWaterCurrent.Value - record.HotWaterPrevious.Value;
                }

                if (record.ColdWaterPrevious.HasValue)
                {
                    // Если есть колонка ХВС конечное, можно добавить аналогично
                    record.ColdWaterConsumption = null; // Пока нет данных
                }

                records.Add(record);
            }

            return records;
        }

        private int FindColumn(IXLWorksheet worksheet, int headerRow, string partialName)
        {
            var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            
            for (int col = 1; col <= lastCol; col++)
            {
                var cellValue = worksheet.Cell(headerRow, col).GetValue<string>()?.ToLower() ?? "";
                if (cellValue.Contains(partialName.ToLower()))
                {
                    return col;
                }
            }

            return 0;
        }

        private double? GetDoubleValue(IXLWorksheet worksheet, int row, int column)
        {
            var cell = worksheet.Cell(row, column);
            if (cell.IsEmpty()) return null;

            try
            {
                return cell.GetValue<double>();
            }
            catch
            {
                if (double.TryParse(cell.GetValue<string>(), out var result))
                {
                    return result;
                }
                return null;
            }
        }

        private async Task InsertIntoDatabaseAsync(AccrualChargeDto record, string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO AccrualCharges 
                (ApartmentNumber, FullName, TotalArea, 
                 HotWaterPrevious, HotWaterCurrent, HotWaterConsumption,
                 ColdWaterPrevious, ColdWaterCurrent, ColdWaterConsumption)
                VALUES 
                (@ApartmentNumber, @FullName, @TotalArea,
                 @HotWaterPrevious, @HotWaterCurrent, @HotWaterConsumption,
                 @ColdWaterPrevious, @ColdWaterCurrent, @ColdWaterConsumption);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ApartmentNumber", record.ApartmentNumber);
            command.Parameters.AddWithValue("@FullName", (object?)record.FullName ?? DBNull.Value);
            command.Parameters.AddWithValue("@TotalArea", (object?)record.TotalArea ?? DBNull.Value);
            command.Parameters.AddWithValue("@HotWaterPrevious", (object?)record.HotWaterPrevious ?? DBNull.Value);
            command.Parameters.AddWithValue("@HotWaterCurrent", (object?)record.HotWaterCurrent ?? DBNull.Value);
            command.Parameters.AddWithValue("@HotWaterConsumption", (object?)record.HotWaterConsumption ?? DBNull.Value);
            command.Parameters.AddWithValue("@ColdWaterPrevious", (object?)record.ColdWaterPrevious ?? DBNull.Value);
            command.Parameters.AddWithValue("@ColdWaterCurrent", (object?)record.ColdWaterCurrent ?? DBNull.Value);
            command.Parameters.AddWithValue("@ColdWaterConsumption", (object?)record.ColdWaterConsumption ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        private void BtnCopySql_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtSqlOutput.Text))
            {
                Clipboard.SetText(TxtSqlOutput.Text);
                MessageBox.Show("SQL скопирован в буфер обмена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
