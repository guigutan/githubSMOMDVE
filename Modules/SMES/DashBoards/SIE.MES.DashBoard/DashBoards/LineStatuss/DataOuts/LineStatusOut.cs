using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.DashBoards.LineStatuss.DataOuts
{
    internal class LineStatusOut
    {
        public double ResourceId { get; set; }
        public double ResourceCode { get; set; }
        public double ResourceName { get; set; }

        public int StatusCode { get; set; }
        public string StatusName { get; set; }

    }
}
