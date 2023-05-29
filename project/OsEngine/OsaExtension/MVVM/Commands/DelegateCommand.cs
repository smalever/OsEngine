using System;
using System.Windows.Input;

namespace OsEngine.Commands
{
    public class DelegateCommand : ICommand
    {
        public DelegateCommand(DelegateFunction function)
        {
            _function = function;
        }

        public DelegateCommand(object v)
        {
            this.v = v;
        }

        public delegate void DelegateFunction(object obj);

        public DelegateFunction _function;
        private object v;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _function?.Invoke(parameter);
        }
    }
}
