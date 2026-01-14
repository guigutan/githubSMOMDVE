using SIE.Domain;
using SIE.EventMessages.WebApis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.KZ.Base.Interfaces
{
    public class LogGroupController : DomainController
    {

        public virtual void InfNcDataLogGroupReUpload(List<double> args)
        {
            RT.Service.Resolve<IWebApi>().InfNcDataLogGroupReUpload(args);
        }

        public virtual EntityList<InfNcDataLogGroup> CriteriaInfNcDataLogGroup(InfNcDataLogGroupCriteria criteria)
        {
            var q = DB.Query<InfNcDataLogGroup>("log");
            if (!criteria.BatchNo.IsNullOrEmpty())
                q.Where(p => p.BatchNo.Contains(criteria.BatchNo));
            if (criteria.InfType != null)
                q.Where(p => p.InfType == criteria.InfType);
            if (!criteria.InvOrg.IsNullOrEmpty())
                q.Where(p => p.InvOrg.Contains(criteria.InvOrg));
            if (!criteria.FactoryName.IsNullOrEmpty())
                q.Where(p => p.FactoryName.Contains(criteria.FactoryName));
            if (criteria.CallResult != null)
                q.Where(p => p.CallResult == criteria.CallResult);
            if (criteria.BeginDate.BeginValue != null)
                q.Where(p => p.BeginDate >= criteria.BeginDate.BeginValue.Value);
            if (criteria.BeginDate.EndValue != null)
                q.Where(p => p.BeginDate <= criteria.BeginDate.EndValue.Value);
            if (criteria.SendState != null)
                q.Where(p => p.SendState == criteria.SendState);
            //大文本clob 模糊查询很影响效率，已提醒，但是仍然坚持使用
            if (!criteria.DataJsons.IsNullOrEmpty())
            {
                if (!criteria.DataJsons.Contains('%'))
                    criteria.DataJsons = "%" + criteria.DataJsons + "%";
                //数据库中Clob类型不能 直接用=查询，需要加%模糊查询
                q.Where(p => p.SQL<bool>($"log.Data_Jsons like '{criteria.DataJsons}'"));
            }
            if (!criteria.SuccessJson.IsNullOrEmpty())
            {
                if (!criteria.SuccessJson.Contains('%'))
                    criteria.SuccessJson = $"%{criteria.SuccessJson}%";
                q.Where(p => p.SQL<bool>($"log.Success_Json like '{criteria.SuccessJson}'"));
            }
            if (!criteria.ResponseContent.IsNullOrEmpty())
            {
                if (!criteria.ResponseContent.Contains('%'))
                    criteria.ResponseContent = $"%{criteria.ResponseContent}%";
                q.Where(p => p.SQL<bool>($"log.Response_Content like '{criteria.ResponseContent}'"));
            }
            if (!criteria.ErrorMsg.IsNullOrEmpty())
            {
                if (!criteria.ErrorMsg.Contains('%'))
                    criteria.ErrorMsg = $"%{criteria.ErrorMsg}%";
                q.Where(p => p.SQL<bool>($"log.Error_Msg like '{criteria.ErrorMsg}'"));
            }
            if (!criteria.FactoryErrorMsg.IsNullOrEmpty())
            {
                if (!criteria.FactoryErrorMsg.Contains('%'))
                    criteria.FactoryErrorMsg = $"%{criteria.FactoryErrorMsg}%";
                q.Where(p => p.SQL<bool>($"log.Factory_Error_Msg like '{criteria.FactoryErrorMsg}'"));
            }

            var list = q.OrderBy(criteria.OrderInfoList).ToList(criteria.PagingInfo, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

    }
}
