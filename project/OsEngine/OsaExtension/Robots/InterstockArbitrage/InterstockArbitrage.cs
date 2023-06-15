using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Diagnostics;

namespace OsEngine.OsaExtension.Robots.InterstockArbitrage
{
    [Bot("InterstockArbitrage")]
    internal class InterstockArbitrage : BotPanel
    {
        public InterstockArbitrage(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            #region  подготовка вкладки ===================================
            // делаем вкладки
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            _tab1.BestBidAskChangeEvent += _tab1_BestBidAskChangeEvent;
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];
            _tab2.BestBidAskChangeEvent += _tab2_BestBidAskChangeEvent;
            //_tab2.CandleFinishedEvent += _tab2_CandleFinishedEvent;
            //TabCreate(BotTabType.Index);
            //_tabIndex = TabsIndex[0];
            //_tabIndex.SpreadChangeEvent += _tabIndex_SpreadChangeEvent;

            Regime = CreateParameter("Regime", "On", new[] { "Off", "On" });
            VolumeInDollars = CreateParameter("VolumeInDollars", 8, 10.0m, 500, 10);






            #endregion конец подготовка вкладки ===================================
        }








        #region TradeLogic ==============================
        private void Trade()
        {



        }



        #endregion end TradeLogic ==============================




        #region Methods ==============================

        private void _tab1_BestBidAskChangeEvent(decimal currentBestBid, decimal currentBestAsk)
        {
            // если не разрешено торговать то возврат
            if (Regime.ValueString == "Off") { return; }
            // вкладки не готовы 
            if (_tab1.IsReadyToTrade == false || _tab2.IsReadyToTrade == false) { return; }


            //знаки после запятой приравняем
            //decimal numberOfDigits1 = _tab1.Securiti.DecimalsVolume;
            //decimal numberOfDigits2 = _tab2.Securiti.DecimalsVolume;
            //if (numberOfDigits1 > numberOfDigits2)
            //{
            //    numberOfDigits1 = numberOfDigits2;
            //}
            //if (numberOfDigits2 > numberOfDigits1)
            //{
            //    numberOfDigits2 = numberOfDigits1;
            //}
            //decimal numberOfDigits = numberOfDigits2;

            decimal _lotEntrySizeTab1 = СalculationVolumePosition(_tab1, VolumeInDollars.ValueDecimal);
            decimal _lotEntrySizeTab2 = СalculationVolumePosition(_tab2, VolumeInDollars.ValueDecimal);



            // buy1 > sell2
            decimal spread = _tab2.PriceBestBid - currentBestAsk;

            if (spread > 0.00001m)
            {
                //PrinTextDebug("spread " + spread + '\n'
                //    + " price1sell " + price1sell);


                // buy price1
                // sell price2
                //_tab1.BuyAtLimit(_lotEntrySize, _tab1.PriceBestAsk);
                //_tab2.SellAtLimit(_lotEntrySize, _tab2.PriceBestBid);


                //SendNewLogMessage("_lotEntrySizeTab1 " + _lotEntrySizeTab1
                //    + '\n'
                //    + " _tab1.BuyAtLimit " + price1sell
                //    + '\n'
                //    + " _tab2.SellAtLimit " + price2buy
                //    + '\n'
                //    + " spread " + spread, LogMessageType.NoName
                //    );
            }

        }


        private void _tab2_BestBidAskChangeEvent(decimal currentBestBid, decimal currentBestAsk)
        {
            // если не разрешено торговать то возврат
            if (Regime.ValueString == "Off") { return; }
            // вкладки не готовы 
            if (_tab1.IsReadyToTrade == false || _tab2.IsReadyToTrade == false)
            { return; }



        }

        //private void _tabIndex_SpreadChangeEvent(List<Candle> candles)
        //{
        //    // если не разрешено торговать то возврат
        //    if (Regime.ValueString == "Off") { return; }
        //    // вкладки не готовы 
        //    if (_tab1.IsReadyToTrade == false || _tab2.IsReadyToTrade == false)
        //    { return; }


        //    Trade(candles);

        //}

        /// <summary>
        /// пересчитывает значение с вкладки BotTabSimple _tabSimp из долларов decimal baks в необходимое количество монет 
        /// </summary>
        /// <param name="_tabSimp"> вкладка </param>
        private decimal СalculationVolumePosition(BotTabSimple _tabSimp, decimal VolumeInDollars)
        {
            if (_tabSimp.StartProgram == StartProgram.IsTester)
            {
                return Rounding(VolumeInDollars / _tabSimp.PriceCenterMarketDepth, _tabSimp.Securiti.DecimalsVolume);
            }
            if (_tabSimp.IsConnected && _tabSimp.StartProgram == StartProgram.IsOsTrader)
            {
                return Rounding(VolumeInDollars / _tabSimp.PriceCenterMarketDepth, _tabSimp.Securiti.DecimalsVolume);

            }
            else return 0;
        }

        /// <summary>
        /// округляет decimal value до int numbers чисел после запятой
        /// </summary>
        public decimal Rounding(decimal value, int numbers)
        {
            return decimal.Round(value, numbers, MidpointRounding.ToEven);
            //return chah;
        }

        /// <summary>
        /// вывод в дебаг текста 
        /// </summary>
        public static void PrinTextDebug(string text, string secondLine = "")
        {
            string Time = DateTime.Now.ToString("hh:mm:ss:ffff");
            string str = Time + " \n "
                + text + " \n "
                + secondLine + " " + "\n";
            Debug.WriteLine(str);
        }

        public override string GetNameStrategyType()
        {
            return nameof(InterstockArbitrage);
        }

        public override void ShowIndividualSettingsDialog()
        {
            return;
        }

        #endregion end Methods ==============================



        #region Fields ==============================
        /// <summary>
        /// вкладка с первым инструметом
        /// </summary>
        private BotTabSimple _tab1;
        /// <summary>
        /// вкладка со вторым инструментом
        /// </summary>
        private BotTabSimple _tab2;
        /// <summary>
        /// вкладка для индекса
        /// </summary>
        private BotTabIndex _tabIndex;

        /// <summary>
        /// режим торгуем - не торгуем
        /// </summary>
        private StrategyParameterString Regime;

        /// <summary>
        /// объем первой бумаги
        /// </summary>
        private StrategyParameterDecimal VolumeInDollars;





        #endregion end Fields ==============================







    }
}
