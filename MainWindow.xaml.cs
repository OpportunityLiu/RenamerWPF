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
            //files = new HashSet<FileData>(new FileDataComparer());
            files = new FileSet();
            gridview = listView.View as GridView;
            listView.View = null;
            listView.ItemsSource = files;
            listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("OldName", System.ComponentModel.ListSortDirection.Ascending));
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
            Task.Run(delegate
            {
                var fileCountOld = files.Count;
                var directoryHandler = new Action<DirectoryInfo>(delegate(DirectoryInfo f)
                    {
                    });
                var fileHandler = new Action<FileInfo>(delegate(FileInfo f)
                    {
                        this.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            try
                            {
                                //载入文件
                                var tempFileData = new FileData(f.FullName, textboxFind.Text, textboxTo.Text);
                                files.AddAndCheck(tempFileData);

                            }
                            catch(PathTooLongException)
                            {
                                //放弃读取
                            }
                        }));
                    });
                directoryHandler = new Action<DirectoryInfo>(delegate(DirectoryInfo d)
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
                    });
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
                this.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if(files.Count != 0)
                    {
                        listView.View = gridview;
                        lableListViewHint.Visibility = System.Windows.Visibility.Hidden;
                    }
                    if(files.Count != fileCountOld)
                        checkboxSelectAll.IsChecked = false;
                    
                    //listView.Items.Refresh();
                }));
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
            if(files.Count == 0)
            {
                checkboxSelectAll.IsChecked = false;
                listView.View = null;
                lableListViewHint.Visibility = System.Windows.Visibility.Visible;
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
            checkboxSelectAll.IsChecked = false;
            listView.View = null;
            lableListViewHint.Visibility = System.Windows.Visibility.Visible;
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
    /// 提供将 <c>Boolean[]</c> 转换为 <c>Visibility</c> 的转换器。
    /// </summary>
    public class BooleansToVisibilityConverter : IMultiValueConverter
    {
        #region IMultiValueConverter 成员

        /// <summary>
        /// 将源值转换为绑定源的值。 数据绑定引擎在将值从绑定源传播给绑定目标时，调用此方法。
        /// </summary>
        /// <param name="values">
        /// System.Windows.Data.MultiBinding 中源绑定生成的值的数组。 
        /// 值 <c>System.Windows.DependencyProperty.UnsetValue</c> 表示源绑定没有要提供以进行转换的值。
        /// </param>
        /// <param name="targetType">绑定目标属性的类型。</param>
        /// <param name="parameter">要使用的转换器参数。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns>
        /// 转换后的值。 如果该方法返回 <c>null</c>，则使用有效的 <c>null</c> 值。 
        /// <c>System.Windows.DependencyProperty.UnsetValue</c> 的返回值表示转换器没有生成任何值，且绑定将使用 <c>System.Windows.Data.BindingBase.FallbackValue</c>（如果可用），否则将使用默认值。
        /// <c>System.Windows.Data.Binding.DoNothing</c> 的返回值表示绑定不传输值，或不使用 <c>System.Windows.Data.BindingBase.FallbackValue</c> 或默认值。
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if((values).Contains(true))
                return (object)Visibility.Visible;
            else
                return (object)Visibility.Hidden;
        }

        /// <summary>
        /// 将绑定目标值转换为源绑定值。
        /// </summary>
        /// <param name="value">绑定目标生成的值。</param>
        /// <param name="targetTypes">要转换到的类型数组。 数组长度指示为要返回的方法所建议的值的数量与类型。</param>
        /// <param name="parameter">要使用的转换器参数。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns>从目标值转换回源值的值的数组。</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
