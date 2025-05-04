using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CounterPlg {
    public class CounterNumber : INotifyPropertyChanged {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CounterId { get; set; }

        private string _number;
        public string Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(nameof(Number)); }
        }
        public int NumberAsInt
        {
            get
            {
                return int.TryParse(Number, out int result) ? result : int.MinValue;
            }
        }

        private string _location;
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        private string _personalAccount;
        public string PersonalAccount
        {
            get => _personalAccount;
            set { _personalAccount = value; OnPropertyChanged(nameof(PersonalAccount)); }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }

        private string? _selectedMonth;
        public string? SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                OnPropertyChanged(nameof(WriteOffDisplay)); // ДОДАЙ ЦЕ
            }
        }

        private string? _selectedYear;
        public string? SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                OnPropertyChanged(nameof(WriteOffDisplay)); // І ЦЕ ТАКОЖ
            }
        }

        private bool _isWrittenOff;
        public bool IsWrittenOff
        {
            get => _isWrittenOff;
            set { _isWrittenOff = value; OnPropertyChanged(nameof(IsWrittenOff)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        [NotMapped]
        public string WriteOffDisplay =>
    string.IsNullOrWhiteSpace(SelectedMonth) || string.IsNullOrWhiteSpace(SelectedYear)
        ? ""
        : $"{SelectedMonth} {SelectedYear}";
    }

}