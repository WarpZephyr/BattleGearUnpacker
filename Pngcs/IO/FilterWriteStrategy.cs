using System;

namespace Pngcs.IO
{
    /// <summary>
    /// Manages the writer strategy for selecting the internal png predictor filter.
    /// </summary>
    internal class FilterWriteStrategy
    {
        private static readonly int COMPUTE_STATS_EVERY_N_LINES = 8;

        private readonly ImageInfo _imgInfo;
        private readonly FilterType _configuredType; // can be negative (fin dout) 
        private FilterType _currentType; // 0-4 
        private int _lastRowTested = -1000000;
        private readonly double[] _lastSums = new double[5];// performance of each filter (less is better) (can be negative)
        private readonly double[] _lastEntropies = new double[5];
        private double[] _preference = new double[] { 1.1, 1.1, 1.1, 1.1, 1.2 }; // a priori preference (NONE SUB UP AVERAGE PAETH)
        private readonly int _discoverEachLines = -1;
        private readonly double[] _histogram1 = new double[256];

        internal FilterWriteStrategy(ImageInfo imageInfo, FilterType configuredType)
        {
            _imgInfo = imageInfo;
            _configuredType = configuredType;
            if (configuredType < 0)
            {
                // First guess
                if (imageInfo.Rows < 8 && imageInfo.Columns < 8 || imageInfo.Indexed || imageInfo.BitDepth < 8)
                {
                    _currentType = FilterType.FILTER_NONE;
                }
                else
                {
                    _currentType = FilterType.FILTER_PAETH;
                }
            }
            else
            {
                _currentType = configuredType;
            }

            if (configuredType == FilterType.FILTER_AGGRESSIVE)
                _discoverEachLines = COMPUTE_STATS_EVERY_N_LINES;

            if (configuredType == FilterType.FILTER_VERYAGGRESSIVE)
                _discoverEachLines = 1;
        }

        internal bool ShouldTestAll(int rown)
        {
            if (_discoverEachLines > 0 && _lastRowTested + _discoverEachLines <= rown)
            {
                _currentType = FilterType.FILTER_UNKNOWN;
                return true;
            }

            return false;
        }

        internal void SetPreference(double none, double sub, double up, double ave, double paeth)
            => _preference = new double[] { none, sub, up, ave, paeth };

        internal bool ComputesStatistics()
            => _discoverEachLines > 0;

        internal void FillResultsForFilter(int rown, FilterType type, double sum, int[] histo, bool tentative)
        {
            _lastRowTested = rown;
            _lastSums[(int)type] = sum;
            if (histo != null)
            {
                double v, alfa, beta, e;
                alfa = rown == 0 ? 0.0 : 0.3;
                beta = 1 - alfa;
                e = 0.0;
                for (int i = 0; i < 256; i++)
                {
                    v = (double)histo[i] / _imgInfo.Columns;
                    v = _histogram1[i] * alfa + v * beta;
                    if (tentative)
                    {
                        e += v > 0.00000001 ? v * Math.Log(v) : 0.0;
                    }
                    else
                    {
                        _histogram1[i] = v;
                    }
                }
                _lastEntropies[(int)type] = -e;
            }
        }

        internal FilterType GimmeFilterType(int rown, bool useEntropy)
        {
            if (_currentType == FilterType.FILTER_UNKNOWN)
            {
                // Get better type
                if (rown == 0)
                {
                    _currentType = FilterType.FILTER_SUB;
                }
                else
                {
                    double bestval = double.MaxValue;
                    double val;
                    for (int i = 0; i < 5; i++)
                    {
                        val = useEntropy ? _lastEntropies[i] : _lastSums[i];
                        val /= _preference[i];
                        if (val <= bestval)
                        {
                            bestval = val;
                            _currentType = (FilterType)i;
                        }
                    }
                }
            }

            if (_configuredType == FilterType.FILTER_CYCLIC)
            {
                _currentType = (FilterType)(((int)_currentType + 1) % 5);
            }

            return _currentType;
        }
    }
}

