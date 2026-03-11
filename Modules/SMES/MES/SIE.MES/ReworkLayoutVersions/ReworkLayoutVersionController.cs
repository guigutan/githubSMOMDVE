using SIE.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.ReworkLayoutVersions
{
    public class ReworkLayoutVersionController : DomainController
    {

        #region 返工信息

        /// <summary>
        /// 根据Id获取返工信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public virtual EntityList<ReworkInfoRecord> GetReworkInfoRecordsByIds(List<double> ids)
        {
            var list = ids.SplitContains(temp =>
            {
                return Query<ReworkInfoRecord>().Where(p => temp.Contains(p.Id)).ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            });
            return list;
        }

        /// <summary>
        /// 根据标签号获取返工信息的标签信息
        /// </summary>
        /// <param name="sn"></param>
        /// <returns></returns>
        public virtual ReworkInfoRecordDtl GetReworkInfoRecordDtl(string sn)
        {
            var first = Query<ReworkInfoRecordDtl>().Where(p => p.WipBatch.BatchNo == sn).FirstOrDefault(new EagerLoadOptions().LoadWithViewProperty());
            return first;
        }

        /// <summary>
        /// 根据标签号获取返工信息的标签信息
        /// </summary>
        /// <param name="sn"></param>
        /// <returns></returns>
        public virtual EntityList<ReworkInfoRecordDtl> GetReworkInfoRecordDtls(string sn)
        {
            var list = Query<ReworkInfoRecordDtl>().Where(p => p.WipBatch.BatchNo == sn).ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            return list;
        }

        #endregion

        #region 返工工艺路线版

        /// <summary>
        /// 根据物料编码获取返工工艺路线版本
        /// </summary>
        /// <param name="itemCodes"></param>
        /// <returns></returns>
        public virtual EntityList<ReworkLayoutVersion>  GetReworkLayoutVersionsByItemCodes(List<string> itemCodes)
        {
            var list = itemCodes.SplitContains(codes =>
            {
                return Query<ReworkLayoutVersion>().Where(p => codes.Contains(p.Item.Code)).ToList(null, new EagerLoadOptions().LoadWithViewProperty());
            });
            return list;
        }

        #endregion
    }
}
