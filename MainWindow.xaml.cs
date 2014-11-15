using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
            regexRefresh = Task.Run(delegate
            {
            });
            files = new FileSet();
            gridview = listView.View as GridView;
            listView.View = null;
            listView.ItemsSource = files;
            listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Path", System.ComponentModel.ListSortDirection.Ascending));
            files.CollectionChanged += files_CollectionChanged;
            regexRefreshTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 文件列表。
        /// </summary>
        private FileSet files;

        /// <summary>
        /// 用于 <c>ListView.View</c> 的 <c>GridView</c>。
        /// </summary>
        private GridView gridview;

        private void listView_Drop(object sender, DragEventArgs e)
        {
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
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
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                files.AddAndCheck(tempFileData);
                            }));
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
                    listView.Cursor = null;
                }));
            });
        }

        private void listView_DragOver(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop) && listView.Cursor==null)
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
            if(files.Count != 0)
            {
                listView.View = gridview;
                lableListViewHint.Visibility = Visibility.Hidden;
            }
            else
            {
                listView.View = null;
                lableListViewHint.Visibility = Visibility.Visible;
            }
            if(listView.SelectedItems.Count != listView.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        private void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = 0;
            regexRefresh.Wait();
            TaskbarItemInfo.ProgressValue = 0.1;
            var progressAdd = 0.45 / files.Count;
            foreach(var item in files)
            {
                try
                {
                    item.RenameToTempFileName();
                }
                catch(InvalidOperationException)
                {
                }
                TaskbarItemInfo.ProgressValue += progressAdd;
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
                TaskbarItemInfo.ProgressValue += progressAdd;
            }
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
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

        private Task regexRefresh;
        private CancellationTokenSource regexRefreshTokenSource;
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
                    item.Replace(find, to);
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
    /// 提供将 <c>int32</c> 转换为 <c>Visibility</c> 的转换器。
    /// </summary>
    public class Int32ToVisibilityConverter : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if((Int32)value == 0)
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
