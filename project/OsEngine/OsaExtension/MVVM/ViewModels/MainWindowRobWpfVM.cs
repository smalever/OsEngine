using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    /// <summary>
    /// VM для главного окна с роботами WPF
    /// </summary>
    public class MainWindowRobWpfVM : BaseVM
    {
        public MainWindowRobWpfVM() 
        {
            
        }

        #region Commands ===========================================================

        private DelegateCommand commandGetListBot;
        public DelegateCommand CommandGetListBot
        {
            get 
            {
                if (commandGetListBot == null)

                {
                    commandGetListBot = new DelegateCommand(GetListBot);
                }
                return commandGetListBot;
            }
        }

        #endregion

        #region Metods ============================================================

        void GetListBot(object o)
        {

        }

        #endregion
    }
}
