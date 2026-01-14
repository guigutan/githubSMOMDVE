using SIE.Andon.Andons;
using SIE.Andon.Andons.Enum;
using SIE.Wpf.Command;
using SIE.Wpf.Workbench;
using System;

namespace SIE.Wpf.Andon.Commands
{
    /// <summary>
    /// 安灯事件驳回命令
    /// </summary>
    [Command(ImageName = "CalendarRemove", Label = "驳回", ToolTip = "驳回", GroupType = CommandGroupType.Edit)]
    public class AndonManageRejectCommand : DetailViewCommand
    {
        /// <summary>
        /// 是否可执行逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        /// <returns>返回是否可执行</returns>
        public override bool CanExecute(DetailLogicalView view)
        {
            return view.Current is AndonManage andonManage && andonManage.State == SIE.Andon.Andons.Enum.AndonManageState.ToAccepted;
        }

        /// <summary>
        /// 执行具体的逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        public override void Execute(DetailLogicalView view)
        {
            AndonManageOperateLog operateLog = new AndonManageOperateLog();

            var template = new DetailsUITemplate(typeof(AndonManageOperateLog),
                AndonManageOperateLogViewConfig.RejectViewGroup, view.ModuleKey);
            var ui = template.CreateUI();
            ui.MainView.Data = operateLog;

            CRT.Workbench.ShowDialog(ui, w =>
            {
                w.Title = "驳回".L10N();
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
                            if (operateLog.Remark.IsNullOrEmpty())
                            {
                                ClientRuntime.MessageService.ShowError("【驳回原因】必须输入！".L10N());
                                e.Cancel = true;
                                return;
                            }

                            var andonManage = view.Current as AndonManage;

                            RT.Service.Resolve<AndonManageController>()
                                .AndonManageReject(andonManage.Id, AndonManageOperateType.Reject, operateLog.Remark);

                            andonManage.State = SIE.Andon.Andons.Enum.AndonManageState.Processing;
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
