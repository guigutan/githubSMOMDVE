using SIE.Domain;
using SIE.Domain.Validation;
using SIE.ManagedProperty;
using SIE.MES.OnOffDutyB;
using SIE.ObjectModel;
using SIE.Resources.Employees;
using SIE.Tech.Processs;
using SIE.Wpf.MES.WIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIE.Wpf.MES.OnOffDutyB
{

    /// <summary>
    /// B上下岗
    /// </summary>
    [RootEntity, Serializable]
    [Label("B上下岗")]
    public partial class OnOffDutyBViewModel : DataCollectionViewModel<OnOffDutyBController>
    {
        /// <summary>
        /// 视图模型，初始化工序类型
        /// </summary>
        public OnOffDutyBViewModel()
        {
            InitWorkstationB(ProcessType.Pqc, ProcessType.Fix, ProcessType.Rework,
            ProcessType.Assembly, ProcessType.Packing, ProcessType.BatchAssembly, ProcessType.BatchPqc,
            ProcessType.BatchFix, ProcessType.BatchPacking, ProcessType.Ageing);


            InitWorkstation(ProcessType.Pqc, ProcessType.Fix, ProcessType.Rework,
            ProcessType.Assembly, ProcessType.Packing, ProcessType.BatchAssembly, ProcessType.BatchPqc,
            ProcessType.BatchFix, ProcessType.BatchPacking, ProcessType.Ageing);

        }

        #region 模块KEY ModuleKey
        /// <summary>
        /// 模块KEY
        /// </summary>
        [Label("B模块KEY")]
        public static readonly Property<string> ModuleKeyProperty = P<OnOffDutyBViewModel>.Register(e => e.ModuleKey);

        /// <summary>
        /// 模块KEY
        /// </summary>
        public string ModuleKey
        {
            get { return this.GetProperty(ModuleKeyProperty); }
            set { this.SetProperty(ModuleKeyProperty, value); }
        }
        #endregion

        #region B采集结果 CollectDetailList
        /// <summary>
        /// B采集结果
        /// </summary>
        [Label("B采集结果")]
        public static readonly ListProperty<OnOffDutyBCollectDetailViewModelList> OnOffDutyBCollectDetailViewModelListProperty = P<OnOffDutyBViewModel>.RegisterList(e => e.OnOffDutyBCollectDetailViewModelList, new ListPropertyMeta
        {
            HasManyType = HasManyType.Aggregation,
            DataProvider = e => (e as OnOffDutyBViewModel).LoadOnOffDutyCollectDetailViewModelList()
        });

        /// <summary>
        /// B采集结果
        /// </summary>
        public OnOffDutyBCollectDetailViewModelList OnOffDutyBCollectDetailViewModelList
        {
            get { return this.GetLazyList(OnOffDutyBCollectDetailViewModelListProperty); }
        }

        /// <summary>
        /// B加载采集结果
        /// </summary>
        /// <returns>B采集结果列表</returns>
        private OnOffDutyBCollectDetailViewModelList LoadOnOffDutyCollectDetailViewModelList()
        {
            return new OnOffDutyBCollectDetailViewModelList();
        }
        #endregion

        #region IsOffDuty 是否上岗
        /// <summary>
        /// 是否上岗
        /// </summary>
        [Label("是否上岗")]
        public static readonly Property<bool> IsOnDutyProperty = P<OnOffDutyBViewModel>.Register(e => e.IsOnDuty, new PropertyMetadata<bool>() { PropertyChangedCallBack = (o, e) => (o as OnOffDutyBViewModel).OnIsOffDuty(e) });

        /// <summary>
        /// 是否上岗值变更
        /// </summary>
        /// <param name="e">参数</param>
        private void OnIsOffDuty(ManagedPropertyChangedEventArgs e)
        {
            FocuseBarcode();
        }

        /// <summary>
        /// 是否上岗
        /// </summary>
        public bool IsOnDuty
        {
            get { return this.GetProperty(IsOnDutyProperty); }
            set { this.SetProperty(IsOnDutyProperty, value); }
        }
        #endregion

       




        /// <summary>
        /// 属性变更事件，重置显示信息及数据
        /// </summary>
        /// <param name="e">参数</param>
        protected override void OnPropertyChanged(ManagedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == IsOnDutyProperty)
            {
                ShowTips(!IsOnDuty ? "请扫描下岗员工工号".L10N() : "请扫描上岗员工工号".L10N());
                FocuseBarcode();
            }
        }     


        /// <summary>
        /// 条码变更事件，采集条码
        /// </summary>
        /// <param name="e">参数</param>
        protected override void OnBarcodeChanged(ManagedPropertyChangedEventArgs e)
        {
            if (Barcode.IsNullOrEmpty()) return;

            ClearInfos();

            var workcell = GetWorkcell();
            Controller.CheckedWorkcellParas(workcell);
            try
            {
                var staff = RT.Service.Resolve<EmployeeController>().GetEmployeeByCode(Barcode);
                if (staff == null)
                {
                    throw new ValidationException("系统不存在当前扫描员工".L10N());

                }
                var onoffDutyBRecord = new OnOffDutyBRecrods();
                onoffDutyBRecord.ProcessId = workcell.ProcessId;
                onoffDutyBRecord.EmployeeId = staff.Id;
                onoffDutyBRecord.StationId = workcell.StationId;
                onoffDutyBRecord.ResourceId = workcell.ResourceId;
                onoffDutyBRecord.OnOffDutyType = IsOnDuty ? OnOffDutyBType.OnDuty : OnOffDutyBType.OffDuty;
                Controller.OnOffDuty(onoffDutyBRecord, workcell, IsOnDuty);
                AddDetail(onoffDutyBRecord);
                this.ShowTips(onoffDutyBRecord.OnOffDutyType == OnOffDutyBType.OffDuty ? "下岗成功！".L10N() : "上岗成功！".L10N());
                //Reset(ResetType.Success);
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
            finally
            {
                Barcode = null;
            }
        }


        /// <summary>
        /// 添加采集结果记录
        /// </summary>
        /// <param name="onOffDutyBRecrods"></param>
        protected virtual void AddDetail(OnOffDutyBRecrods onOffDutyBRecrods)
        {
            OnOffDutyBCollectDetailViewModelList.Add(new OnOffDutyBCollectDetailViewModel
            {
                OnOffDutyType = onOffDutyBRecrods.OnOffDutyType,
                CollectUseName = RT.Identity.Name,
                InputDate = DateTime.Now,
                CollectDate = DateTime.Now,
                ProcessName = onOffDutyBRecrods.Process.Name,
                StationName = onOffDutyBRecrods.Station.Name,
                StaffNO = onOffDutyBRecrods.Employee.Code,
                StaffName = onOffDutyBRecrods.Employee.Name,
                ResourceName = onOffDutyBRecrods.Resource.Name

            });
        }


        /// <summary>
        /// 加载工作单元数据
        /// </summary>
        protected override void LoadWorkstationData()
        {
            base.LoadWorkstationData();
        }


        /// <summary>
        /// 重新开始
        /// </summary>
        public override void Reset(ResetType resetType)
        {
            base.Reset(ResetType.None);
            ShowTips(!IsOnDuty ? "请扫描下岗员工工号".L10N() : "请扫描上岗员工工号".L10N());
        }

        /// <summary>
        /// 能否提交
        /// </summary>
        /// <returns>能提交返回true，否则返回false</returns>
        public bool CanSubmit()
        {
            return Workstation.EmployeeId.HasValue
                && Workstation.ProcessId.HasValue
                && Workstation.StationId.HasValue
                && Workstation.ResourceId.HasValue;
        }





    }
}
