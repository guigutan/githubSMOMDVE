using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Irony.Parsing;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Utils;
using NPOI.SS.Formula.Functions;
using SIE.Andon.Andons;
using SIE.Andon.Andons.Enum;
using SIE.Api;
using SIE.Core.ApiModels;
using SIE.Core.Common;
using SIE.Domain;
using SIE.Domain.ORM;
using SIE.Domain.Validation;
using SIE.Items;
using SIE.KZ.Base.SmomControl;
using SIE.KZ.Group.SmomControl.BaseDatas;
using SIE.MES.Capacitys;
using SIE.MES.DashBoard.KzBoard.Datas;
using SIE.MES.DashBoard.KzBoard.RegionBoards;
using SIE.MES.DashBoard.KzReport.Datas;
using SIE.MES.DashBoard.KzReport.OrganizeCodes;
using SIE.MES.DashBoard.KzReport.ProductionLineProcesss;
using SIE.MES.DashBoard.KzReport.ProductionProcesss;
using SIE.MES.DashBoard.KzReport.ProductionProcesss.Enums;
using SIE.MES.ItemLine;
using SIE.MES.ProcessProperty;
using SIE.MES.TaskManagement.Dispatchs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.KzReport
{
    /// <summary>
    /// 
    /// </summary>
    public partial class KzReportController : DomainController
    {
        /// <summary>
        /// 获取下拉物料类型
        /// </summary>
        /// <returns></returns>
        [ApiService("获取下拉物料类型")]
        public virtual List<object> GetItemType()
        {
            List<object> list = new List<object>();
            list.Add(new { item = "铜" });
            list.Add(new { item = "铝" });
            list.Add(new { item = "全部" });
            return list;
            //List<string> itemTypes = new List<string>();
            //itemTypes.Add("铜");
            //itemTypes.Add("铝");
            //itemTypes.Add("全部");
            //return itemTypes;
        }

        /// <summary>
        /// 获取下拉部门
        /// </summary>
        /// <param name="productLine"></param>
        /// <returns></returns>
        [ApiService("获取下拉部门")]
        public virtual List<string> GetDepartments(string productLine)
        {
            var plantCodes = Query<OrganizeCode>().Where(p=>p.ProductLine == productLine).Select(p => p.PlantCode).Distinct().ToList<string>().ToList();
            return plantCodes;
        }

        /// <summary>
        /// 获取下拉产品线
        /// </summary>
        /// <returns></returns>
        [ApiService("获取下拉产品线")]
        public virtual List<string> GetProductLine()
        {
            var productLines = Query<OrganizeCode>().Select(p => p.ProductLine).Distinct().ToList<string>().ToList();
            return productLines;
        }

        #region 安灯异常统计报表

        /// <summary>
        /// 安灯异常统计报表柱形图
        /// </summary>
        /// <param name="line"></param>
        /// <param name="factory"></param>
        /// <param name="resource"></param>
        /// <param name="andonName"></param>
        /// <param name="equipAccountCode"></param>
        /// <param name="state"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("安灯异常统计报表柱形图")]
        public virtual List<AndonReportBarChartData> GetAndonReportBarChartDatas(string line, string factory, string resource, string andonName, string equipAccountCode, int? state, DateTime? beginTime, DateTime? endTime)
        {

            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!factory.IsNullOrEmpty())
                q.Where(p => p.FactoryName == factory);
            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);

            List<AndonReportBarChartData> factoryDatas = new List<AndonReportBarChartData>();

            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //找出相同产品线的
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine }))
                    {
                        var factoryCs = plang.Select(p => p.FactoryCode).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = factoryCs },
                    new SmomParam { Value = resource },
                    new SmomParam{ Value = andonName  },
                    new SmomParam{ Value = equipAccountCode  },
                    new SmomParam{ Value = state  },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<AndonReportBarChartData>>("KzReportController", "GetAndonReportBarChartDatasFactory", g.Key, smomParam);
                        foreach (var r in response)
                        {
                            r.ProductLine = plang.Key.ProductLine;
                            factoryDatas.Add(r);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            List<AndonReportBarChartData> datas = new List<AndonReportBarChartData>();

            foreach (var g in factoryDatas.GroupBy(p => p.ProductLine))
            {
                AndonReportBarChartData data = new AndonReportBarChartData();

                data.ProductLine = g.Key;
                data.Standby = g.Sum(p => p.Standby);
                data.Processing = g.Sum(p => p.Processing);
                data.ToAccepted = g.Sum(p => p.ToAccepted);

                datas.Add(data);
            }

            return datas;            
        }

        /// <summary>
        /// 获取状态下拉
        /// </summary>
        /// <returns></returns>
        [ApiService("获取状态下拉")]
        public virtual List<AndonReportStateData> GetAndonReportStateDatas()
        {
            List<AndonReportStateData> datas = new List<AndonReportStateData>();

            foreach (AndonManageState type in Enum.GetValues(typeof(AndonManageState)))
            {
                AndonReportStateData data = new AndonReportStateData();

                data.Value = (int)type;
                data.Key = type.ToLabel();

                datas.Add(data);
            }

            return datas;
        }

        /// <summary>
        /// 安灯异常统计报表
        /// </summary>
        /// <param name="line"></param>
        /// <param name="factory"></param>
        /// <param name="resource"></param>
        /// <param name="andonName"></param>
        /// <param name="EquipAccountCode"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("安灯异常统计报表")]
        public virtual List<AndonReportData> GetAndonReportDatas(string line, string factory, string resource, string andonName, string equipAccountCode,int? state, DateTime? beginTime, DateTime? endTime)
        {
            List<AndonReportData> datas = new List<AndonReportData>();

            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!factory.IsNullOrEmpty())
                q.Where(p => p.FactoryName == factory);
            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);

            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //找出相同产品线的
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine }))
                    {
                        var factoryCs = plang.Select(p => p.FactoryCode).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = factoryCs },
                    new SmomParam { Value = resource },
                    new SmomParam{ Value = andonName  },
                    new SmomParam{ Value = equipAccountCode  },
                    new SmomParam{ Value = state  },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<AndonReportData>>("KzReportController", "GetAndonReportDatasFactory", g.Key, smomParam);
                        foreach (var r in response)
                        {
                            r.ProductLine = plang.Key.ProductLine;
                            datas.Add(r);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            int index = 1;
            foreach (var data in datas)
            {
                data.Num = index;
                index++;
            }
            return datas;
        }

        #endregion

        #region 可疑品处理报表

        /// <summary>
        /// 可疑品处理报表
        /// </summary>
        /// <param name="line"></param>
        /// <param name="department"></param>
        /// <param name="process"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("可疑品处理报表")]
        public virtual List<SuspectReportData> GetSuspectReportDatas(string line, string department, string process, DateTime? beginTime, DateTime? endTime)
        {
            List<SuspectReportData> datas = new List<SuspectReportData>();

            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!department.IsNullOrEmpty())
                q.Where(p => p.PlantName == department);

            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);

            List<SuspectReportData> factoryDatas = new List<SuspectReportData>();

            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //对产品线和部门进行分组(按照前端显示),查出的数据就只会是相同产品线和部门，调用的接口中会对物料类型分组，下方会将相同产品线和部门进行合并计算
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine, p.PlantName }))
                    {
                        var mrpControllers = plang.Select(p => p.MrpController).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = fCs },
                    new SmomParam { Value = mrpControllers },
                    new SmomParam{ Value = process  },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<SuspectReportData>>("KzReportController", "GetSuspectReportDatasFactory", g.Key, smomParam);
                        foreach (var r in response)
                        {
                            r.ProductLine = plang.Key.ProductLine;
                            r.Department = plang.Key.PlantName;
                            factoryDatas.Add(r);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            int index = 1;
            foreach (var g in factoryDatas.GroupBy(p => new { p.ProductLine, p.Department, p.Process }))
            {
                SuspectReportData data = new SuspectReportData();
                data.Num = index;
                data.ProductLine = g.Key.ProductLine;
                data.Department = g.Key.Department;
                data.Process = g.Key.Process;
                data.TotalQty = g.Sum(p => p.TotalQty) / 10000;
                data.TotalSuspectQty = g.Sum(p => p.TotalSuspectQty) / 10000;
                data.TotalNgQty = g.Sum(p => p.TotalNgQty) / 10000;
                data.NgQtyRate = data.TotalQty == 0 ? 0 : Math.Round((data.TotalNgQty * 100) / data.TotalQty, 4);
                data.SuspectRate = data.TotalQty == 0 ? 0 : Math.Round((data.TotalSuspectQty * 100) / data.TotalQty, 4);
                data.OkRate = data.TotalQty == 0 ? 0 : Math.Round(1 - ((data.TotalSuspectQty * 100) / data.TotalQty), 4);
                datas.Add(data);
                index++;
            }
            return datas;
        }

        /// <summary>
        /// 缺陷报表
        /// </summary>
        /// <param name="line"></param>
        /// <param name="department"></param>
        /// <param name="process"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("缺陷报表")]
        public virtual List<SuspectDefectData> GetSuspectDefectDatas(string line, string department, string process, DateTime? beginTime, DateTime? endTime)
        {
            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!department.IsNullOrEmpty())
                q.Where(p => p.PlantName == department);

            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);

            List<SuspectDefectData> factoryDatas = new List<SuspectDefectData>();

            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //对产品线和部门进行分组(按照前端显示),查出的数据就只会是相同产品线和部门，调用的接口中会对物料类型分组，下方会将相同产品线和部门进行合并计算
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine, p.PlantName }))
                    {
                        var mrpControllers = plang.Select(p => p.MrpController).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = fCs },
                    new SmomParam { Value = mrpControllers },
                    new SmomParam{ Value = process  },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<SuspectDefectData>>("KzReportController", "GetSuspectDefectDatasFactory", g.Key, smomParam);
                        foreach (var r in response)
                        {
                            factoryDatas.Add(r);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            List<SuspectDefectData> datas = new List<SuspectDefectData>();
            int index = 1;
            var total = factoryDatas.Sum(p => p.Qty);
            //相同缺陷合并分组
            foreach (var g in factoryDatas.GroupBy(p => new { p.DefectCode, p.DefectName }))
            {
                SuspectDefectData data = new SuspectDefectData();
                data.Num = index;
                data.DefectCode = g.Key.DefectCode;
                data.DefectName = g.Key.DefectName;
                data.Qty = g.Sum(p => p.Qty);
                data.Rate = total == 0 ? 0 : Math.Round((data.Qty * 100) / total, 4);
                datas.Add(data);
            }
            //从多到少排序
            datas = datas.OrderByDescending(p => p.Qty).ToList();
            return datas;
        }
        

        #endregion

        #region 产品直通率报表

        /// <summary>
        /// 产品直通率报表
        /// </summary>
        /// <param name="line"></param>
        /// <param name="department"></param>
        /// <param name="product"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("产品直通率报表")]
        public virtual List<ProductFirstPassYieldData> GetProductFirstPassYieldDatas(string line, string department, string product, DateTime? beginTime, DateTime? endTime)
        {

            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!department.IsNullOrEmpty())
                q.Where(p => p.PlantName == department);

            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);

            List<ProductFirstPassYieldData> factoryDatas = new List<ProductFirstPassYieldData>();
            Dictionary<string, List<ProductFirstPassYieldFactoryData>> dic = new Dictionary<string, List<ProductFirstPassYieldFactoryData>>();
            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //对产品线和部门进行分组(按照前端显示),查出的数据就只会是相同产品线和部门，调用的接口中会对物料类型分组，下方会将相同产品线和部门进行合并计算
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine, p.PlantName }))
                    {
                        var mrpControllers = plang.Select(p => p.MrpController).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = fCs },
                    new SmomParam { Value = mrpControllers },
                    new SmomParam{ Value = product  },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<ProductFirstPassYieldFactoryData>>("KzReportController", "GetProductFirstPassYieldDatasFactory", g.Key, smomParam);
                        foreach (var item in response.GroupBy(p => p.Product))
                        {
                            factoryDatas.Add(new ProductFirstPassYieldData() {
                                Department = plang.Key.PlantName,
                                ProductLine = plang.Key.ProductLine,
                                ProductCode = item.Key
                            });
                            //将数据存起来 ，后续用于计算直通率,按照产品线+部门+产品去分组
                            if (dic.ContainsKey(plang.Key.PlantName + "-" + plang.Key.ProductLine + "-" + item.Key))
                            {
                                dic[plang.Key.PlantName + "-" + plang.Key.ProductLine + "-" + item.Key].AddRange(item.ToList());
                            }
                            else
                            {
                                dic.Add(plang.Key.PlantName + "-" + plang.Key.ProductLine + "-" + item.Key, item.ToList());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            List<ProductFirstPassYieldData> datas = new List<ProductFirstPassYieldData>();

            int index = 1;
            foreach (var g in factoryDatas.GroupBy(p => new { p.Department, p.ProductLine,p.ProductCode }))
            {
                ProductFirstPassYieldData data = new ProductFirstPassYieldData();
                data.Num = index;
                data.ProductLine = g.Key.ProductLine;
                data.Department = g.Key.Department;
                data.ProductCode = g.Key.ProductCode;
                data.FirstPassYield = 0;
                if (dic.ContainsKey(g.Key.Department + "-" + g.Key.ProductLine + "-" + g.Key.ProductCode))
                {
                    var list = dic[g.Key.Department + "-" + g.Key.ProductLine + "-" + g.Key.ProductCode];
                    if (list.Count > 0)
                    {
                        data.FirstPassYield = 1;
                        //找到相同工序，然后合并他们的数量，计算每个工序的直通率，然后相乘得到主表的直通率
                        foreach (var l in list.SelectMany(p => p.datas).GroupBy(p => p.Process))
                        {
                            data.FirstPassYield *= (l.Sum(p => p.FeedingQty) == 0 ? 1 : 1 - (l.Sum(p => p.SuspectQty) / l.Sum(p => p.FeedingQty)));
                        }
                    }
                }

                datas.Add(data);
                index++;
            }

            return datas;
        }

        /// <summary>
        /// 产品直通率报表-明细
        /// </summary>
        /// <param name="line"></param>
        /// <param name="department"></param>
        /// <param name="product"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [ApiService("产品直通率报表明细")]
        public virtual List<ProductFirstPassYieldDtlData> GetProductFirstPassYieldDtlDatas(string productLine, string department, string productCode, DateTime? beginTime, DateTime? endTime)
        {
            var q = Query<OrganizeCode>();
            q.Where(p => p.ProductLine == productLine);
            q.Where(p => p.PlantName == department);
            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);
            List<ProductFirstPassYieldDtlData> factoryDatas = new List<ProductFirstPassYieldDtlData>();

            foreach (var g in settings.GroupBy(p => p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    var mrpControllers = organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).Select(p => p.MrpController).Distinct().ToList();
                    var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = fCs },
                    new SmomParam { Value = mrpControllers },
                    new SmomParam{ Value = productCode.IsNullOrEmpty()?new List<string>(): new List<string>(){ productCode } },
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                    var response = SmomControlHepler.SmomPost<List<ProductFirstPassYieldDtlData>>("KzReportController", "GetProductFirstPassYieldDtlDatasFactory", g.Key, smomParam);
                    factoryDatas.AddRange(response);
                }
                catch (Exception ex)
                {

                }
            }

            List<ProductFirstPassYieldDtlData> datas = new List<ProductFirstPassYieldDtlData>();

            var index = 1;
            foreach (var g in factoryDatas.GroupBy(p => p.Process))
            {
                ProductFirstPassYieldDtlData data = new ProductFirstPassYieldDtlData();
                data.Num = index;
                data.Process = g.Key;
                data.SuspectQty = g.Sum(p => p.SuspectQty);
                data.FeedingQty = g.Sum(p => p.FeedingQty);
                data.FirstPassYield = data.FeedingQty == 0 ? 1 : 1 - (data.SuspectQty / data.FeedingQty);
                datas.Add(data);
                index++;
            }

            return datas;
        }

        #endregion

        #region 物料平衡报表

        /// <summary>
        /// 物料平衡报表
        /// </summary>
        /// <param name="line">产品线</param>
        /// <param name="department">部门</param>
        /// <param name="workShopt">车间</param>
        /// <param name="itemType">物料类型</param>
        /// <param name="beginTime">开始日期</param>
        /// <param name="endTime">结束日期</param>
        /// <returns></returns>
        [ApiService("物料平衡报表")]
        public virtual List<ItemBalanceData> GetItemBalanceDatas(string line, string department, string itemType, DateTime? beginTime, DateTime? endTime)
        {
            List<ItemBalanceData> factoryDatas = new List<ItemBalanceData>();

            var q = Query<OrganizeCode>();
            if (!line.IsNullOrEmpty())
                q.Where(p => p.ProductLine == line);
            if (!department.IsNullOrEmpty())
                q.Where(p => p.PlantName == department);

            var organizeCodes = q.ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            var factoryCodes = organizeCodes.Select(p => p.FactoryCode).Distinct().ToList();
            var settings = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingByFactoryCodes(factoryCodes);
            foreach (var g in settings.GroupBy(p=>p.FactoryUrl))
            {
                try
                {
                    var fCs = g.Select(p => p.FactoryCode).Distinct().ToList();
                    //对产品线和部门进行分组(按照前端显示),查出的数据就只会是相同产品线和部门，调用的接口中会对物料类型分组，下方会将相同产品线和部门进行合并计算
                    foreach (var plang in organizeCodes.Where(p => fCs.Contains(p.FactoryCode)).GroupBy(p => new { p.ProductLine, p.PlantName }))
                    {
                        var mrpControllers = plang.Select(p => p.MrpController).Distinct().ToList();
                        var smomParam = new List<SmomParam>()
                    {
                    new SmomParam { Value = fCs },
                    new SmomParam { Value = mrpControllers },
                    new SmomParam{ Value = itemType},
                    new SmomParam{ Value = beginTime},
                    new SmomParam{ Value = endTime}
                                 }.ToArray();
                        var response = SmomControlHepler.SmomPost<List<ItemBalanceData>>("KzReportController", "GetItemBalanceDatasFactory", g.Key, smomParam);
                        foreach (var item in response)
                        {
                            item.Department = plang.Key.PlantName;
                            item.ProductLine = plang.Key.ProductLine;
                        }
                        factoryDatas.AddRange(response);
                    }
                }
                catch (Exception ex)
                { 
                    
                }
            }

            if (factoryDatas.Count < 1)
                return new List<ItemBalanceData>();

            List<ItemBalanceData> datas = new List<ItemBalanceData>();

            var dic = factoryDatas.GroupBy(p => new { p.ProductLine, p.Department }).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var d in dic)
            {
                var oC = organizeCodes.Where(p => p.ProductLine == d.Key.ProductLine && p.PlantName == d.Key.Department).FirstOrDefault();

                foreach (var item in d.Value)
                {
                    ItemBalanceData data = new ItemBalanceData();
                    data.ProductLine = oC.ProductLine;
                    data.Department = oC.PlantName;
                    //data.FactoryCode = d.Key.FactoryCode;
                    //data.WorkShopCode = d.Key.WorkShopCode;
                    data.ItemType = item.ItemType;
                    data.FeedingQty = item.FeedingQty;
                    data.ProductQty = item.ProductQty;
                    data.RemainingQty = item.RemainingQty;
                    data.OutputProductQty = item.OutputProductQty;
                    data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
                    data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);

                    datas.Add(data);
                }

            }

            return datas;

            #region 

