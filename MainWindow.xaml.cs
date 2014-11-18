using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Reflection;

namespace RenamerWpf
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            listView.ItemsSource = files;
            listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Path", System.ComponentModel.ListSortDirection.Ascending));
            files.CollectionChanged += files_CollectionChanged;
        }

        /// <summary>
        /// 文件列表。
        /// </summary>
        private FileSet files = new FileSet();

        private void listView_Drop(object sender, DragEventArgs e)
        {
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            blurProgressBar.ProgressState = BlurProgressState.Indeterminate;
            listView.Cursor = Cursors.Wait;
            var fileCountOld = files.Count;
            var findText = textboxFind.Text;
            var toText = textboxTo.Text;
            Task.Run(delegate
            {
                Action<DirectoryInfo> directoryHandler = null;
                Action<FileInfo> fileHandler = null;
                directoryHandler = delegate(DirectoryInfo d)
                    {
                        try
                        {
                            foreach(var item in d.GetFiles())
                                fileHandler(item);
                            foreach(var item in d.GetDirectories())
                                directoryHandler(item);
                        }
                        catch(UnauthorizedAccessException)
                        {
                            //没有读取权限时直接放弃该目录的读取
                        }
                    };
                fileHandler = delegate(FileInfo f)
                    {
                        try
                        {
                            //载入文件
                            var tempFileData = new FileData(f, findText, toText);
                            //Dispatcher.BeginInvoke(new Action(delegate
                            //{
                            files.AddAndCheck(tempFileData, Dispatcher);
                            //}));
                        }
                        catch(PathTooLongException)
                        {
                            //放弃读取
                        }
                    };
                foreach(String item in e.Data.GetData(DataFormats.FileDrop) as String[])
                {
                    if(File.Exists(item))
                    {
                        var file = new FileInfo(item);
                        fileHandler(file);
                    }
                    else if(Directory.Exists(item))
                    {
                        var directory = new DirectoryInfo(item);
                        directoryHandler(directory);
                    }
                }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                    blurProgressBar.ProgressState = BlurProgressState.None;
                    listView.Cursor = null;
                }));
            });
        }

        private void listView_DragOver(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop) && TaskbarItemInfo.ProgressState == System.Windows.Shell.TaskbarItemProgressState.None)
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
            listView.UnselectAll();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(listView.SelectedItems.Count != listView.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        #endregion

        void files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(listView.SelectedItems.Count != listView.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        private void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            blurProgressBar.ProgressState = BlurProgressState.Normal;
            TaskbarItemInfo.ProgressValue = 0;
            while(!regexRefresh.Wait(100))
                TaskbarItemInfo.ProgressValue += (1 - TaskbarItemInfo.ProgressValue) / 50;
            var progressAdd = (1 - TaskbarItemInfo.ProgressValue) / 2 / files.Count;
            Task.Run(delegate
            {
                var addProgress=new Action(delegate
                { 
                    TaskbarItemInfo.ProgressValue += progressAdd;
                });
                foreach(var item in files)
                {
                    try
                    {
                        item.RenameToTempFileName();
                    }
                    catch(InvalidOperationException)
                    {
                    }
                    Dispatcher.BeginInvoke(addProgress);
                }
                foreach(var item in files)
                {
                    try
                    {
                        item.RenameToNewFileName();
                    }
                    catch(InvalidOperationException)
                    {
                    }
                    Dispatcher.BeginInvoke(addProgress);
                }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                    blurProgressBar.ProgressState = BlurProgressState.None;
                }));
            });
        }

        private void menuitemDelete_Click(object sender, RoutedEventArgs e)
        {
            if(checkboxSelectAll.IsChecked == true)
            {
                files.Clear();
            }
            else
            {
                for(; listView.SelectedItem != null; )
                {
                    files.Remove((FileData)listView.SelectedItem);
                }
            }
        }

        private Task regexRefresh=Task.Run(delegate{});
        private CancellationTokenSource regexRefreshTokenSource = new CancellationTokenSource();
        private CancellationToken regexRefreshToken;

        private void textboxTextChanged(object sender, TextChangedEventArgs e)
        {
            regexRefreshTokenSource.Cancel();
            regexRefreshTokenSource = new CancellationTokenSource();
            regexRefreshToken = regexRefreshTokenSource.Token;
            regexRefresh = Task.Run(async delegate()
            {
                string find = "", to = "";
                await Dispatcher.BeginInvoke(new Action(delegate
                {
                    find = textboxFind.Text;
                    to = textboxTo.Text;
                }));
                foreach(var item in files)
                {
                    if(regexRefreshToken.IsCancellationRequested)
                        return;
                    try
                    {
                        item.Replace(find, to);
                    }
                    catch(InvalidOperationException)
                    {
                    }
                }
            }, regexRefreshToken);
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
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
    }

    /// <summary>
    /// 提供将 <c>Int32</c> 转换为 <c>Object</c> 的转换器。
    /// </summary>
    public class Int32ToObjectConverter : IValueConverter
    {
        #region IValueConverter 成员

        /// <summary>
        /// 转换值。
        /// </summary>
        /// <param name="value">绑定源生成的值。</param>
        /// <param name="targetType">绑定目标属性的类型。</param>
        /// <param name="parameter">要使用的转换器参数，数组，表示大于 0，等于 0，小于 0 时的返回值。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns> 转换后的值。 如果该方法返回 null，则使用有效的 null 值。</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var returns = (Array)parameter;
                if(returns.Length != 3)
                    throw new ArgumentException("parameter");
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }   
    
    /// <summary>
    /// 提供将 <c>ProgressState</c> 转换为 <c>Visibility</c> 的转换器。
    /// </summary>
    public class ProgressStateToBooleanConverter : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(((System.Windows.Shell.TaskbarItemProgressState)value)== System.Windows.Shell.TaskbarItemProgressState.None)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
