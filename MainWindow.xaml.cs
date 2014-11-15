using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

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
            files = new FileSet();
            gridview = listView.View as GridView;
            listView.View = null;
            listView.ItemsSource = files;
            listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Path", System.ComponentModel.ListSortDirection.Ascending));
            files.CollectionChanged += files_CollectionChanged;
        }

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
                            this.Dispatcher.BeginInvoke(new Action(delegate
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
            });
        }

        private void listView_DragOver(object sender, DragEventArgs e)
        {
            if(!e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.None;
            else
                e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        #region 全选

        private void checkboxSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
        }

        private void checkboxSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if(listView.SelectedItems.Count == listView.Items.Count)
                listView.UnselectAll();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sdr = sender as ListView;
            if(sdr.SelectedItems.Count != sdr.Items.Count)
                checkboxSelectAll.IsChecked = false;
        }

        #endregion

        private void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            foreach(var item in files)
            {
                if(item.State == FileData.FileState.Prepared)
                    item.RenameToTempFileName();
            }
            foreach(var item in files)
            {
                if(item.State == FileData.FileState.Renaming)
                    item.RenameToNewFileName();
            }
        }

        private void menuitemDelete_Click(object sender, RoutedEventArgs e)
        {
            for(; listView.SelectedItem != null; )
            {
                files.Remove((FileData)listView.SelectedItem);
            }
        }

        private void textboxTextChanged(object sender, TextChangedEventArgs e)
        {
            foreach(var item in files)
            {
                item.Replace(textboxFind.Text, textboxTo.Text);
            }
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
