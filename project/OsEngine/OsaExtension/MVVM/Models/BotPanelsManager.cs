using OsEngine.OsTrader;
using System;
using OsEngine.OsTrader.Panels;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.OsaExtension.MVVM.ViewModels;
using OsEngine.Entity;

namespace OsEngine.OsaExtension.MVVM.Models
{

    /// <summary>
    /// Менеджер BotPanel осы 
    /// </summary>
    public class BotPanelsManager
    {
        /// <summary>
        /// поле содеражащие роботов осы
        /// </summary> 
        public OsTraderMaster _master = new OsTraderMaster(StartProgram.IsOsTrader);

        public BotPanelsManager()
        {          
            _master.BotCreateEvent += _master_BotCreateEvent;
            _master.BotDeleteEvent += _master_BotDeleteEvent;
        }

        private void _master_BotDeleteEvent(BotPanel obj)
        {
            
        }

        private void _master_BotCreateEvent(BotPanel obj)
        {
           
        }

        /// <summary>
        /// заполняем свойство BotPanel
        /// </summary>
        void InitBotPanel()
        {
            //// _listBots = _botTradeMaster.PanelsArray;
            //List<BotPanel> ListBots = OsTraderMaster.Master.PanelsArray;
            //int count = ListBots.Count;
            //BotPanels.Clear();
            //for (int i = 0; i < count; i++)
            //{
            //    BotPanels.Add(ListBots[i]);
            //}
        }

        /// <summary>
        /// присвоили заголовкам  роботов WPF имена панелей осы
        /// </summary>
        public void InitSetingBotPanel()
        {

            //foreach (BotPanel panel in BotPanels) // перебрали все BotPanel осы
            //{
            //    BaseBotbVM myrob = new BaseBotbVM(); // создал экземпляр въюхи WPF робота

            //    myrob.Header = panel.NameStrategyUniq; ; // присвоил заголовку  робота WPF имя панели осы

            //    //myrob.DescriptionBot = bots[1].NameStrategyUniq;

            //    Robots.Add(myrob);// отправил экземпляр в колекцию с роботами WPF


            //}
        }

    }
}
   
