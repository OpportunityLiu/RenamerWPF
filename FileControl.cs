﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        /// <param name="path">文件的绝对路径</param>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public FileData(string path, string pattern, string replacement)
        {
            this.path = System.IO.Path.GetDirectoryName(path) + "\\";
            this.OldName = System.IO.Path.GetFileName(path);
            try
            {
                this.FileIcon = fileIconGetter.GetFileIcon(path);
            }
            catch(FileLoadException)
            {
                //返回默认图标
                this.FileIcon = new BitmapImage(new Uri(@"Resources/DefaultFileIcon.png", UriKind.Relative));
            }
            this.Replace(pattern, replacement);
            this.tempFileName = this.OldName + "." + System.IO.Path.GetRandomFileName();
        }

        /// <summary>
        /// 通过正则匹配对 <c>FileData.OldName</c> 进行处理，并更新 <c>FileData.NewName</c> 和 <c>FileData.State</c> 的值。
        /// </summary>
        /// <param name="pattern">要匹配的正则表达式模式。</param>
        /// <param name="replacement">替换字符串。</param>
        public void Replace(string pattern, string replacement)
        {
            try
            {
                var tempName = Regex.Replace(this.OldName, pattern, replacement, RegexOptions.None, TimeSpan.FromMilliseconds(5));
                if(tempName == this.OldName)
                {
                    this.State = FileState.Loaded;
                    this.NewName = Resources.RegexMatchNotFoundError;
                }
                else
                {
                    try
                    {
                        tempName = transformToValidFileName(tempName);
                    }
                    catch(ArgumentNullException)
                    {
                        this.NewName = Resources.EmptyFileNameError;
                        this.State = FileState.Loaded;
                        return;
                    }
                    catch(ArgumentException ex)
                    {
                        this.NewName = ex.Message;
                        this.State = FileState.Loaded;
                        return;
                    }
                    this.NewName = tempName;
                    this.State = FileState.Prepared;
                }
            }
            catch(ArgumentException)
            {
                this.State = FileState.Loaded;
                this.NewName = Resources.RegexPatternError;
            }
            catch(RegexMatchTimeoutException)
            {
                this.State = FileState.Error;
                this.NewName = Resources.RegexTimeOutError;
            }
        }

        /// <summary>
        /// 将对应的文件重命名为 <c>FileData.tempFileName</c>。
        /// </summary>
        public void RenameToTempFileName()
        {
            try
            {
                File.Move(this.Path, this.path + this.tempFileName);
                this.State = FileState.Renaming;
            }
            catch(Exception ex)
            {
                this.State = FileState.Error;
                this.NewName = Resources.RenameError;
                this.RenameErrorInfo = Resources.RenameErrorInfo.Replace("{0}", ex.Message);
            }
        }

        /// <summary>
        /// 将对应的文件重命名为 <c>FileData.NewName</c>，必须先调用 <c>FileData.RenameToTempFileName</c>。
        /// </summary>
        /// <exception cref="InvalidOperationException">没有预先调用 <c>FileData.RenameToTempFileName</c>。</exception>
        public void RenameToNewFileName()
        {
            if(this.State != FileState.Renaming)
                throw new InvalidOperationException("必须先调用 RenameToTempFileName 。");
            try
            {
                File.Move(this.path + this.tempFileName, this.path + this.NewName);
                this.State = FileState.Renamed;
            }
            catch(Exception ex)
            {
                this.State = FileState.Error;
                this.NewName = Resources.RenameError;
                this.RenameErrorInfo = Resources.RenameErrorInfo.Replace("{0}", ex.Message);
            }
        }

        /// <summary>
        /// 临时文件名。
        /// </summary>
        private readonly string tempFileName;

        /// <summary>
        /// 用于删除文件名两边的空格以进行格式化的正则表达式。
        /// </summary>
        private static readonly Regex fileNameFormatter = new Regex(@"^\s*(.+?)[\s\.]*$", RegexOptions.Compiled);

        /// <summary>
        /// 用于测试是否符合文件名要求的正则表达式。
        /// </summary>
        private static readonly Regex fileNameTest = new Regex("^([^\\\\\\s\\*\\?\\\"\\|<>/:](\\x20|[^\\s\\\\/:\\*\\?\\\"<>\\|])*[^\\\\\\s\\*\\?\\\"\\|\\.<>/:]|[^\\\\\\s\\*\\?\\\"\\|\\.<>/:])$", RegexOptions.Compiled);

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
                throw new ArgumentNullException("文件名为空", (Exception)null);
            fileName = fileNameFormatter.Replace(fileName, "$1");
            if(fileName.Length > 255)
                throw new ArgumentException(fileName);
            if(fileNameTest.IsMatch(fileName))
                return fileName;
            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("文件名为空", (Exception)null);
            throw new ArgumentException(fileName);
        }

        /// <summary>
        /// 确定指定的对象是否相等。
        /// </summary>
        /// <param name="obj">要与当前对象进行比较的对象。</param>
        /// <returns>如果指定的对象相等，则为 <c>true</c>；否则为 <c>false</c>。</returns>
        public override bool Equals(object obj)
        {
            try
            {
                var item = (FileData)obj;
                if(this != null && obj != null)
                    return (this.Path == item.Path) && (this.OldName == item.OldName);
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 不包含文件名的路径。
        /// </summary>
        private string path;

        /// <summary>
        /// 文件的完整路径。
        /// </summary>
        public string Path
        {
            get
            {
                //path != Path
                return this.path + this.OldName;
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
                    this.NotifyPropertyChanged();
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
            get;
            private set;
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
                NativeMethods.SHFILEINFO _SHFILEINFO = new NativeMethods.SHFILEINFO();
                IntPtr _IconIntPtr = NativeMethods.SHGetFileInfo(p_Path, 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(NativeMethods.SHGFI.SHGFI_ICON | NativeMethods.SHGFI.SHGFI_LARGEICON | NativeMethods.SHGFI.SHGFI_USEFILEATTRIBUTES));
                if(_IconIntPtr.Equals(IntPtr.Zero))
                    throw new FileLoadException();
                ImageSource img = Imaging.CreateBitmapSourceFromHIcon(_SHFILEINFO.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                NativeMethods.DestroyIcon(_SHFILEINFO.hIcon);
                return img;
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
        /// <param name="propertyName">更改的属性名，无特殊情况应保持参数默认值。</param>
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// 比较两个 <c>FileData</c> 实例是否相等的比较器。
    /// </summary>
    public class FileDataComparer : IEqualityComparer<FileData>
    {
        #region IEqualityComparer<fileData> 成员

        /// <summary>
        /// 确定指定的对象是否相等。
        /// </summary>
        /// <param name="x">要比较的第一个类型为 <c>FileData</c> 的对象。</param>
        /// <param name="y">要比较的第二个类型为 <c>FileData</c> 的对象。</param>
        /// <returns>如果指定的对象相等，则为 <c>true</c>；否则为 <c>false</c>。</returns>
        public bool Equals(FileData x, FileData y)
        {
            if(x != null && y != null)
                return (x.Path == y.Path) && (x.OldName == y.OldName);
            else
                return false;
        }

        /// <summary>
        /// 返回指定对象的哈希代码。
        /// </summary>
        /// <param name="obj"><c>FileData</c>，将为其返回哈希代码。</param>
        /// <returns>指定对象的哈希代码。</returns>
        /// <exception cref="System.ArgumentNullException">obj 的类型为引用类型，obj 为 null。</exception>
        public int GetHashCode(FileData obj)
        {
            if(obj != null)
                return (obj.Path + obj.OldName).GetHashCode();
            else
                throw new ArgumentNullException("obj");
        }

        #endregion
    }


    public class FileSet : ObservableCollection<FileData>
    {
        /// <summary>
        /// 将不重复的对象添加到 <c>FileSet</c> 的结尾处。
        /// </summary>
        /// <param name="item">
        /// 要添加到 <c>FileSet</c> 的末尾处的对象。
        /// 对于引用类型，该值可以为 <c>null</c>。</param>
        public void AddAndCheck(FileData item)
        {
            foreach(var i in this)
            {
                if(i.Equals(item))
                    return;
            }
            base.Add(item);
        }
    }
}