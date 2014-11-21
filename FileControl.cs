using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RenamerWpf.Properties;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Globalization;

namespace RenamerWpf
{
    /// <summary>
    /// 表示文件信息的类。
    /// </summary>
    public class FileData : INotifyPropertyChanged
    {

        /// <summary>
        /// 通过文件的绝对路径建立文件信息类。
        /// </summary>
        /// <param name="fileInfo">文件的信息</param>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public FileData(FileInfo fileInfo, string pattern, string replacement)
        {
            if(fileInfo == null)
                throw new ArgumentNullException("fileInfo");
            this.fullName = fileInfo.FullName;
            this.directory = fileInfo.DirectoryName + Path.DirectorySeparatorChar;
            this.OldName = fileInfo.Name;
            this.tempFullName = this.fullName + "." + System.IO.Path.GetRandomFileName();
            this.Replace(pattern, replacement);
            this.hashCode = this.fullName.GetHashCode();
            Task.Run(() =>
            {
                this.fileIcon = fileIconGetter.GetFileIcon(this.fullName);
                this.NotifyPropertyChanged("FileIcon");
            });
        }

        /// <summary>
        /// 将对应的文件重命名为 <c>FileData.tempFileName</c>。
        /// </summary>
        public void RenameToTempFileName()
        {
            if(this.State != FileState.Prepared)
                throw new InvalidOperationException("必须先获得 NewName。");
            try
            {
                File.Move(this.FullName, this.tempFullName);
                this.State = FileState.Renaming;
            }
            catch(Exception ex)
            {
                this.State = FileState.Error;
                this.NewName = Resources.ErrorRename;
                this.RenameErrorInfo = Resources.ErrorInfoRename.Replace("{0}", ex.Message);
            }
        }

        /// <summary>
        /// 将对应的文件重命名为 <c>FileData.NewName</c>，必须先调用 <c>FileData.RenameToTempFileName</c>。
        /// </summary>
        /// <exception cref="InvalidOperationException">没有预先调用 <c>FileData.RenameToTempFileName</c>。</exception>
        public void RenameToNewFileName()
        {
            if(this.State != FileState.Renaming)
                throw new InvalidOperationException("必须先调用 RenameToTempFileName。");
            try
            {
                File.Move(this.tempFullName, this.directory + this.NewName);
                this.State = FileState.Renamed;
            }
            catch(Exception ex)
            {
                this.State = FileState.Error;
                this.NewName = Resources.ErrorRename;
                this.RenameErrorInfo = Resources.ErrorInfoRename.Replace("{0}", ex.Message);
            }
        }

        /// <summary>
        /// 临时文件名，包含路径。
        /// </summary>
        private readonly string tempFullName;

        /// <summary>
        /// 用于测试是否符合文件名要求的正则表达式。
        /// </summary>
        private static readonly Regex fileNameTest = FileData.InitializeFileNameTest();

        private static Regex InitializeFileNameTest()
        {
            var regexStr = "^[^";
            foreach(var item in Path.GetInvalidFileNameChars())
            {
                regexStr += @"\x" + ((byte)item).ToString("X2", CultureInfo.CurrentCulture);
            }
            regexStr += "]+$";
            return new Regex(regexStr, RegexOptions.Compiled);
        }

        /// <summary>
        /// 用于删除文件名两边的空格以进行格式化的正则表达式。
        /// </summary>
        private static readonly Regex fileNameFormatter = new Regex(@"^\s*(.+?)[\s\.]*$", RegexOptions.Compiled);

