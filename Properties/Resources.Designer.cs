﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.34014
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace RenamerWpf.Properties {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RenamerWpf.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   使用此强类型资源类，为所有资源查找
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 清空(_C) 的本地化字符串。
        /// </summary>
        public static string ButtonClear {
            get {
                return ResourceManager.GetString("ButtonClear", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 重命名(_R) 的本地化字符串。
        /// </summary>
        public static string ButtonRename {
            get {
                return ResourceManager.GetString("ButtonRename", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找 System.Drawing.Bitmap 类型的本地化资源。
        /// </summary>
        public static System.Drawing.Bitmap DefaultFileIcon {
            get {
                object obj = ResourceManager.GetObject("DefaultFileIcon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   查找类似 文件名为空 的本地化字符串。
        /// </summary>
        public static string ErrorEmptyFileName {
            get {
                return ResourceManager.GetString("ErrorEmptyFileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 新文件名有误，
        ///文件名不能为空。 的本地化字符串。
        /// </summary>
        public static string ErrorInfoEmptyFileName {
            get {
                return ResourceManager.GetString("ErrorInfoEmptyFileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 新文件名有误，
        ///文件名不应包含特殊字符，
        ///也不能长于 225 个字符。 的本地化字符串。
        /// </summary>
        public static string ErrorInfoNotAllowedName {
            get {
                return ResourceManager.GetString("ErrorInfoNotAllowedName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 没有找到可以匹配的项目，‎
        ///请尝试更换正则表达式。 的本地化字符串。
        /// </summary>
        public static string ErrorInfoRegexMatchNotFound {
            get {
                return ResourceManager.GetString("ErrorInfoRegexMatchNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 正则表达式有误，
        ///无法进行匹配。 的本地化字符串。
        /// </summary>
        public static string ErrorInfoRegexPattern {
            get {
                return ResourceManager.GetString("ErrorInfoRegexPattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 正则匹配超时，
        ///请尝试更换正则表达式。 的本地化字符串。
        /// </summary>
        public static string ErrorInfoRegexTimeOut {
            get {
                return ResourceManager.GetString("ErrorInfoRegexTimeOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 在重命名操作的过程中出现错误
        ///错误信息：
        ///    {0} 的本地化字符串。
        /// </summary>
        public static string ErrorInfoRename {
            get {
                return ResourceManager.GetString("ErrorInfoRename", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 匹配失败 的本地化字符串。
        /// </summary>
        public static string ErrorRegexMatchNotFound {
            get {
                return ResourceManager.GetString("ErrorRegexMatchNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 表达式有误 的本地化字符串。
        /// </summary>
        public static string ErrorRegexPattern {
            get {
                return ResourceManager.GetString("ErrorRegexPattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 匹配超时 的本地化字符串。
        /// </summary>
        public static string ErrorRegexTimeOut {
            get {
                return ResourceManager.GetString("ErrorRegexTimeOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 重命名错误 的本地化字符串。
        /// </summary>
        public static string ErrorRename {
            get {
                return ResourceManager.GetString("ErrorRename", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 拖动以添加文件... 的本地化字符串。
        /// </summary>
        public static string HintDragToAddFile {
            get {
                return ResourceManager.GetString("HintDragToAddFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 项目总数 的本地化字符串。
        /// </summary>
        public static string HintFileCount {
            get {
                return ResourceManager.GetString("HintFileCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 所选项目数量 的本地化字符串。
        /// </summary>
        public static string HintSelectedFileCount {
            get {
                return ResourceManager.GetString("HintSelectedFileCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 查找... 的本地化字符串。
        /// </summary>
        public static string LableFind {
            get {
                return ResourceManager.GetString("LableFind", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 替换为... 的本地化字符串。
        /// </summary>
        public static string LableTo {
            get {
                return ResourceManager.GetString("LableTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找 System.Drawing.Bitmap 类型的本地化资源。
        /// </summary>
        public static System.Drawing.Bitmap loading {
            get {
                object obj = ResourceManager.GetObject("loading", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   查找类似 删除(_D) 的本地化字符串。
        /// </summary>
        public static string MenuDelete {
            get {
                return ResourceManager.GetString("MenuDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 {0} 个项目 的本地化字符串。
        /// </summary>
        public static string StatusFileCount {
            get {
                return ResourceManager.GetString("StatusFileCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 已选择 {0} 个项目 的本地化字符串。
        /// </summary>
        public static string StatusSelectedFileCount {
            get {
                return ResourceManager.GetString("StatusSelectedFileCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 新文件名 的本地化字符串。
        /// </summary>
        public static string ViewNewFileName {
            get {
                return ResourceManager.GetString("ViewNewFileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 原文件名 的本地化字符串。
        /// </summary>
        public static string ViewOldFileName {
            get {
                return ResourceManager.GetString("ViewOldFileName", resourceCulture);
            }
        }
    }
}