//            List<ItemBalanceData> datas = new List<ItemBalanceData>();

//            string query = " 1 = 1";
//            if (!line.IsNullOrEmpty())
//                query += $" and t0.Product_Line = '{line}'";
//            if (!department.IsNullOrEmpty())
//                query += $" and t0.Plant_Code = '{department}'";
//            if (beginTime != null)
//                query += $" and t1.report_time >= to_date('{beginTime}','yyyy-mm-dd hh24:mi:ss')";
//            if (endTime != null)
//                query += $" and t1.report_time <= to_date('{endTime}','yyyy-mm-dd hh24:mi:ss')";
//            if (!itemType.IsNullOrEmpty() && itemType != "全部")
//                query += $" and t1.Product_Name like '%{itemType}%'";

//            string sql = $@"
//                        with 
//can1 as ( SELECT T0.Product_Line,T0.Plant_Name,t1.Wo,t1.Factory,
//    CASE
//        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END as item_type
//FROM ORGANIZE_CODE T0
//inner join FACTORY_REPORT_RECORD_V t1 on t1.Work_Shop_Code = t0.mrp_controller
//WHERE T0.IS_PHANTOM = 0 and {query} 
//GROUP BY T0.Product_Line,T0.Plant_Name,t1.Wo,t1.Factory,
//    CASE
//        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END
//    ), 
//can2 as (                               --获取取样净重详情,计算产出用量
//select 
//(select t1.Finish_Qty * t1.Weight from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory and rownum = 1) ProductQty,t0.wo
//,t0.Factory,
//    CASE
//        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END as item_type
//from can1 t0
//inner join FAC_WEIGHT_OF_SAMP_REPORT_V t1 on  t1.Wo_No = t0.Wo and t1.Factory = t0.Factory
//group by t0.wo,t0.factory,
//    CASE
//        WHEN t1.Product_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.Product_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END
//),
//can3 as                      --工单BOM,计算产出用量
//(
//select
//         SUM(
//        CASE 
//            WHEN t1.Bwart = '261' THEN t1.Single_Qty  -- 261的A2正常累加
//            WHEN t1.Bwart = '531' THEN -t1.Single_Qty -- 531的A2按负数累加（等价于相减）
//            ELSE 0                   -- 其他A1值不参与计算
//        END
//    ) AS ProductQty,t0.wo,t0.Factory,
//        CASE
//        WHEN t1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END as item_type
//from can1 t0
//inner join FACTORY_WO_BOM_V t1 on t1.Wo_no = t0.wo and t1.Factory = t0.Factory
//where not exists(select 1 from FAC_WEIGHT_OF_SAMP_REPORT_V t1 where t1.Wo_No = t0.Wo and t1.Factory = t0.Factory)
//group by t0.wo,t0.Factory,
//    CASE
//        WHEN t1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN t1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END
//),
//can4 as      --计算出投料量,计算余料量
//(
//     select sum(nvl(v1.Feeding_Qty,0)) feedingQty,sum(nvl(v1.Remaining_Qty,0)) Remaining_Qty,t1.wo,t1.Factory,
//         CASE
//        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END as item_type
//     from FACTORY_FEEDING_RECORD_V v1
//     inner join can1 t1 on v1.wo_no = t1.wo and v1.factory = t1.Factory
//     group by t1.wo,t1.Factory,
//         -- GROUP BY需和SELECT的CASE判断完全一致
//    CASE
//        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END
//),
//can5 as                 --联/副产品入库
//(
//     select sum(nvl(v1.qty,0)) Output_Product_Qty,t1.wo,t1.Factory,
//              CASE
//        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END as item_type
//     from FACTORY_OUTPUT_PRO_REC_V v1
//     inner join can1 t1 on v1.Work_Order_No = t1.Wo and t1.Factory = v1.factory
//     group by          t1.wo,t1.Factory,
//     CASE
//        WHEN v1.item_Name LIKE '%铜%' THEN '铜'
//        WHEN v1.item_Name LIKE '%铝%' THEN '铝'
//        ELSE NULL
//    END
//)
//select T1.Product_Line,T1.Plant_Name,t1.item_type,t1.wo,t2.ProductQty ProductQty2,t3.ProductQty ProductQty3,t4.feedingQty,t4.Remaining_Qty,t5.Output_Product_Qty,t1.factory
//from can1 t1
//left join can2 t2 on t2.wo = t1.wo and t2.item_type = t1.item_type and t2.factory = t1.factory
//left join can3 t3 on t3.wo = t1.wo and t3.item_type = t1.item_type and t3.factory = t1.factory
//left join can4 t4 on t4.wo = t1.wo and t4.item_type = t1.item_type and t4.fasctory = t1.factory
//left join can5 t5 on t5.wo = t1.wo and t5.item_type = t1.item_type and t5.factory = t1.factory
//                        ";
//            List<ItemBalanceData> list = new List<ItemBalanceData>();
//            using (var db = DB.Create("MES"))
//            {
//                try
//                {
//                    var dt = db.ExecuteDataTable(sql, CommandType.Text);
//                    foreach (DataRow row in dt.Rows)
//                    {
//                        var productLine = row["Product_Line"].ToString();
//                        var plantName = row["Plant_Name"].ToString();
//                        var iType = row["item_type"].ToString();
//                        var productQty2 = row["ProductQty2"].ToString();
//                        var productQty3 = row["ProductQty3"].ToString();
//                        var feedingQty = row["feedingQty"].ToString();
//                        var remainingQty = row["Remaining_Qty"].ToString();
//                        var outputProductQty = row["Output_Product_Qty"].ToString();
//                        var factory = row["factory"].ToString();
//                        ItemBalanceData data = new ItemBalanceData();

