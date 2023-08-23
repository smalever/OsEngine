using OsEngine.Entity;
using OsEngine.Market.Servers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OsEngine.OsaExtension.MVVM.ViewModels.GridRobotVM;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{

    /// <summary>
    /// интерфейс для VM роботов 
    /// </summary>
    public interface IRobotVM
    {
        /// <summary>
        /// заголовок робота
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// номер вкладки
        /// </summary>
        public int NumberTab { get; set; }

        /// <summary>
        /// название статегии 
        /// </summary>
        public NameStrat NameStrat { get; set; }

        /// <summary>
        /// событие подписки на бумагу
        /// </summary>
        //public event selectedSecurity OnSelectedSecurity;

        public IServer Server { get; set; }

        /// <summary>
        /// список портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; }

        /// <summary>
        /// выбранная бумага
        /// </summary>
        public Security SelectedSecurity { get; set; }
 
        /// <summary>
        /// метод сбора названий кошельков (бирж)
        /// </summary>
        public ObservableCollection<string> GetStringPortfolios(IServer server); 

    }
}
