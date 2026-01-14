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
