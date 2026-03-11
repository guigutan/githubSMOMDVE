using Microsoft.Scripting.Interpreter;
using SIE.Barcodes.WipBatchs;
using SIE.Domain;
using SIE.MES.Report.BatchWipProducts;
using SIE.MES.TaskManagement.Reports;
using SIE.MES.TaskManagement.SuspectProductLabels;
using SIE.Tech.Processs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.Report.BatchTracebacks
{
    public class BatchTracebacksController : DomainController
    {
        /// <summary>
        /// 根据标签号和工序编码获取开机准备记录
        /// </summary>
        /// <param name="batchNo"></param>
        /// <param name="processCode"></param>
        /// <param name="pagingInfo"></param>
        /// <param name="orderInfos"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackPreSetup> GetBatchTracebackPreSetups(string batchNo,string processCode, PagingInfo pagingInfo, IList<OrderInfo> orderInfos)
        {
            var list = Query<BatchTracebackPreSetup>()
                .Join<ReportRecord>((x, y) => x.DispatchTaskId == y.DispatchTaskId)
                .Join<ReportRecord, Process>((x, y) => x.ProcessId == y.Id && y.Code == processCode)
                .Join<ReportRecord, ReportWipBatch>((x, y) => x.Id == y.ReportRecordId && y.BatchNo == batchNo)
                .OrderBy(orderInfos)
                .ToList(pagingInfo, new EagerLoadOptions().LoadWithViewProperty());

            return list;

        }

        /// <summary>
        /// 根据可疑品标签Id获取缺陷代码
        /// </summary>
        /// <param name="suspectProductLabelId"></param>
        /// <param name="pagingInfo"></param>
        /// <param name="orderInfos"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackDefetctDtl> GetBatchTracebackDefetctDtlsById(double suspectProductLabelId, PagingInfo pagingInfo, IList<OrderInfo> orderInfos)
        { 
            var list =Query<BatchTracebackDefetctDtl>().Where(p=>p.SuspectProductLabelId == suspectProductLabelId).OrderBy(orderInfos).ToList(pagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

        /// <summary>
        /// 根据id获取产品缺陷记录明细
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pagingInfo"></param>
        /// <param name="orderInfos"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackDefetctLabelDtl> GetBatchTracebackDefetctLabelDtlsById(string batchNo,string ProcessCode, PagingInfo pagingInfo, IList<OrderInfo> orderInfos)
        {
            var list = Query<BatchTracebackDefetctLabelDtl>().Where(p => p.BatchNo == batchNo && p.Process.Code == ProcessCode).OrderBy(orderInfos).ToList(pagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;

        }

        /// <summary>
        /// 根据报工记录id获取产品生产关键件明细
        /// </summary>
        /// <param name="reportRecordId"></param>
        /// <param name="pagingInfo"></param>
        /// <param name="orderInfos"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackKeyDtl> GetBatchTracebackKeyDtlsByReportRecordId(double reportRecordId, PagingInfo pagingInfo, IList<OrderInfo> orderInfos)
        {
            var list = Query<BatchTracebackKeyDtl>().Where(p => p.ReportRecordId == reportRecordId).OrderBy(orderInfos).ToList(pagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

        /// <summary>
        /// 根据id获取批次采集记录明细
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackReportDtl> GetBatchTracebackReportDtlsByIds(double id, PagingInfo pagingInfo, IList<OrderInfo> orderInfos)
        {
            var list = Query<BatchTracebackReportDtl>().Where(p => p.Id == id).OrderBy(orderInfos).ToList(pagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

        /// <summary>
        /// 查询方法
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public virtual EntityList<BatchTracebackReport> CriteriaBatchTracebackReports(BatchTracebackReportCriteria criteria)
        {
            var q = Query<BatchTracebackReport>();
            if (!criteria.WorkOrderNo.IsNullOrEmpty())
                q.Where(p => p.WorkOrder.No.Contains(criteria.WorkOrderNo));
            if (!criteria.ItemLabelLot.IsNullOrEmpty())
            {
                q.Exists<BatchTracebackKeyDtl>((x, y) => y.Join<BatchTracebackReportDtl>((x1, y1) => x1.ReportRecordId == y1.ReportRecordId && y1.Id == x.Id).Where(p => p.ItemLabelLot.Contains(criteria.ItemLabelLot)));
            }
            if (!criteria.ProductCode.IsNullOrEmpty())
                q.Where(p => p.WorkOrder.Product.Code.Contains(criteria.ProductCode));
            if (!criteria.ShortDescription.IsNullOrEmpty())
                q.Where(p => p.WorkOrder.Product.ShortDescription.Contains(criteria.ShortDescription));
            if (!criteria.WorkShopName.IsNullOrEmpty())
                q.Where(p => p.WorkOrder.Fevor.Contains(criteria.WorkShopName));
            if (criteria.ProcessId > 0)
                q.Where(p => p.ReportRecord.ProcessId == criteria.ProcessId);
            if (criteria.NextProcessId > 0)
                q.Where(p => p.NextProcessId == criteria.NextProcessId);
            if (criteria.BatchType == BatchType.Rework)
            {
                q.Join<WipBatch>((x, y) => x.BatchNo == y.BatchNo && (y.IsSuspectProduct == YesNo.No || y.IsSuspectProduct == null) && y.IsRework == true);
            }
            if (criteria.BatchType == BatchType.Scraped)
            {
                q.Join<WipBatch>((x, y) => x.BatchNo == y.BatchNo && (y.IsSuspectProduct == YesNo.No || y.IsSuspectProduct == null) && y.IsScraped == true);
            }
            if (criteria.BatchType == BatchType.Good)
            {
                q.Join<WipBatch>((x, y) => x.BatchNo == y.BatchNo && (y.IsSuspectProduct == YesNo.No || y.IsSuspectProduct == null) && y.IsScraped == false && y.IsRework == false);
            }
            if (criteria.BatchType == BatchType.Suspect)
            {
                q.Join<WipBatch>((x, y) => x.BatchNo == y.BatchNo && y.IsSuspectProduct == YesNo.Yes);
            }
            if (criteria.IsFinish == YesNo.Yes)
            {
                q.Where(p => p.NextProcessId == null || p.NextProcessId == 0);
            }
            if (criteria.IsFinish == YesNo.No)
            {
                q.Where(p => p.NextProcessId > 0);
            }

            var list = q.ToList(criteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());

            var batchNos = list.Select(p => p.BatchNo).Distinct().ToList();
            var wipBatchs = RT.Service.Resolve<WipBatchController>().GetWipBatches(batchNos);
            //计算批次类型
            foreach (var wipBatch in wipBatchs)
            {
                var P_Type = RT.Service.Resolve<WipBatchController>().GetWipBatchType(wipBatch);
                list.Where(p => p.BatchNo == wipBatch.BatchNo).ForEach(p => p.BatchType = P_Type);
            }
            //计算工单返工数
            var woNos = list.Select(p => p.WorkOrderNo).Distinct().ToList();
            var reportRecords = RT.Service.Resolve<ReportController>().GetReportRecordExaminesByWoNos(woNos);
            foreach (var item in woNos)
            {
                var reworkQty = reportRecords.Where(p => p.Wo == item).Sum(p => p.ReworkQty);
                var suspectQty = reportRecords.Where(p => p.Wo == item).Sum(p => p.SuspectQty);
                list.Where(p => p.WorkOrderNo == item).ForEach(p => { p.ReworkQty = reworkQty; p.SuspectQty = suspectQty; });
            }
            //获取委外发货明细
            var processingOutbounds = RT.Service.Resolve<Outsourcing.OutsourcingController>().GetProcessingOutboundsBySns(batchNos);
            //获取委外收货明细
            var processingInStocks = RT.Service.Resolve<Outsourcing.OutsourcingController>().GetProcessingInStocksBySns(batchNos);

            list.ForEach(p => p.IsOutsourcing = false);
            foreach (var batchNo in batchNos)
            {
                //在委外发货中存在,在委外收货中不存在,就是还在委外中
                if (processingOutbounds.Any(p => p.SN == batchNo) && processingInStocks.All(p => p.SN != batchNo))
                {
                    list.Where(p => p.BatchNo == batchNo).ForEach(p => p.IsOutsourcing = true);
                }
            }

            var nextProcessIds = list.Where(p => p.NextProcessId > 0).Select(p => p.NextProcessId.Value).Distinct().ToList();
            var nextProcesses = RT.Service.Resolve<ProcessController>().GetProcessByIds(nextProcessIds, true);
            foreach (var nextProcess in nextProcesses)
            {
                list.Where(p => p.NextProcessId == nextProcess.Id).ForEach(p => p.NextProcessCode = nextProcess.Code);
            }
            //根据工单号获取可疑品标签
            var suspectProductLabels = RT.Service.Resolve<SuspectProductLabelController>().GetSuspectProductLabelsByWoNos(woNos);
            foreach (var woNo in woNos)
            {
                //计算报废数
                var ScrapQty = suspectProductLabels.Where(p => p.WorkOrderNo == woNo).Sum(p => p.ScrapQty);
                list.Where(p => p.WorkOrderNo == woNo).ForEach(p => p.ScrapQty = ScrapQty);
            }

            return list;
        }
    }
}
