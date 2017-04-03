﻿namespace Moonscraper
{
    namespace ChartParser
    {
        public abstract class ChartObject : SongObject
        {
            public Chart chart;

            public ChartObject(uint position) : base(position) { }

            public override void Delete(bool update = true)
            {
                base.Delete(update);
                if (chart != null)
                    chart.Remove(this, update);
            }
        }
    }
}
