using SIE.Common.InvOrg;
using SIE.Domain;
using SIE.Domain.Validation;
using SIE.MES.WIP;
using SIE.Resources.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.OnOffDutyB
{
    /// <summary>
    /// B上下岗控制器
    /// </summary>
    public partial class OnOffDutyBController : WipController
    {

        /// <summary>
        /// 检查工作台
        /// </summary>
        /// <param name="workcell"></param>
        /// <returns></returns>
        public virtual void CheckedWorkcellParas(Workcell workcell)
        {
            //if (workcell.ProcessId <= 0)
            //{
            //    throw new ValidationException("请选择工序".L10N());
            //}
            //if (workcell.StationId <= 0)
            //{
            //    throw new ValidationException("请选择工位".L10N());
            //}
            if (workcell.ResourceId <= 0)
            {
                throw new ValidationException("请选择资源".L10N());
            }
        }

        /// <summary>
        /// B上下岗
        /// </summary>
        /// <param name="onOffDutyBRecrod"></param>
        /// <param name="workcell"></param>
        /// <param name="isOnDuty"></param>
        /// <exception cref="ValidationException"></exception>
        public virtual void OnOffDuty(OnOffDutyBRecrods onOffDutyBRecrod, Workcell workcell, bool isOnDuty)
        {
            CheckedWorkcellParas(workcell);
            var onDuty = Query<OnOffDutyBRecrods>().Where(p => p.EmployeeId == onOffDutyBRecrod.EmployeeId && p.ResourceId == onOffDutyBRecrod.ResourceId
                && p.ProcessId == onOffDutyBRecrod.ProcessId && p.StationId == onOffDutyBRecrod.StationId && !p.IsAdditionalRecording && p.OnOffDutyType == OnOffDutyBType.OnDuty)
                .FirstOrDefault();
            var dbTime = RF.Find<OnOffDutyBRecrods>().GetDbTime();
            if (onOffDutyBRecrod.Employee.EmployeeStatus == EmployeeStatus.UnJob)
            {
                new ValidationException("员工【{0}】已离职，无法上下岗，请检查".L10nFormat(onOffDutyBRecrod.Employee.Name));
            }
            if (!isOnDuty)//下岗
            {
                if (onDuty == null)
                {
                    throw new ValidationException("员工【{0}】在资源【{1}】、工序【{2}】、工位【{3}】上不存在上岗信息，下岗失败！请先上岗！".L10nFormat(
                        onOffDutyBRecrod.Employee.Name, onOffDutyBRecrod.Resource.Name, onOffDutyBRecrod.Process.Name, onOffDutyBRecrod.Station.Name
                        ));
                }
                onDuty.OnOffDutyType = OnOffDutyBType.OffDuty;
                onDuty.OffDutyTime = dbTime;
                onDuty.OnDutyDuration = (onDuty.OffDutyTime - onDuty.OnDutyTime).Value.TotalMinutes;
                RF.Save(onDuty);
                onOffDutyBRecrod.Id = onDuty.Id;
            }
            else//上岗
            {
                if (onDuty != null)
                {
                    throw new ValidationException("员工【{0}】在资源【{1}】、工序【{2}】、工位【{3}】上已存在上岗信息，上岗失败，请先下岗！".L10nFormat(
                        onOffDutyBRecrod.Employee.Name, onOffDutyBRecrod.Resource.Name, onOffDutyBRecrod.Process.Name, onOffDutyBRecrod.Station.Name
                        ));
                }
                onOffDutyBRecrod.OnOffDutyType = OnOffDutyBType.OnDuty;
                onOffDutyBRecrod.OnDutyTime = dbTime;
                onOffDutyBRecrod.PersistenceStatus = PersistenceStatus.New;
                RF.Save(onOffDutyBRecrod);
            }


        }


        /// <summary>
        /// B查询
        /// </summary>
        /// <param name="onOffDutyBRecrodsCriteria"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual EntityList<OnOffDutyBRecrods> Fetch(OnOffDutyBRecrodsCriteria onOffDutyBRecrodsCriteria)
        {
            var q = Query<OnOffDutyBRecrods>();
            //员工
            if (onOffDutyBRecrodsCriteria.Staff.IsNotEmpty())
            {
                q.Where(p => p.Employee.Code.Contains(onOffDutyBRecrodsCriteria.Staff) || p.Employee.Name.Contains(onOffDutyBRecrodsCriteria.Staff));
            }
            //工序
            if (onOffDutyBRecrodsCriteria.ProcessId.HasValue)
            {
                q.Where(p => p.ProcessId == onOffDutyBRecrodsCriteria.ProcessId);
            }
            //工位
            if (onOffDutyBRecrodsCriteria.StationId.HasValue)
            {
                q.Where(p => p.StationId == onOffDutyBRecrodsCriteria.StationId);
            }
            //资源
            if (onOffDutyBRecrodsCriteria.ResourceId.HasValue)
            {
                q.Where(p => p.ResourceId == onOffDutyBRecrodsCriteria.ResourceId);
            }
            //是否补录
            if (onOffDutyBRecrodsCriteria.IsAdditionalRecording.HasValue)
            {
                q.Where(p => p.IsAdditionalRecording == (onOffDutyBRecrodsCriteria.IsAdditionalRecording == YesNo.Yes));
            }
            //下岗时间
            if (onOffDutyBRecrodsCriteria.OffDutyTime.BeginValue.HasValue)
            {
                q.Where(p => p.OffDutyTime >= onOffDutyBRecrodsCriteria.OffDutyTime.BeginValue);
            }
            if (onOffDutyBRecrodsCriteria.OffDutyTime.EndValue.HasValue)
            {
                q.Where(p => p.OffDutyTime <= onOffDutyBRecrodsCriteria.OffDutyTime.EndValue);
            }
            //上岗时间
            if (onOffDutyBRecrodsCriteria.OnDutyTime.BeginValue.HasValue)
            {
                q.Where(p => p.OnDutyTime >= onOffDutyBRecrodsCriteria.OnDutyTime.BeginValue);
            }
            if (onOffDutyBRecrodsCriteria.OnDutyTime.EndValue.HasValue)
            {
                q.Where(p => p.OnDutyTime <= onOffDutyBRecrodsCriteria.OnDutyTime.EndValue);
            }
            //创建时间
            if (onOffDutyBRecrodsCriteria.CreateTime.BeginValue.HasValue)
            {
                q.Where(p => p.CreateDate >= onOffDutyBRecrodsCriteria.CreateTime.BeginValue);
            }
            if (onOffDutyBRecrodsCriteria.CreateTime.EndValue.HasValue)
            {
                q.Where(p => p.CreateDate <= onOffDutyBRecrodsCriteria.CreateTime.EndValue);
            }

            if (onOffDutyBRecrodsCriteria.OnOffDutyType.HasValue)
            {
                q.Where(p => p.OnOffDutyType == onOffDutyBRecrodsCriteria.OnOffDutyType);
            }
            return q.OrderBy(onOffDutyBRecrodsCriteria.OrderInfoList).ToList(onOffDutyBRecrodsCriteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());
        }



        /// <summary>
        /// B查询员工
        /// </summary>
        /// <param name="onOffDutyBRecrodsInputCriteria"></param>
        /// <returns></returns>
        public virtual EntityList<OnOffDutyBRecrods> FetchOnOffDutyRecrodsInput(OnOffDutyBRecrodsInputCriteria onOffDutyBRecrodsInputCriteria)
        {
            var q = Query<Employee>().LeftJoin<EmployeeGroup>((x, y) => x.EmployeeGroupId == y.Id);
            //员工
            if (onOffDutyBRecrodsInputCriteria.StaffCode.IsNotEmpty())
            {
                q.Where(p => p.Code.Contains(onOffDutyBRecrodsInputCriteria.StaffCode));
            }
            if (onOffDutyBRecrodsInputCriteria.StaffName.IsNotEmpty())
            {
                q.Where(p => p.Name.Contains(onOffDutyBRecrodsInputCriteria.StaffName));
            }
            if (onOffDutyBRecrodsInputCriteria.StaffGroupName.IsNotEmpty())
            {
                q.Where<EmployeeGroup>((p, z) => z.Code.Contains(onOffDutyBRecrodsInputCriteria.StaffGroupName) || z.Name.Contains(onOffDutyBRecrodsInputCriteria.StaffGroupName));
            }
            EntityList<Employee> employeeReuslt;
            using (InvOrgs.WithAll())
            {
                employeeReuslt = q.OrderBy(onOffDutyBRecrodsInputCriteria.OrderInfoList).ToList(onOffDutyBRecrodsInputCriteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            }
            EntityList<OnOffDutyBRecrods> onOffDutyBRecrodsList = new EntityList<OnOffDutyBRecrods>();
            foreach (var item in employeeReuslt)
            {
                onOffDutyBRecrodsList.Add(new OnOffDutyBRecrods
                {
                    EmployeeId = item.Id,
                    EmployeeCode = item.Code,
                    EmployeeName = item.Name,
                    EmployeeGroupName = item.EmployeeGroupName,
                    UserCode = item.UserCode,
                });
            }
            onOffDutyBRecrodsList.SetTotalCount(employeeReuslt.TotalCount);
            return onOffDutyBRecrodsList;
        }


        /// <summary>
        /// 补录上下岗
        /// </summary>
        /// <param name="selectedList"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void SetOnOffDuty(EntityList<OnOffDutyBRecrods> selectedList)
        {
            RF.BatchInsert(selectedList);
        }














    }
}
