using DocumentFormat.OpenXml.Wordprocessing;
using SIE.Domain;
using SIE.MES.DesignerAreas;
using SIE.MetaModel.View;
using SIE.Web.MES.DesignerAreas.Commands;
using System;



namespace SIE.Web.MES.DesignerAreas
{
    public class DesignerAreaReourceViewConfig : WebViewConfig<DesignerAreaReource>
    {
        protected override void ConfigListView()
        {

            View.UseDefaultCommands();

            View.UseCommands(typeof(DesignerAreaResourceSelectACommand).FullName);
            using (View.OrderProperties())
            {
                View.Property(p => p.ResourceCode).ShowInList(150);
                View.Property(p => p.ResourceName).ShowInList(150);
            }

            //View.UseCommands(typeof(FeedingAreaResourceSelectCommand).FullName, typeof(FeedingAreaResourceDelCommand).FullName, WebCommandNames.ExportXls, WebCommandNames.ExportXlsAll, typeof(FeedingAreaReourceImportCommand).FullName, "SIE.Web.Common.Import.Commands.DownloadTemplateCommand");
            //using (View.OrderProperties())
            //{
            //    View.Property(p => p.ResourceCode).ShowInList(150);
            //    View.Property(p => p.ResourceName).ShowInList(150);
            //}



        }
        protected override void ConfigImportView()
        {
            using (View.OrderProperties())
            {
                View.PropertyRef(p => p.Resource.Code).ShowInList(150).HasLabel("产线编码");
            }
        }


    }
}
