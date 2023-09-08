using OsEngine.Market.Servers;
using OsEngine.Market;
using OsEngine.OsaExtension.MVVM.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using OsEngine.OsTrader.Gui;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    /// <summary>
    /// Модель отображения выбора бумаг 
    /// </summary>
    public class ChangeEmitentVM : BaseVM
    {
        public ChangeEmitentVM(RobotBreakVM rob)
        {
             _robot = rob;

            Init();
        }   

        #region Свойства ===============================================================================

        public ObservableCollection<ExChenge> ExChanges { get; set; } = new ObservableCollection<ExChenge>();

        /// <summary>
        /// колекция классов бумаг на бирже 
        /// </summary>
        public ObservableCollection<EmitClasses> EmitClasses { get; set; } = new ObservableCollection<EmitClasses>();
        /// <summary>
        /// колеккция списков всех бумаг
        /// </summary>
        public ObservableCollection<Emitent> Securites { get; set; } = new ObservableCollection<Emitent>();

        public Emitent SelectedEmitent
        {
            get => _selectedEmitent;
            set
            {
                _selectedEmitent = value;
                OnPropertyChanged(nameof(SelectedEmitent));
            }
        }
        private Emitent _selectedEmitent;

        #endregion

        #region Поля ===================================================================================

        /// <summary>
        /// словарь имен классов бумаг
        /// </summary>
        Dictionary<string, List<Security>> _classes = new Dictionary<string, List<Security>>();

        private  RobotBreakVM _robot;
        /// <summary>
        ///  выбранный сервер
        /// </summary>
        private IServer _server = null;

        #endregion 

        #region Команды ===============================================================================

        private DelegateCommand commandSetEmitClass;
        public DelegateCommand CommandSetEmitClass
        {
            get
            {
                if (commandSetEmitClass == null)
                {
                    commandSetEmitClass = new DelegateCommand(SetEmitClass);
                }
                return commandSetEmitClass;
            }
        }

        private DelegateCommand commandSetExChange;
        public DelegateCommand CommandSetExChange
        {
            get
            {
                if (commandSetExChange == null)
                {
                    commandSetExChange = new DelegateCommand(SetExChange);
                }
                return commandSetExChange;
            }
        }
        private DelegateCommand commandChenge;
        public DelegateCommand CommandChenge
        {
            get
            {
                if (commandChenge == null)
                {
                    commandChenge = new DelegateCommand(Chenge);
                }
                return commandChenge;
            }
        }

        #endregion

        #region Методы ===============================================================================

        void Chenge(object o)
        {
            if (SelectedEmitent != null && SelectedEmitent.Security != null)
            {
                _robot.Server = _server;
                _robot.SelectedSecurity = SelectedEmitent.Security;
                _robot.StringPortfolios = _robot.GetStringPortfolios(_robot.Server);

            }
        }

        void SetEmitClass(object o)
        {
            string classEmit = (string)o;
            List<Security> securitList = _classes[classEmit];
            ObservableCollection<Emitent> emis = new ObservableCollection<Emitent>();   // собираем все бумаги 
            foreach (Security security in securitList)
            {
                emis.Add(new Emitent(security));
            }
            Securites = emis;
            OnPropertyChanged(nameof(Securites));
        }

        void SetExChange(object ob)
        {
            ServerType type = (ServerType)ob;

            List<IServer> servers = ServerMaster.GetServers(); // список подключенных серверов
            List<Security> securities = null;
            foreach (IServer server in servers)
            {
                if (server.ServerType == type)
                {
                    securities = server.Securities;
                    _server = server;
                    break;
                }
            }
            if (securities == null)
            {
                return;
            }

            _classes.Clear();
            EmitClasses.Clear();

            foreach (Security secu in securities)
            {
                if (_classes.ContainsKey(secu.NameClass))
                {
                    _classes[secu.NameClass].Add(secu);
                }
                else
                {
                    List<Security> secs = new List<Security>();
                    secs.Add(secu);
                    _classes.Add(secu.NameClass, secs);
                    EmitClasses.Add(new EmitClasses(secu.NameClass));
                }
            }
        }

        /// <summary>
        ///  Инициализация бумаг сервера для отображения 
        /// </summary>
        void Init()
        {
            List<IServer> servers = ServerMaster.GetServers();
            ExChanges.Clear();

            if (servers == null)
            {
                return;
            }

            foreach (IServer server in servers)
            {
                ExChanges.Add(new ExChenge(server.ServerType));

            }

            OnPropertyChanged(nameof(ExChanges));
        }

        #endregion
    }
}

