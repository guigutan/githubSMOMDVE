using SIE.Domain;
using SIE.ObjectModel;
using SIE.Resources.WipResources;
using SIE.Tech.Processs;
using SIE.Wpf.MES.WIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIE.Resources;


namespace SIE.MES.OnOffDutyB
{

    /// <summary>
    /// B工作站信息
    /// </summary>
    [RootEntity, Serializable]
    public class WorkstationB : ViewModel
    {
        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public WorkstationB(WorkCellViewModelB  workCellViewModelB)
        {
            Resources = new List<WipResource>();
            ProcessTypes = new List<ProcessType>();
            WorkCellViewModelB = workCellViewModelB;
        }
        #endregion



        /// <summary>
        /// 资源列表(用于限定资源范围)
        /// </summary>
        public List<WipResource> Resources { get; set; }
        /// <summary>
        /// 工序类型
        /// </summary>
        public List<ProcessType> ProcessTypes { get; private set; }

        /// <summary>
        /// 采集功能基类
        /// </summary>
        public WorkCellViewModelB WorkCellViewModelB { get; set; }



        #region User 人员
        /// <summary>
        /// 人员ID
        /// </summary>
        [Label("人员")]
        public static readonly IRefIdProperty EmployeeIdProperty =
            P<WorkstationB>.RegisterRefId(e => e.EmployeeId, ReferenceType.Normal);

        /// <summary>
        /// 人员ID
        /// </summary>
        public double? EmployeeId
        {
            get { return (double?)this.GetRefNullableId(EmployeeIdProperty); }
            set { this.SetRefNullableId(EmployeeIdProperty, value); }
        }

        /// <summary>
        /// 人员
        /// </summary>
        public static readonly RefEntityProperty<Employee> UserProperty =
            P<WorkstationB>.RegisterRef(e => e.Employee, EmployeeIdProperty);

        /// <summary>
        /// 人员
        /// </summary>
        public Employee Employee
        {
            get { return this.GetRefEntity(UserProperty); }
            set { this.SetRefEntity(UserProperty, value); }
        }
        #endregion


        #region Resource 资源
        /// <summary>
        /// 资源ID
        /// </summary>
        [Label("资源")]
        public static readonly IRefIdProperty ResourceIdProperty =
            P<WorkstationB>.RegisterRefId(e => e.ResourceId, ReferenceType.Normal);

        /// <summary>
        /// 资源ID
        /// </summary>
        public double? ResourceId
        {
            get { return (double?)this.GetRefNullableId(ResourceIdProperty); }
            set { this.SetRefNullableId(ResourceIdProperty, value); }
        }

        /// <summary>
        /// 资源
        /// </summary>
        public static readonly RefEntityProperty<WipResource> ResourceProperty =
            P<WorkstationB>.RegisterRef(e => e.Resource, ResourceIdProperty);

        /// <summary>
        /// 资源
        /// </summary>
        public WipResource Resource
        {
            get { return this.GetRefEntity(ResourceProperty); }
            set { this.SetRefEntity(ResourceProperty, value); }
        }
        #endregion











    }
}
