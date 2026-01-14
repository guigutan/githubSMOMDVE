using SIE.MES.TaskManagement.SuspectProductLabels.ViewModels;
using SIE.MetaModel.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.Web.MES.TaskManagement.SuspectProductLabels
{
    internal class ScrapDetailViewConfig:WebViewConfig<ScrapDetailViewModel>
    {
        protected override void ConfigListView()
        {
            View.DisableEditing();
            View.UseCommands(WebCommandNames.ExportXls, WebCommandNames.ExportXlsAll);
            using (View.OrderProperties())
            {
                View.Property(p => p.BatchNo).ShowInList(150);
                View.Property(p => p.ItemCode).ShowInList(150);
                View.Property(p => p.ProductName).ShowInList(150);
                View.Property(p => p.Mtart).ShowInList(150);
                View.Property(p => p.ItemType).ShowInList(150);
                View.Property(p => p.UnitCode).ShowInList(150);
                View.Property(p => p.MrpController).ShowInList(150);
                View.Property(p => p.LineTypeCode).ShowInList(150);
                View.Property(p => p.LineTypeName).ShowInList(150);
                View.Property(p => p.ProcessCode).ShowInList(150);
                View.Property(p => p.ProcessName).ShowInList(150);
                View.Property(p => p.ClassType).ShowInList(150);
                View.Property(p => p.BadCode).ShowInList(150);
                View.Property(p => p.BadName).ShowInList(150);
                View.Property(p => p.ScrapNum).ShowInList(150);
                View.Property(p => p.HandleName).ShowInList(150);
                View.Property(p => p.HandleDate).ShowInList(150);
                View.Property(p => p.CreateName).ShowInList(150);
                View.Property(p => p.ScrapDate).ShowInList(150).HasLabel("创建时间");
            }
        }
    }
}
