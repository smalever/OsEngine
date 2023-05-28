/*
 * вводим пару валют в одну строку типа TRBUSDT/AAVEUSDT
 * вводим сумму на которую хотим открыть пару
 * выбираем направление торговли Long Short
 * вводим ожидаемую прибыль в процентах на парную сделку
 * 
 * исходя из текущих цен по каждой бумаги бот должен посчитать количество лотов на каждую бумагу
 * и выставить соответственно если Long то Long по первой бумаге и Short  по второй
 * и наоборот если выбран Short d настройках
 * 
 * после открытия позиции ведет суммарную позицию до ожидаемой прибыли от сделки в процентах
 * после достижения ожидаемой прибыли начинает трейлить каждую ногу на размер трейлинг в процентах от начальной сделки
 * 
 *  после закрытия обоих ног удаляет себя и закрывает за собой вкладки
 * 
 */


/*
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;

namespace OsEngine.Robots.MyBots.PairTrade
{
    [Bot("PairTrade")]
    public class PairTrade : BotPanel
    {

        public PairTrade(string name, StartProgram startProgram) : base(name, startProgram)
        {



            TabCreate(BotTabType.Index);
            _tabSynthetics = TabsIndex[0];

            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];


            Regime = CreateParameter("Regime", "On", new[] { "Off", "On", "ClosePosition" });



            // open leg1 leg2

        }





        #region TradeLogic ==============================

        /// <summary>
        /// trade logic
        /// торговая логика
        /// </summary>
        private void OpenPairLegs(Leg1, List<Candle> candlesTab2, List<Candle> candlesIndex)
        {



            // open leg1
            // open leg2



        }





            #endregion TradeLogic ==============================





            #region Methods ==============================


            private void LogicOpenPosition1(BotTabSimple tab, decimal lastRsi)
        {


        }

        private void LogicOpenPosition2(BotTabSimple tab, decimal lastRsi)
        {


        }

        public override string GetNameStrategyType()
        {
            return "PairTrade";
        }

        public override void ShowIndividualSettingsDialog()
        {
            // nothing
        }

        #endregion Methods ==============================




        #region Fields ==============================

        /// <summary>
        /// index tab
        /// вкладка для формирования синтетики пары
        /// </summary>
        private BotTabIndex _tabSynthetics;

        /// <summary>
        /// trade tab
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab1;

        /// <summary>
        /// trade tab
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab2;

        /// <summary>
        /// regime
        /// режим работы робота
        /// </summary>
        public StrategyParameterString Regime;






        #endregion Fields ==============================

    }
}
// */