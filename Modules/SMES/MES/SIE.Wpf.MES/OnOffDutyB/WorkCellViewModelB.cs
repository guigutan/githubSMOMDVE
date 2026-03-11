using Newtonsoft.Json;
using SIE.Domain;
using SIE.Domain.Validation;
using SIE.MetaModel;
using SIE.Resources.Employees;
using SIE.Resources.WipResources;
using SIE.Tech.Processs;
using SIE.Threading;
using SIE.Wpf;
using SIE.Wpf.Common;
using SIE.Wpf.MES.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SIE.MES.OnOffDutyB
{
    /// <summary>
    /// B数据采集泛型基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WorkCellViewModelB<T> : WorkCellViewModelB where T : SIE.MES.WIP.WipController
    {
        /// <summary>
        /// 采集控制器，通过泛型参数确定控制器的类型
        /// </summary>
        protected virtual T Controller { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected WorkCellViewModelB()
        {
            Controller = RT.Service.Resolve<T>();
        }
    }







        /// <summary>
        /// B数据采集基类
        /// </summary>
        [RootEntity, Serializable]
    public class WorkCellViewModelB : ViewModel, IFocusTrigger
    {
        #region IFocusTrigger

        /// <summary>
        /// 聚焦事件
        /// </summary>
        public event EventHandler Focused;

        /// <summary>
        /// 触发条码输入框获取焦点
        /// </summary>
        public void FocuseBarcode()
        {
            Focused?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 消息提示
        /// <summary>
        /// 显示错误信息
        /// </summary>
        /// <param name="error">错误信息</param>
        public virtual void ShowError(string error)
        {
            if (error == null)
            {
                return;
            }

           ClientRuntime.MessageService.ShowError(error.Replace("\r\n", string.Empty));
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        /// <param name="exc">异常</param>
        public virtual void ShowError(Exception exc)
        {
            var validationException = exc.GetBaseException() as ValidationException;
            if (validationException != null)
            {
                ShowError(DisplayHelper.Display(validationException.Message));
            }
            else
            {
                Extenstion.Alert(exc);
            }
        }
        #endregion

        /// <summary>
        /// 异步执行操作
        /// </summary>
        /// <param name="action">执行内容</param>
        protected void AsyncExecute(Action action)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    CRT.MainThread.InvokeAsync(() =>
                    {
                        action();
                    });
                }
                catch (Exception exc)
                {
                    ShowError(exc);
                }
            }).WithCurrentThreadContext());
        }





        /// <summary>
        /// 工作站信息
        /// </summary>
        public WorkstationB  workstationB;

        /// <summary>
        /// 工作站
        /// </summary>
        public WorkstationB WorkstationB
        {
            get
            {
                if (workstationB == null)
                {
                    workstationB = new WorkstationB(this);
                }

                return workstationB;
            }
        }


        /// <summary>
        /// 初始化工作站信息
        /// </summary>
        /// <param name="processTypes">工序类型数值</param>
        public virtual void InitWorkstationB(params ProcessType[] processTypes)
        {
            WorkstationB.PropertyChanged += OnWorkstationPropertyChanged;

            WorkstationB.ProcessTypes.AddRange(processTypes); //设置工作站工序类型
            WorkstationB.EmployeeId = CRT.IdentityId;


          

            if (!LoadWorkstation()//如果工作站信息不存在，或者与上次登录用户的资源工序工位分配不一样，重新选择
                && WorkstationBSelector.SelectOperation(WorkstationB))
            {
                //有切换工作单元，则将工作单元信息保存在本地配置文件中
                SaveWorkstation();
            }

            var broken = WorkstationB.Validate(ValidatorActions.None);

            if (broken.Count > 0)
            {
                ShowError(broken.ToString());
            }
        }



        /// <summary>
        /// 工作单元属性变更
        /// </summary>
        /// <param name="sender">所有者</param>
        /// <param name="e">参数</param>
        private void OnWorkstationPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WorkstationB.Employee))
            {
                EmployeeChanged(WorkstationB.Employee);
            }

            if (e.PropertyName == nameof(WorkstationB.Resource))
            {
                ResourceChanged(WorkstationB.Resource);
            }
           
        }

        /// <summary>
        /// 人员变更事件
        /// </summary>
        /// <param name="employee"></param>
        protected virtual void EmployeeChanged(SIE.Resources.Employee employee)
        {
        }
        /// <summary>
        /// 资源变更事件
        /// </summary>
        /// <param name="resource"></param>
        protected virtual void ResourceChanged(WipResource resource)
        {
        }


        /// <summary>
        /// 加载工作单元信息
        /// </summary>
        /// <returns>加载成功返回true，失败返回false</returns>
        bool LoadWorkstation()
        {
            var setting = Settings.Default.Workcell;
            if (setting.IsNotEmpty())
            {
                var workcellbs = JsonConvert.DeserializeObject<Dictionary<string, WorkcellB>>(setting);
                var key = GetType().GetQualifiedName();
                if (workcellbs.ContainsKey(key))   //匹配工作单元
                {
                    var workcellb = workcellbs[key];

                    if (workcellb.ResourceId == 0 /*|| workcellb.StationId == 0 || workcellb.ProcessId == 0 */ )
                    {
                        return false;
                    }




                    ////如果与上次登录用户的资源工序工位分配不一样，打开时需要重新选
                    if (!CheckUserWorkStation(CRT.IdentityId, workcellb.ResourceId, workcellb.ProcessId, workcellb.StationId))
                    {
                        return false;
                    }

                    WorkstationB.EmployeeId = CRT.IdentityId;
                    WorkstationB.ResourceId = workcellb.ResourceId;
                    //WorkstationB.ProcessId = workcellb.ProcessId;


                    ////检查员工是否具有当前工序所需的技能
                    //if (WorkstationB.ProcessId.HasValue
                    //    && !RT.Service.Resolve<ProcessController>().IsEmpHasProcessSkill(
                    //        WorkstationB.ProcessId.Value, WorkstationB.EmployeeId.Value))
                    //{
                    //    return false;
                    //}
                    //WorkstationB.StationId = workcellb.StationId;




                    EmployeeChanged(WorkstationB.Employee);
                    ResourceChanged(WorkstationB.Resource);
                    //ProcessChanged(WorkstationB.Process);
                    //StationChanged(WorkstationB.Station);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 根据用户查找是否匹配传入的工序，资源，工位
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="resourceId">产线ID</param>
        /// <param name="processId">工序ID</param>
        /// <param name="stationId">工位ID</param>
        /// <returns>匹配返回true，否则返回false</returns>
        bool CheckUserWorkStation(double userId, double resourceId, double processId, double stationId)
        {
            var ctlResource = RT.Service.Resolve<EmployeeController>();
            if (!ctlResource.UserHasResource(userId, resourceId))
            {
                return false;
            }
            /*
            var ctlProcess = RT.Service.Resolve<ProcessController>();

            if (!ctlProcess.EmployeeHasProcess(userId, processId))
            {
                return false;
            }

            var ctlStation = RT.Service.Resolve<StationController>();
            var station = ctlStation.GetStation(stationId);

            if (station == null)
            {
                return false;
            }
           
            if (station.ResourceId != resourceId)
            {
                return false;
            }

            var processIds = station.StationProcessList.Select(p => p.ProcessId).ToList();

            if (!processIds.Contains(processId))
            {
                return false;
            }
            */
            return true;
        }







        /// <summary>
        /// 保存工作单元信息
        /// </summary>
        public void SaveWorkstation()
        {
            if (WorkstationB == null)
            {
                throw new PlatformException("工作单元未初始化".L10N());
            }

            workcellB = new WorkcellB()
            {
                EmployeeId = WorkstationB.EmployeeId ?? 0,
                ResourceId = WorkstationB.ResourceId ?? 0,
                //ProcessId = WorkstationB.ProcessId ?? 0,
                //StationId = WorkstationB.StationId ?? 0,
            };

            var setting = Settings.Default.Workcell;
            Dictionary<string, WorkcellB> data = null;
            if (setting.IsNotEmpty())
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, WorkcellB>>(setting);
            }

            if (data == null)
            {
                data = new Dictionary<string, WorkcellB>();
            }

            var key = GetType().GetQualifiedName();
            data[key] = workcellB;
            Settings.Default.Workcell = JsonConvert.SerializeObject(data);
            Settings.Default.Save();
        }




        /// <summary>
        /// 获取采集单元信息
        /// </summary>
        /// <returns>工作单元</returns>
        public WorkcellB GetWorkcell()
        {
            if (workcellB == null)
            {
                var broken = WorkstationB.Validate(ValidatorActions.None);
                if (broken.Count > 0)
                    throw new ValidationException(broken.ToString());
                workcellB = new WorkcellB();
                workcellB.EmployeeId = WorkstationB.EmployeeId.Value;
                //workcellB.ProcessId = WorkstationB.ProcessId.Value;
                //workcellB.StationId = WorkstationB.StationId.Value;
                workcellB.ResourceId = WorkstationB.ResourceId.Value;
            }

            return workcellB;
        }

        /// <summary>
        /// 工作单元信息
        /// </summary>
        WorkcellB workcellB;














    }



}