//                        data.ProductLine = productLine;
//                        data.Department = plantName;
//                        data.ItemType = iType;
//                        decimal productQty = 0;
//                        if (!productQty2.IsNullOrEmpty())
//                            productQty = Convert.ToDecimal(productQty2);
//                        else if (!productQty3.IsNullOrEmpty())
//                            productQty = Convert.ToDecimal(productQty3);
//                        data.ProductQty = productQty;
//                        data.FeedingQty = feedingQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(feedingQty);
//                        data.RemainingQty = remainingQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(remainingQty);
//                        data.OutputProductQty = outputProductQty.IsNullOrEmpty() ? 0 : Convert.ToDecimal(outputProductQty);
//                        data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
//                        data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);
//                        list.Add(data);
//                    }
//                }
//                catch (Exception e)
//                {
//                    throw new ValidationException(e.GetBaseException().Message);
//                }
//            }

//            var dic = list.GroupBy(p => new { p.ProductLine, p.Department, p.ItemType }).ToDictionary(p => p.Key, p => p.ToList());
//            foreach (var d in dic)
//            {
//                ItemBalanceData data = new ItemBalanceData();

//                data.ProductLine = d.Key.ProductLine;
//                data.Department = d.Key.Department;
//                data.ItemType = d.Key.ItemType;
//                data.ProductQty = d.Value.Sum(p => p.ProductQty);
//                data.FeedingQty = d.Value.Sum(p => p.FeedingQty);
//                data.RemainingQty = d.Value.Sum(p => p.RemainingQty);
//                data.OutputProductQty = d.Value.Sum(p => p.OutputProductQty);
//                data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
//                data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);

