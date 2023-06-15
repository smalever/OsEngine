using OsEngine.OsTrader;
using System;
using OsEngine.OsTrader.Panels;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.OsaExtension.MVVM.ViewModels;
using OsEngine.Entity;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OsEngine.OsaExtension.MVVM.Models
{

    /// <summary>
    /// Менеджер роботов осы 
    /// </summary>
    public class BotPanelsManager
    {
        /// <summary>
        /// поле содеражащие роботов осы
        /// </summary> 
        public OsTraderMaster _botTradeMaster = new OsTraderMaster(StartProgram.IsOsTrader);

        public BotPanelsManager()
        {
          
            _botTradeMaster.BotCreateEvent += _master_BotCreateEvent;
            _botTradeMaster.BotDeleteEvent += _master_BotDeleteEvent;

            CreateBotWPF();
            InitRobotsIsBotPanel();
        }

        private void _master_BotDeleteEvent(BotPanel obj)
        {
            CreateBotWPF();
            InitRobotsIsBotPanel();
        }

        private void _master_BotCreateEvent(BotPanel obj)
        {
            CreateBotWPF();
            InitRobotsIsBotPanel();
        }

        /// <summary>
        /// создать робота из BotPanel
        /// </summary>
        public void CreateBotWPF()
        {//TODO: нужно изменить алгоритм добавления и удаления панелей по имени, что бы не переписывать весь список 

            List<BotPanel> ListBots = OsTraderMaster.Master.PanelsArray;
            int count = ListBots.Count;

            ObservableCollection<BotPanel> BotPan = new ObservableCollection<BotPanel>();    
            BotPan.Clear();

            for (int i = 0; i < count; i++)
            {
                BotPan.Add(ListBots[i]);
            }
            
            MainWindowRobWpfVM.BotPanels = BotPan;
        }

        /// <summary>
        /// инициализация полей BotPanel
        /// </summary> 
        public void InitRobotsIsBotPanel()
        {//TODO: нужно изменить алгоритм добавления и удаления панелей по имени, что бы не переписывать весь список 

            MainWindowRobWpfVM.Robots.Clear();

            foreach (BotPanel panel in MainWindowRobWpfVM.BotPanels) // перебрали все BotPanel осы
            {
                BaseBotVM myrob = new BaseBotVM(); // создал экземпляр въюхи WPF робота

                myrob.Header = panel.NameStrategyUniq; ; // присвоил заголовку  робота WPF имя панели осы

                MainWindowRobWpfVM.Robots.Add(myrob);// отправил экземпляр в колекцию с роботами WPF
            }
        }
    }
}
   
