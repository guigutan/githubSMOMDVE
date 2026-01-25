using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.DashBoards.LineStatuss.DataArrts
{


    /// <summary>
    ///库存组织
    /// </summary>
    public class InvOrgInfo
    {
        /// <summary>
        /// 库存组织ID
        /// </summary>
        public double InvID { get; set; }    

        /// <summary>
        /// 库存组织编码
        /// </summary>
        public int InvCode { get; set; }

        /// <summary>
        /// 库存组织名称
        /// </summary>
        public string InvName { get; set; }

    }

    /// <summary>
    /// 工序
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// 工序Id
        /// </summary>
        public double ProcessId { get; set; }      


        /// <summary>
        /// 工序编码
        /// </summary>
        public string ProcessCode { get; set; }


        /// <summary>
        /// 工序名称
        /// </summary>
        public string ProcessName { get; set; }

    }

    /// <summary>
    /// 产线
    /// </summary>
    public class ResourceInfo
    {
        /// <summary>
        ///产线ID
        /// </summary>
        public double ResourceId { get; set; }
        /// <summary>
        /// 产线编码
        /// </summary>
        public string ResourceCode { get; set; }
        /// <summary>
        /// 产线名称
        /// </summary>
        public string ResourceName { get; set; }
    }

    /// <summary>
    /// 产线状态
    /// </summary>
    public class LineStatusInfo
    {
        public ResourceInfo ResourceId { get; set; }
        public string ResourceCode { get; set; }
        public string ResourceName { get; set; }

        public int StatusCode { get; set; }
        public string StatusName { get; set; }
    }






}
