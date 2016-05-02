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

namespace RenamerWpf
{
    /// <summary>
    /// 主窗口。
    /// </summary>
    public sealed partial class MainWindow : IDisposable
    {
        /// <summary>
        /// 生成 <see cref="RenamerWpf.MainWindow"/> 类的新实例。
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = ((AssemblyTitleAttribute)Application.ResourceAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title;
            this.listView.ItemsSource = this.files;
            this.listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("FullName", System.ComponentModel.ListSortDirection.Ascending));
            this.files.CollectionChanged += this.files_CollectionChanged;
        }

        /// <summary>
        /// 文件列表。
        /// </summary>
        private FileSet files = new FileSet();

        private async void listView_Drop(object sender, DragEventArgs e)
        {
            if(this.isInOperation())
                return;
            this.setProgressState(BlurProgressState.Indeterminate);
            this.listView.Cursor = Cursors.Wait;
            var findText = this.textboxFind.Text;
            var toText = this.textboxTo.Text;
            await Task.Run(() =>
            {
                try
                {
                    foreach(var item in e.Data.GetData(DataFormats.FileDrop) as string[])
                    {
                        if(File.Exists(item))
                        {
                            this.files.Add(new FileInfo(item), this.Dispatcher, findText, toText);
                        }
                        else if(Directory.Exists(item))
                        {
                            this.files.Add(new DirectoryInfo(item), this.Dispatcher, findText, toText);
                        }
                    }
                }
                catch(System.Runtime.InteropServices.COMException)
                {
                    //路径过长
                }
            });
            this.setProgressState(BlurProgressState.None);
            this.listView.Cursor = null;
        }

        /// <summary>
        /// 设置进度条和任务栏进度条的状态。
        /// </summary>
        /// <param name="state">要设置的状态。</param>
        private void setProgressState(BlurProgressState state)
        {
            this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            this.blurProgressBar.ProgressState = state;
            switch(state)
            {
            case BlurProgressState.None:
                this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                break;
            case BlurProgressState.Indeterminate:
                this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                break;
            case BlurProgressState.Normal:
                this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
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
            this.listView.SelectAll();
        }

        private void checkboxSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if(this.listView.SelectedItems.Count == this.files.Count)
                this.listView.UnselectAll();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.listView.SelectedItems.Count != this.listView.Items.Count)
                this.checkboxSelectAll.IsChecked = false;
        }

        #endregion

        private void files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(this.listView.SelectedItems.Count != this.listView.Items.Count)
                this.checkboxSelectAll.IsChecked = false;
        }

        private async void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            if(this.files.Count == 0 || this.isInOperation())
                return;
            this.regexHandle();
            this.setProgressState(BlurProgressState.Normal);
            this.TaskbarItemInfo.ProgressValue = 0;
            await Task.Run(() =>
            {
                while(!this.regexRefresh.Wait(100))
                    this.Dispatcher.BeginInvoke(new Action(() => this.TaskbarItemInfo.ProgressValue += (1 - this.TaskbarItemInfo.ProgressValue) / 50));
                Action addProgress = null;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var progressAdd = (1 - this.TaskbarItemInfo.ProgressValue) / this.files.Count;
                    addProgress = () => this.Dispatcher.BeginInvoke(new Action(() => this.TaskbarItemInfo.ProgressValue += progressAdd));
                })).Wait();
                foreach(var item in this.files)
                {
                    try
                    {
                        item.Rename();
                    }
                    catch(InvalidOperationException)
                    {
                        //跳过错误状态的 item
                    }
                    addProgress();
                }
            });
            this.setProgressState(BlurProgressState.None);
        }

        private void menuitemDelete_Click(object sender, RoutedEventArgs e)
        {
            if(this.isInOperation())
                return;
            if(this.listView.SelectedItems.Count == this.files.Count)
                this.files.Clear();
            else
            {
                object sel;
                while((sel = this.listView.SelectedItem) != null)
                    this.files.Remove((FileData)sel);
            }
        }

        private Task regexRefresh = Task.Run(() =>
        {
        });

        private CancellationTokenSource regexRefreshTokenSource = new CancellationTokenSource();

        private void textboxTextChanged(object sender, TextChangedEventArgs e)
        {
            this.regexHandle();
        }

        /// <summary>
        /// 停止当前的正则匹配并新建正则匹配。
        /// </summary>
        private void regexHandle()
        {
            //终止当前的匹配操作并释放资源
            this.regexRefreshTokenSource.Cancel();
            this.regexRefreshTokenSource.Dispose();
            //新的匹配操作
            this.regexRefreshTokenSource = new CancellationTokenSource();
            this.regexRefresh = Task.Run(async () =>
            {
                string find = null, to = null;
                await this.Dispatcher.BeginInvoke(new Action(delegate
                {
                    find = this.textboxFind.Text;
                    to = this.textboxTo.Text;
                }));
                foreach(var item in this.files)
                {
                    if(this.regexRefreshTokenSource.Token.IsCancellationRequested)
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
            }, this.regexRefreshTokenSource.Token);
        }

        private DispatcherOperation showMessageBox;

        /// <summary>
        /// 检测当前是否正在执行操作，并发出提示。
        /// </summary>
        /// <param name="showWarning">是否发出提示。</param>
        /// <returns>当前是否正在执行操作。</returns>
        private bool isInOperation(bool showWarning = true)
        {
            if(this.TaskbarItemInfo.ProgressState == TaskbarItemProgressState.None)
                return false;
            if(showWarning && (this.showMessageBox == null || this.showMessageBox.Status == DispatcherOperationStatus.Completed))
                this.showMessageBox = this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(Properties.Resources.HintWait, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)));
            return true;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            if(!this.isInOperation())
                this.files.Clear();
        }

        private void DeleteListViewItem_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.menuitemDelete_Click(sender, e);
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
            this.regexRefreshTokenSource.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 提供将 <see cref="System.Int32"/> 转换为 <see cref="System.Object"/> 的转换器。
    /// </summary>
    public class Int32ToObjectConverter : IValueConverter
    {
        #region IValueConverter 成员

        /// <summary>
        /// 转换值。
        /// </summary>
        /// <param name="value">绑定源生成的值。</param>
        /// <param name="targetType">绑定目标属性的类型。</param>
        /// <param name="parameter">长度为 3 的 <see cref="System.Array"/>，表示大于 <c>0</c>，等于 <c>0</c>，小于 <c>0</c> 时的返回值。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns> 转换后的值。如果该方法返回 <c>null</c>，则使用有效的 <c>null</c> 值。</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var returns = (Array)parameter;
                if(returns.Length != 3)
                    throw new ArgumentException("必须使用长度为 3 的一维数组。", nameof(parameter));
                var val = (int)value;
                if(val > 0)
                    return returns.GetValue(0);
                if(val == 0)
                    return returns.GetValue(1);
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
        /// <exception cref="NotImplementedException">未实现此功能。</exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
