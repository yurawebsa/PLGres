using System.ComponentModel;

namespace CounterPlg {
    public class CounterViewModel : Counter, INotifyPropertyChanged {
        private int _numbersCount;
        private string _counterNumber;
        private string _brand;
        private string _kcm;
        private int _skladCount;      // Кількість склад
        private int _shyryaevoCount;  // Кількість Ширяєво
        private int _zakharivkaCount; // Кількість Захарівка
        private int _writtenOffCount; // Кількість списано

        public new string CounterNumber
        {
            get => _counterNumber;
            set { _counterNumber = value; OnPropertyChanged(nameof(CounterNumber)); }
        }

        public new string Brand
        {
            get => _brand;
            set { _brand = value; OnPropertyChanged(nameof(Brand)); }
        }

        public new string KCM
        {
            get => _kcm;
            set { _kcm = value; OnPropertyChanged(nameof(KCM)); }
        }

        public int NumbersCount
        {
            get => _numbersCount;
            set { _numbersCount = value; OnPropertyChanged(nameof(NumbersCount)); OnPropertyChanged(nameof(RemainingCount)); }
        }

        public int SkladCount
        {
            get => _skladCount;
            set { _skladCount = value; OnPropertyChanged(nameof(SkladCount)); }
        }

        public int ShyryaevoCount
        {
            get => _shyryaevoCount;
            set { _shyryaevoCount = value; OnPropertyChanged(nameof(ShyryaevoCount)); }
        }

        public int ZakharivkaCount
        {
            get => _zakharivkaCount;
            set { _zakharivkaCount = value; OnPropertyChanged(nameof(ZakharivkaCount)); }
        }

        public int WrittenOffCount
        {
            get => _writtenOffCount;
            set { _writtenOffCount = value; OnPropertyChanged(nameof(WrittenOffCount)); OnPropertyChanged(nameof(RemainingCount)); }
        }

        public int RemainingCount => NumbersCount - WrittenOffCount; // Залишилося

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

