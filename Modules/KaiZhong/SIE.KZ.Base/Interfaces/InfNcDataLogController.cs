using SIE.Domain;
using SIE.KZ.Base.Interfaces.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.KZ.Base.Interfaces
{
    public class InfNcDataLogController : DomainController
    {

        public virtual EntityList<InfNcDataLog> CriteriaInfNcDataLog(InfNcDataLogCriteria criteria)
        {
            var q = DB.Query<InfNcDataLog>("log");
            if (criteria.InfType != null)
                q.Where(p => p.InfType == criteria.InfType);
            if (!criteria.InfCode.IsNullOrEmpty())
                q.Where(p => p.InfCode.Contains(criteria.InfCode));
            if (!criteria.OperationType.IsNullOrEmpty())
                q.Where(p => p.OperationType.Contains(criteria.OperationType));
            if (criteria.CallResult != null)
                q.Where(p => p.CallResult == criteria.CallResult);
            if (!criteria.DataJsons.IsNullOrEmpty())
            {
                if (criteria.DataJsons.Contains('%'))
                    criteria.DataJsons = $"%{criteria.DataJsons}%";
                q.Where(p => p.SQL<bool>($"log.Data_Jsons like '{criteria.DataJsons}'"));
            }
            if (!criteria.ErrorMsg.IsNullOrEmpty())
            {
                if (criteria.ErrorMsg.Contains('%'))
                    criteria.ErrorMsg = $"%{criteria.ErrorMsg}%";
                q.Where(p => p.SQL<bool>($"log.Error_Msg like '{criteria.ErrorMsg}'"));
            }
            if (!criteria.GroupGuid.IsNullOrEmpty())
                q.Where(p => p.GroupGuid == criteria.GroupGuid);

            var list = q.OrderBy(criteria.OrderInfoList).ToList(criteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

        /*  public virtual EntityList<InfNcDataLogSO> CriteriaInfNcDataLogSO(InfNcDataLogSOCriteria criteria)
          {
              var q = DB.Query<InfNcDataLogSO>("log");
              if (criteria.InfType != null)
                  q.Where(p => p.InfType == criteria.InfType);
              if (!criteria.InfCode.IsNullOrEmpty())
                  q.Where(p => p.InfCode.Contains(criteria.InfCode));
              if (!criteria.OperationType.IsNullOrEmpty())
                  q.Where(p => p.OperationType.Contains(criteria.OperationType));
              if (criteria.CallResult != null)
                  q.Where(p => p.CallResult == criteria.CallResult);
              if (!criteria.DataJsons.IsNullOrEmpty())
              {
                  if (criteria.DataJsons.Contains('%'))
                      criteria.DataJsons = $"%{criteria.DataJsons}%";
                  q.Where(p => p.SQL<bool>($"log.Data_Jsons like '{criteria.DataJsons}'"));
              }
              if (!criteria.WO.IsNullOrEmpty())
              {
                  if (criteria.WO.Contains('%'))
                      criteria.WO = $"%{criteria.WO}%";
                  q.Where(p => p.SQL<bool>($"JSON_VALUE(log.Data_Jsons, '$.workOrderInfs[0].AUFNR' RETURNING VARCHAR2(100)) like '{criteria.WO}'"));
              }
              if (!criteria.ErrorMsg.IsNullOrEmpty())
              {
                  if (criteria.ErrorMsg.Contains('%'))
                      criteria.ErrorMsg = $"%{criteria.ErrorMsg}%";
                  q.Where(p => p.SQL<bool>($"log.Error_Msg like '{criteria.ErrorMsg}'"));
              }
              if (!criteria.GroupGuid.IsNullOrEmpty())
                  q.Where(p => p.GroupGuid == criteria.GroupGuid);

              var list = q.OrderBy(criteria.OrderInfoList).ToList(criteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());
              return list;
  *//*            var result = list.Select(item => new InfNcDataLogSO
              {
                  InfType = item.InfType
                  // 设置其他必要字段
              }).ToList<InfNcDataLogSO>(); // 先转成List，再转成EntityList

              return result;*//*
          }*/

        public virtual EntityList<InfNcDataLogSO> CriteriaInfNcDataLogSO(InfNcDataLogSOCriteria criteria)
        {
            // 使用视图进行查询
            var q = DB.Query<InfNcDataLogSO>("log");

            // 基础条件过滤
            if (criteria.InfType != null)
                q.Where(p => p.InfType == criteria.InfType);

            if (!criteria.InfCode.IsNullOrEmpty())
                q.Where(p => p.InfCode.Contains(criteria.InfCode));

            if (!criteria.OperationType.IsNullOrEmpty())
                q.Where(p => p.OperationType.Contains(criteria.OperationType));

            if (criteria.CallResult != null)
                q.Where(p => p.CallResult == criteria.CallResult);

            // 工单查询 - 现在可以直接查询，因为视图中有工单字段
            if (!criteria.WO.IsNullOrEmpty())
            {
                if (criteria.WO.Contains('%'))
                    criteria.WO = $"%{criteria.WO}%";
                q.Where(p => p.WO.Contains(criteria.WO));
            }

            // JSON数据内容查询
            if (!criteria.DataJsons.IsNullOrEmpty())
            {
                if (criteria.DataJsons.Contains('%'))
                    criteria.DataJsons = $"%{criteria.DataJsons}%";
                q.Where(p => p.DataJsons.Contains(criteria.DataJsons));
            }

            // 错误信息查询
            if (!criteria.ErrorMsg.IsNullOrEmpty())
            {
                if (criteria.ErrorMsg.Contains('%'))
                    criteria.ErrorMsg = $"%{criteria.ErrorMsg}%";
                q.Where(p => p.ErrorMsg.Contains(criteria.ErrorMsg));
            }

            // 分组GUID查询
            if (!criteria.GroupGuid.IsNullOrEmpty())
                q.Where(p => p.GroupGuid == criteria.GroupGuid);

            // 执行查询
            var list = q.OrderBy(criteria.OrderInfoList).ToList(criteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());

            return list;
        }

        public virtual InfNcDataLog SaveInfNcDataLog(string systemCode
                                                    , string infCode
                                                    , string operationType
                                                    , string dataJsons
                                                    , DateTime beginDate
                                                    , InfType? infType
                                                    , CallDirection callDirection
                                                    , CallResult callResult
                                                    ,string groupGuid
                                                    , string errorMsg = null
                                                    , string remark = null)
        {
            InfNcDataLog entity = new InfNcDataLog();

            entity.SystemCode = systemCode;
            entity.InfCode = infCode;
            entity.OperationType = operationType;
            entity.DataJsons = dataJsons;
            entity.BeginDate = beginDate;
            entity.GroupGuid = groupGuid;
            entity.InfType = infType;
            entity.CallDirection = callDirection;
            entity.CallResult = callResult;
            entity.Remark = remark;
            entity.ErrorMsg = errorMsg;
            RF.Save(entity);
            return entity;
        }
    }
}
