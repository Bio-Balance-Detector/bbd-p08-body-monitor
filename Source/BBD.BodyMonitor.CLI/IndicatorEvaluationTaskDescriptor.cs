using System;

namespace BBD.BodyMonitor
{
    internal class IndicatorEvaluationTaskDescriptor
    {
        public int BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public string IndicatorName { get; set; }
        public MLProfile MLProfile { get; set; }
        public Type MLModelType { get; set; }
        public string DisplayText { get; set; }
    }
}