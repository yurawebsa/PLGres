using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontAwesome.Sharp;
using Microsoft.Win32;

namespace CounterPlg {
    public partial class MainWindow : Window {
        private double _normalWidth;
        private double _normalHeight;
        private double _normalLeft;
        private double _normalTop;
        private bool _isCustomMaximized;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            _normalWidth = Width;
            _normalHeight = Height;
            _normalLeft = (SystemParameters.PrimaryScreenWidth - _normalWidth) / 2;
            _normalTop = (SystemParameters.PrimaryScreenHeight - _normalHeight) / 2;

            MaximizeToWorkArea();
            _isCustomMaximized = true;
            MaximizeRestoreIcon.Icon = IconChar.WindowRestore;

            // Реєстрація обробника повідомлення для вибору файлу
            Messenger.Instance.Register<SelectFileMessage>(this, HandleSelectFileMessage);
        }

        private void HandleSelectFileMessage(SelectFileMessage message)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel файли (*.xlsx)|*.xlsx|Усі файли (*.*)|*.*",
                Title = "Виберіть файл із адресами"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Properties.Settings.Default.AccountsFilePath = filePath;
                Properties.Settings.Default.Save();

                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.AddressFilePath = filePath;
                    // Відправлення повідомлення для логування
                    Messenger.Instance.Send(new LogActionMessage($"Оновлено файл адрес: {filePath}", "File"));
                }
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isCustomMaximized)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCustomMaximized)
            {
                Width = _normalWidth;
                Height = _normalHeight;
                Left = _normalLeft;
                Top = _normalTop;
                _isCustomMaximized = false;
                MaximizeRestoreIcon.Icon = IconChar.WindowMaximize;
            }
            else
            {
                MaximizeToWorkArea();
                _isCustomMaximized = true;
                MaximizeRestoreIcon.Icon = IconChar.WindowRestore;
            }
        }

        private void MaximizeToWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CounterViewModel counter)
            {
                System.Diagnostics.Debug.WriteLine($"Selection changed to Counter: Id={counter.Id}, CounterNumber={counter.CounterNumber}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Selection changed: No valid Counter selected");
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CounterViewModel counter)
            {
                var viewModel = DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Double-clicked Counter: Id={counter.Id}, CounterNumber={counter.CounterNumber}");
                    viewModel.SelectedCounter = counter; // Оновлюємо SelectedCounter
                    viewModel.ShowCounterNumbers(); // Викликаємо ShowCounterNumbers
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Double-click ignored: Invalid DataGrid or SelectedItem");
            }
        }
    }
}
