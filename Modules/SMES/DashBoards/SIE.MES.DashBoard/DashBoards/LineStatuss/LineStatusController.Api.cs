using SIE.Api;
using SIE.Domain;
using SIE.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIE.MES.DashBoard.DashBoards.LineStatuss.DataArrts;
using SIE.MES.DashBoard.DashBoards.LineStatuss.DataEntitys;



namespace SIE.MES.DashBoard.DashBoards.LineStatuss
{
    
    public partial class LineStatusController: DomainController
    {

        /// <summary>
        /// 接口1-获取库存组织列表
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiService("获取库存组织列表")]
        public virtual List<InvOrgInfo> GetInvOrgs()
        {
            List<InvOrgInfo> InvOrgInfoList = new List<InvOrgInfo>();
            var data = Query<Rbac.InvOrgs.InvOrg>().ToList();
            foreach (var item in data)
            {
                InvOrgInfo invOrgInfo = new InvOrgInfo();
                invOrgInfo.InvName = item.Name;
                invOrgInfo.InvCode = item.Code;
                invOrgInfo.InvID = item.Id;
                InvOrgInfoList.Add(invOrgInfo);
            }
            return InvOrgInfoList;
        }


        /// <summary>
        /// 接口2-获取库存组织下的工序列表
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiService("获取库存组织下的工序列表")]
        public virtual List<ProcessInfo> GetProcessDatas(int invOrgId)
        {
            List<ProcessInfo> ProcessInfoList = new List<ProcessInfo>();            
            RT.InvOrg = invOrgId;
            RT.InvOrg = 7;
            //var data = Query<SIE.MES.ProcessProperty.ProcessPty>().ToList();
            var data = Query<ProcessPty>().ToList();
            foreach (var item in data)
            {
                ProcessInfo processInfo   = new  ProcessInfo();
                processInfo.ProcessId = item.ProcessId;              
                processInfo.ProcessCode = item.TechProess.Code;
                processInfo.ProcessName = item.TechProess.Name;
                ProcessInfoList.Add(processInfo);
            }
            return ProcessInfoList;
        }





        /// <summary>
        /// 接口3-获取产线状态列表
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiService("获取产线状态列表")]
        public virtual List<LineStatusInfo> GetLineStatuss(int processId)
        {
            List<LineStatusInfo>  lineStatusInfoList = new List<LineStatusInfo>();





            return lineStatusInfoList;
        }











    }
}
