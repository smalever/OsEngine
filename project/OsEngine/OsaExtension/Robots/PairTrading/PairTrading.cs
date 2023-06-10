using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Line = OsEngine.Charts.CandleChart.Indicators.Line;

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
            // надо сделать авто заполнение формулы спреда и самих бумаг при создании бота
            _tabIndex.UserFormula = "A0/A1";

            Regime = CreateParameter("Regime", "On", new[] { "Off", "On" });
            VolumeInDollars = CreateParameter("VolumeInDollars", 50, 50.0m, 500, 10);
            //_lotEntrySize = CreateParameter("_lotEntrySize  ", 1m, 0.00001m, 100m, 0.00001m);
            //Volume2 = CreateParameter("Volume 2", 1, 1.0m, 50, 1);

            // добавим боллинжер для входа
            BollingerEntryLength = CreateParameter("BollingerEntry Length", 720, 100, 10000, 20);
            BollingerEntryDeviation = CreateParameter("BollingerEntry Deviation", 3, 0.5m, 5, 0.1m);
            _bollingerEntry = new Bollinger(name + "BollingerEntry", false);
            _bollingerEntry = (Bollinger)_tabIndex.CreateCandleIndicator(_bollingerEntry, "Prime");
            _bollingerEntry.ColorUp = System.Drawing.Color.LightBlue;
            _bollingerEntry.ColorDown = System.Drawing.Color.LightBlue;
            _bollingerEntry.Lenght = BollingerEntryLength.ValueInt;
            _bollingerEntry.Deviation = BollingerEntryDeviation.ValueDecimal;
            //_bollingerEntry.Save();

            // добавим боллинжер для стопов
            BollingerStopLength = CreateParameter("BollingerStop Length", 720, 100, 10000, 20);
            BollingerStopDeviation = CreateParameter("BollingerStop Deviation", 4.2m, 0.5m, 5, 0.1m);
            _bollingerStop = new Bollinger(name + "BollingerStop", false);
            _bollingerStop = (Bollinger)_tabIndex.CreateCandleIndicator(_bollingerStop, "Prime");
            _bollingerStop.ColorUp = System.Drawing.Color.Red;
            _bollingerStop.ColorDown = System.Drawing.Color.Red;
            _bollingerStop.Lenght = BollingerStopLength.ValueInt;
            _bollingerStop.Deviation = BollingerStopDeviation.ValueDecimal;
            //_bollingerStop.Save();

            // добавим MA центр болинжера
            _moving = new MovingAverage(name + "Moving", false);
            _moving = (MovingAverage)_tabIndex.CreateCandleIndicator(_moving, "Prime");
            _moving.Lenght = BollingerEntryLength.ValueInt;
            _moving.ColorBase = System.Drawing.Color.Yellow;


            //////линия верхнего стопа
            ///
            //_stopUpLevelLine = new Line(name + "_stopUpLevelLine", "Prime", false);
            //_stopUpLevelLine = (Line)_tabIndex.CreateCandleIndicator(_stopUpLevelLine, "Prime");
            //_stopUpLevelLine.ColorBase = System.Drawing.Color.Red;
            ////_stopUpLevelLine.TimeEnd = DateTime.MaxValue;

            //////линия нижнего стопа
            //_stopDownLevelLine = new Line(name + "_stopDownLevelLine", "Prime", false);
            //_stopDownLevelLine = (Line)_tabIndex.CreateCandleIndicator(_stopDownLevelLine, "Prime");
            //_stopDownLevelLine.ColorBase = System.Drawing.Color.Red;
            ////_stopDownLevelLine.TimeEnd = DateTime.MaxValue;


            #endregion конец подготовка вкладки ===================================




        }



        #region TradeLogic ==============================
        private void Trade(List<Candle> candles)
        {
            // значения переменных на сейчас

            bollingerEntryLastPriceUp = _bollingerEntry.ValuesUp[_bollingerEntry.ValuesUp.Count - 1];
            bollingerEntryLastPriceDown = _bollingerEntry.ValuesDown[_bollingerEntry.ValuesDown.Count - 1];
            bollingerEntrySpread = bollingerEntryLastPriceUp - bollingerEntryLastPriceDown;
            moving = _moving.Values[_moving.Values.Count - 1];
            bollingerEntryLastPriceCenter = bollingerEntrySpread / 2 + bollingerEntryLastPriceDown;
            currentIndexPrice = candles[candles.Count - 1].Close;


            // были выше болинжера
            if (currentIndexPrice > bollingerEntryLastPriceUp) { wereWeUpBollinger = true; }
            // были ниже болинжера
            if (currentIndexPrice < bollingerEntryLastPriceDown) { wereWeDownBollinger = true; }

            // надо ли открывать позы?
            if (!indexIsSell && !indexIsBuy)
            {
                OpenPairPositions();
            }

            // пересекли центр боллинжера с низу ? пора закрываться
            if (indexIsBuy && currentIndexPrice > bollingerEntryLastPriceCenter)
            {
                CloseAllPositions();
            }

            // пересекли центр боллинжера с верху ? пора закрываться
            if (indexIsSell && currentIndexPrice < bollingerEntryLastPriceCenter)
            {
                CloseAllPositions();
            }

            // если ушли за стоп болинжер
            if ((indexIsBuy && currentIndexPrice > stopUp)
                || (indexIsSell && currentIndexPrice < stopDown))
            {
                CloseAllPositions();
            }



        }

        private void OpenPairPositions()
        {

            // откроем на возврате за болинжер сверху
            if (currentIndexPrice < bollingerEntryLastPriceUp
                && wereWeUpBollinger)
            {
                _lotEntrySize = VolumeInDollars.ValueDecimal / _tab1.PriceCenterMarketDepth;
                _tab1.SellAtMarket(_lotEntrySize);

                _lotEntrySize = VolumeInDollars.ValueDecimal / _tab2.PriceCenterMarketDepth;
                _tab2.BuyAtMarket(_lotEntrySize);

                wereWeUpBollinger = false;
                indexIsSell = true;
                stopUp = _bollingerStop.ValuesUp[_bollingerStop.ValuesUp.Count - 1];

                SendNewLogMessage("stopUp " + stopUp.ToString(), LogMessageType.NoName);
                //MessageBox.Show("stopUp " + stopUp.ToString());

                // нарисуем линию стопа 
                //_stopUpLevelLine.Value = stopUp;
                //_stopUpLevelLine.Refresh();

            }

            // откроем на возврате за болинжер снизу
            if (currentIndexPrice > bollingerEntryLastPriceDown
                && wereWeDownBollinger)
            {
                _lotEntrySize = VolumeInDollars.ValueDecimal / _tab1.PriceCenterMarketDepth;
                _tab1.BuyAtMarket(_lotEntrySize);

                _lotEntrySize = VolumeInDollars.ValueDecimal / _tab2.PriceCenterMarketDepth;
                _tab2.SellAtMarket(_lotEntrySize);

                wereWeDownBollinger = false;
                indexIsBuy = true;
                stopDown = _bollingerStop.ValuesDown[_bollingerStop.ValuesDown.Count - 1];

                SendNewLogMessage("stopDown " + stopDown.ToString(), LogMessageType.NoName);
                //MessageBox.Show("stopDown " + stopDown.ToString());

                // нарисуем линию входа 
                //_stopDownLevelLine.Value = stopDown;
                //_stopDownLevelLine.Refresh();
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
            wereWeDownBollinger = false;
            wereWeUpBollinger = false;
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
        private StrategyParameterDecimal VolumeInDollars;
        /// <summary>
        /// объем второй бумаги
        /// </summary>
        private StrategyParameterDecimal Volume2;

        /// <summary>
        /// bollinger
        /// боллинжер входа
        /// </summary>
        private Bollinger _bollingerEntry;
        private StrategyParameterDecimal BollingerEntryDeviation;
        private StrategyParameterInt BollingerEntryLength;

        /// <summary>
        /// bollinger
        /// боллинжер стопа
        /// </summary>
        private Bollinger _bollingerStop;
        private StrategyParameterDecimal BollingerStopDeviation;
        private StrategyParameterInt BollingerStopLength;

        /// <summary>
        /// MA
        /// мувинг
        /// </summary>
        private MovingAverage _moving;
        private decimal moving;

        /// <summary>
        /// линия  верхнего стопа 
        /// </summary>
        //private  Line _stopUpLevelLine;

        /// <summary>
        /// линия  нижнего стопа 
        /// </summary>
        //private Line _stopDownLevelLine;
  
        /// <summary>
        /// цена верхнего болинжера
        /// </summary>
        private decimal bollingerEntryLastPriceUp;
        /// <summary>
        /// цена нижнего болинжера
        /// </summary>
        private decimal bollingerEntryLastPriceDown;
        /// <summary>
        /// цена центра болинжера
        /// </summary>
        private decimal bollingerEntryLastPriceCenter;
        /// <summary>
        /// размах болинжера (от низа до верха, типа волатильность)
        /// </summary>
        private decimal bollingerEntrySpread;

        /// <summary>
        /// текущая цена индекса
        /// </summary>
        private decimal currentIndexPrice;

        /// <summary>
        /// размер лота для входа
        /// </summary>
        private decimal _lotEntrySize;

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

        /// <summary>
        /// стоп верхний
        /// </summary>
        private decimal stopUp= decimal.MaxValue;

        /// <summary>
        /// стоп нижний
        /// </summary>
        private decimal stopDown=decimal.MinValue;




        #endregion end Fields ==============================

    }
}
