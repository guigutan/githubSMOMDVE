using DocumentFormat.OpenXml.EMMA;
using SIE.Andon.Andons;
using SIE.Api;
using SIE.Core.ApiModels;
using SIE.Core.Common;
using SIE.Core.Enums;
using SIE.Domain;
using SIE.Domain.Validation;
using SIE.MES.DashBoard.KzReport.Datas;
using SIE.MES.DashBoard.KzReport.OrganizeCodes;
using SIE.MES.DashBoard.KzReport.ProductionProcesss;
using SIE.MES.TaskManagement.Dispatchs;
using SIE.MES.TaskManagement.FeedingRecords;
using SIE.MES.TaskManagement.Reports;
using SIE.ObjectModel;
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
