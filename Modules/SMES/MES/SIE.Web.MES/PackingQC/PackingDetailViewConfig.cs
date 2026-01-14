using SIE.MES.PackingQC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.Web.MES.PackingQC
{
    /// <summary>
    /// 装箱明细确认
    /// </summary>
    public class PackingDetailViewConfig : WebViewConfig<PackingDetail>
    {
        protected override void ConfigListView()
        {
            using (View.OrderProperties())
            {
                View.Property(p => p.WorkOrderNo).ShowInList(width: 200);
                View.Property(p => p.ProductLabel).ShowInList(width:250);
                View.Property(p => p.ReportsType).ShowInList(width: 80);
                View.Property(p => p.LabelType).ShowInList(width: 80);
                View.Property(p => p.PackingNum).ShowInList(width: 80);
                View.Property(p => p.BatchLabel).ShowInList(width: 200);
                View.Property(p => p.TestValue).ShowInList(width: 200);
            }
        }
    }
}
