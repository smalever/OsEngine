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
            InitSetingBotPanel();
        }

        private void _master_BotDeleteEvent(BotPanel obj)
        {
            CreateBotWPF();
            InitSetingBotPanel();
        }

        private void _master_BotCreateEvent(BotPanel obj)
        {
            CreateBotWPF();
            InitSetingBotPanel();
        }

        /// <summary>
        /// создать робота из BotPanel
        /// </summary>
        public void CreateBotWPF()
        {
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

        public void InitSetingBotPanel()
        {
            MainWindowRobWpfVM.Robots.Clear();

            foreach (BotPanel panel in MainWindowRobWpfVM.BotPanels) // перебрали все BotPanel осы
            {
                BaseBotbVM myrob = new BaseBotbVM(); // создал экземпляр въюхи WPF робота

                myrob.Header = panel.NameStrategyUniq; ; // присвоил заголовку  робота WPF имя панели осы

                //myrob.DescriptionBot = bots[1].NameStrategyUniq;

                MainWindowRobWpfVM.Robots.Add(myrob);// отправил экземпляр в колекцию с роботами WPF
            }
        }
    }
}
   
