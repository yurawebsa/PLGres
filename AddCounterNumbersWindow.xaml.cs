using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace CounterPlg {
    public partial class AddCounterNumbersWindow : Window, INotifyPropertyChanged, IDataErrorInfo {
        private readonly Counter _counter;
        private readonly AppDbContext _dbContext;
        private string _startNumber;
        private int _count;
        private string _location;

        public string StartNumber
        {
            get => _startNumber;
            set
            {
                _startNumber = value;
                OnPropertyChanged(nameof(StartNumber));
            }
        }

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }

        public List<CounterNumber> GeneratedNumbers { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;
                if (columnName == nameof(StartNumber))
                {
                    if (string.IsNullOrWhiteSpace(StartNumber))
                    {
                        error = "Введіть початковий номер.";
                    }
                    else if (!long.TryParse(StartNumber, out long startNum) || startNum < 0)
                    {
                        error = "Номер має містити тільки цифри і бути невід’ємним.";
                    }
                }
                return error;
            }
        }

        public AddCounterNumbersWindow(Counter counter)
        {
            InitializeComponent();
            _counter = counter ?? throw new ArgumentNullException(nameof(counter));
            _dbContext = new AppDbContext();
            GeneratedNumbers = new List<CounterNumber>();
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка всіх полів
            if (!string.IsNullOrWhiteSpace(this[nameof(StartNumber)]))
            {
                MessageBox.Show(this[nameof(StartNumber)], "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Count <= 0)
            {
                MessageBox.Show("Кількість має бути більше 0.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(Location))
            {
                MessageBox.Show("Виберіть місце розташування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Завантажуємо всі існуючі номери з бази
            var existingNumbers = _dbContext.CounterNumbers
                .Select(n => n.Number)
                .ToList();

            GeneratedNumbers.Clear();
            var skippedNumbers = new List<string>();

            long startNum = long.Parse(StartNumber); // Уже перевірено, що це число
            for (int i = 0; i < Count; i++)
            {
                string newNumber = (startNum + i).ToString();
                if (existingNumbers.Contains(newNumber))
                {
                    skippedNumbers.Add(newNumber);
                    continue; // Пропускаємо номер, якщо він уже існує
                }

                GeneratedNumbers.Add(new CounterNumber
                {
                    Number = newNumber,
                    Location = Location,
                    CounterId = _counter.Id,
                    PersonalAccount = "",
                    Address = "",
                    SelectedMonth = null,
                    SelectedYear = null,
                    IsWrittenOff = false
                });
            }

            if (skippedNumbers.Any())
            {
                string message = $"Наступні номери вже існують і не були додані:\n{string.Join(", ", skippedNumbers)}\n";
                message += $"Додано {GeneratedNumbers.Count} унікальних номерів.";
                MessageBox.Show(message, "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (GeneratedNumbers.Count == 0)
            {
                MessageBox.Show("Жоден номер не додано, оскільки всі запропоновані номери вже існують.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                MessageBox.Show($"Успішно додано {GeneratedNumbers.Count} унікальних номерів.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            _dbContext.Dispose(); // Очищаємо контекст бази даних
            base.OnClosed(e);
        }
    }
}