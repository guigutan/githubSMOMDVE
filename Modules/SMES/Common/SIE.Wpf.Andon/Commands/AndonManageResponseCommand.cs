using DevExpress.Xpf.Editors;
using SIE.Andon.Andons;
using SIE.Andon.Andons.Enum;
using SIE.Domain;
using SIE.Domain.Validation;
using SIE.Reflection;
using SIE.Resources.Employees;
using SIE.Security;
using SIE.Wpf.Command;
using SIE.Wpf.Workbench;
using System;

namespace SIE.Wpf.Andon.Commands
{

    /// <summary>
    /// 安灯事件验收命令
    /// </summary>
    [Command(ImageName = "ArrowRightDropCircleOutline", Label = "响应", ToolTip = "响应", GroupType = CommandGroupType.Edit)]
    public class AndonManageResponseCommand : DetailViewCommand
    {
        /// <summary>
        /// 是否可执行逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        /// <returns>返回是否可执行</returns>
        public override bool CanExecute(DetailLogicalView view)
        {
            var andonManage = view.Current as AndonManage;
            return andonManage != null && andonManage.State == AndonManageState.Standby;
        }

        /// <summary>
        /// 执行具体的逻辑
        /// </summary>
        /// <param name="view">视图对象</param>
        public override void Execute(DetailLogicalView view)
        {
            Employee employee = view.Current as Employee;
            var model = new AndonEmpViewModel();
            var template = new DetailsUITemplate<AndonEmpViewModel>();
            template.ViewGroup = ViewConfig.DetailsView;
            var ui = template.CreateUI();
            var textEdit = ui.MainView.LayoutControl.GetLogicalChild<TextEdit>();
            ui.MainView.Data = model;
            var result = CRT.Workbench.ShowDialog(ui, w =>
             {
                 w.Title = "扫描响应人".L10N();
                 w.Height = 200;
                 w.Width = 500;
                 var dc = (w as DialogContent);
                 dc.Loaded += (s, e) => { WipLayoutHelper.ResizeChildrenStyle(dc); };
                 w.Closing += (s, e) =>
                {
                    if (w.Result == 0)
                    {
                        var AndomData = ui.MainView.Data;
                        var empNo = ((AndonEmpViewModel)AndomData).AndonEmpNo;
                        employee = RT.Service.Resolve<AndonManageController>().EmpId(empNo);
                        if (employee == null)
                        {
                            CRT.MessageService.ShowError("员工号输入错误".L10N());
                            e.Cancel = true;
                        }
                        textEdit?.Focus();
                    }
                };
                 CRT.MainThread.InvokeIfRequired(() =>
                 {
                     textEdit?.Focus();
                 });
             });

            if (employee == null)
            {
                return;
                //throw new ValidationException("员工号输入错误！".L10N());
            }
            #region
            var andonManage = view.Current as AndonManage;

            var andonManageId = andonManage.Id;
            var nowHandler = RT.Identity;
            var oldHandler = andonManage.Handler;
            var reason = "";
            if (oldHandler != null)
            {
                reason = "处理人由" + andonManage.Handler.Name + "变更为" + nowHandler.Name;
            }
            else
            {
                reason = "处理人更新为" + nowHandler.Name;
            }
            RT.Service.Resolve<AndonManageController>().AndonManageResponse(andonManageId, AndonManageOperateType.Response, reason.L10N(), employee.Id);
            ClientRuntime.MessageService.ShowMessage("响应成功".L10N());
            andonManage.State = AndonManageState.Processing;
            #endregion
        }
    }
}
