using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Indicators;
using System.Windows.Forms;
using System.Linq;

[Bot("PriceChanelFix")]


public class PriceChanelFix : BotPanel
{
    public PriceChanelFix(string name, StartProgram startProgram) : base(name, startProgram)
    {
        #region  подготовка вкладки ===================================
        // делаем вкладку
        this.TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Mode = this.CreateParameter("Mode", "Off", new[] { "Off", "On" });

        // parameters for indicator PriceChannel
        LengthChannelUp = CreateParameter("Length Channel Up", 12, 5, 80, 2);
        LengthChannelDown = CreateParameter("Length Channel Down", 12, 5, 80, 2);
        // add indicator PriceChannel
        _pc = IndicatorsFactory.CreateIndicatorByName("PriceChannel",name + "PriceChannel", false);
        _pc.ParametersDigit[0].Value = LengthChannelUp.ValueInt;
        _pc.ParametersDigit[1].Value = LengthChannelDown.ValueInt;
        _pc = (Aindicator)_tab.CreateCandleIndicator(_pc, "Prime"); // 
        _pc.Save();
 

        Lot = CreateParameter("Lot", 1, 5, 20, 1);

        RiskInPercents = CreateParameter("RiskInPercents", 1m, 1m, 10m, 0.1m);
        #endregion конец подготовка вкладки ===================================

        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;


    }







    #region Trade Logic ==============================

    // 
    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {
        if (Mode.ValueString == "Off") return;
        
        // защита чтоб индикатор уже набрался свечек
        if (
            _pc.DataSeries[0].Values == null
            || _pc.DataSeries[1].Values == null
            || _pc.DataSeries[0].Values.Count < LengthChannelUp.ValueInt + 1
            || _pc.DataSeries[1].Values.Count < LengthChannelDown.ValueInt + 1
            ) return;

        Candle candle = candles[candles.Count - 1];

        // предыдущее за последней свечкой
        decimal lastUp = _pc.DataSeries[0].Values[_pc.DataSeries[0].Values.Count - 2];
        decimal lastDown = _pc.DataSeries[1].Values[_pc.DataSeries[1].Values.Count - 2];

        List<Position> positions = _tab.PositionsOpenAll;

        if (candle.Close > lastUp
            && candle.Open<lastUp
            && positions.Count ==0)
        {

            // сколько дененег от депо на 1 лот 
            decimal riskMoney = _tab.Portfolio.ValueBegin * RiskInPercents.ValueDecimal / 100;
            // получим стоимость шага цены
            decimal costPriceStep = _tab.Securiti.PriceStepCost;
            

            // закоментить на боевом
            costPriceStep = 1;

            // шагов цены на стоп 
            decimal steps = (lastUp - lastDown) / _tab.Securiti.PriceStepCost ;

            // 
            decimal lot = riskMoney / (steps * costPriceStep);

            // _tab.BuyAtMarket(Lot.ValueInt);
            _tab.BuyAtMarket((int)lot);

        }

        if (positions.Count > 0)
        {
            Trailing(positions);
        }



    }
    #endregion end Trade Logic ==============================




    #region Methods ==============================


    private void Trailing(List<Position> positions)
    {
        // последнее значение нижней линии индикатора прайсченел
        decimal lastDown = _pc.DataSeries[1].Values[_pc.DataSeries[1].Values.Count - 1];

        foreach (Position pos in positions)
        {
            if (pos.State == PositionStateType.Open)
            {
                if (pos.Direction == Side.Buy)
                {
                    _tab.CloseAtTrailingStop(pos, lastDown, lastDown - 10 * _tab.Securiti.PriceStep);
                }
            }

        }
    }


    // вернуть имя
    public override string GetNameStrategyType()
    {
        return nameof(PriceChanelFix);
    }
    // для настроек
    public override void ShowIndividualSettingsDialog()
    {

    }

    #endregion end Methods ==============================




    #region Fields ==============================
    /// <summary>
    /// вкладка для торговли
    /// </summary>
    private BotTabSimple _tab;

    /// <summary>
    /// PriceChannel
    /// </summary>
    private Aindicator _pc;

    /// <summary>
    /// PriceChannel up line length
    /// период PriceChannel Up
    /// </summary>
    private StrategyParameterInt LengthChannelUp;

    /// <summary>
    /// PriceChannel down line length
    /// период PriceChannel Down
    /// </summary>
    private StrategyParameterInt LengthChannelDown;

    /// <summary>
    /// режим торгуем - редактируем
    /// </summary>
    private StrategyParameterString Mode;

    /// <summary>
    /// кол-во лотов на сделку
    /// </summary>
    private StrategyParameterInt Lot;

    /// <summary>
    /// риск на сделку в процентах
    /// </summary>
    private StrategyParameterDecimal RiskInPercents;


    #endregion end Fields ==============================


}

