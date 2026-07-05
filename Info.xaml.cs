using System;
using System.IO;
using System.Security;
using System.Text;
using System.Windows;
using Market.Models;
using Market.Services;
using Microsoft.Win32;

namespace Market
{
    public partial class Info : Window
    {
        private readonly ApiClient apiClient = new ApiClient();
        private DateTime? appliedStartDate;
        private DateTime? appliedEndDate;

        public Info()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0) { MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); new MainWindow().Show(); Close(); return; }

            start_period.SelectedDate = DateTime.Today;
            end_period.SelectedDate = DateTime.Today;

            data_list.Items.Clear();
            period_output.Content = "Тут будут данные за выбранный период";
            export_btn.IsEnabled = false;

            _ = LoadInitialStats();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private async System.Threading.Tasks.Task LoadInitialStats()
        {
            try
            {
                DateTime todayStart = DateTime.Today;
                DateTime todayEnd = todayStart.AddDays(1).AddSeconds(-1);
                var day = await GetTotals(todayStart, todayEnd);
                day_output.Content = $"День: продано {day.totalItems} товаров на сумму {day.totalPrice:F0}R";

                DateTime weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                {
                    weekStart = weekStart.AddDays(-7);
                }
                DateTime weekEnd = weekStart.AddDays(7).AddSeconds(-1);
                var week = await GetTotals(weekStart, weekEnd);
                week_output.Content = $"Неделя: продано {week.totalItems} товаров на сумму {week.totalPrice:F0}R";

                DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
                var month = await GetTotals(monthStart, monthEnd);
                month_output.Content = $"Месяц: продано {month.totalItems} товаров на сумму {month.totalPrice:F0}R";

                var total = await GetTotals(new DateTime(2000, 1, 1), DateTime.Today.AddYears(100));
                total_output.Content = $"Всего: продано {total.totalItems} товаров на сумму {total.totalPrice:F0}R";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<ReportTotalsResponse> GetTotals(DateTime startDate, DateTime endDate)
        {
            string start = Uri.EscapeDataString(startDate.ToString("s"));
            string end = Uri.EscapeDataString(endDate.ToString("s"));
            return await apiClient.GetAsync<ReportTotalsResponse>($"/auth/reports/totals/{CurrentStoreId}?startDate={start}&endDate={end}");
        }

        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            var main = new Market.Main();
            main.Show();
            this.Close();
        }

        private async void apply_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!start_period.SelectedDate.HasValue || !end_period.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите начальную и конечную даты периода.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime startDate = start_period.SelectedDate.Value;
            DateTime selectedEndDate = end_period.SelectedDate.Value;
            DateTime endDate = selectedEndDate.AddDays(1).AddSeconds(-1);

            if (startDate > endDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            data_list.Items.Clear();
            appliedStartDate = null;
            appliedEndDate = null;
            export_btn.IsEnabled = false;

            try
            {
                string start = Uri.EscapeDataString(startDate.ToString("s"));
                string end = Uri.EscapeDataString(endDate.ToString("s"));
                var period = await apiClient.GetAsync<ReportPeriodResponse>($"/auth/reports/by-product/{CurrentStoreId}?startDate={start}&endDate={end}");

                if (period.items != null)
                {
                    foreach (var item in period.items)
                    {
                        data_list.Items.Add($"{item.name} - {item.quantitySold} шт. - {item.totalPrice:F0}R");
                    }
                }

                period_output.Content = $"За период с {startDate:dd.MM.yyyy} по {selectedEndDate:dd.MM.yyyy} продано {period.totalItems} товаров на сумму {period.totalPrice:F0}R";
                appliedStartDate = startDate;
                appliedEndDate = selectedEndDate;
                export_btn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void export_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!appliedStartDate.HasValue || !appliedEndDate.HasValue)
            {
                MessageBox.Show("Сначала выберите даты и нажмите \"Применить\".", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить Excel отчёт",
                Filter = "Excel 97-2003 (*.xls)|*.xls",
                FileName = $"Отчет_{appliedStartDate.Value:yyyy-MM-dd}_{appliedEndDate.Value:yyyy-MM-dd}.xls",
                AddExtension = true,
                DefaultExt = ".xls",
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, BuildExcelReport(), Encoding.UTF8);
                MessageBox.Show("Отчёт успешно сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildExcelReport()
        {
            var report = new StringBuilder();
            report.AppendLine("<?xml version=\"1.0\"?>");
            report.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            report.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            report.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            report.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            report.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            report.AppendLine("<Styles>");
            report.AppendLine("<Style ss:ID=\"Title\"><Font ss:Bold=\"1\" ss:Size=\"14\"/><Alignment ss:Horizontal=\"Center\"/><Interior ss:Color=\"#C9F3F7\" ss:Pattern=\"Solid\"/></Style>");
            report.AppendLine("<Style ss:ID=\"Header\"><Font ss:Bold=\"1\"/><Alignment ss:Horizontal=\"Center\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders><Interior ss:Color=\"#D2D2D2\" ss:Pattern=\"Solid\"/></Style>");
            report.AppendLine("<Style ss:ID=\"Text\"><Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Center\" ss:WrapText=\"1\"/></Style>");
            report.AppendLine("<Style ss:ID=\"Summary\"><Font ss:Bold=\"1\"/><Alignment ss:Horizontal=\"Left\" ss:WrapText=\"1\"/></Style>");
            report.AppendLine("</Styles>");
            report.AppendLine("<Worksheet ss:Name=\"Отчёт\">");
            report.AppendLine("<Table>");
            report.AppendLine("<Column ss:Width=\"260\"/>");
            report.AppendLine("<Column ss:Width=\"180\"/>");
            report.AppendLine("<Column ss:Width=\"160\"/>");
            AddMergedRow(report, "Отчёт по продажам", "Title", 3);
            AddMergedRow(report, $"Период: {appliedStartDate.Value:dd.MM.yyyy} - {appliedEndDate.Value:dd.MM.yyyy}", "Summary", 3);
            AddEmptyRow(report);
            AddMergedRow(report, "Продажи по товарам", "Header", 3);
            AddRow(report, "Товар", "Количество и сумма", "", "Header");

            if (data_list.Items.Count == 0)
            {
                AddMergedRow(report, "Нет продаж за выбранный период", "Text", 3);
            }
            else
            {
                foreach (var item in data_list.Items)
                {
                    AddMergedRow(report, item.ToString(), "Text", 3);
                }
            }

            AddEmptyRow(report);
            AddMergedRow(report, "Итоги", "Header", 3);
            AddMergedRow(report, period_output.Content?.ToString() ?? "", "Summary", 3);
            AddMergedRow(report, day_output.Content?.ToString() ?? "", "Text", 3);
            AddMergedRow(report, week_output.Content?.ToString() ?? "", "Text", 3);
            AddMergedRow(report, month_output.Content?.ToString() ?? "", "Text", 3);
            AddMergedRow(report, total_output.Content?.ToString() ?? "", "Text", 3);
            report.AppendLine("</Table>");
            report.AppendLine("<WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\"><FitToPage/><Print><FitWidth>1</FitWidth><FitHeight>0</FitHeight></Print></WorksheetOptions>");
            report.AppendLine("</Worksheet>");
            report.AppendLine("</Workbook>");
            return report.ToString();
        }

        private static void AddRow(StringBuilder report, string first, string second, string third, string style)
        {
            report.AppendLine("<Row>");
            AddCell(report, first, style);
            AddCell(report, second, style);
            AddCell(report, third, style);
            report.AppendLine("</Row>");
        }

        private static void AddMergedRow(StringBuilder report, string value, string style, int mergeColumns)
        {
            report.AppendLine("<Row>");
            report.AppendLine($"<Cell ss:StyleID=\"{style}\" ss:MergeAcross=\"{mergeColumns - 1}\"><Data ss:Type=\"String\">{Escape(value)}</Data></Cell>");
            report.AppendLine("</Row>");
        }

        private static void AddEmptyRow(StringBuilder report)
        {
            report.AppendLine("<Row/>");
        }

        private static void AddCell(StringBuilder report, string value, string style)
        {
            report.AppendLine($"<Cell ss:StyleID=\"{style}\"><Data ss:Type=\"String\">{Escape(value)}</Data></Cell>");
        }

        private static string Escape(string value)
        {
            return SecurityElement.Escape(value ?? "") ?? "";
        }
    }
}

