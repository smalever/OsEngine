using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace OsEngine.OsaExtension.Robots.PairTrading
{
    [Bot("PairTrading")]

    internal class PairTrading : BotPanel
    {

        public PairTrading(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            #region  подготовка вкладки ===================================

            // делаем вкладки
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            //_tab1.CandleFinishedEvent += _tab1_CandleFinishedEvent;
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];
            //_tab2.CandleFinishedEvent += _tab2_CandleFinishedEvent;
            TabCreate(BotTabType.Index);
            _tabIndex = TabsIndex[0];
            _tabIndex.SpreadChangeEvent += _tabIndex_SpreadChangeEvent;
            //_tabIndex.CandleFinishedEvent += _tabIndex_CandleFinishedEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Volume1 = CreateParameter("Volume 1", 1, 1.0m, 50, 1);
            Volume2 = CreateParameter("Volume 2", 1, 1.0m, 50, 1);

            // добавим боллинжер
            BollingerEntryLength = CreateParameter("BollingerEntry Length", 720, 100, 10000, 20);
            BollingerEntryDeviation = CreateParameter("BollingerEntry Deviation", 3, 0.5m, 5, 0.1m);
            _bollingerEntry = new Bollinger(name + "Bollinger", false);
            _bollingerEntry = (Bollinger)_tabIndex.CreateCandleIndicator(_bollingerEntry, "Prime");
            _bollingerEntry.Lenght = BollingerEntryLength.ValueInt;
            _bollingerEntry.Deviation = BollingerEntryDeviation.ValueDecimal;
            //_bollingerEntry.Save();

            // добавим MA центр болинжера
            _moving = new MovingAverage(name + "Moving", false);
            _moving = (MovingAverage)_tabIndex.CreateCandleIndicator(_moving, "Prime");
            _moving.Lenght = BollingerEntryLength.ValueInt;
            _moving.ColorBase = System.Drawing.Color.Yellow;

            //_bollingerEntry = IndicatorsFactory.CreateIndicatorByName("Bollinger", name + "Bollinger", false);
            //_bollingerEntry = (Aindicator)_tabIndex.CreateCandleIndicator(_bollingerEntry, "Prime");
            //_bollingerEntry.ParametersDigit[0].Value = BollingerEntryLength.ValueInt;
            //_bollingerEntry.ParametersDigit[1].Value = BollingerEntryDeviation.ValueDecimal;

            //линия входа в позу
            //entryPositionPriceLevelLine = new LineHorisontal("entryPositionPriceLevelLine", "Prime", false)
            //{ Color = Color.YellowGreen, Value = 0, TimeEnd = DateTime.MaxValue };
            //_tabIndex.SetChartElement(entryPositionPriceLevelLine);


            _lotEntrySize = CreateParameter("_lotEntrySize  ", 1m, 0.00001m, 100m, 0.00001m);


            #endregion конец подготовка вкладки ===================================




        }



        #region TradeLogic ==============================
        private void Trade(List<Candle> candles)
        {
            // значения переменных на сейчас

            bollingerLastPriceUp = _bollingerEntry.ValuesUp[_bollingerEntry.ValuesUp.Count - 1];
            bollingerLastPriceDown = _bollingerEntry.ValuesDown[_bollingerEntry.ValuesDown.Count - 1];
            bollingerSpread = bollingerLastPriceUp - bollingerLastPriceDown;
            moving = _moving.Values[_moving.Values.Count - 1];
            bollingerLastPriceCenter = bollingerSpread / 2 + bollingerLastPriceDown;

            //bollingerLastPriceUp = _bollingerEntry.DataSeries[0].Last;
            //bollingerLastPriceDown = _bollingerEntry.DataSeries[1].Last;
            //bollingerLastPriceCenter = _bollingerEntry.DataSeries[2].Last;
            //bollingerSpread = bollingerLastPriceUp - bollingerLastPriceDown;
            ////currentIndexPrice = _tabIndex.PriceCenterMarketDepth;
            currentIndexPrice = candles[candles.Count - 1].Close;


            // были выше болинжера
            if (currentIndexPrice > bollingerLastPriceUp) { wereWeUpBollinger = true; }
            // были ниже болинжера
            if (currentIndexPrice < bollingerLastPriceDown) { wereWeDownBollinger = true; }

            // надо ли открывать позы?
            if (!indexIsSell || !indexIsBuy) { OpenPairPositions(); }

            // пересекли центр боллинжера с низу ? пора закрываться
            if (indexIsBuy && currentIndexPrice > bollingerLastPriceCenter)
            {
                CloseAllPositions();
            }

            // пересекли центр боллинжера с верху ? пора закрываться
            if (indexIsSell && currentIndexPrice < bollingerLastPriceCenter)
            {
                CloseAllPositions();
            }



        }

        private void OpenPairPositions()
        {


            // откроем на возврате за болинжер сверху
            if (currentIndexPrice < bollingerLastPriceUp
                && wereWeUpBollinger)
            {
                _tab1.SellAtMarket(_lotEntrySize.ValueDecimal);
                _tab2.BuyAtMarket(_lotEntrySize.ValueDecimal);

                wereWeUpBollinger = false;
                indexIsSell = true;

                // нарисуем линию входа 
                //entryPositionPriceLevelLine.Value = currentIndexPrice;
                //entryPositionPriceLevelLine.Refresh();

            }

            // откроем на возврате за болинжер снизу
            if (currentIndexPrice > bollingerLastPriceDown
                && wereWeDownBollinger)
            {
                _tab1.BuyAtMarket(_lotEntrySize.ValueDecimal);
                _tab2.SellAtMarket(_lotEntrySize.ValueDecimal);
                wereWeDownBollinger = false;
                indexIsBuy = true;

                // нарисуем линию входа 
                //entryPositionPriceLevelLine.Value = currentIndexPrice;
                //entryPositionPriceLevelLine.Refresh();
            }
        }


        private void CloseAllPositions()
        {
            //List<Position> positions1 = _tab1.PositionsOpenAll;
            //List<Position> positions2 = _tab2.PositionsOpenAll;

            foreach (Position position in _tab1.PositionsOpenAll)
            {
                if (position.State == PositionStateType.Open)
                {
                    _tab1.CloseAtMarket(position, position.OpenVolume);
                }
            }

            foreach (Position position in _tab2.PositionsOpenAll)
            {
                if (position.State == PositionStateType.Open)
                {
                    _tab2.CloseAtMarket(position, position.OpenVolume);
                }
            }

            //if (positions1.Count != 0 && positions1[0].State == PositionStateType.Open)
            //{
            //    _tab1.CloseAtMarket(positions1[0], positions1[0].OpenVolume);
            //}
            //if (positions2.Count != 0 && positions2[0].State == PositionStateType.Open)
            //{
            //    _tab2.CloseAtMarket(positions2[0], positions2[0].OpenVolume);
            //}

            indexIsSell = false;
            indexIsBuy = false;
        }

        #endregion end TradeLogic ==============================


        #region Methods ==============================
        /// <summary>
        ///открылась новая свечка на вкладке индекса
        /// </summary>
        private void _tabIndex_SpreadChangeEvent(List<Candle> candles)
        {
            // если не разрешено торговать то возврат
            if (Regime.ValueString == "Off") { return; }
            // вкладки не готовы 
            if (_tab1.IsReadyToTrade == false || _tab2.IsReadyToTrade == false)
            { return; }

            // ждем накопления свечек  для корректного расчета болинжера
            if (candles.Count < BollingerEntryLength.ValueInt + 2)
            { return; }



            Trade(candles);
        }

        private void _tab1_CandleFinishedEvent(List<Candle> list)
        {
            throw new NotImplementedException();
        }
        private void _tab2_CandleFinishedEvent(List<Candle> list)
        {
            throw new NotImplementedException();
        }

        public override string GetNameStrategyType()
        {
            return nameof(PairTrading);
        }

        public override void ShowIndividualSettingsDialog()
        {


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
        private StrategyParameterDecimal Volume1;
        /// <summary>
        /// объем второй бумаги
        /// </summary>
        private StrategyParameterDecimal Volume2;
        /// <summary>
        /// bollinger
        /// боллинжер
        /// </summary>
        private Bollinger _bollingerEntry;
        private StrategyParameterDecimal BollingerEntryDeviation;
        private StrategyParameterInt BollingerEntryLength;
        /// <summary>
        /// MA
        /// мувинг
        /// </summary>
        private MovingAverage _moving;
        private decimal moving;


        ///// <summary>
        ///// боллинжер 
        ///// </summary>
        //private Aindicator _bollingerEntry;
        //private StrategyParameterDecimal BollingerEntryDeviation;
        //private StrategyParameterInt BollingerEntryLength;



        /// <summary>
        /// линия  цены входа на график 
        /// </summary>
        private LineHorisontal entryPositionPriceLevelLine;

        /// <summary>
        /// цена верхнего болинжера
        /// </summary>
        private decimal bollingerLastPriceUp;
        /// <summary>
        /// цена нижнего болинжера
        /// </summary>
        private decimal bollingerLastPriceDown;
        /// <summary>
        /// цена центра болинжера
        /// </summary>
        private decimal bollingerLastPriceCenter;
        /// <summary>
        /// размах болинжера (от низа до верха, типа волатильность)
        /// </summary>
        private decimal bollingerSpread;

        /// <summary>
        /// текущая цена индекса
        /// </summary>
        private decimal currentIndexPrice;

        /// <summary>
        /// размер лота для входа
        /// </summary>
        private StrategyParameterDecimal _lotEntrySize;

        /// <summary>
        /// признак что были выше верхнего болинжер
        /// </summary>
        bool wereWeUpBollinger = false;
        /// <summary>
        /// признак что были ниже нижнего болинжер
        /// </summary>
        bool wereWeDownBollinger = false;
        /// <summary>
        /// признак что продали индекс
        /// </summary>
        bool indexIsSell = false;
        /// <summary>
        /// признак что купили индекс
        /// </summary>
        bool indexIsBuy = false;




        #endregion end Fields ==============================

    }
}