//                datas.Add(data);
//            }

//            return datas;
            #endregion

            #region 旧逻辑

            //var organizeCodes = Query<OrganizeCode>().WhereIf(!line.IsNullOrEmpty(), p => p.ProductLine == line).WhereIf(!department.IsNullOrEmpty(), p => p.PlantCode == department).ToList(null, new EagerLoadOptions().LoadWithViewProperty());

            //List<string> itemTypes = new List<string>();
            //if (itemType.IsNullOrEmpty())
            //{
            //    //对物料类型进行分组
            //    itemTypes.Add("铜");
            //    itemTypes.Add("铝");
            //    itemTypes.Add("");
            //}
            //else
            //{
            //    itemTypes.Add(itemType);
            //}

            ////var mrpControllers = organizeCodes.Select(p => p.MrpController).Distinct().ToList();

            //List<ItemBalanceData> datas = new List<ItemBalanceData>();
            ////根据产品线+厂区进行分组(下面还会对物料类型进一步分组)
            //foreach (var group in organizeCodes.GroupBy(p => new { p.ProductLine, p.PlantCode }))
            //{
            //    //获取分组后的车间(即MRB控制者)
            //    var mcs = group.Select(p => p.MrpController).Distinct().ToList();
            //    EntityList<FactoryReportRecord> reportRecords = new EntityList<FactoryReportRecord>();
            //    var pageSize = 50000;
            //    var pageNumber = 1;
            //    PagingInfo pagingInfo = new PagingInfo(pageNumber, pageSize);
            //    //var records = query.ToList(pagingInfo);
            //    //reportRecords.AddRange(records);
            //    //while (records.Count == pageSize)
            //    //{
            //    //    pagingInfo.PageNumber += 1;
            //    //    records = query.ToList(pagingInfo);
            //    //    reportRecords.AddRange(records);
            //    //}

            //    ////获取分组后的报工记录
            //    //var gRrs = reportRecords.ToList();

            //    //记录那些按照BOM去计算的工单，他们需要按照物料类型再去算一次
            //    List<string> specialWoNos = new List<string>();
            //    foreach (var it in itemTypes)
            //    {
            //        var query = Query<FactoryReportRecord>().Where(p => mcs.Contains(p.WorkShopCode));
            //        if (beginTime != null)
            //            query.Where(p => p.ReportTime >= beginTime);
            //        if (endTime != null)
            //            query.Where(p => p.ReportTime <= endTime);
            //        //if (!itemType.IsNullOrEmpty() && itemType != "全部")
            //        //    query.Where(p => p.ProductName.Contains("%" + itemType + "%"));

            //        var woNos = new List<string>();
            //        if (it != "")
            //        {
            //            woNos = query.Where(p => !p.ProductName.Contains("%铜%") && !p.ProductName.Contains("%铝%")).Select(p => p.Wo).Distinct().ToList<string>().ToList();
            //        }
            //        else
            //        {
            //            woNos = query.Where(p => p.ProductName.Contains("%"+it+"%")).Select(p => p.Wo).Distinct().ToList<string>().ToList();
            //        }
            //        if (specialWoNos.Count > 0)
            //        {
            //            woNos.AddRange(specialWoNos);
            //            woNos = woNos.Distinct().ToList();
            //        }
            //        //用工单号去找上料记录,计算出上料数量
            //        decimal feedingQty = 0;
            //        woNos.SplitDataExecute(temp => {
            //            feedingQty += Query<FactoryFeedingRecord>().Where(p => temp.Contains(p.WoNo)).Select(p => (decimal)p.FeedingQty.SUM()).FirstOrDefault<decimal>();
            //        });

            //        //计算产出用量
            //        //获取取样净重详情
            //        decimal ProductQty = 0;
            //        var weightOfSamplingReports = woNos.SplitContains(temp => {
            //            return Query<FactoryWeightOfSamplingReport>().Where(p => temp.Contains(p.WoNo)).ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            //        });

            //        var boms = woNos.SplitContains(temp =>
            //         {
            //             return Query<FactoryWorkOrderBom>().Where(p => temp.Contains(p.WoNo) && (p.Bwart == "261" || p.Bwart == "531")).WhereIf(it == "", p => !p.ItemName.Contains("%铜%") && !p.ItemName.Contains("%铝%")).WhereIf(it != "", p => p.ItemName.Contains("%" + it + "%")).ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            //         });

            //        foreach (var woNo in woNos)
            //        {
            //            //var gRr = gRrs.FirstOrDefault(p => p.Wo == woNo);
            //            //判断是否存在在取样净重中，如果存在就直接用取样净重去算，如果不存在就要找出BOM再按照物料类型去计算
            //            var weightOfSamplingReport = weightOfSamplingReports.FirstOrDefault(p => p.WoNo == woNo);
            //            if (weightOfSamplingReport != null)
            //            {
            //                ProductQty += weightOfSamplingReport.FinishQty * weightOfSamplingReport.Weight;
            //            }
            //            else
            //            {
            //                //此处记得区分工厂，不同工厂可能存在相同工单，防止数量叠加计算
            //                var woBoms = boms.Where(p => /*p.Factory == gRr.Factory && */p.WoNo == woNo && (p.Bwart == "261" || p.Bwart == "531")).ToList();
            //                //用261的单位耗用量-531的单位耗用量
            //                ProductQty += (woBoms.Where(p => p.Bwart == "261").Sum(p => p.SingleQty) - woBoms.Where(p => p.Bwart == "531").Sum(p => p.SingleQty)) * (woBoms.FirstOrDefault()?.FinishQty ?? 0);

            //                //记录下工单，等一下需要循环再去计算
            //                specialWoNos.Add(woNo);
            //            }
            //        }
            //        //联/副产品入库
            //        decimal OutputProductQty = 0;
            //        woNos.SplitDataExecute(temp => {
            //            OutputProductQty += Query<FactoryOutputProductRecord>().WhereIf(it == "", p => !p.ItemName.Contains("%铜%") && !p.ItemName.Contains("%铝%")).WhereIf(it != "", p => p.ItemName.Contains("%" + it + "%")).Where(p => temp.Contains(p.WorkOrderNo)).Select(p => p.Qty.SUM()).FirstOrDefault<decimal>();
            //        });
            //        //计算余料量
            //        decimal RemainingQty = 0;
            //        //用工单号去找上料记录,计算出上料数量
            //        woNos.SplitDataExecute(temp => {
            //            RemainingQty += Query<FactoryFeedingRecord>().Where(p => temp.Contains(p.WoNo)).Select(p => (decimal)p.RemainingQty.SUM()).FirstOrDefault<decimal>();
            //        });

            //        ItemBalanceData data = new ItemBalanceData();
            //        data.ProductLine = group.Key.ProductLine;
            //        data.Department = group.Key.PlantCode;
            //        data.ItemType = it;
            //        data.FeedingQty = feedingQty;
            //        data.ProductQty = ProductQty;
            //        data.OutputProductQty = OutputProductQty;
            //        data.RemainingQty = RemainingQty;
            //        data.DiffQty = data.FeedingQty - data.ProductQty - data.OutputProductQty - data.RemainingQty;
            //        data.Rate = data.FeedingQty == 0 ? 0 : Math.Round(data.DiffQty / data.FeedingQty * 100, 2);
            //        datas.Add(data);
            //    }
            //}
            //return datas;
            #endregion
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rateData"></param>
        /// <returns></returns>
        [ApiService("生产达成率报表")]
        public virtual List<ProductionAchievementRateData> ProductionAchievementRate(RequestProductionAchievementRateData rateData)
        {
            //获取库存组织下的工序
             var  dicInvCodeProcessCode = RT.Service.Resolve<ProductionLineProcessController>().GetInvCodeProcessCode(rateData.ProductLine, rateData.PlantName, rateData.ProcessCodes);
            //获取Mrp
            var mrpDics = RT.Service.Resolve<OrganizeCodeController>().GetMrps(rateData.ProductLine, rateData.PlantName);
          var list = RF.GetAll<OrganizeCode>();

            List<Task<List<ProductionAchievementRateData>>> tasks = new List<Task<List<ProductionAchievementRateData>>>();

            List<ProductionAchievementRateData> datas = new List<ProductionAchievementRateData>();
            var setting = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingUrl();
            foreach (var item in setting)
            {
                Task<List<ProductionAchievementRateData>> task = Task.Run(() => SmomControlHepler.SmomPost<List<ProductionAchievementRateData>>("KzReportController", "ProductionAchievementRateFactory", item, new List<SmomParam>()
                    {
                    new SmomParam { Value =rateData },
                    new SmomParam { Value =mrpDics },
                    new SmomParam { Value =dicInvCodeProcessCode },
                    new SmomParam { Value =list },
                                 }.ToArray()));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                // 检查任务是否成功完成
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    datas.AddRange(task.Result);
                }
            }
            datas = datas.Where(p => p.ProductLine != null).ToList();
            if (datas.Count == 0)
                datas.Add(new ProductionAchievementRateData());
            return datas;
        }

        /// <summary>
        /// 产能利用率报表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ApiService("产能利用率报表")]
        public virtual List<CapacityUtilizationRateData> CapacityUtilizationRate(RequestCapacityUtilizationRateData model)
        {
            List<CapacityUtilizationRateData> datas = new List<CapacityUtilizationRateData>();
            var productionProcesses = RT.Service.Resolve<ProductionProcessController>().GetProductionProcesses(model.ProductLine,model.PlantName,model.ProcessCode);
            //获取派工任务列表
            var dicpProcessCodes =  productionProcesses.GroupBy(p => p.InventoryCode).ToList()
                .ToDictionary(p=>p.Select(x=>x.InventoryCode).FirstOrDefault().ToString(),p=>p.Select(x => x.ProcessCode).ToList());

            var setting = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingUrl();

            List<Task<List<CapacityUtilizationRateData>>> tasks = new List<Task<List<CapacityUtilizationRateData>>>();
            foreach (var item in setting)
            {
                Task<List<CapacityUtilizationRateData>> task = Task.Run(() => SmomControlHepler.SmomPost<List<CapacityUtilizationRateData>>("KzReportController", "CapacityUtilizationRateFactory", item, new List<SmomParam>()
                    {
                    new SmomParam { Value =productionProcesses },
                    new SmomParam { Value =model },
                    new SmomParam { Value =dicpProcessCodes.Select(p => new DictionaryData() { DicKey = p.Key, DicValue = p.Value }).ToList<DictionaryData>()},
                                 }.ToArray()));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                // 检查任务是否成功完成
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    datas.AddRange(task.Result);
                }
            }
            datas = datas.Where(p => p.ProductLine != null).ToList();
            if (datas.Count == 0)
                datas.Add(new CapacityUtilizationRateData());
            return datas;
        }

        /// <summary>
        /// 获取下拉选择数据源
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        [ApiService("获取下拉选择数据源")]
        public virtual List<string> GetNum(CapacityDataType dataType,int year, int month)
        {
            List<string> str = new List<string>();
            switch (dataType)
            {
                case CapacityDataType.Moon:
                    str = new List<string>();
                    break;
                case CapacityDataType.Week:
                    var week = GetWeeksInMonth(year, month);
                    str = GenStr(week);
                    break;
                case CapacityDataType.Day:
                    var days = GetDaysInMonth(year,month);
                    str = GenStr(days);
                    break;
                default:
                    str = new List<string>();
                    break;
            }
            return str;
        }

        private List<string> GenStr(int num)
        {
            List<string> str = new List<string>();
            for (int i = 1; i < num+1; i++)
            {
                str.Add(i.ToString());
            }
            return str;
        }

        /// <summary>
        /// 计算指定年份和月份的总天数
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份（1-12）</param>
        /// <returns>该月份的总天数</returns>
        private int GetDaysInMonth(int year, int month)
        {
            try
            {
                // 验证月份合法性
                if (month < 1 || month > 12)
                {
                    throw new ValidationException("月份必须在1-12之间".L10N());
                }

                // 获取当月最后一天，其Day属性就是总天数
                DateTime lastDayOfMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
                return lastDayOfMonth.Day;
            }
            catch (Exception ex)
            {
                throw new ValidationException("计算天数时出错:{0}".L10nFormat(ex.Message));
            }
        }

        /// <summary>
        /// 计算指定年份和月份的总周数
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份（1-12）</param>
        /// <returns>该月份的总周数</returns>
        private  int GetWeeksInMonth(int year, int month)
        {
            try
            {
                // 验证月份是否合法
                if (month < 1 || month > 12)
                {
                    throw new ValidationException("月份必须在1-12之间".L10N());
                }

                // 获取本月第一天
                DateTime firstDayOfMonth = new DateTime(year, month, 1);
                // 获取本月最后一天
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // 计算本月总天数
                int totalDays = lastDayOfMonth.Day;

                // 计算第一天是星期几（将周日(0)转换为7，方便计算）
                int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                firstDayOfWeek = firstDayOfWeek == 0 ? 7 : firstDayOfWeek;

                // 计算总周数：(月初缺失天数 + 总天数) / 7 向上取整
                int totalWeeks = (firstDayOfWeek - 1 + totalDays + 6) / 7;

                return totalWeeks;
            }
            catch (Exception ex)
            {
                throw new ValidationException("计算周数时出错:{0}".L10nFormat(ex.Message));
            }
        }

        [ApiService("安灯异常统计报表")]
        public virtual List<AndonAnomalyData> AndonAnomaly(RequestAndonAnomalyData model)
        {
            //获取组织代码
            var dicOrganizeCode = RT.Service.Resolve<OrganizeCodeController>().GetOrganizeCodeList(model.ProductLine,model.PlantName);
            var dicWids = new List<DictionaryData>();
            dicOrganizeCode.ForEach((p) =>
            {
                var entityList = p.DicValue.Select(p => p as OrganizeCode).ToList<OrganizeCode>();
                dicWids.Add(new DictionaryData()
                {
                    DicKey= p.DicKey,
                    DicValue = entityList.Select(p=>p.WorkshopCode).ToList()
                });

            });

             List<AndonAnomalyData> datas = new List<AndonAnomalyData>();
            List<Task<List<AndonAnomalyData>>> tasks = new List<Task<List<AndonAnomalyData>>>();
            var setting = RT.Service.Resolve<SmomBaseController>().GetSmomControlSettingUrl();
            foreach (var item in setting)
            {
                Task<List<AndonAnomalyData>> task = Task.Run(() => SmomControlHepler.SmomPost<List<AndonAnomalyData>>("KzReportController", "AndonAnomalyFactory", item, new List<SmomParam>()
                    {
                    new SmomParam { Value =model },
                    new SmomParam { Value =dicOrganizeCode},
                    new SmomParam { Value = dicWids},
                                 }.ToArray()));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                // 检查任务是否成功完成
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    datas.AddRange(task.Result);
                }
            }
            datas = datas.Where(p => p.ProductLine != null).ToList();
            if (datas.Count == 0)
                datas.Add(new AndonAnomalyData());
            return datas;
        }
    }
}
