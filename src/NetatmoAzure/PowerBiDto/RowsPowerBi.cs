using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetatmoAzure
{
    public sealed class RowsPowerBi
    {
        public IEnumerable<MeasurementPowerBi> rows { get; set; }
    }
}