        /// <summary>
        /// 对 <c>fileName</c> 进行判断并试图格式化。
        /// </summary>
        /// <param name="fileName">要进行格式化的文件名字符串。</param>
        /// <returns>格式化后的 <paramref name="fileName"/>。</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileName"/> 为 <c>null</c> 或格式化后的 <paramref name="fileName"/> 为空字符串。
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> 过长或含有非法的字符。</exception>
        private static string transformToValidFileName(String fileName)
        {
            if(string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(Resources.ErrorEmptyFileName, (Exception)null);
            fileName = fileNameFormatter.Replace(fileName, "$1");
            if(fileName.Length > 255)
                throw new ArgumentException(fileName);
            if(fileNameTest.IsMatch(fileName))
                return fileName;
            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(Resources.ErrorEmptyFileName, (Exception)null);
            throw new ArgumentException(fileName);
        }

        private int hashCode;

        /// <summary>
        /// 提供基于哈希值的相等比较。
        /// </summary>
        /// <param name="obj">要比较的 <c>object</c>。</param>
        /// <returns>如果 <paramref name="obj"/> 为 <c>FileData</c> 类型且哈希值相等，返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public override bool Equals(object obj)
        {
            try
            {
                return this == (FileData)obj;
            }
            catch(InvalidCastException)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取相应 <c>FileData</c> 的哈希值。
        /// </summary>
        /// <returns><c>FileData</c> 实例的哈希值。</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        /// <summary>
        /// 不包含文件名的路径。
        /// </summary>
        private readonly string directory;

        private readonly string fullName;

        /// <summary>
        /// 文件的完整路径。
        /// </summary>
        public string FullName
        {
            get
            {
                return this.fullName;
            }
        }

        /// <summary>
        /// 原文件名。
        /// </summary>
        public string OldName
        {
            private set;
            get;
        }

        /// <summary>
        /// 通过正则匹配对 <c>FileData.OldName</c> 进行处理，更改 <c>FileData.NewName</c> 和 <c>FileData.State</c> 的值。
        /// </summary>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        /// <exception cref="InvalidOperationException"><c>FileData.State</c> 错误。</exception>
        public void Replace(string pattern, string replacement)
        {
            if(this.state != FileState.Loaded && this.state != FileState.Prepared)
                throw new InvalidOperationException("FileData.State 错误，必须为 FileData.FileState.Loaded 或 FileData.FileState.Prepared。");
            try
            {
                var tempName = Regex.Replace(this.OldName, pattern, replacement, RegexOptions.None);
                try
                {
                    tempName = transformToValidFileName(tempName);
                    if(tempName == this.OldName)
                    {
                        this.State = FileState.Loaded;
                        this.NewName = Resources.ErrorRegexMatchNotFound;
                    }
                    else
                    {
                        this.NewName = tempName;
                        this.State = FileState.Prepared;
                    }
                }
                catch(ArgumentNullException)
                {
                    this.NewName = Resources.ErrorEmptyFileName;
                    this.State = FileState.Loaded;
                }
                catch(ArgumentException ex)
                {
                    this.NewName = ex.Message;
                    this.State = FileState.Loaded;
                }
            }
            catch(ArgumentException)
            {
                this.State = FileState.Loaded;
                this.NewName = Resources.ErrorRegexPattern;
            }
            catch(RegexMatchTimeoutException)
            {
                this.State = FileState.Error;
                this.NewName = Resources.ErrorRegexTimeOut;
            }
        }

        private string newName;

        /// <summary>
        /// 新文件名。
        /// </summary>
        public string NewName
        {
            private set
            {
                if(value != this.newName)
                {
                    this.newName = value;
                    NotifyPropertyChanged();
                }
            }
            get
            {
                return this.newName;
            }
        }

        private FileState state;

        /// <summary>
        /// 当前状态。
        /// </summary>
        public FileState State
        {
            private set
            {
                if(value != this.state)
                {
                    this.state = value;
                    this.NotifyPropertyChanged();
                }
            }
            get
            {
                return this.state;
            }
        }

        private string renamerErrorInfo;

        /// <summary>
        /// 重命名中出现错误的信息。
        /// </summary>
        public string RenameErrorInfo
        {
            private set
            {
                if(value != this.renamerErrorInfo)
                {
                    this.renamerErrorInfo = value;
                    this.NotifyPropertyChanged();
                }
            }
            get
            {
                return this.renamerErrorInfo;
            }
        }

        private ImageSource fileIcon = null;

        /// <summary>
        /// 文件的图标。
        /// </summary>
        public ImageSource FileIcon
        {
            get
            {
                return this.fileIcon;
            }
        }

        /// <summary>
        /// 提供用于获取文件图标的方法的静态类。
        /// </summary>
        private static class fileIconGetter
        {
            /// <summary>
            /// 用于封装非托管代码的静态类。
            /// </summary>
            private static class NativeMethods
            {
                [StructLayout(LayoutKind.Sequential)]
                public struct SHFILEINFO
                {
                    public IntPtr hIcon;
                    public IntPtr iIcon;
                    public uint dwAttributes;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                    public string szDisplayName;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                    public string szTypeName;
                }

                /// <summary>
                /// 返回系统设置的图标
                /// </summary>
                /// <param name="pszPath">文件路径 如果为""  返回文件夹的</param>
                /// <param name="dwFileAttributes">0</param>
                /// <param name="psfi">结构体</param>
                /// <param name="cbSizeFileInfo">结构体大小</param>
                /// <param name="uFlags">枚举类型</param>
                /// <returns>-1失败</returns>
                [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
                public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

                /// <summary>
                /// The DestroyIcon function destroys an icon and frees any memory the icon occupied.
                /// </summary>
                /// <param name="hIcon">图标句柄</param>
                /// <returns>非零表示成功，零表示失败。
                /// 会设置GetLastError</returns>
                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool DestroyIcon(IntPtr hIcon);

                public enum SHGFI
                {
                    SHGFI_ICON = 0x100,
                    SHGFI_LARGEICON = 0x0,
                    SHGFI_USEFILEATTRIBUTES = 0x10
                }
            }

            private static object gettingFileIcon=new object();

            /// <summary>
            /// 获取文件图标。
            /// </summary>
            /// <param name="p_Path">文件全路径</param>
            /// <returns>图标</returns>
            /// <exception cref="FileLoadException">
            /// 未找到图标。
            /// </exception>
            public static ImageSource GetFileIcon(string p_Path)
            {
                lock(gettingFileIcon)
                {
                    var _SHFILEINFO = new NativeMethods.SHFILEINFO();
                    var _IconIntPtr = NativeMethods.SHGetFileInfo(p_Path, 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(NativeMethods.SHGFI.SHGFI_ICON | NativeMethods.SHGFI.SHGFI_LARGEICON | NativeMethods.SHGFI.SHGFI_USEFILEATTRIBUTES));
                    if(_IconIntPtr.Equals(IntPtr.Zero))
                        return new BitmapImage(new Uri(@"Resources/DefaultFileIcon.png", UriKind.Relative));
                    var img = Imaging.CreateBitmapSourceFromHIcon(_SHFILEINFO.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()).GetAsFrozen();
                    NativeMethods.DestroyIcon(_SHFILEINFO.hIcon);
                    return (ImageSource)img;
                }
            }
        }

        #region INotifyPropertyChanged 成员

        /// <summary>
        /// 在更改属性值时发生。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// 在属性更改时产生相应的事件。
        /// </summary>
        /// <param name="propertyName">更改的属性名，参数默认值表示当前更改的属性。</param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// 表示文件状态的枚举。
    /// </summary>
    public enum FileState
    {
        /// <summary>
        /// 读取文件信息完毕。
        /// </summary>
        Loaded,
        /// <summary>
        /// 已经准备好新文件名。
        /// </summary>
        Prepared,
        /// <summary>
        /// 正在重命名。
        /// </summary>
        Renaming,
        /// <summary>
        /// 已经重命名。
        /// </summary>
        Renamed,
        /// <summary>
        /// 出现错误。
        /// </summary>
        Error
    }

    /// <summary>
    /// 表示 <c>FlieDate</c> 的无重复项集合。
    /// </summary>
    public class FileSet : ObservableCollection<FileData>
    {

        /// <summary>
        /// 将不重复的对象添加到 <c>FileSet</c> 的结尾处。
        /// </summary>
        /// <param name="item">
        /// 要添加到 <c>FileSet</c> 的末尾处的对象。
        /// 对于引用类型，该值可以为 <c>null</c>。
        /// </param>
        /// <param name="dispatcher">用于更新数据的队列。</param>
        private void addAndCheck(FileData item, Dispatcher dispatcher)
        {
            foreach(var i in this)
            {
                if(i.Equals(item))
                    return;
            }
            dispatcher.BeginInvoke(new Action(() => base.Add(item))).Wait();
        }

        /// <summary>
        /// 添加单个文件到当前 <c>FileSet</c>。
        /// </summary>
        /// <param name="item">要添加的文件。</param>
        /// <param name="dispatcher">用于更新数据的队列。</param>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public void Add(FileInfo item, Dispatcher dispatcher, string pattern, string replacement)
        {
            try
            {
                var data = new FileData(item, pattern, replacement);
                this.addAndCheck(data, dispatcher);
            }
            //放弃读取。
            catch(UnauthorizedAccessException)
            {
            }
            catch(PathTooLongException)
            {
            }
        }

        /// <summary>
        /// 添加目录内的文件到当前 <c>FileSet</c>。
        /// </summary>
        /// <param name="item">要添加的目录。</param>
        /// <param name="dispatcher">用于更新数据的队列。</param>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public void Add(DirectoryInfo item, Dispatcher dispatcher, string pattern, string replacement)
        {
            Action<FileData> fileHandler = d => this.addAndCheck(d, dispatcher);
            Action<DirectoryInfo> directoryHandler = null;
            directoryHandler = d =>
                {
                    try
                    {
                        foreach(var file in d.GetFiles())
                            fileHandler(new FileData(file, pattern, replacement));
                        foreach(var directory in d.GetDirectories())
                            directoryHandler(directory);
                    }
                    //放弃读取。
                    catch(UnauthorizedAccessException)
                    {
                    }
                    catch(PathTooLongException)
                    {
                    }
                };
            directoryHandler(item);
        }
    }

    /// <summary>
    /// 表示扩展方法静态类。
    /// </summary>
    public static class ExtendMethods
    {
        #region FileSystemInfo
        /// <summary>
        /// 测试 <paramref name="testParent"/> 是否为 <paramref name="item"/> 的父目录。 
        /// </summary>
        /// <param name="item">待测试的子目录。</param>
        /// <param name="testParent">待测试的父目录。</param>
        /// <returns>
        /// 若 <paramref name="testParent"/> 为 <paramref name="item"/> 的父目录，则返回 <c>true</c>，否则返回 <c>false</c>。
        /// </returns>
        public static bool IsChildOf(this FileSystemInfo item, FileSystemInfo testParent)
        {
            if(item == null)
                throw new ArgumentNullException("item");
            if(testParent == null)
                throw new ArgumentNullException("testParent");
            return item.IsChildOf(testParent.FullName);
        }

        /// <summary>
        /// 测试 <paramref name="testParent"/> 是否为 <paramref name="item"/> 的父目录。 
        /// </summary>
        /// <param name="item">待测试的子目录。</param>
        /// <param name="testParent">待测试的父目录。</param>
        /// <returns>
        /// 若 <paramref name="testParent"/> 为 <paramref name="item"/> 的父目录，则返回 <c>true</c>，否则返回 <c>false</c>。
        /// </returns>
        public static bool IsChildOf(this FileSystemInfo item, string testParent)
        {
            if(item == null)
                throw new ArgumentNullException("item");
            if(testParent == null)
                throw new ArgumentNullException("testParent");
            return item.FullName.StartsWith(testParent + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }
        #endregion
    }
}