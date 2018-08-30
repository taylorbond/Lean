using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    public class AETHIchiAlgo : QCAlgorithm
    {
        private readonly Symbol _symbol = QuantConnect.Symbol.Create("ETHUSD", SecurityType.Crypto, Market.GDAX);
        private IchimokuKinkoHyo _ichi;
        private RollingWindow<decimal> _close;
        private int _runningPeriods;
        private decimal _entry = 0;


        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(DateTime.Now.Date.AddDays(-365));  //Set Start Date
            SetEndDate(DateTime.Now.Date.AddDays(-1));    //Set End Date
            SetCash(1000);             //Set Strategy Cash

            _close = new RollingWindow<decimal>(2);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Hour);
            _ichi = ICHIMOKU(_symbol, 20, 60, 30, 120, 30, 120, Resolution.Hour);
            _runningPeriods = 0;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {

            if (!_ichi.IsReady) return;


            _close.Add(data[_symbol].Close);


            var holding = Portfolio[_symbol];
            
            var SLMacd = ((_close[0] * Convert.ToDecimal(1.00225)) - _close[0]);
            var RStopInc = (_runningPeriods * ((_close[0] * Convert.ToDecimal(1.00115)) - _close[0]));
            var RStop = SLMacd + RStopInc;
            var DollarStopLoss = ((_close[0] * Convert.ToDecimal(1.01)) - _close[0]);
            

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity == 0 && _ichi.Tenkan > _ichi.Kijun && _ichi.SenkouA > _ichi.SenkouB && _close[0] > _ichi.Tenkan) // 0.01%
            {
                SetHoldings(_symbol, 0.9);
                _runningPeriods = 0;
                _entry = data[_symbol].Close;

            }
            else if (holding.Quantity >= 0)
            {
                if (_ichi.Tenkan < _ichi.Kijun || _close[0] < _ichi.Tenkan || _ichi.SenkouA < _ichi.SenkouB)
                {
                    Liquidate(_symbol);
                }
                // MACD crossed down under the MACD stop loss
                else if (holding.Quantity >= 0 && _close[0] <= SLMacd)
                {
                    Liquidate(_symbol);
                }
                // MACD went below our trailing stop on the MACD
                else if (holding.Quantity >= 0 && _close[0] <= RStop)
                {
                    Liquidate(_symbol);
                }
                else if (_close[0] < (_entry - DollarStopLoss))
                {
                    Liquidate(_symbol);
                }
                else if (holding.Quantity > 0)
                {
                    _runningPeriods++; // our trade is open
                }
            }



            // plot both lines
            //Plot("MACD", _macd, _macd.Signal);
            //Plot(_symbol, "Open", data[_symbol].Open);
            //Plot(_symbol, _macd.Fast, _macd.Slow);
            
        }
    }
}