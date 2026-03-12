using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.KzReport.Datas
{
    /// <summary>
    /// 可疑品处理报表
    /// </summary>
    [Serializable]
    public class SuspectReportData
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Num { get; set; }

        /// <summary>
        /// 产品线
        /// </summary>
        public string ProductLine { get; set; }

        /// <summary>
        /// 部门(厂部)
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 工序
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// 总产量(万)
        /// </summary>
        public decimal TotalQty { get; set; }

        /// <summary>
        /// 报废总量(万)
        /// </summary>
        public decimal TotalNgQty { get; set; }

        /// <summary>
        /// 可疑品总量(万)
        /// </summary>
        public decimal TotalSuspectQty { get; set; }

        /// <summary>
        /// 报废率
        /// </summary>
        public decimal NgQtyRate { get; set; }

        /// <summary>
        /// 可疑品率
        /// </summary>
        public decimal SuspectRate { get; set; }

        /// <summary>
        /// 一次下线合格率
        /// </summary>
        public decimal OkRate { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SuspectReportDataFactory
    {
        /// <summary>
        /// 工序
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// 总产量(万)
        /// </summary>
        public decimal TotalQty { get; set; }

        /// <summary>
        /// 报废总量(万)
        /// </summary>
        public decimal TotalNgQty { get; set; }

        /// <summary>
        /// 可疑品总量(万)
        /// </summary>
        public decimal TotalSuspectQty { get; set; }
    }

    /// <summary>
    /// 可疑品缺陷
    /// </summary>
    [Serializable]
    public class SuspectDefectData
    { 
        /// <summary>
        /// 序号
        /// </summary>
        public int Num { get; set; }

        /// <summary>
        /// 缺陷代码
        /// </summary>
        public string DefectCode { get; set; }

        /// <summary>
        /// 缺陷名称
        /// </summary>
        public string DefectName { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Qty { get; set; }

        /// <summary>
        /// 占比
        /// </summary>
        public decimal Rate { get; set; }
    }
}
