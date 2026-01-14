using SIE.Andon.Andons;
using SIE.Andon.Andons.Enum;
using SIE.Wpf.Command;
using SIE.Wpf.Workbench;
using System;

namespace SIE.Wpf.Andon.Commands
{
    /// <summary>
    /// 安灯事件验收命令
    /// </summary>
    [Command(ImageName = "PlaylistCheck", Label = "验收", ToolTip = "验收", GroupType = CommandGroupType.Edit)]
    public class AndonManageAcceptCommand : DetailViewCommand
    {
        /// <summary>
        /// 是否可执行逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        /// <returns>返回是否可执行</returns>
        public override bool CanExecute(DetailLogicalView view)
        {
            var andonManage = view.Current as AndonManage;
            return andonManage != null && andonManage.State == AndonManageState.ToAccepted;
        }

        /// <summary>
        /// 执行具体的逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        public override void Execute(DetailLogicalView view)
        {
            var andonManage = view.Current as AndonManage;

            //弹窗填写实际影响时间：大于0，保留1位小数，默认为【当前时间减去触发时间】
            andonManage.ActualTime = RT.Service.Resolve<AndonManageController>().ComputerActualTime(andonManage.Id);

            var template = new DetailsUITemplate(typeof(AndonManage),
                AndonManageViewConfig.AcceptViewGroup, view.ModuleKey);
            var ui = template.CreateUI();
            ui.MainView.Data = andonManage;

            CRT.Workbench.ShowDialog(ui, w =>
            {
                w.Title = "验收".L10N();
                w.Height = 200;
                w.Width = 500;
                var dc = (w as DialogContent);
                dc.Loaded += (s, e) => { WipLayoutHelper.ResizeChildrenStyle(dc); };
                w.Closing += (s, e) =>
                {
                    if (w.Result == 0)
                    {
                        try
                        {
                            RT.Service.Resolve<AndonManageController>()
                            .AndonManageCheck(andonManage.Id, AndonManageOperateType.Check, "验收成功", andonManage.ActualTime);

                            andonManage.State = SIE.Andon.Andons.Enum.AndonManageState.Closed;
                        }
                        catch (Exception ex)
                        {
                            ClientRuntime.MessageService.ShowException(ex);
                            e.Cancel = true;
                        }
                    }
                };
            });

        }
    }
}
