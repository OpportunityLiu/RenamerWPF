using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using RenamerWpf.Properties;

namespace RenamerWpf
{
    /// <summary>
    /// 主窗口。
    /// </summary>
    public sealed partial class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// 生成 <c>RenamerWpf.MainWindow</c> 类的新实例。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = ((AssemblyTitleAttribute)App.ResourceAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title;
            listView.ItemsSource = files;
            listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("FullName", System.ComponentModel.ListSortDirection.Ascending));
            files.CollectionChanged += files_CollectionChanged;
        }

        /// <summary>
        /// 文件列表。
        /// </summary>
        private FileSet files = new FileSet();

        private void listView_Drop(object sender, DragEventArgs e)
        {
            if(isInOperation())
                return;
            setProgressState(BlurProgressState.Indeterminate);
            listView.Cursor = Cursors.Wait;
            var findText = textboxFind.Text;
            var toText = textboxTo.Text;
            Task.Run(() =>
            {
                try
                {
                    foreach(var item in e.Data.GetData(DataFormats.FileDrop) as string[])
                    {
                        if(File.Exists(item))
                        {
                            files.Add(new FileInfo(item), Dispatcher, findText, toText);
                        }
                        else if(Directory.Exists(item))
                        {
                            files.Add(new DirectoryInfo(item), Dispatcher, findText, toText);
                        }
                    }
                }
                catch(System.Runtime.InteropServices.COMException)
                {
                    //路径过长
                }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    setProgressState(BlurProgressState.None);
                    listView.Cursor = null;
                }));
            });
        }

        /// <summary>
        /// 设置进度条和任务栏进度条的状态。
        /// </summary>
        /// <param name="state">要设置的状态。</param>
        private void setProgressState(BlurProgressState state)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            blurProgressBar.ProgressState = state;
            switch(state)
            {
                case BlurProgressState.None:
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    break;
                case BlurProgressState.Indeterminate:
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                    break;
                case BlurProgressState.Normal:
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    break;
                default:
                    break;
            }
        }

        private void listView_DragOver(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        #region 全选

        private void checkboxSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
        }

        private void checkboxSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if(listView.SelectedItems.Count == files.Count)
                listView.UnselectAll();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(listView.SelectedItems.Count != listView.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        #endregion

        private void files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(listView.SelectedItems.Count != listView.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        private void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            if(files.Count == 0 || isInOperation())
                return;
            regexHandle();
            setProgressState(BlurProgressState.Normal);
            TaskbarItemInfo.ProgressValue = 0;
            Task.Run(() =>
            {
                while(!regexRefresh.Wait(100))
                    Dispatcher.BeginInvoke(new Action(() => TaskbarItemInfo.ProgressValue += (1 - TaskbarItemInfo.ProgressValue) / 50));
                Action addProgress = null;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var progressAdd = (1 - TaskbarItemInfo.ProgressValue) / 2 / files.Count;
                    addProgress = new Action(() => Dispatcher.BeginInvoke(new Action(() => TaskbarItemInfo.ProgressValue += progressAdd)));
                })).Wait();
                foreach(var item in files)
                {
                    try
                    {
                        item.RenameToTempFileName();
                    }
                    catch(InvalidOperationException)
                    {
                        //跳过错误状态的 item
                    }
                    addProgress();
                }
                foreach(var item in files)
                {
                    try
                    {
                        item.RenameToNewFileName();
                    }
                    catch(InvalidOperationException)
                    {
                        //跳过错误状态的 item
                    }
                    addProgress();
                }
                Dispatcher.BeginInvoke(new Action(() => setProgressState(BlurProgressState.None)));
            });
        }

        private void menuitemDelete_Click(object sender, RoutedEventArgs e)
        {
            if(isInOperation())
                return;
            if(listView.SelectedItems.Count == files.Count)
                files.Clear();
            else
            {
                object sel;
                while((sel = listView.SelectedItem) != null)
                    files.Remove((FileData)sel);
            }
        }

        private Task regexRefresh = Task.Run(() =>
        {
        });
        private CancellationTokenSource regexRefreshTokenSource = new CancellationTokenSource();

        private void textboxTextChanged(object sender, TextChangedEventArgs e)
        {
            regexHandle();
        }

        /// <summary>
        /// 停止当前的正则匹配并新建正则匹配。
        /// </summary>
        private void regexHandle()
        {
            //终止当前的匹配操作并释放资源
            regexRefreshTokenSource.Cancel();
            regexRefreshTokenSource.Dispose();
            //新的匹配操作
            regexRefreshTokenSource = new CancellationTokenSource();
            regexRefresh = Task.Run(() =>
            {
                string find = "", to = "";
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    find = textboxFind.Text;
                    to = textboxTo.Text;
                })).Wait();
                foreach(var item in files)
                {
                    if(regexRefreshTokenSource.Token.IsCancellationRequested)
                        return;
                    try
                    {
                        item.Replace(find, to);
                    }
                    catch(InvalidOperationException)
                    {
                        //忽略不能进行替换的项（由于 State 错误）
                    }
                }
            }, regexRefreshTokenSource.Token);
        }

        private DispatcherOperation showMessageBox;

        /// <summary>
        /// 检测当前是否正在执行操作，并发出提示。
        /// </summary>
        /// <param name="showWarning">是否发出提示。</param>
        /// <returns>当前是否正在执行操作。</returns>
        private bool isInOperation(bool showWarning = true)
        {
            if(TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.None)
            {
                if(showWarning && (showMessageBox == null || showMessageBox.Status == DispatcherOperationStatus.Completed))
                    showMessageBox = Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(RenamerWpf.Properties.Resources.HintWait, Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)));
                return true;
            }
            else
                return false;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            if(isInOperation())
                return;
            files.Clear();
        }

        private void DeleteListViewItem_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            menuitemDelete_Click(sender, e);
        }

        private void DeleteListViewItem_CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #region IDisposable 成员

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            regexRefreshTokenSource.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 提供将 <c>System.Int32</c> 转换为 <c>System.Object</c> 的转换器。
    /// </summary>
    public class Int32ToObjectConverter : IValueConverter
    {
        #region IValueConverter 成员

        /// <summary>
        /// 转换值。
        /// </summary>
        /// <param name="value">绑定源生成的值。</param>
        /// <param name="targetType">绑定目标属性的类型。</param>
        /// <param name="parameter">长度为 3 的 <c>System.Array</c>，表示大于 <c>0</c>，等于 <c>0</c>，小于 <c>0</c> 时的返回值。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns> 转换后的值。如果该方法返回 <c>null</c>，则使用有效的 <c>null</c> 值。</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var returns = (Array)parameter;
                if(returns.Length != 3)
                    throw new ArgumentException("必须使用长度为 3 的一维数组。", "parameter");
                var val = (Int32)value;
                if(val > 0)
                    return returns.GetValue(0);
                else if(val == 0)
                    return returns.GetValue(1);
                else
                    return returns.GetValue(2);
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }

        /// <summary>
        /// 转换值，未实现此功能。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
