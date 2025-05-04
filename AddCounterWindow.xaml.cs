using System.ComponentModel;
using System.Windows;

namespace CounterPlg {
    public partial class AddCounterWindow : Window, INotifyPropertyChanged {
        public Counter NewCounter { get; private set; }
        private string _windowTitle;

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AddCounterWindow()
        {
            InitializeComponent();
            NewCounter = new Counter
            {
                CounterNumber = "",
                Brand = "Новий лічильник",
                KCM = "0"
            };
            WindowTitle = "Додати лічильник";
            DataContext = this;
        }

        public AddCounterWindow(Counter counter)
        {
            InitializeComponent();
            NewCounter = counter ?? throw new ArgumentNullException(nameof(counter));
            WindowTitle = "Виправити лічильник";
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCounter.CounterNumber))
            {
                MessageBox.Show("Номер лічильника не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(NewCounter.CounterNumber, out _))
            {
                MessageBox.Show("Номер лічильника має бути числом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewCounter.Brand))
            {
                MessageBox.Show("Бренд не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewCounter.KCM))
            {
                MessageBox.Show("KCM не може бути порожнім.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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
    }
}