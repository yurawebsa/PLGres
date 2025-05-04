using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace CounterPlg {
    public partial class WriteOffNumberWindow : Window {
        public CounterNumber EditedNumber { get; private set; }
        private ExcelDataService _excelDataService;
        private string _accountsFilePath;

        public WriteOffNumberWindow(CounterNumber numberToEdit)
        {
            InitializeComponent();

            // Завантажуємо збережений шлях із налаштувань
            _accountsFilePath = Properties.Settings.Default.AccountsFilePath;
            if (string.IsNullOrWhiteSpace(_accountsFilePath) || !System.IO.File.Exists(_accountsFilePath))
            {
                _accountsFilePath = null;
                InfoIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 57, 43));
                InfoIcon.ToolTip = "Додайте адресу в налаштуваннях";
            }
            else
            {
                InfoIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                InfoIcon.ToolTip = _accountsFilePath;
            }

            // Ініціалізація ExcelDataService
            if (!string.IsNullOrWhiteSpace(_accountsFilePath))
            {
                try
                {
                    _excelDataService = new ExcelDataService(_accountsFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при завантаженні Excel-файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _accountsFilePath = null;
                    InfoIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 57, 43));
                    InfoIcon.ToolTip = "Додайте адресу в налаштуваннях";
                }
            }

            // Отримуємо поточний місяць українською
            var currentUAMonth = CultureInfo
                .GetCultureInfo("uk-UA")
                .DateTimeFormat
                .GetMonthName(DateTime.Now.Month);

            // Якщо немає місяця — встановлюємо поточний
            var selectedMonth = string.IsNullOrWhiteSpace(numberToEdit.SelectedMonth)
                ? currentUAMonth
                : numberToEdit.SelectedMonth;

            var selectedYear = string.IsNullOrWhiteSpace(numberToEdit.SelectedYear)
                ? DateTime.Now.Year.ToString()
                : numberToEdit.SelectedYear;

            // Копія редагованого об’єкта
            EditedNumber = new CounterNumber
            {
                Id = numberToEdit.Id,
                CounterId = numberToEdit.CounterId,
                Number = numberToEdit.Number,
                PersonalAccount = numberToEdit.PersonalAccount,
                Address = numberToEdit.Address,
                Location = numberToEdit.Location,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                IsWrittenOff = numberToEdit.IsWrittenOff
            };

            // Заповнюємо роки
            YearComboBox.ItemsSource = Enumerable
                .Range(2000, DateTime.Now.Year - 2000 + 11)
                .Select(y => y.ToString())
                .ToList();

            DataContext = EditedNumber;

            // Встановлюємо вибраний місяць у ComboBox вручну
            foreach (ComboBoxItem item in MonthComboBox.Items)
            {
                if ((item.Content?.ToString() ?? "").Equals(EditedNumber.SelectedMonth, StringComparison.OrdinalIgnoreCase))
                {
                    MonthComboBox.SelectedItem = item;
                    break;
                }
            }

            // Якщо особовий рахунок уже заповнений, спробуємо підставити адресу
            if (!string.IsNullOrWhiteSpace(EditedNumber.PersonalAccount))
            {
                UpdateAddressFromPersonalAccount(EditedNumber.PersonalAccount);
            }
        }

        private void PersonalAccountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddressFromPersonalAccount(PersonalAccountTextBox.Text);
        }

        private void UpdateAddressFromPersonalAccount(string accountNumber)
        {
            if (_excelDataService == null || string.IsNullOrWhiteSpace(_accountsFilePath) || !System.IO.File.Exists(_accountsFilePath))
            {
                AddressTextBox.Text = "Помилка: Excel-файл не вибрано";
                EditedNumber.Address = "";
                return;
            }

            if (string.IsNullOrEmpty(accountNumber))
            {
                AddressTextBox.Text = "";
                EditedNumber.Address = "";
                return;
            }

            try
            {
                var address = _excelDataService.GetAddressByPersonalAccount(accountNumber);
                if (!string.IsNullOrWhiteSpace(address))
                {
                    AddressTextBox.Text = address;
                    EditedNumber.Address = address;
                }
                else
                {
                    AddressTextBox.Text = "Адреса не знайдена";
                    EditedNumber.Address = "";
                }
            }
            catch (Exception ex)
            {
                AddressTextBox.Text = "Помилка при зчитуванні даних";
                EditedNumber.Address = "";
                MessageBox.Show($"Помилка при зчитуванні даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Валідація полів
            if (string.IsNullOrWhiteSpace(EditedNumber.PersonalAccount))
            {
                MessageBox.Show("Введіть особовий рахунок.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditedNumber.Address))
            {
                MessageBox.Show("Введіть адресу.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditedNumber.Location))
            {
                MessageBox.Show("Виберіть місце розташування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditedNumber.SelectedMonth) || string.IsNullOrWhiteSpace(EditedNumber.SelectedYear))
            {
                MessageBox.Show("Виберіть місяць і рік.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка коректності місяця та року
            var monthNames = CultureInfo.GetCultureInfo("uk-UA").DateTimeFormat.MonthNames;
            int month = Array.FindIndex(monthNames, m => string.Equals(m, EditedNumber.SelectedMonth?.Trim(), StringComparison.OrdinalIgnoreCase)) + 1;

            if (!int.TryParse(EditedNumber.SelectedYear, out int year) || month <= 0)
            {
                MessageBox.Show("Виберіть коректний місяць і рік.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EditedNumber.IsWrittenOff = true;

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
    }
}