using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RenamerWpf.Properties;

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
        /// <exception cref="System.ArgumentNullException">当参数为空时引发。</exception>
        /// <exception cref="System.IO.PathTooLongException">读取 <paramref name="fileInfo"/> 信息时发生。</exception>
        public FileData(FileInfo fileInfo, string pattern, string replacement)
        {
            if(fileInfo == null)
                throw new ArgumentNullException("fileInfo");
            try
            {
                this.fullName = fileInfo.FullName;
                this.directory = fileInfo.DirectoryName + Path.DirectorySeparatorChar;
                this.oldName = fileInfo.Name;
            }
            catch(PathTooLongException)
            {
                throw;
            }
            if(this.directory.Length > 248)
                throw new PathTooLongException("路径过长。");
            this.maxFileNameLength = 260 - this.directory.Length;
            this.Replace(pattern, replacement);
            this.hashCode = this.fullName.GetHashCode();
        }

        /// <summary>
        /// 将对应的文件重命名为 <c>RenamerWpf.FileData.NewName</c>。
        /// </summary>
        /// <exception cref="InvalidOperationException">没有有效的 <c>RenamerWpf.FileData.NewName</c>。</exception>
        public void Rename()
        {
            if(this.State != FileState.Prepared)
                throw new InvalidOperationException("没有有效的 RenamerWpf.FileData.NewName");
            try
            {
                try
                {
                    File.Move(this.FullName, this.directory + this.NewName);
                    this.State = FileState.Renamed;
                }
                catch(Exception ex)
                {
                    throw new IOException("重命名时发生错误", ex);
                }
            }
            catch(IOException ex)
            {
                this.State = FileState.Error;
                this.NewName = Resources.ErrorRename;
                this.RenameErrorInfo = Resources.ErrorInfoRename.Replace("{0}", ex.InnerException.Message);
            }
        }

        private static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// 对 <paramref name="fileName"/> 进行判断并试图格式化。
        /// </summary>
        /// <param name="fileName">要进行格式化的文件名字符串。</param>
        /// <returns>格式化后的 <paramref name="fileName"/>。</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileName"/> 为 <c>null</c>。
        /// </exception>
        /// <exception cref="InvalidNewNameException"><paramref name="fileName"/> 为空字符串或过长或含有非法的字符。</exception>
        private string transformToValidFileName(String fileName)
        {
            if(fileName == null)
                throw new ArgumentNullException("fileName");
            fileName = fileName.TrimStart(' ').TrimEnd(' ', '.');
            if(fileName.Length > this.maxFileNameLength)
                throw new InvalidNewNameException(fileName, InvalidNewNameException.ExceptionReason.TooLong);
            if(string.IsNullOrEmpty(fileName))
                throw new InvalidNewNameException(fileName, InvalidNewNameException.ExceptionReason.Empty);
            if(!fileName.Contains(InvalidFileNameChars))
                return fileName;
            throw new InvalidNewNameException(fileName, InvalidNewNameException.ExceptionReason.InvalidChar);
        }

        /// <summary>
        /// 通过正则匹配对 <c>RenamerWpf.FileData.OldName</c> 进行处理，
        /// 更改 <c>RenamerWpf.FileData.NewName</c> 和 <c>RenamerWpf.FileData.State</c> 的值。
        /// </summary>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        /// <exception cref="InvalidOperationException">
        /// <c>RenamerWpf.FileData.State</c> 错误。
        /// </exception>
        public void Replace(string pattern, string replacement)
        {
            if(this.State != FileState.Loaded && this.State != FileState.Prepared)
                throw new InvalidOperationException("FileData.State 错误，必须为 FileData.FileState.Loaded 或 FileData.FileState.Prepared。");
            string tempName = "";
            try
            {
                tempName = Regex.Replace(this.OldName, pattern, replacement, RegexOptions.None);
            }
            catch(ArgumentException)
            {
                this.State = FileState.Loaded;
                this.NewName = Resources.ErrorRegexPattern;
                return;
            }
            catch(RegexMatchTimeoutException)
            {
                this.State = FileState.Loaded;
                this.NewName = Resources.ErrorRegexTimeOut;
                return;
            }
            try
            {
                tempName = transformToValidFileName(tempName);
            }
            catch(InvalidNewNameException ex)
            {
                switch(ex.Reason)
                {
                    case InvalidNewNameException.ExceptionReason.Empty:
                        this.NewName = Resources.ErrorEmptyFileName;
                        break;
                    case InvalidNewNameException.ExceptionReason.TooLong:
                    case InvalidNewNameException.ExceptionReason.InvalidChar:
                        this.NewName = ex.NewName;
                        break;
                    default:
                        break;
                }
                this.State = FileState.Loaded;
                return;
            }
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

        #region 属性与字段

        private readonly int maxFileNameLength;

        /// <summary>
        /// 不包含文件名的路径（包含末尾的分隔符）。
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

        private readonly string oldName;

        /// <summary>
        /// 原文件名。
        /// </summary>
        public string OldName
        {
            get
            {
                return this.oldName;
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

        /// <summary>
        /// 文件的图标。
        /// </summary>
        public ImageSource FileIcon
        {
            get
            {
                return fileIconGetter.GetFileIcon(this.fullName);
            }
        }

        #endregion

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
                /// <summary>
                /// 表示文件信息的结构体。
                /// </summary>
                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                public struct FileInfo
                {
                    /// <summary>
                    /// 文件的图标句柄。
                    /// </summary>
                    public IntPtr IconPtr;
                    /// <summary>
                    /// 图标的系统索引号。
                    /// </summary>
                    public int IconIndex;
                    /// <summary>
                    /// 文件的属性值。
                    /// </summary>
                    public uint Attributes;
                    /// <summary>
                    /// 文件的显示名。
                    /// </summary>
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                    public string DisplayName;
                    /// <summary>
                    /// 文件的类型名。
                    /// </summary>
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                    public string TypeName;
                }

                /// <summary>
                /// 返回文件的信息。
                /// </summary>
                /// <param name="path">文件路径，如果为<c>""</c>，返回文件夹的。</param>
                /// <param name="attributes">文件属性，没有设置相应的 <paramref name="flags"/> 时忽略此项。</param>
                /// <param name="fileInfo">用于接收文件信息的 <c>FileInfo</c> 结构体。</param>
                /// <param name="sizeFileInfo">结构体大小。</param>
                /// <param name="flags">枚举类型。</param>
                /// <returns><c>0</c>，表示失败。</returns>
                [DllImport("shell32.dll", EntryPoint = "SHGetFileInfo", CharSet = CharSet.Unicode)]
                public static extern IntPtr GetFileInfo(string path, uint attributes, ref FileInfo fileInfo, uint sizeFileInfo, uint flags);

                /// <summary>
                /// 清理图标并释放内存。
                /// </summary>
                /// <param name="iconPtr">图标句柄。</param>
                /// <returns>
                /// <c>true</c> 表示成功，<c>false</c> 表示失败。
                /// </returns>
                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool DestroyIcon(IntPtr iconPtr);

                /// <summary>
                /// 用于 <c>GetFileInfo</c> 的标识。
                /// </summary>
                public enum Flags
                {
                    /// <summary>
                    /// get icon
                    /// </summary>
                    Icon = 0x000000100,
                    /// <summary>
                    /// get display name
                    /// </summary>
                    DisplayName = 0x000000200,
                    /// <summary>
                    /// get type name
                    /// </summary>
                    TypeName = 0x000000400,
                    /// <summary>
                    /// get attributes
                    /// </summary>
                    Attributes = 0x000000800,
                    /// <summary>
                    /// get icon location
                    /// </summary>
                    IconLocation = 0x000001000,
                    /// <summary>
                    /// return exe type
                    /// </summary>
                    ExeType = 0x000002000,
                    /// <summary>
                    /// get system icon index
                    /// </summary>
                    SystemIconIndex = 0x000004000,
                    /// <summary>
                    /// put a link overlay on icon
                    /// </summary>
                    LinkOverlay = 0x000008000,
                    /// <summary>
                    /// show icon in selected state
                    /// </summary>
                    Selected = 0x000010000,
                    /// <summary>
                    /// get only specified attributes
                    /// </summary>
                    SpecifiedAttributes = 0x000020000,
                    /// <summary>
                    /// get large icon
                    /// </summary>
                    LargeIcon = 0x000000000,
                    /// <summary>
                    /// get small icon
                    /// </summary>
                    SmallIcon = 0x000000001,
                    /// <summary>
                    /// get open icon
                    /// </summary>
                    OpenIcon = 0x000000002,
                    /// <summary>
                    /// get shell size icon
                    /// </summary>
                    ShellSizeIcon = 0x000000004,
                    /// <summary>
                    /// Indicates that the function should not attempt to access the file specified by pszPath. 
                    /// Rather, it should act as if the file specified by pszPath exists with the file attributes passed in dwFileAttributes. 
                    /// This flag cannot be combined with the <c>Attributes</c> or <c>ExeType</c> flags. 
                    /// </summary>
                    UseFileAttributes = 0x000000010
                }
            }

            /// <summary>
            /// 正在进行操作。
            /// </summary>
            private static object gettingFileIcon = new object();

            /// <summary>
            /// 获取文件图标。
            /// </summary>
            /// <param name="path">文件全路径。</param>
            /// <returns>图标。</returns>
            /// <exception cref="System.NotImplementedException">获取图标失败。</exception>
            public static ImageSource GetFileIcon(string path)
            {
                lock(gettingFileIcon)
                {
                    var fileInfo = new NativeMethods.FileInfo();
                    var iconIntPtr = NativeMethods.GetFileInfo(path, 0, ref fileInfo, (uint)Marshal.SizeOf(fileInfo), (uint)(NativeMethods.Flags.Icon | NativeMethods.Flags.SmallIcon));
                    if(iconIntPtr.Equals(IntPtr.Zero))
                        throw new NotImplementedException("获取图标失败。");
                    var img = Imaging.CreateBitmapSourceFromHIcon(fileInfo.IconPtr, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()).GetAsFrozen();
                    NativeMethods.DestroyIcon(fileInfo.IconPtr);
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

        private int hashCode;

        #region object 成员

        /// <summary>
        /// 提供基于哈希值的相等比较。
        /// </summary>
        /// <param name="obj">要比较的 <c>system.Object</c>。</param>
        /// <returns>
        /// 如果 <paramref name="obj"/> 为 <c>RenamerWpf.FileData</c> 类型且哈希值相等，返回 <c>true</c>，否则返回 <c>false</c>。
        /// </returns>
        public override bool Equals(object obj)
        {
            try
            {
                return this.hashCode == ((FileData)obj).hashCode;
            }
            catch(InvalidCastException)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取相应 <c>RenamerWpf.FileData</c> 的哈希值。
        /// </summary>
        /// <returns><c>RenamerWpf.FileData</c> 实例的哈希值。</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        #endregion
    }

    /// <summary>
    /// 表示 <c>RenamerWpf.FileData</c> 状态的枚举。
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
        /// 已经重命名。
        /// </summary>
        Renamed,
        /// <summary>
        /// 出现错误。
        /// </summary>
        Error
    }

    /// <summary>
    /// 表示 <c>RenamerWpf.FileData</c> 的无重复项集合。
    /// </summary>
    public class FileSet : ObservableCollection<FileData>
    {
        /// <summary>
        /// 添加单个文件到当前 <c>RenamerWpf.FileSet</c>。
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
                if(!this.Contains(data) && dispatcher != null)
                    dispatcher.BeginInvoke(new Action(() => this.Add(data))).Wait();
            }
            catch(PathTooLongException)
            {
                //TODO: 给出提示
            }
        }

        /// <summary>
        /// 添加目录内的文件到当前 <c>RenamerWpf.FileSet</c>。
        /// </summary>
        /// <param name="item">要添加的目录。</param>
        /// <param name="dispatcher">用于更新数据的队列。</param>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public void Add(DirectoryInfo item, Dispatcher dispatcher, string pattern, string replacement)
        {
            Action<FileInfo> fileHandler = data => this.Add(data, dispatcher, pattern, replacement);
            Action<DirectoryInfo> directoryHandler = null;
            directoryHandler = data =>
                {
                    try
                    {
                        foreach(var file in data.GetFiles())
                            fileHandler(file);
                        foreach(var directory in data.GetDirectories())
                            directoryHandler(directory);
                    }
                    //放弃读取。
                    catch(UnauthorizedAccessException)
                    {
                    }
                    catch(PathTooLongException)
                    {
                    }
                    catch(System.Security.SecurityException)
                    {
                    }
                    catch(DirectoryNotFoundException)
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

        #region string

        /// <summary>
        /// 返回一个值，该值指示指定的 <c>System.Char</c> 对象是否出现在此字符串中。
        /// </summary>
        /// <param name="item">从此字符串中搜寻。</param>
        /// <param name="value">要搜寻的字符。</param>
        /// <returns>
        /// 如果 <paramref name="value"/> 集合中的任意一个字符出现在此字符串中，则为 true；否则为 false。
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="item"/> 为 <c>null</c>，或者 <paramref name="value"/> 为空。
        /// </exception>
        public static bool Contains(this string item,params char[] value)
        {
            if(item == null)
                throw new ArgumentNullException("item");
            if(value == null || value.Length == 0)
                throw new ArgumentNullException("value");
            foreach(var i in item)
            {
                foreach(var v in value)
                {
                    if(i == v)
                        return true;
                }
            }
            return false;
        }

        #endregion 
    }

    /// <summary>
    /// 生成的新文件名不符合规范时发生的异常。
    /// </summary>
    [Serializable]
    public class InvalidNewNameException : Exception
    {
        /// <summary>
        /// 初始化 <c>RenamerWpf.InvalidNewNameException</c> 的新实例。
        /// </summary>
        public InvalidNewNameException()
            : base()
        {
            this.newName = "";
            this.reason = new ExceptionReason();
        }

        /// <summary>
        /// 初始化 <c>RenamerWpf.InvalidNewNameException</c> 的新实例。
        /// </summary>
        /// <param name="message">错误信息。</param>
        public InvalidNewNameException(string message)
            : base(message)
        {
            this.newName = "";
            this.reason = new ExceptionReason();
        }

        /// <summary>
        /// 初始化 <c>RenamerWpf.InvalidNewNameException</c> 的新实例。
        /// </summary>
        /// <param name="message">错误信息。</param>
        /// <param name="inner">导致当前异常的异常。</param>
        public InvalidNewNameException(string message, Exception inner)
            : base(message, inner)
        {
            this.newName = "";
            this.reason = new ExceptionReason();
        }

        /// <summary>
        /// 初始化 <c>RenamerWpf.InvalidNewNameException</c> 的新实例。
        /// </summary>
        /// <param name="newName">无效的新文件名。</param>
        /// <param name="reason">新文件名无效的原因。</param>
        public InvalidNewNameException(string newName, ExceptionReason reason)
            : base()
        {
            this.newName = newName;
            this.reason = reason;
        }

        /// <summary>
        /// 用序列化数据初始化 <c>RenamerWpf.InvalidNewNameException</c> 的新实例。
        /// </summary>
        /// <param name="info"><c>System.Runtime.Serialization.SerializationInfo</c>，它存有有关所引发的异常的序列化对象数据。</param>
        /// <param name="context"><c>System.Runtime.Serialization.StreamingContext</c>，它包含有关源或目标的上下文信息。</param>
        protected InvalidNewNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.newName = info.GetString("newName");
            this.reason = (ExceptionReason)info.GetValue("reason", typeof(ExceptionReason));
        }

        private readonly string newName;

        /// <summary>
        /// 无效的新文件名。
        /// </summary>
        public string NewName
        {
            get
            {
                return this.newName;
            }
        }

        private readonly ExceptionReason reason;

        /// <summary>
        /// 新文件名无效的原因。
        /// </summary>
        public ExceptionReason Reason
        {
            get
            {
                return this.reason;
            }
        }

        /// <summary>
        /// 表示产生 <c>RenamerWpf.InvalidNewNameException</c> 异常的原因。
        /// </summary>
        public enum ExceptionReason
        {
            /// <summary>
            /// 新文件名为空字符串。
            /// </summary>
            Empty,
            /// <summary>
            /// 新文件名过长。
            /// </summary>
            TooLong,
            /// <summary>
            /// 新文件名中包含非法的字符。
            /// </summary>
            InvalidChar
        }

        /// <summary>
        /// 当在派生类中重写时，用关于异常的信息设置 <c>System.Runtime.Serialization.SerializationInfo。</c>
        /// </summary>
        /// <param name="info">
        /// <c>System.Runtime.Serialization.SerializationInfo</c>，它存有有关所引发的异常的序列化对象数据。
        /// </param>
        /// <param name="context">
        /// <c>System.Runtime.Serialization.StreamingContext</c>，它包含有关源或目标的上下文信息。
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="info"/> 参数是空引用（Visual Basic 中为 <c>Nothing</c>）。
        /// </exception>
        protected virtual new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("newName", this.newName);
            info.AddValue("reason", this.reason);
        }
    }
}