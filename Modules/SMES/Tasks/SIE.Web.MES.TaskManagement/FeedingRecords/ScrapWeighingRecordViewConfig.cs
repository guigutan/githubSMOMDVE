using DevExpress.Web.Rendering;
using SIE.MES.TaskManagement.FeedingRecords;
using SIE.MetaModel.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.Web.MES.TaskManagement.FeedingRecords
{
    public class ScrapWeighingRecordViewConfig : WebViewConfig<ScrapWeighingRecord>
    {
        protected override void ConfigView()
        {
            View.AssignAuthorize(typeof(ScrapWeighingRecord));
            base.ConfigView();
        }

        protected override void ConfigListView()
        {
            using (View.OrderProperties())
            {
                View.UseCommands(WebCommandNames.ExportXls, WebCommandNames.ExportXlsAll);
                View.Property(p => p.Sn).Show().Readonly();
                View.Property(p => p.Lot).Show().Readonly();
                View.Property(p => p.ItemCode).Show().Readonly();
                View.Property(p => p.ItemName).Show().Readonly();
                View.Property(p => p.ItemLabelState).Show().Readonly();
                View.Property(p => p.RemainingQty).Show().Readonly();
                View.Property(p => p.ActualQty).Show().Readonly();
                View.Property(p => p.DiffQty).Show().Readonly();
            }
        }
    }
}
