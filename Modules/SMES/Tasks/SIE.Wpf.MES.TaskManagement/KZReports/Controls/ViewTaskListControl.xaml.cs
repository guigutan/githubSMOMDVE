using DocumentFormat.OpenXml.EMMA;
using SIE.Collections;
using SIE.Domain;
using SIE.Items;
using SIE.MES.TaskManagement.Dispatchs;
using SIE.MES.TaskManagement.ForWinform;
using SIE.MES.TaskManagement.Models;
using SIE.Threading;
using SIE.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SIE.Wpf.MES.TaskManagement.KZReports.Controls
{
    /// <summary>
    /// ViewTaskListControl.xaml 的交互逻辑
    /// </summary>
    public partial class ViewTaskListControl : UserControl
    {
        KZTaskReportViewModelBase model;
        KZReportHelper kZReportHelper;

        public double TaskId { get; set; }
        public double? ResourceId { get; set; }
        public double? ProcessId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ViewTaskListControl()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_model"></param>
        public ViewTaskListControl(KZTaskReportViewModelBase _model)
        {
            InitializeComponent();
            this.model = _model;
            this.DataContext = _model;
            kZReportHelper = model.kZReportHelper;
            this.Loaded += TaskListControl_Loaded;
            this.Unloaded -= TaskListControl_Loaded;
        }

        private void TaskListControl_Loaded(object sender, RoutedEventArgs e)
        {
            model.DispatchTaskList.Clear();
            Task.Run(new Action(() =>
            {
                LoadTaskList();
            }).WithCurrentThreadContext());

        }

        void close()
        {
            var parent = Window.GetWindow(this);
            if (parent != null && parent is Window)
            {
                (parent as Window)?.Close();
            }
        }


        /// <summary>
        /// 获取任务单列表
        /// </summary>
        public virtual void LoadTaskList()
        {            
            TaskQueryInfo info = new TaskQueryInfo()
            {
                EmployeeId = model.ReportEmployee.Id,
                ResourceId = ResourceId,
                TaskType = 1,
                ProcessArray = ProcessId.ToString()
            };
            var status = new List<DispatchTaskStatus>() { DispatchTaskStatus.Dispatched, DispatchTaskStatus.Dispatching, DispatchTaskStatus.ToDispatch, DispatchTaskStatus.Executing, DispatchTaskStatus.Pause/*, DispatchTaskStatus.Finished, DispatchTaskStatus.Closed*/ };
            PagingInfo pagingInfo = new PagingInfo(info.PageNumber ?? 1, info.PageSize ?? int.MaxValue - 1, true);
            var tasks = RT.Service.Resolve<DispatchController>().GetDispatchTasksByEmployee(info, status, pagingInfo, false).OrderBy(p => p.ProductCode).ThenBy(p => p.PlanEndTime).ToList();

            CRT.MainThread.InvokeIfRequired(() =>
            {
                model.DispatchTaskList.Clear();
                model.DispatchTaskList.AddRange(tasks);
                model.DispatchTaskList.MarkSaved();
            });
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (model.IsReportManual)
            {
                var task = this.dataGridRecord.SelectedItem as DispatchTask;
                if (task == null)
                {
                    MessageBox.Show("请选择一行数据".L10N());
                    return;
                }
                TaskId = task.Id;
            }
            else
            {
                var queue = model.DispatchTaskQueueList.OrderBy(p => p.Seq).FirstOrDefault();
                if (queue == null)
                {
                    CRT.MessageService.ShowInstantMessage("生产队列中没有生产任务,请确认".L10N(), "提示", 3);
                    return;
                }
                TaskId = queue.DispatchTaskId;
            }
            model.DispatchTask = RF.GetById<DispatchTask>(TaskId, new EagerLoadOptions().LoadWithViewProperty());
            if (model.DispatchTask != null)
            {
                var parentItem = RT.Service.Resolve<ItemController>().GetParentItemByItemId(model.DispatchTask.ProductId);
                if (parentItem != null)
                    model.DispatchTask.ParShortDescription = parentItem.Bismt;
            }
            close();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            close();
        }


        private void btnAddQueue_Click(object sender, RoutedEventArgs e)
        {
            var task = this.dataGridRecord.SelectedItem as DispatchTask;
            if (task == null)
            {
                CRT.MessageService.ShowInstantMessage("请选择一行数据".L10N(), "提示", 3);
                return;
            }
            model.AddQueueTask(task);
        }

        private void btnTaskQueueList_Click(object sender, RoutedEventArgs e)
        {
            close();
            kZReportHelper.ShowTaskQueueList();
        }
    }
}
