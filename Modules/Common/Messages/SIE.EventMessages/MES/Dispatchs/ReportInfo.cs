using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.EventMessages.MES.Dispatchs
{

    /// <summary>
    /// 报工信息
    /// </summary>
    [Serializable]
    public class ReportInfo
    {

        /// <summary>
        /// 工单Id
        /// </summary>
        public double WorkOrderId { get; set; }

        /// <summary>
        /// 任务单Id
        /// </summary>
        public double? TaskId { get; set; }

        /// <summary>
        /// 资源Id
        /// </summary>
        public double ResourceId { get; set; }

        /// <summary>
        /// 工序标签
        /// </summary>
        public string Sn { get; set; }

        /// <summary>
        /// 良品数量
        /// </summary>
        public decimal GoodQty { get; set; }

        /// <summary>
        /// 可疑品数量
        /// </summary>
        public decimal SuspectQty { get; set; }


        /// <summary>
        /// 是否自动上料(适用于成品工单包装报工)
        /// </summary>
        public bool IsAutoFeeding { get; set; }

    }
}
