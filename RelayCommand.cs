using System;
using System.Windows.Input;

namespace CounterPlg {
    public class RelayCommand : ICommand {
        private readonly Action _execute;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand {
        private readonly Action<T> _execute;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute((T)parameter);
    }
}
