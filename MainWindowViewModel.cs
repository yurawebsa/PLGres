using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace CounterPlg {
    public enum Tab {
        Counters,
        History,
        Settings,
        Report
    }

    public class MainWindowViewModel : INotifyPropertyChanged {
        private readonly AppDbContext _dbContext;
        private readonly ExcelDataService _excelDataService;
        private bool _isShowingCounters = true;
        private string _dataGridTitle = "Лічильники";
        private CounterNumber _selectedCounterNumber;
        private ObservableCollection<CounterNumber> _currentCounterNumbers;
        private int _totalCounters;
        private int _writtenOffCounters;
        private int _remainingCounters;
        private ObservableCollection<ActionLog> _actionLogs;
        private Tab _selectedTab = Tab.Counters;
        private string _addressFilePath;
        private ReportViewModel _reportViewModel;

        public ObservableCollection<CounterViewModel> Counters { get; }
        public ObservableCollection<CounterNumber> CounterNumbers { get; }
        public ObservableCollection<ActionLog> ActionLogs
        {
            get => _actionLogs;
            set
            {
                _actionLogs = value;
                OnPropertyChanged(nameof(ActionLogs));
            }
        }

        public ReportViewModel ReportViewModel
        {
            get => _reportViewModel;
            set
            {
                _reportViewModel = value;
                OnPropertyChanged(nameof(ReportViewModel));
            }
        }

        private readonly CollectionViewSource _numbersViewSource;

        public ICollectionView NumbersView
        {
            get => _numbersViewSource.View;
        }

        private CounterViewModel _selectedCounter;
        public CounterViewModel SelectedCounter
        {
            get => _selectedCounter;
            set
            {
                if (_selectedCounter != value)
                {
                    _selectedCounter = value;
                    System.Diagnostics.Debug.WriteLine($"SelectedCounter changed to Id={_selectedCounter?.Id}");
                    if (_selectedCounter != null)
                    {
                        UpdateCurrentCounterNumbers(_selectedCounter.Id);
                    }
                    OnPropertyChanged(nameof(SelectedCounter));
                    OnPropertyChanged(nameof(CurrentCounterNumbers));
                }
            }
        }

        public CounterNumber SelectedCounterNumber
        {
            get => _selectedCounterNumber;
            set
            {
                _selectedCounterNumber = value;
                OnPropertyChanged(nameof(SelectedCounterNumber));
            }
        }

        public ObservableCollection<CounterNumber> CurrentCounterNumbers
        {
            get => _currentCounterNumbers;
            set
            {
                _currentCounterNumbers = value;
                OnPropertyChanged(nameof(CurrentCounterNumbers));
            }
        }

        public bool IsShowingCounters
        {
            get => _isShowingCounters;
            set
            {
                _isShowingCounters = value;
                OnPropertyChanged(nameof(IsShowingCounters));
                OnPropertyChanged(nameof(IsShowingNumbers));
            }
        }

        public bool IsShowingNumbers => !_isShowingCounters;

        public string DataGridTitle
        {
            get => _dataGridTitle;
            set
            {
                _dataGridTitle = value;
                OnPropertyChanged(nameof(DataGridTitle));
            }
        }

        public int TotalCounters
        {
            get => _totalCounters;
            set
            {
                _totalCounters = value;
                OnPropertyChanged(nameof(TotalCounters));
            }
        }

        public int WrittenOffCounters
        {
            get => _writtenOffCounters;
            set
            {
                _writtenOffCounters = value;
                OnPropertyChanged(nameof(WrittenOffCounters));
            }
        }

        public int RemainingCounters
        {
            get => _remainingCounters;
            set
            {
                _remainingCounters = value;
                OnPropertyChanged(nameof(RemainingCounters));
            }
        }

        public Tab SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    UpdateMainTitle();
                    OnPropertyChanged(nameof(SelectedTab));
                }
            }
        }

        public string AddressFilePath
        {
            get => _addressFilePath;
            set
            {
                _addressFilePath = value;
                OnPropertyChanged(nameof(AddressFilePath));
            }
        }

        public string MainTitle
        {
            get
            {
                switch (_selectedTab)
                {
                    case Tab.Counters:
                        return "Лічильники";
                    case Tab.History:
                        return "Історія дій";
                    case Tab.Settings:
                        return "Налаштування";
                    case Tab.Report:
                        return "Створити звіт";
                    default:
                        return "Ширяївське УЕГГ";
                }
            }
        }

        public ICommand AddCounterCommand { get; }
        public ICommand EditCounterCommand { get; }
        public ICommand DeleteCounterCommand { get; }
        public ICommand AddCounterNumbersCommand { get; }
        public ICommand WriteOffNumberCommand { get; }
        public ICommand ShowCountersCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowReportCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand IncrementCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand EditCounterNumberCommand { get; }
        public ICommand DeleteCounterNumberCommand { get; }
        public ICommand SelectAddressFileCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public MainWindowViewModel()
        {
            _dbContext = new AppDbContext();
            _dbContext.Database.EnsureCreated();
            _addressFilePath = Properties.Settings.Default.AccountsFilePath;
            if (string.IsNullOrWhiteSpace(_addressFilePath) || !System.IO.File.Exists(_addressFilePath))
            {
                _addressFilePath = @"D:\адреса.xlsx";
                Properties.Settings.Default.AccountsFilePath = _addressFilePath;
                Properties.Settings.Default.Save();
            }
            _excelDataService = new ExcelDataService(_addressFilePath);
            _reportViewModel = new ReportViewModel(_dbContext, _excelDataService);

            Counters = new ObservableCollection<CounterViewModel>();
            CounterNumbers = new ObservableCollection<CounterNumber>();
            CurrentCounterNumbers = new ObservableCollection<CounterNumber>();
            ActionLogs = new ObservableCollection<ActionLog>();
            _numbersViewSource = new CollectionViewSource { Source = CurrentCounterNumbers };
            _numbersViewSource.SortDescriptions.Add(new SortDescription("NumberAsInt", ListSortDirection.Ascending));

            // Реєстрація обробника для LogActionMessage
            Messenger.Instance.Register<LogActionMessage>(this, HandleLogActionMessage);

            LoadData();

            AddCounterCommand = new RelayCommand(AddCounter);
            EditCounterCommand = new RelayCommand(EditCounter);
            DeleteCounterCommand = new RelayCommand(DeleteCounter);
            AddCounterNumbersCommand = new RelayCommand(AddCounterNumbers);
            WriteOffNumberCommand = new RelayCommand(WriteOffNumber);
            ShowCountersCommand = new RelayCommand(() => ShowTab(Tab.Counters));
            ShowHistoryCommand = new RelayCommand(() => ShowTab(Tab.History));
            ShowSettingsCommand = new RelayCommand(() => ShowTab(Tab.Settings));
            ShowReportCommand = new RelayCommand(() => ShowTab(Tab.Report));
            LogoutCommand = new RelayCommand(Logout);
            IncrementCommand = new RelayCommand<string>(_ => { /* Заглушка для майбутньої логіки */ });
            ResetCommand = new RelayCommand<string>(_ => { /* Заглушка для майбутньої логіки */ });
            EditCounterNumberCommand = new RelayCommand(EditCounterNumber);
            DeleteCounterNumberCommand = new RelayCommand(DeleteCounterNumber);
            SelectAddressFileCommand = new RelayCommand(SelectAddressFile);
            GenerateReportCommand = new RelayCommand(GenerateReport);
        }

        private void GenerateReport()
        {
            ShowTab(Tab.Report);
        }

        private void HandleLogActionMessage(LogActionMessage message)
        {
            LogAction(message.Title, message.Icon);
        }

        private void ShowTab(Tab tab)
        {
            SelectedTab = tab;
            if (tab == Tab.Counters)
            {
                IsShowingCounters = true;
                DataGridTitle = "Лічильники";
                SelectedCounter = null;
                CurrentCounterNumbers.Clear();
            }
            else
            {
                IsShowingCounters = false;
                DataGridTitle = tab == Tab.Report ? "Списані лічильники" : "";
            }
        }

        private void UpdateMainTitle()
        {
            OnPropertyChanged(nameof(MainTitle));
        }

        private void SelectAddressFile()
        {
            Messenger.Instance.Send(new SelectFileMessage());
        }

        private void Logout()
        {
            Application.Current.Shutdown();
        }

        private void LogAction(string title, string icon)
        {
            var timestamp = DateTime.Now;
            var timestampStr = timestamp.ToString("dd.MM.yyyy HH:mm");
            var actionLog = new ActionLog
            {
                Title = title,
                Description = timestampStr,
                Icon = icon
            };
            System.Diagnostics.Debug.WriteLine($"Adding to ActionLogs: Title={title}, Description={timestampStr}, Icon={icon}");
            ActionLogs.Insert(0, actionLog);

            var actionLogEntity = new ActionLogEntity
            {
                Title = title,
                Description = timestampStr,
                Icon = icon,
                Timestamp = timestamp
            };
            _dbContext.ActionLogs.Add(actionLogEntity);
            _dbContext.SaveChanges();

            while (ActionLogs.Count > 9)
            {
                ActionLogs.RemoveAt(ActionLogs.Count - 1);
            }

            var logCount = _dbContext.ActionLogs.Count();
            if (logCount > 1000)
            {
                var oldestLogs = _dbContext.ActionLogs
                    .OrderBy(l => l.Timestamp)
                    .Take(logCount - 1000)
                    .ToList();
                _dbContext.ActionLogs.RemoveRange(oldestLogs);
                _dbContext.SaveChanges();
            }
        }

        private void LoadData()
        {
            System.Diagnostics.Debug.WriteLine("Loading data...");
            Counters.Clear();
            CounterNumbers.Clear();
            CurrentCounterNumbers.Clear();
            ActionLogs.Clear();

            var counters = _dbContext.Counters.ToList();
            var numbers = _dbContext.CounterNumbers.ToList();

            TotalCounters = numbers.Count;
            WrittenOffCounters = numbers.Count(n => n.IsWrittenOff);
            RemainingCounters = numbers.Count(n => !n.IsWrittenOff);

            foreach (var number in numbers)
            {
                if (number != null && !string.IsNullOrWhiteSpace(number.Number))
                {
                    CounterNumbers.Add(number);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping invalid number: Id={number?.Id}, Number={number?.Number}");
                }
            }

            foreach (var counter in counters)
            {
                var counterNumbers = numbers.Where(n => n.CounterId == counter.Id).ToList();
                int count = counterNumbers.Count;
                int skladCount = counterNumbers.Count(n => n.Location == "Склад" && !n.IsWrittenOff);
                int shyryaevoCount = counterNumbers.Count(n => n.Location == "Ширяєво" && !n.IsWrittenOff);
                int zakharivkaCount = counterNumbers.Count(n => n.Location == "Захарівка" && !n.IsWrittenOff);
                int writtenOffCount = counterNumbers.Count(n => n.IsWrittenOff);

                Counters.Add(new CounterViewModel
                {
                    Id = counter.Id,
                    CounterNumber = counter.CounterNumber,
                    Brand = counter.Brand,
                    KCM = counter.KCM,
                    NumbersCount = count,
                    SkladCount = skladCount,
                    ShyryaevoCount = shyryaevoCount,
                    ZakharivkaCount = zakharivkaCount,
                    WrittenOffCount = writtenOffCount
                });
            }

            var recentLogs = _dbContext.ActionLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(9)
                .Select(l => new ActionLog
                {
                    Title = l.Title,
                    Description = l.Description,
                    Icon = l.Icon
                })
                .ToList();

            foreach (var log in recentLogs)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded from DB: Title={log.Title}, Description={log.Description}, Icon={log.Icon}");
                ActionLogs.Add(log);
            }

            if (Counters.Any())
            {
                _selectedCounter = Counters.First();
                System.Diagnostics.Debug.WriteLine($"Set initial SelectedCounter to Id={_selectedCounter.Id}");
                UpdateCurrentCounterNumbers(_selectedCounter.Id);
                CurrentCounterNumbers = new ObservableCollection<CounterNumber>(CurrentCounterNumbers);
                OnPropertyChanged(nameof(CurrentCounterNumbers));
                OnPropertyChanged(nameof(SelectedCounter));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No counters available, CurrentCounterNumbers remains empty");
            }

            OnPropertyChanged(nameof(Counters));
            OnPropertyChanged(nameof(ActionLogs));
        }

        private void UpdateCounterStats()
        {
            var numbers = _dbContext.CounterNumbers.ToList();
            TotalCounters = numbers.Count;
            WrittenOffCounters = numbers.Count(n => n.IsWrittenOff);
            RemainingCounters = numbers.Count(n => !n.IsWrittenOff);
        }

        private void AddCounter()
        {
            var addWindow = new AddCounterWindow();
            if (addWindow.ShowDialog() == true)
            {
                var counterToAdd = addWindow.NewCounter;
                try
                {
                    _dbContext.Counters.Add(counterToAdd);
                    _dbContext.SaveChanges();

                    Counters.Add(new CounterViewModel
                    {
                        Id = counterToAdd.Id,
                        CounterNumber = counterToAdd.CounterNumber,
                        Brand = counterToAdd.Brand,
                        KCM = counterToAdd.KCM,
                        NumbersCount = 0
                    });

                    SelectedCounter = Counters.Last();
                    LogAction($"Додано лічильник {counterToAdd.CounterNumber}", "Plus");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка при збереженні лічильника: {ex}");
                    MessageBox.Show($"Помилка при збереженні лічильника: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditCounter()
        {
            if (SelectedCounter == null)
            {
                MessageBox.Show("Виберіть лічильник для редагування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var counterInDb = _dbContext.Counters.Find(SelectedCounter.Id);
            if (counterInDb == null) return;

            var editWindow = new AddCounterWindow(counterInDb);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    _dbContext.SaveChanges();
                    SelectedCounter.CounterNumber = counterInDb.CounterNumber;
                    SelectedCounter.Brand = counterInDb.Brand;
                    SelectedCounter.KCM = counterInDb.KCM;
                    LogAction($"Змінено лічильник {counterInDb.CounterNumber}", "Edit");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при збереженні змін: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteCounter()
        {
            if (SelectedCounter == null)
            {
                MessageBox.Show("Виберіть лічильник для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Ви впевнені, що хочете видалити цей лічильник разом з усіма номерами?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var counterInDb = _dbContext.Counters.Find(SelectedCounter.Id);
                var relatedNumbers = _dbContext.CounterNumbers.Where(n => n.CounterId == counterInDb.Id).ToList();

                _dbContext.CounterNumbers.RemoveRange(relatedNumbers);
                _dbContext.Counters.Remove(counterInDb);
                _dbContext.SaveChanges();

                foreach (var number in relatedNumbers)
                {
                    CounterNumbers.Remove(number);
                    CurrentCounterNumbers.Remove(number);
                }

                Counters.Remove(SelectedCounter);
                LogAction($"Видалено лічильник {counterInDb.CounterNumber}", "Trash");
                SelectedCounter = null;

                UpdateCounterStats();

                OnPropertyChanged(nameof(Counters));
            }
        }

        private void AddCounterNumbers()
        {
            if (SelectedCounter == null)
            {
                MessageBox.Show("Виберіть лічильник.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var counterInDb = _dbContext.Counters.Find(SelectedCounter.Id);
            if (counterInDb == null)
            {
                MessageBox.Show($"Обраний лічильник (Id={SelectedCounter.Id}) не знайдено в базі даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var addNumbersWindow = new AddCounterNumbersWindow(counterInDb);
            if (addNumbersWindow.ShowDialog() == true)
            {
                var numbersToAdd = addNumbersWindow.GeneratedNumbers;
                try
                {
                    Console.WriteLine($"Adding {numbersToAdd.Count} numbers for CounterId={counterInDb.Id}");
                    foreach (var number in numbersToAdd)
                    {
                        Console.WriteLine($"Number: Number={number.Number}, CounterId={number.CounterId}, Location={number.Location}");
                    }
                    _dbContext.CounterNumbers.AddRange(numbersToAdd);
                    _dbContext.SaveChanges();

                    foreach (var number in numbersToAdd)
                    {
                        CounterNumbers.Add(number);
                    }

                    Console.WriteLine($"Updating CurrentCounterNumbers after adding numbers for CounterId={SelectedCounter.Id}");
                    UpdateCurrentCounterNumbers(SelectedCounter.Id);

                    var counterNumbers = CounterNumbers.Where(n => n.CounterId == SelectedCounter.Id).ToList();
                    SelectedCounter.NumbersCount = counterNumbers.Count;
                    SelectedCounter.SkladCount = counterNumbers.Count(n => n.Location == "Склад" && !n.IsWrittenOff);
                    SelectedCounter.ShyryaevoCount = counterNumbers.Count(n => n.Location == "Ширяєво" && !n.IsWrittenOff);
                    SelectedCounter.ZakharivkaCount = counterNumbers.Count(n => n.Location == "Захарівка" && !n.IsWrittenOff);
                    SelectedCounter.WrittenOffCount = counterNumbers.Count(n => n.IsWrittenOff);

                    UpdateCounterStats();
                    LogAction($"Додано {numbersToAdd.Count} номерів до лічильника {SelectedCounter.CounterNumber}", "Plus");

                    OnPropertyChanged(nameof(SelectedCounter));
                    OnPropertyChanged(nameof(CurrentCounterNumbers));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving numbers: {ex}");
                    Console.WriteLine($"InnerException: {ex.InnerException?.Message}");
                    MessageBox.Show($"Помилка при збереженні номерів: {ex.Message}\nВнутрішня помилка: {ex.InnerException?.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditCounterNumber()
        {
            if (SelectedCounterNumber == null)
            {
                MessageBox.Show("Виберіть номер для редагування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new EditCounterNumberWindow(SelectedCounterNumber);
            if (editWindow.ShowDialog() == true)
            {
                var edited = editWindow.EditedNumber;

                var entity = _dbContext.CounterNumbers.Find(edited.Id);
                if (entity != null)
                {
                    entity.Number = edited.Number;
                    entity.PersonalAccount = edited.PersonalAccount;
                    entity.Address = edited.Address;
                    entity.Location = edited.Location;
                    entity.IsWrittenOff = edited.IsWrittenOff;
                    entity.SelectedMonth = edited.SelectedMonth;
                    entity.SelectedYear = edited.SelectedYear;

                    try
                    {
                        _dbContext.SaveChanges();
                        Console.WriteLine($"Saved edit for Number={entity.Number}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка при збереженні змін: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedCounterNumber.Number = edited.Number;
                    SelectedCounterNumber.PersonalAccount = edited.PersonalAccount;
                    SelectedCounterNumber.Address = edited.Address;
                    SelectedCounterNumber.Location = edited.Location;
                    SelectedCounterNumber.IsWrittenOff = edited.IsWrittenOff;
                    SelectedCounterNumber.SelectedMonth = edited.SelectedMonth;
                    SelectedCounterNumber.SelectedYear = edited.SelectedYear;

                    var counterNumbers = CounterNumbers.Where(n => n.CounterId == SelectedCounter.Id).ToList();
                    SelectedCounter.NumbersCount = counterNumbers.Count;
                    SelectedCounter.SkladCount = counterNumbers.Count(n => n.Location == "Склад" && !n.IsWrittenOff);
                    SelectedCounter.ShyryaevoCount = counterNumbers.Count(n => n.Location == "Ширяєво" && !n.IsWrittenOff);
                    SelectedCounter.ZakharivkaCount = counterNumbers.Count(n => n.Location == "Захарівка" && !n.IsWrittenOff);
                    SelectedCounter.WrittenOffCount = counterNumbers.Count(n => n.IsWrittenOff);

                    Console.WriteLine($"Updating CurrentCounterNumbers after edit for CounterId={SelectedCounter.Id}");
                    UpdateCurrentCounterNumbers(SelectedCounter.Id);

                    UpdateCounterStats();
                    LogAction($"Змінено № лічильника {entity.Number}", "Edit");

                    OnPropertyChanged(nameof(SelectedCounter));
                    OnPropertyChanged(nameof(CurrentCounterNumbers));
                }
            }
        }

        private void DeleteCounterNumber()
        {
            if (SelectedCounterNumber == null)
            {
                MessageBox.Show("Виберіть номер лічильника для видалення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Ви впевнені, що хочете видалити номер лічильника '{SelectedCounterNumber.Number}'?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var numberInDb = _dbContext.CounterNumbers.Find(SelectedCounterNumber.Id);
                if (numberInDb == null)
                {
                    MessageBox.Show($"Номер лічильника (Id={SelectedCounterNumber.Id}) не знайдено в базі даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    _dbContext.CounterNumbers.Remove(numberInDb);
                    _dbContext.SaveChanges();

                    CounterNumbers.Remove(SelectedCounterNumber);
                    CurrentCounterNumbers.Remove(SelectedCounterNumber);

                    var counterNumbers = CounterNumbers.Where(n => n.CounterId == SelectedCounter.Id).ToList();
                    SelectedCounter.NumbersCount = counterNumbers.Count;
                    SelectedCounter.SkladCount = counterNumbers.Count(n => n.Location == "Склад" && !n.IsWrittenOff);
                    SelectedCounter.ShyryaevoCount = counterNumbers.Count(n => n.Location == "Ширяєво" && !n.IsWrittenOff);
                    SelectedCounter.ZakharivkaCount = counterNumbers.Count(n => n.Location == "Захарівка" && !n.IsWrittenOff);
                    SelectedCounter.WrittenOffCount = counterNumbers.Count(n => n.IsWrittenOff);

                    UpdateCounterStats();
                    LogAction($"Видалено № лічильника {numberInDb.Number}", "Trash");

                    SelectedCounterNumber = null;

                    OnPropertyChanged(nameof(CurrentCounterNumbers));
                    OnPropertyChanged(nameof(SelectedCounter));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при видаленні номера: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateCurrentCounterNumbers(int counterId)
        {
            var numbers = CounterNumbers
                .Where(n => n.CounterId == counterId)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Updating CurrentCounterNumbers for CounterId={counterId}, Found {numbers.Count} numbers");
            foreach (var number in numbers)
            {
                System.Diagnostics.Debug.WriteLine($"Number: Id={number.Id}, Number={number.Number}, CounterId={number.CounterId}");
            }

            CurrentCounterNumbers.Clear();
            foreach (var number in numbers)
            {
                CurrentCounterNumbers.Add(number);
            }
            _numbersViewSource.Source = CurrentCounterNumbers;

            _numbersViewSource.SortDescriptions.Clear();
            _numbersViewSource.SortDescriptions.Add(new SortDescription("NumberAsInt", ListSortDirection.Ascending));

            _numbersViewSource.View.Refresh();
            OnPropertyChanged(nameof(CurrentCounterNumbers));
            OnPropertyChanged(nameof(NumbersView));
        }

        private void WriteOffNumber()
        {
            if (SelectedCounterNumber == null)
            {
                MessageBox.Show("Виберіть номер для списання.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var writeOffWindow = new WriteOffNumberWindow(SelectedCounterNumber);
            if (writeOffWindow.ShowDialog() == true)
            {
                var edited = writeOffWindow.EditedNumber;

                var entity = _dbContext.CounterNumbers.Find(edited.Id);
                if (entity != null)
                {
                    entity.PersonalAccount = edited.PersonalAccount;
                    entity.Address = edited.Address;
                    entity.Location = edited.Location;
                    entity.IsWrittenOff = true;
                    entity.SelectedMonth = edited.SelectedMonth;
                    entity.SelectedYear = edited.SelectedYear;

                    try
                    {
                        _dbContext.SaveChanges();
                        Console.WriteLine($"Saved write-off for Number={entity.Number}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка при збереженні змін: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    SelectedCounterNumber.PersonalAccount = edited.PersonalAccount;
                    SelectedCounterNumber.Address = edited.Address;
                    SelectedCounterNumber.Location = edited.Location;
                    SelectedCounterNumber.IsWrittenOff = true;
                    SelectedCounterNumber.SelectedMonth = edited.SelectedMonth;
                    SelectedCounterNumber.SelectedYear = edited.SelectedYear;

                    var counterNumbers = CounterNumbers.Where(n => n.CounterId == SelectedCounter.Id).ToList();
                    SelectedCounter.NumbersCount = counterNumbers.Count;
                    SelectedCounter.SkladCount = counterNumbers.Count(n => n.Location == "Склад" && !n.IsWrittenOff);
                    SelectedCounter.ShyryaevoCount = counterNumbers.Count(n => n.Location == "Ширяєво" && !n.IsWrittenOff);
                    SelectedCounter.ZakharivkaCount = counterNumbers.Count(n => n.Location == "Захарівка" && !n.IsWrittenOff);
                    SelectedCounter.WrittenOffCount = counterNumbers.Count(n => n.IsWrittenOff);

                    Console.WriteLine($"Updating CurrentCounterNumbers after write-off for CounterId={SelectedCounter.Id}");
                    UpdateCurrentCounterNumbers(SelectedCounter.Id);

                    UpdateCounterStats();
                    LogAction($"Списано № лічильника {entity.Number}", "UndoAlt");

                    OnPropertyChanged(nameof(SelectedCounter));
                    OnPropertyChanged(nameof(CurrentCounterNumbers));
                }
            }
        }

        public void ShowCounterNumbers()
        {
            if (SelectedCounter == null)
            {
                System.Diagnostics.Debug.WriteLine("ShowCounterNumbers skipped: SelectedCounter is null");
                return;
            }
            IsShowingCounters = false;
            DataGridTitle = $"{SelectedCounter.Brand}";
            UpdateCurrentCounterNumbers(SelectedCounter.Id);
            System.Diagnostics.Debug.WriteLine($"ShowCounterNumbers called for CounterId={SelectedCounter.Id}");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}