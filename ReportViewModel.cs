using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace CounterPlg {
    public class ReportViewModel : INotifyPropertyChanged {
        private readonly AppDbContext _dbContext;
        private readonly ExcelDataService _excelDataService;
        private string _selectedMonth;
        private int _selectedYear;
        private ObservableCollection<CounterNumber> _writtenOffCounters;

        public ObservableCollection<CounterNumber> WrittenOffCounters
        {
            get => _writtenOffCounters;
            set
            {
                _writtenOffCounters = value;
                OnPropertyChanged(nameof(WrittenOffCounters));
            }
        }

        public List<string> Months { get; } = new List<string>
        {
            "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
            "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
        };

        public List<int> Years { get; } = Enumerable.Range(2000, DateTime.Now.Year - 2000 + 1).ToList();

        public string SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                UpdateWrittenOffCounters();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                UpdateWrittenOffCounters();
            }
        }

        public ICommand GenerateReportCommand { get; }

        public ReportViewModel(AppDbContext dbContext, ExcelDataService excelDataService)
        {
            _dbContext = dbContext;
            _excelDataService = excelDataService;
            _writtenOffCounters = new ObservableCollection<CounterNumber>();
            _selectedMonth = Months[DateTime.Now.Month - 1];
            _selectedYear = DateTime.Now.Year;

            GenerateReportCommand = new RelayCommand(GenerateReport);

            UpdateWrittenOffCounters();
        }

        private void UpdateWrittenOffCounters()
        {
            WrittenOffCounters.Clear();
            var counters = _dbContext.Counters
                .Include(c => c.CounterNumbers)
                .ToList();

            foreach (var counter in counters)
            {
                foreach (var number in counter.CounterNumbers
                    .Where(n => n.IsWrittenOff &&
                                n.SelectedMonth == _selectedMonth &&
                                int.TryParse(n.SelectedYear, out int year) && year == _selectedYear))
                {
                    WrittenOffCounters.Add(number);
                }
            }
        }

        private void GenerateReport()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel файли (*.xlsx)|*.xlsx",
                    Title = "Зберегти звіт ПЛГ",
                    DefaultExt = ".xlsx",
                    FileName = $"Звіт_ПЛГ_{SelectedYear}_{Months.IndexOf(SelectedMonth) + 1:D2}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var counters = _dbContext.Counters
                        .Include(c => c.CounterNumbers)
                        .ToList()
                        .Where(c => c.CounterNumbers.Any(n => n.IsWrittenOff &&
                                                             n.SelectedMonth == _selectedMonth &&
                                                             int.TryParse(n.SelectedYear, out int year) && year == _selectedYear))
                        .ToList();

                    if (!counters.Any())
                    {
                        MessageBox.Show("За обраний період немає списаних лічильників.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _excelDataService.GeneratePlgReport(counters, saveFileDialog.FileName, Months.IndexOf(_selectedMonth) + 1, _selectedYear);

                    Messenger.Instance.Send(new LogActionMessage($"Створено звіт ПЛГ: {saveFileDialog.FileName}", "FileExcel"));
                    MessageBox.Show("Звіт успішно створено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при створенні звіту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}