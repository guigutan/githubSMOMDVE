using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
using DotLiquid.Util;
using IronPython.Runtime.Operations;
using SIE.Andon.Andons;
using SIE.Andon.Andons.Enum;
using SIE.Api;
using SIE.Common.InvOrg;
using SIE.Core.ApiModels;
using SIE.Core.Common;
using SIE.Core.Enums;
using SIE.Defects;
using SIE.Domain;
using SIE.Domain.Validation;
using SIE.Items;
using SIE.MES.DashBoard.KzBoard.Datas;
using SIE.MES.DashBoard.KzReport.Datas;
using SIE.MES.DashBoard.KzReport.OrganizeCodes;
using SIE.MES.DashBoard.KzReport.ProductionProcesss;
using SIE.MES.TaskManagement.Dispatchs;
using SIE.MES.TaskManagement.FeedingRecords;
using SIE.MES.TaskManagement.Reports;
using SIE.MES.TaskManagement.SuspectProductLabels;
using SIE.MES.WorkOrders;
using SIE.ObjectModel;
using SIE.Rbac.InvOrgs;
using SIE.Resources.Enterprises;
using SIE.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.KzReport
{
    public partial class KzReportController
    {
        #region 安灯异常统计报表

        /// <summary>
        /// 安灯异常统计报表-工厂
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="resource"></param>
        /// <param name="andonName"></param>
        /// <param name="equipAccountCode"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("安灯异常统计报表-工厂")]
        [AllowAnonymous]
        public virtual List<AndonReportData> GetAndonReportDatasFactory(List<string> factoryCodes, string resource, string andonName, string equipAccountCode, int? state, DateTime? beginTime, DateTime? endTime)
        {
            List<AndonReportData> datas = new List<AndonReportData>();

            var querySql = "1 = 1";
            if (factoryCodes.Count > 0)
            {
                var escapedItems = factoryCodes.Select(s => $"'{s}'");
                querySql += $" and sio.External_Id in ({string.Join(",", escapedItems)})";
            }
            if (!resource.IsNullOrEmpty())
            {
                if (resource.Contains("%"))
                    querySql += $" and rws.code like '{resource}' or rws.name like '{resource}'";
                else
                    querySql += $" and rws.code = '{resource}' or rws.name = '{resource}'";
            }
            if (!andonName.IsNullOrEmpty())
            {
                if (andonName.Contains("%"))
                    querySql += $" and ma.andon_Name like '{andonName}'";
                else
                    querySql += $" and ma.andon_Name = '{andonName}'";
            }
            if (!equipAccountCode.IsNullOrEmpty())
            {
                if (equipAccountCode.Contains("%"))
                    querySql += $" and eea.code like '{equipAccountCode}'";
                else
                    querySql += $" and eea.code = '{equipAccountCode}'";
            }
            if (state != null)
            {
                querySql += $" and am.state = {state}";
            }
            var sql = $@"
                with
--先将安灯责任维护基础表中的人名用/拼接好，方便下面可以直接匹配编码
andonGroup as
(
  select ag.code,ag.inv_org_id,(select xmlagg(xmlparse(content re.name || '/' wellformed) order by agd.is_responser).getclobval() from ANDON_GROUP_DTL agd 
  inner join SYS_USER su on su.id = agd.user_id and su.is_phantom = 0 
  inner join RES_EMP re on re.id = su.employee_id and re.is_phantom = 0
  where agd.is_phantom = 0 and agd.andon_group_id = ag.id) levelname
  from ANDON_GROUP ag
    --判断必须有明细才能拼接人名
  where exists(select 1 from ANDON_GROUP_DTL agd inner join SYS_USER su on su.id = agd.user_id and su.is_phantom = 0 inner join RES_EMP re on re.id = su.employee_id and re.is_phantom = 0 where agd.is_phantom = 0 and agd.andon_group_id = ag.id) and ag.is_phantom = 0
),
--获取安灯管理的操作明细，对库存组织、安灯管理Id、操作类型进行分组，然后去每组第一条，方便后面找出他的每一组的操作时间去计算
mao as (
select *
from (select mao.inv_org_id,mao.andon_manage_id,mao.operate_type,mao.Operate_Time,row_number() over (partition by mao.inv_org_id,mao.andon_manage_id,mao.operate_type order by mao.create_date desc) as row_num
from MES_ANDONMANAGEOPERATELOG mao)
where row_num = 1
)
SELECT sio.name Factory,rws.code ResourceCode,eea.code EquipAccountCode,ma.andon_name AndonName,am.problem_desc ProblemDesc,am.Fault_Time FaultTime
,am.Last_Time LastTime,nvl(lv4.levelname,'') lv4,nvl(lv3.levelname,'') lv3,nvl(lv2.levelname,'') lv2,nvl(lv1.levelname,'') lv1,ROUND((mao1.Operate_Time - am.Fault_Time) * 24, 2) AS ResponseTime,
ROUND((mao3.Operate_Time - am.Fault_Time) * 24, 2) AS HandleTime,am.state
FROM MES_ANDONMANAGE am         --安灯管理
inner join SYS_INV_ORG sio on sio.code = am.inv_org_id and sio.is_phantom = 0               --库存组织
inner join RES_WIP_SCHE rws on rws.id = am.wip_resource_id and rws.is_phantom = 0           --生产资源
left join EMS_EQUIP_ACCOUNT eea on eea.id = am.equip_account_id and eea.is_phantom = 0      --设备台账
inner join MES_ANDON ma on ma.id = am.andon_id and ma.is_phantom = 0                        --安灯维护
left join ANDON_LINE al on al.machine_code = rws.code and al.is_phantom = 0                 --产线与安灯区域
left join andonGroup lv4 on lv4.code = al.andon_code||ma.andon_name||'LV4' and lv4.inv_org_id = am.inv_org_id   --获取LV4的人名
left join andonGroup lv3 on lv4.code = al.andon_code||ma.andon_name||'LV3' and lv3.inv_org_id = am.inv_org_id   --获取LV3的人名
left join andonGroup lv2 on lv4.code = al.andon_code||ma.andon_name||'LV2' and lv2.inv_org_id = am.inv_org_id   --获取LV2的人名
left join andonGroup lv1 on lv4.code = al.andon_code||ma.andon_name||'LV1' and lv1.inv_org_id = am.inv_org_id   --获取LV1的人名
left join mao mao1 on mao1.andon_manage_id = am.id and mao1.inv_org_id = am.inv_org_id and mao1.operate_type = 1    --获取操作时间
left join mao mao3 on mao3.andon_manage_id = am.id and mao3.inv_org_id = am.inv_org_id and mao3.operate_type = 3    --获取操作时间
where {querySql}
";
            using (var db = DB.Create("MES"))
            {
                try
                {
                    var dt = db.ExecuteDataTable(sql, CommandType.Text);
                    foreach (DataRow row in dt.Rows)
                    {
                        var Factory = row["Factory"].ToString();
                        var Resource = row["ResourceCode"].ToString();
                        var EquipAccountCode = row["EquipAccountCode"].ToString();
                        var AndonName = row["AndonName"].ToString();
                        var ProblemDesc = row["ProblemDesc"].ToString();
                        var FaultTime = row["FaultTime"].ToString();
                        var LastTime = row["LastTime"].ToString();
                        var lv4 = row["lv4"].ToString();
                        var lv3 = row["lv3"].ToString();
                        var lv2 = row["lv2"].ToString();
                        var lv1 = row["lv1"].ToString();
                        var ResponseTime = row["ResponseTime"].ToString();
                        var HandleTime = row["HandleTime"].ToString();
                        var State = row["state"].ToString();

                        AndonReportData data = new AndonReportData();
                        data.Factory = Factory;
                        data.Resource = Resource;
                        data.EquipAccountCode = EquipAccountCode;
                        data.AndonName = AndonName;
                        data.ProblemDesc = ProblemDesc;
                        data.FaultTime = FaultTime;
                        data.LastTime = LastTime;
                        data.Level4 = lv4.TrimEnd('/');
                        data.Level3 = lv3.TrimEnd('/');
                        data.Level2 = lv2.TrimEnd('/');
                        data.Level1 = lv1.TrimEnd('/');
                        data.ResponseTime = ResponseTime;
                        data.HandleTime = HandleTime;
                        if (!State.IsNullOrEmpty())
                        {
                            data.State = ((AndonManageState)Convert.ToInt32(State)).ToLabel();
                        }
                        datas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    throw new ValidationException(e.GetBaseException().Message);
                }
            }
            return datas;
        }

        /// <summary>
        /// 安灯异常统计报表柱形图-工厂
        /// </summary>
        /// <param name="factoryCodes"></param>
        /// <param name="resource"></param>
        /// <param name="andonName"></param>
        /// <param name="equipAccountCode"></param>
        /// <param name="state"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("安灯异常统计报表柱形图-工厂")]
        [AllowAnonymous]
        public virtual List<AndonReportBarChartData> GetAndonReportBarChartDatasFactory(List<string> factoryCodes, string resource, string andonName, string equipAccountCode, int? state, DateTime? beginTime, DateTime? endTime)
        {
            List<AndonReportBarChartData> datas = new List<AndonReportBarChartData>();

            using (InvOrgs.WithAll())
            {
                var q = DB.Query<AndonManage>("am")
                    .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("am.Inv_Org_Id") == y.Code)
                    //只找待响应、待验收、处理中数据
                    .Where(p => p.State == AndonManageState.Standby || p.State == AndonManageState.Processing || p.State == AndonManageState.ToAccepted);
                if (state != null)
                    q.Where(p => p.State == (AndonManageState)(int)state);
                if (!resource.IsNullOrEmpty())
                    q.Where(p => p.WipResource.Code.Contains(resource) || p.WipResource.Name.Contains(resource));
                if (!andonName.IsNullOrEmpty())
                    q.Where(p => p.Andon.AndonName.Contains(andonName));
                if (!equipAccountCode.IsNullOrEmpty())
                    q.Where(p => p.EquipAccount.Code.Contains(equipAccountCode));
                if (beginTime != null)
                    q.Where(p => p.FaultTime >= beginTime);
                if (endTime != null)
                    q.Where(p => p.FaultTime <= endTime);
                if (factoryCodes.Count > 0)
                    q.Where(p => factoryCodes.Contains(p.SQL<string>("org.External_Id")));

                var factoryDatas = q.GroupBy(p => p.State).Select(p => new { State = p.State, Qty = p.SQL<decimal>("count(1) Qty") }).ToList<AndonReportBarChartDataFactory>().ToList();

                if (factoryDatas.Count > 0)
                {
                    AndonReportBarChartData data = new AndonReportBarChartData();

                    data.Standby = factoryDatas.Where(p => p.State == AndonManageState.Standby).FirstOrDefault()?.Qty ?? 0;
                    data.Processing = factoryDatas.Where(p => p.State == AndonManageState.Processing).FirstOrDefault()?.Qty ?? 0;
                    data.ToAccepted = factoryDatas.Where(p => p.State == AndonManageState.ToAccepted).FirstOrDefault()?.Qty ?? 0;

                    datas.Add(data);
                }
            }

            return datas;
        }

        #endregion

        #region 可疑品处理报表

        /// <summary>
        /// 可疑品处理报表-工厂
        /// </summary>
        /// <param name="factoryCodes"></param>
        /// <param name="mrpControllers"></param>
        /// <param name="process"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("可疑品处理报表-工厂")]
        [AllowAnonymous]
        public virtual List<SuspectReportData> GetSuspectReportDatasFactory(List<string> factoryCodes, List<string> mrpControllers, string process, DateTime? beginTime, DateTime? endTime)
        {
            List<SuspectReportData> datas = new List<SuspectReportData>();

            using (InvOrgs.WithAll())
            {
                var q = DB.Query<ReportRecord>("rr")
                    .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("rr.Inv_Org_Id") == y.Code)
                    .Join<DispatchTask>((x, y) => x.DispatchTaskId == y.Id);
                q.WhereIf(beginTime != null, p => p.ReportTime >= beginTime);
                q.WhereIf(endTime != null, p => p.ReportTime <= endTime);
                if (factoryCodes.Count > 0)
                {
                    q.Where(p => factoryCodes.Contains(p.SQL<string>("org.External_Id")));
                }
                q.WhereIf(mrpControllers.Count > 0, p => mrpControllers.Contains(p.WorkOrder.WorkShop.Code));
                q.WhereIf(!process.IsNullOrEmpty(), p => p.Process.Code.Contains(process));
                //先在报工记录中按照工序分组，计算总量、报废总量
                datas = q.GroupBy(p => p.Process.Code).Select(p => new { Process = p.Process.Code, TotalQty = p.ReportQty.SUM(), TotalNgQty = p.NgQty.SUM() }).ToList<SuspectReportData>().ToList();

                //再单独计算可疑品数
                //同样按照工序分组
                var qSuppect = DB.Query<SuspectProductLabel>("spl")
                               .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("spl.Inv_Org_Id") == y.Code);
                qSuppect.WhereIf(beginTime != null, p => p.CreateDate >= beginTime);
                qSuppect.WhereIf(endTime != null, p => p.CreateDate <= endTime);
                if (factoryCodes.Count > 0)
                {
                    q.Where(p => factoryCodes.Contains(p.SQL<string>("org.External_Id")));
                }
                qSuppect.WhereIf(mrpControllers.Count > 0, p => mrpControllers.Contains(p.WorkOrder.WorkShop.Code));
                qSuppect.WhereIf(!process.IsNullOrEmpty(), p => p.Process.Code.Contains(process));
                var rSuppect = qSuppect.GroupBy(p => p.Process.Code).Select(p => new { Process = p.Process.Code, TotalSuspectQty = p.Qty.SUM() }).ToList<SuspectReportData>().ToList();

                //然后再以result为主，将两个集合的数量合在一起
                foreach (var r in datas)
                {
                    r.TotalSuspectQty = rSuppect.Where(p => p.Process == r.Process).Sum(p => p.TotalSuspectQty);
                }

            }

            return datas;
        }

        /// <summary>
        /// 缺陷报表-工厂
        /// </summary>
        /// <param name="factoryCodes"></param>
        /// <param name="mrpControllers"></param>
        /// <param name="process"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("缺陷报表-工厂")]
        [AllowAnonymous]
        public virtual List<SuspectDefectData> GetSuspectDefectDatasFactory(List<string> factoryCodes, List<string> mrpControllers, string process, DateTime? beginTime, DateTime? endTime)
        {
            List<SuspectDefectData> datas = new List<SuspectDefectData>();

            using (InvOrgs.WithAll())
            {
                var q = DB.Query<Defect>("d")
                .Join<SuspectProductLabelDetail>("spld",(x, y) => x.Id == y.DefectId)
                .Join<SuspectProductLabelDetail, SuspectProductLabel>((x, y) => x.SuspectProductLabelId == y.Id)
                .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("d.Inv_Org_Id") == y.Code);

                if (beginTime != null)
                {
                    q.Where<SuspectProductLabel>((d, spl) => spl.CreateDate >= beginTime);
                }
                if (endTime != null)
                {
                    q.Where<SuspectProductLabel>((d, spl) => spl.CreateDate <= endTime);
                }
                if (factoryCodes.Count > 0)
                {
                    q.Where(p => factoryCodes.Contains(p.SQL<string>("org.External_Id")));
                }
                if (mrpControllers.Count > 0)
                {
                    q.Where<SuspectProductLabel>((d, spl) => mrpControllers.Contains(spl.WorkOrder.WorkShop.Code));
                }
                if (!process.IsNullOrEmpty())
                {
                    q.Where<SuspectProductLabel>((d, spl) => spl.Process.Code.Contains(process));
                }
                datas = q.GroupBy(p => p.Code).GroupBy(p => p.Description).Select(p => new { DefectCode = p.Code, DefectName = p.Description, Qty = p.SQL<decimal>("Sum(nvl(spld.Qty,0)) Qty") }).ToList<SuspectDefectData>().ToList();
            }

            return datas;
        }

        #endregion

        #region 产品直通率报表

        /// <summary>
        /// 物料平衡报表-工厂
        /// </summary>
        /// <param name="factoryCodes"></param>
        /// <param name="mrpControllers"></param>
        /// <param name="itemType"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("产品直通率报表-工厂")]
        [AllowAnonymous]
        public virtual List<ProductFirstPassYieldFactoryData> GetProductFirstPassYieldDatasFactory(List<string> factoryCodes, List<string> mrpControllers, string product, DateTime? beginTime, DateTime? endTime)
        {
            List<ProductFirstPassYieldFactoryData> datas = new List<ProductFirstPassYieldFactoryData>();

            using (InvOrgs.WithAll())
            {
                var q = DB.Query<WorkOrder>("wo")
                    .Join<Enterprise>("e", (x, y) => x.WorkShopId == y.Id)
                    .Join<Item>("i", (x, y) => x.ProductId == y.Id)
                    .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("wo.Inv_Org_Id") == y.Code);
                if (factoryCodes.Count > 0)
                {
                    q.Where(p => factoryCodes.Contains(p.SQL<string>("org.External_Id")));
                }
                if (mrpControllers.Count > 0)
                {
                    q.Where(p => mrpControllers.Contains(p.WorkShop.Code));
                }
                if (!product.IsNullOrEmpty())
                {
                    q.Where(p => p.Product.Code.Contains(product));
                }
                q.Exists<ReportRecord>((x, y) => y.Where(p => p.WorkOrderId == x.Id).WhereIf(beginTime != null, p => p.ReportTime >= beginTime).WhereIf(endTime != null, p => p.ReportTime <= endTime));
                //要对车间和物料分组,后面需要用这个进行查询直通率
                var result = q.GroupBy(p => p.Product.Code).GroupBy(p => p.SQL<string>("org.External_Id")).GroupBy(p => p.WorkShop.Code).Select(p => new { Product = p.Product.Code, Inv_Org_Id = p.SQL<string>("org.External_Id Inv_Org_Id"), WorkShopCode = p.WorkShop.Code }).ToList<ProductFirstPassYieldFactoryData>().ToList();

                //将查出来的按照库存组织去分，这样就只会查询3次
                foreach (var g in result.GroupBy(p=>p.Inv_Org_Id))
                {
                    //调用明细的接口，获取他们的投料量和可疑品数
                    var dtlDatas = GetProductFirstPassYieldDtlDatasFactory(new List<string>() { g.Key }, g.Select(p => p.WorkShopCode).ToList(), g.Select(p => p.Product).ToList(), beginTime, endTime);
                    //根据车间和物料去区分
                    foreach (var g1 in g.GroupBy(p => p.Product))
                    {
                        ProductFirstPassYieldFactoryData r = new ProductFirstPassYieldFactoryData();
                        r.Inv_Org_Id = g.Key;
                        r.Product = g1.Key;
                        r.datas = dtlDatas.Where(p => p.ProductCode == g1.Key).ToList();
                        datas.Add(r);
                    }
                }
            }

            return datas;
        }

        /// <summary>
        /// 物料平衡报表明细-工厂
        /// </summary>
        /// <param name="factoryCodes"></param>
        /// <param name="mrpControllers"></param>
        /// <param name="product"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTim"></param>
        /// <returns></returns>
        [ApiService("物料平衡报表明细-工厂")]
        [AllowAnonymous]
        public virtual List<ProductFirstPassYieldDtlData> GetProductFirstPassYieldDtlDatasFactory(List<string> factoryCodes, List<string> mrpControllers, List<string> products, DateTime? beginTime, DateTime? endTime)
        {
            List<ProductFirstPassYieldDtlData> datas = new List<ProductFirstPassYieldDtlData>();

            using (InvOrgs.WithAll())
            {
                datas = DB.Query<ReportRecord>("rr")
                    .Join<Rbac.InvOrgs.InvOrg>("org", (x, y) => x.SQL<int>("rr.Inv_Org_Id") == y.Code && factoryCodes.Contains(y.ExternalId))
                    .Where(p => mrpControllers.Contains(p.DispatchTask.WorkOrder.WorkShop.Code) && products.Contains(p.DispatchTask.WorkOrder.Product.Code))
                    .WhereIf(beginTime != null, p => p.ReportTime >= beginTime)
                    .WhereIf(endTime != null, p => p.ReportTime <= endTime)
                    .GroupBy(p => p.Process.Code)
                    .GroupBy(p => p.DispatchTask.WorkOrder.Product.Code)
                    .Select(p => new { Process = p.Process.Code, FeedingQty = p.SQL<decimal>("sum(nvl(rr.Report_Qty,0) + nvl(rr.Suspect_Qty,0)) FeedingQty"), SuspectQty = p.SQL<decimal>("sum(nvl(rr.Suspect_Qty,0)) SuspectQty"), ProductCode = p.DispatchTask.WorkOrder.Product.Code })
                    .ToList<ProductFirstPassYieldDtlData>().ToList();
            }
            return datas;
        }

        #endregion

        #region 物料平衡报表

        [ApiService("物料平衡报表-工厂")]
        [AllowAnonymous]
        public virtual List<ItemBalanceData> GetItemBalanceDatasFactory(List<string> factoryCodes, List<string> mrpControllers, string itemType, DateTime? beginTime, DateTime? endTime)
        {

            List<ItemBalanceData> datas = new List<ItemBalanceData>();

            string query = " 1 = 1";
            var can4Query = " 1 = 1";

            if (mrpControllers != null && mrpControllers.Count > 0)
            {
                var escapedItems = mrpControllers.Select(s => $"'{s}'");
                query += $" and t1.Work_Shop_Code in ({string.Join(",", escapedItems)})";
                can4Query+= $" and t5.Code in ({string.Join(",", escapedItems)})";
            }
            if (beginTime != null)
            {
                query += $" and t1.report_time >= to_date('{beginTime}','yyyy-mm-dd hh24:mi:ss')";
                can4Query += $" and DEDUCTION_RECORD.Create_Date >= to_date('{beginTime}','yyyy-mm-dd hh24:mi:ss')";
            }
            if (endTime != null)
            {
                query += $" and t1.report_time <= to_date('{endTime}','yyyy-mm-dd hh24:mi:ss')";
                can4Query += $" and DEDUCTION_RECORD.Create_Date <= to_date('{endTime}','yyyy-mm-dd hh24:mi:ss')";
            }
            if (!itemType.IsNullOrEmpty() && itemType != "全部")
                query += $" and t1.Product_Name like '%{itemType}%'";
            if (factoryCodes != null && factoryCodes.Count > 0)
            {
                var escapedItems = factoryCodes.Select(s => $"'{s}'");
                query += $" and t1.Factory in ({string.Join(",", escapedItems)})";
                can4Query += $" and t3.external_id in ({string.Join(",", escapedItems)})";
            }

            //以下每个字段再SQL中都是独立计算的，一块计算一个字段内容
            string sql = $@"
                                    with 
            can1 as ( SELECT t1.Wo,t1.Factory,--t1.Work_Shop_Code,
                CASE
                    WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
                END as item_type
            FROM FACTORY_REPORT_RECORD_V t1
            where {query} 
            GROUP BY --t1.Work_Shop_Code,
                t1.Wo,t1.Factory,
                CASE
                    WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
                END
                ), 
            --通过扣料记录的创建时间，获取到对应的标签，然后再通过这些标签再去上料记录查找
            w1 as (
    select DEDUCTION_RECORD.Feeding_Item_Label,DEDUCTION_RECORD.Inv_Org_Id
    from DEDUCTION_RECORD
    inner join TM_REPORT_RECORD t1 on t1.Is_Phantom = 0 and t1.id = DEDUCTION_RECORD.Report_Record_Id
    inner join wo on wo.is_phantom = 0 and wo.id =  t1.Work_Order_Id
    inner join RES_ENTERPRISE t5 on t5.id = wo.work_shop_id and t5.is_phantom = 0  --车间
    inner join SYS_INV_ORG t3 on t3.code = DEDUCTION_RECORD.inv_org_id and t3.is_phantom = 0
    where DEDUCTION_RECORD.Is_Phantom = 0 and {can4Query}
    group by DEDUCTION_RECORD.Feeding_Item_Label,DEDUCTION_RECORD.Inv_Org_Id
    ),
            can2 as (                               --获取取样净重详情,计算产出用量
            select 
            (select t1.Finish_Qty * t1.Weight from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory and rownum = 1) ProductQty,t0.wo
            ,t0.Factory,--t0.Work_Shop_Code,
                CASE
                    WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
                END as item_type
            from can1 t0
            inner join FAC_WEIGHT_OF_SAMP_REPORT_V t1 on  t1.Wo_No = t0.Wo and t1.Factory = t0.Factory
            where (t1.Product_Name LIKE '%铜%' or t1.Product_Name LIKE '%铝%')
            group by t0.wo,t0.factory,--t0.Work_Shop_Code,
                CASE
                    WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
                END
            ),
            can3 as                      --工单BOM,计算产出用量
            (
            select
                     SUM(
                    CASE 
                        WHEN t1.Bwart = '261' THEN t1.Single_Qty * t1.Finish_Qty  -- 261的A2正常累加
                        WHEN t1.Bwart = '531' THEN -t1.Single_Qty * t1.Finish_Qty -- 531的A2按负数累加（等价于相减）
                        ELSE 0                   -- 其他A1值不参与计算
                    END
                ) AS ProductQty,
                    CASE
                    WHEN t1.item_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.item_Name LIKE '%铝%' THEN '铝'
                END as item_type
            from can1 t0
            inner join FACTORY_WO_BOM_V t1 on t1.Wo_no = t0.wo and t1.Factory = t0.Factory
            where not exists(select 1 from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory) and (t1.item_Name LIKE '%铜%' or t1.item_Name LIKE '%铝%')
            group by 
                CASE
                    WHEN t1.item_Name LIKE '%铜%' THEN '铜'
                    WHEN t1.item_Name LIKE '%铝%' THEN '铝'
                END
            ),
            can4 as      --计算出投料量,计算余料量
            (
                   select sum(nvl(FEEDING_RECORD.Feeding_Qty,0)) feedingQty,sum(nvl(FEEDING_RECORD.Remaining_Qty,0)) Remaining_Qty
                    ,CASE
        WHEN t1.name LIKE '%铜%' THEN '铜'
        WHEN t1.name LIKE '%铝%' THEN '铝'
    END as item_type
    from FEEDING_RECORD
    inner join w1 on w1.Feeding_Item_Label = FEEDING_RECORD.Feeding_Item_Label and w1.Inv_Org_Id = FEEDING_RECORD.Inv_Org_Id
    inner join item t1 on t1.id = FEEDING_RECORD.item_id and t1.is_phantom = 0
    inner join SYS_INV_ORG t3 on t3.code = FEEDING_RECORD.inv_org_id and t3.is_phantom = 0
    where FEEDING_RECORD.Is_Phantom = 0 and (t1.name LIKE '%铜%' or t1.name LIKE '%铝%')
    group by 
    CASE
        WHEN t1.name LIKE '%铜%' THEN '铜'
        WHEN t1.name LIKE '%铝%' THEN '铝'
    END
            ),
            can5 as                 --联/副产品入库
            (
                 select sum(nvl(v1.qty,0)) Output_Product_Qty,
                          CASE
                    WHEN v1.item_Name LIKE '%铜%' THEN '铜'
                    WHEN v1.item_Name LIKE '%铝%' THEN '铝'
                END as item_type
                 from FACTORY_OUTPUT_PRO_REC_V v1
                 inner join can1 t1 on v1.Work_Order_No = t1.Wo and t1.Factory = v1.factory
                 where (v1.item_Name LIKE '%铜%' or v1.item_Name LIKE '%铝%')
                 group by 
                 CASE
                    WHEN v1.item_Name LIKE '%铜%' THEN '铜'
                    WHEN v1.item_Name LIKE '%铝%' THEN '铝'
                END
            )
            select t1.item_type,t2.ProductQty ProductQty2,t3.ProductQty ProductQty3,t4.feedingQty,t4.Remaining_Qty,t5.Output_Product_Qty
            --构建一个只有铜和铝两行数据，后面可以根据这两行显示数据
            from (SELECT CASE LEVEL WHEN 1 THEN '铜' WHEN 2 THEN '铝' END AS item_type FROM dual CONNECT BY LEVEL <= 2) t1
            left join (select sum(ProductQty) ProductQty,item_type from can2 group by item_type) t2 on t2.item_type = t1.item_type --can2 t2 on t2.item_type = t1.item_type 
            left join can3 t3 on t3.item_type = t1.item_type 
            left join can4 t4 on t4.item_type = t1.item_type 
            left join can5 t5 on t5.item_type = t1.item_type 
                                    ";

            //            string sql = $@"
            //                        with 
            //can1 as ( SELECT t1.Wo,t1.Factory,--t1.Work_Shop_Code,
            //    CASE
            //        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
            //    END as item_type
            //FROM FACTORY_REPORT_RECORD_V t1
            //where (t1.Product_Name LIKE '%铜%' or t1.Product_Name LIKE '%铝%') and {query} 
            //GROUP BY --t1.Work_Shop_Code,
            //    t1.Wo,t1.Factory,
            //    CASE
            //        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
            //    END
            //    ), 
            //can2 as (                               --获取取样净重详情,计算产出用量
            //select 
            //(select t1.Finish_Qty * t1.Weight from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory and rownum = 1) ProductQty,t0.wo
            //,t0.Factory,--t0.Work_Shop_Code,
            //    CASE
            //        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
            //    END as item_type
            //from can1 t0
            //inner join FAC_WEIGHT_OF_SAMP_REPORT_V t1 on  t1.Wo_No = t0.Wo and t1.Factory = t0.Factory
            //where (t1.Product_Name LIKE '%铜%' or t1.Product_Name LIKE '%铝%')
            //group by t0.wo,t0.factory,--t0.Work_Shop_Code,
            //    CASE
            //        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
            //    END
            //),
            //can3 as                      --工单BOM,计算产出用量
            //(
            //select
            //         SUM(
            //        CASE 
            //            WHEN t1.Bwart = '261' THEN t1.Single_Qty * t1.Finish_Qty  -- 261的A2正常累加
            //            WHEN t1.Bwart = '531' THEN -t1.Single_Qty * t1.Finish_Qty -- 531的A2按负数累加（等价于相减）
            //            ELSE 0                   -- 其他A1值不参与计算
            //        END
            //    ) AS ProductQty,t0.wo,t0.Factory,
            //        CASE
            //        WHEN t1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.item_Name LIKE '%铝%' THEN '铝'
            //    END as item_type
            //    --,t0.Work_Shop_Code
            //from can1 t0
            //inner join FACTORY_WO_BOM_V t1 on t1.Wo_no = t0.wo and t1.Factory = t0.Factory
            //where not exists(select 1 from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory) and (t1.item_Name LIKE '%铜%' or t1.item_Name LIKE '%铝%')
            //group by t0.wo,t0.Factory,--t0.Work_Shop_Code,
            //    CASE
            //        WHEN t1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN t1.item_Name LIKE '%铝%' THEN '铝'
            //    END
            //),
            //can4 as      --计算出投料量,计算余料量
            //(
            //     select sum(nvl(v1.Feeding_Qty,0)) feedingQty,sum(nvl(v1.Remaining_Qty,0)) Remaining_Qty,t1.wo,t1.Factory,--t1.Work_Shop_Code,
            //         CASE
            //        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
            //    END as item_type
            //     from Factory_Wo_FEEDINGQty_V v1
            //     inner join can1 t1 on v1.WoNo = t1.wo and v1.factory = t1.Factory
            //where (v1.item_Name LIKE '%铜%' or v1.item_Name LIKE '%铝%')
            //     group by t1.wo,t1.Factory,--t1.Work_Shop_Code,
            //         -- GROUP BY需和SELECT的CASE判断完全一致
            //    CASE
            //        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
            //    END
            //),
            //can5 as                 --联/副产品入库
            //(
            //     select sum(nvl(v1.qty,0)) Output_Product_Qty,t1.wo,t1.Factory,--t1.Work_Shop_Code,
            //              CASE
            //        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
            //    END as item_type
            //     from FACTORY_OUTPUT_PRO_REC_V v1
            //     inner join can1 t1 on v1.Work_Order_No = t1.Wo and t1.Factory = v1.factory
            //     where (v1.item_Name LIKE '%铜%' or v1.item_Name LIKE '%铝%')
            //     group by          t1.wo,t1.Factory,--t1.Work_Shop_Code,
            //     CASE
            //        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
            //        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
            //    END
            //)
            //select t1.item_type,t1.wo,t2.ProductQty ProductQty2,t3.ProductQty ProductQty3,t4.feedingQty,t4.Remaining_Qty,t5.Output_Product_Qty,t1.factory--,t1.Work_Shop_Code
            //from can1 t1
            //left join can2 t2 on t2.wo = t1.wo and t2.item_type = t1.item_type --and t2.Work_Shop_Code = t1.Work_Shop_Code
            //left join can3 t3 on t3.wo = t1.wo and t3.item_type = t1.item_type --and t3.Work_Shop_Code = t1.Work_Shop_Code
            //left join can4 t4 on t4.wo = t1.wo and t4.item_type = t1.item_type --and t4.Work_Shop_Code = t1.Work_Shop_Code
            //left join can5 t5 on t5.wo = t1.wo and t5.item_type = t1.item_type --and t5.Work_Shop_Code = t1.Work_Shop_Code
            //                        ";
            List<ItemBalanceData> list = new List<ItemBalanceData>();
            using (var db = DB.Create("MES"))
            {
                try
                {
                    var dt = db.ExecuteDataTable(sql, CommandType.Text);
                    foreach (DataRow row in dt.Rows)
                    {
                        //var productLine = row["Product_Line"].ToString();
                        //var plantName = row["Plant_Name"].ToString();
                        //var workShopCode = row["Work_Shop_Code"].ToString();
                        var iType = row["item_type"].ToString();
                        var productQty2 = row["ProductQty2"].ToString();
                        var productQty3 = row["ProductQty3"].ToString();
                        var feedingQty = row["feedingQty"].ToString();
                        var remainingQty = row["Remaining_Qty"].ToString();
                        var outputProductQty = row["Output_Product_Qty"].ToString();

                        //var factory = row["factory"].ToString();
                        ItemBalanceData data = new ItemBalanceData();

                        //data.ProductLine = productLine;
                        //data.Department = plantName;
                        //data.FactoryCode = factory;
                        //data.WorkShopCode = workShopCode;
                        data.ItemType = iType;
                        decimal productQty = 0;
                        if (!productQty2.IsNullOrEmpty())
                            productQty = Convert.ToDecimal(productQty2);
                        else if (!productQty3.IsNullOrEmpty())
                            productQty = Convert.ToDecimal(productQty3);
                        data.ProductQty = productQty;
                        data.FeedingQty = feedingQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(feedingQty);
                        data.RemainingQty = remainingQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(remainingQty);
                        data.OutputProductQty = outputProductQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(outputProductQty);
                        data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
                        data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);
                        list.Add(data);
                    }
                }
                catch (Exception e)
                {
                    throw new ValidationException(e.GetBaseException().Message);
                }
            }

            var dic = list.GroupBy(p => new { p.ItemType }).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var d in dic)
            {
                ItemBalanceData data = new ItemBalanceData();

                //data.ProductLine = d.Key.ProductLine;
                //data.Department = d.Key.Department;
                //data.FactoryCode = d.Key.FactoryCode;
                //data.WorkShopCode = d.Key.WorkShopCode;
                data.ItemType = d.Key.ItemType;
                data.ProductQty = d.Value.Sum(p => p.ProductQty);
                data.FeedingQty = d.Value.Sum(p => p.FeedingQty);
                data.RemainingQty = d.Value.Sum(p => p.RemainingQty);
                data.OutputProductQty = d.Value.Sum(p => p.OutputProductQty);
                data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
                data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);

                datas.Add(data);
            }

            return datas;
        }

        #endregion

        /// <summary>
        /// 生产达成率报表 - 工厂
        /// </summary>
        /// <param name="rateData"></param>
        /// <param name="mrpDics"></param>
        /// <param name="dicInvCodeProcessCode"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        [ApiService("生产达成率报表- 工厂")]
        [AllowAnonymous]
        public virtual List<ProductionAchievementRateData> ProductionAchievementRateFactory
            (RequestProductionAchievementRateData rateData, List<DictionaryData> mrpDics,
            List<DictionaryData>  dicInvCodeProcessCode, EntityList<OrganizeCode> list)
        {
            var entityList = RT.Service.Resolve<DispatchTaskController>().GetDispatchTaskList(mrpDics, dicInvCodeProcessCode, rateData.DateRange?.BeginValue ?? null, rateData.DateRange?.EndValue ?? null);

            List<ProductionAchievementRateData> datas = new List<ProductionAchievementRateData>();
            foreach (var item in mrpDics)
            {
                foreach (var mrp in item.DicValue)
                {
                    var entity = list.Where(p => p.MrpController == mrp).FirstOrDefault();
                    if (!entityList.ContainsKey(item.DicKey) || entityList[item.DicKey] == null || entityList[item.DicKey].Count == 0)
                        continue;
                    var dispatchTaskList = entityList[item.DicKey].Where(p => p.WorkShopCode == mrp).ToList();
                    if (dispatchTaskList == null || dispatchTaskList.Count == 0) continue;
                    var processNameLists = dispatchTaskList.Select(p => p.ProcessCode).Distinct().ToList();
                    foreach (var processName in processNameLists)
                    {
                        var qtyEntityList = dispatchTaskList.Where(p => p.ProcessCode == processName).ToList();
                        var planQty = qtyEntityList.Sum(p => p.DispatchQty);
                        var actualQty = qtyEntityList.Sum(p => p.ReportQty);
                        if (entity != null)
                            datas.Add(new ProductionAchievementRateData()
                            {
                                ProductLine = entity.ProductLine,
                                PlantName = entity.PlantName,
                                ProcessName = processName,
                                PlanQty = planQty / 10000,
                                ActualQty = actualQty / 10000,
                                UnitName = qtyEntityList.FirstOrDefault()?.UnitName ?? "",
                                ProductionAchievement = (planQty == 0 || actualQty == 0) ? 0 : (actualQty / planQty)
                            });
                    }
                }
            }
            if (datas.Count == 0)
                datas.Add(new ProductionAchievementRateData());
            return datas;
        }

        /// <summary>
        /// 产能利用率报表 - 工厂
        /// </summary>
        /// <param name="productionProcesses"></param>
        /// <param name="dataType"></param>
        /// <param name="dicpProcessCodes"></param>
        /// <returns></returns>
        [ApiService("产能利用率报表- 工厂")]
        [AllowAnonymous]
        public virtual List<CapacityUtilizationRateData> CapacityUtilizationRateFactory(EntityList<ProductionProcess> productionProcesses, RequestCapacityUtilizationRateData model, List<DictionaryData> dicpProcessCodes)
        {
            List<CapacityUtilizationRateData> datas = new List<CapacityUtilizationRateData>();
            DateRange dateRange = new DateRange();
            int Year = model.Year.IsNullOrEmpty() ? 0 : int.Parse(model.Year);
            int Month = model.Month.IsNullOrEmpty() ? 0 : int.Parse(model.Month);
            int Num = model.Num.IsNullOrEmpty() ? 0 : int.Parse(model.Num);
            switch (model.CapacityDataType)
            {
                case ProductionProcesss.Enums.CapacityDataType.Moon:
                    var dateTime = new DateTime(Year, Month, 1, 0, 0, 0);
                    dateRange.BeginValue = dateTime;
                    dateRange.EndValue = dateTime.AddMonths(1);
                    break;
                case ProductionProcesss.Enums.CapacityDataType.Week:
                    var weekDay = GetWeekDateRange(Year, Month, Num);
                    dateRange.BeginValue = weekDay;
                    dateRange.EndValue = weekDay.AddDays(7);
                    break;
                case ProductionProcesss.Enums.CapacityDataType.Day:
                    var day = new DateTime(Year, Month, Num, 0, 0, 0);
                    dateRange.BeginValue = day;
                    dateRange.EndValue = day.AddDays(1);
                    break;
            }

            var entityList = RT.Service.Resolve<DispatchTaskController>().GetDispatchTaskList(dateRange.BeginValue, dateRange.EndValue, dicpProcessCodes);
            var uphList = RT.Service.Resolve<CapacityResourceController>().GetStandardCapacity(model.CapacityDataType, dicpProcessCodes);

            foreach (var item in productionProcesses.OrderBy(p => p.ProductLine))
            {
                if (entityList[item.InventoryCode] == null)
                    continue;
                var dispatchTaskList = entityList[item.InventoryCode].Where(p => p.ProcessCode == item.ProcessCode).ToList();
                var actualQty = dispatchTaskList.Sum(p => p.ReportQty);
                var standardCapacity = uphList[item.InventoryCode + item.ProcessCode];
                var entity = new CapacityUtilizationRateData();
                entity.ProductLine = item.ProductLine;
                entity.PlantName = item.PlantName;
                entity.ProcessName = item.ProcessCode;
                entity.ActualQty = actualQty;
                entity.StandardCapacity = standardCapacity;
                entity.CapacityUtilization = entity.ActualQty == 0 || entity.StandardCapacity == 0 ? 0 : entity.ActualQty / entity.StandardCapacity;
                datas.Add(entity);
            }
            if (datas.Count == 0)
                datas.Add(new CapacityUtilizationRateData());
            return datas;
        }

        private DateTime GetWeekDateRange(
          int year,
          int month,
          int weekNumber,
          DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            // 获取当月第一天
            DateTime firstDayOfMonth = new DateTime(year, month, 1);

            // 计算当月第一天与一周起始日的差值，确定第一周的起始日期
            int daysToAdd = ((int)startOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
            DateTime firstDayOfFirstWeek = firstDayOfMonth.AddDays(daysToAdd);

            // 如果当月第一天就是一周的起始日，第一周起始日就是当月第一天
            if (firstDayOfFirstWeek > firstDayOfMonth)
            {
                firstDayOfFirstWeek = firstDayOfFirstWeek.AddDays(-7);
            }

            // 计算目标周的起始日期
            DateTime startDate = firstDayOfFirstWeek.AddDays((weekNumber - 1) * 7);

            return startDate;
        }

        /// <summary>
        ///  安灯异常统计报表- 工厂
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dicOrganizeCode"></param>
        /// <param name="dicWids"></param>
        /// <returns></returns>
        [ApiService("安灯异常统计报表- 工厂")]
        [AllowAnonymous]
        public virtual List<AndonAnomalyData> AndonAnomalyFactory(RequestAndonAnomalyData model,
            List<DictionaryObjData> dicOrganizeCode, List<DictionaryData> dicWids)
        {
            var entityList = RT.Service.Resolve<AndonManageController>().GetAndonAnomaly(dicWids, model.DateRange?.BeginValue ?? null, model.DateRange?.EndValue ?? null);
            List<AndonAnomalyData> datas = new List<AndonAnomalyData>();

            foreach (var item in entityList)
            {
                var organizeCodeList = dicOrganizeCode.Where(p => p.DicKey == item.FactoryCode).Select(p => p.DicValue).ToList<object>();
                OrganizeCode organizeCode = null;
                organizeCodeList.ForEach(p =>
                {
                    var entity = p as OrganizeCode;
                    if(entity.WorkshopCode == item.WorkShopCode)
                    {
                        organizeCode = entity;
                        return;
                    }
                });
                datas.Add(new AndonAnomalyData()
                {
                    ProductLine = organizeCode?.ProductLine ?? "",
                    PlantName = organizeCode?.PlantName ?? "",
                    AnDengCount = item.AndonNum,
                    AndonClass = item.AndonBigType,
                    AnDengType = item.AndonType,
                    OnTimeProcessCount = item.OnTimeProcessCount,
                    OnTimeResponseCount = item.OnTimeResponseCount,
                    OnTimeResponseRate = Math.Round(item.OnTimeResponseRate, 2),
                    OnTimeProcessRate = Math.Round(item.OnTimeProcessRate, 2),
                    ExceptionProcessTime = item.ExceptionProcessTime,
                    ExceptionResponseTime = item.ExceptionResponseTime,
                });
            }
            if (datas.Count == 0)
                datas.Add(new AndonAnomalyData());
            return datas;
        }
    }
}
