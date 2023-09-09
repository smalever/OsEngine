using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OsEngine.OsaExtension.MVVM.Commands
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

#pragma warning disable CS0067 // Событие "DelegateCommand.CanExecuteChanged" никогда не используется.
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067 // Событие "DelegateCommand.CanExecuteChanged" никогда не используется.

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
