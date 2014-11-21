using System;
using System.Windows;
using System.Windows.Controls;

namespace RenamerWpf
{
    /// <summary>
    /// 表示用于贴附在控件边缘的进度条。
    /// </summary>
    public partial class BlurProgressBar : UserControl
    {
        /// <summary>
        /// 生成 <c>RenamerWpf.BlurProgressBar</c> 类的新实例。
        /// </summary>
        public BlurProgressBar()
        {
            InitializeComponent();
            VisualStateManager.GoToState(this, this.ProgressState.ToString(), true);
        }

        /// <summary>
        /// 标识 <c>RenamerWpf.BlurProgressBar.ProgressState</c> 依赖项属性。
        /// </summary>
        public static readonly DependencyProperty ProgressStateProperty = DependencyProperty.Register("ProgressState", typeof(BlurProgressState), typeof(BlurProgressBar),new FrameworkPropertyMetadata(BlurProgressState.Normal, FrameworkPropertyMetadataOptions.AffectsRender,OnProgressStateChanged));

        private static void OnProgressStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = (BlurProgressBar)d;
            pb.RaiseEvent(new ProgressStateChangedEventArgs((BlurProgressState)e.OldValue, (BlurProgressState)e.NewValue));
            VisualStateManager.GoToState(pb, e.NewValue.ToString(), true);
        }

        /// <summary>
        /// 标识 <c>RenamerWpf.BlurProgressBar.ProgressStateChanged</c> 路由事件。
        /// </summary>
        public static readonly RoutedEvent ProgressStateChangedEvent = EventManager.RegisterRoutedEvent("ProgressStateChanged", RoutingStrategy.Direct, typeof(EventHandler<ProgressStateChangedEventArgs>), typeof(BlurProgressBar));

        /// <summary>
        /// 当 <c>RenamerWpf.BlurProgressBar.ProgressState</c> 更改时发生。
        /// </summary>
        public event EventHandler<ProgressStateChangedEventArgs> ProgressStateChanged
        {
            add
            {
                AddHandler(ProgressStateChangedEvent, value);
            }
            remove
            {
                RemoveHandler(ProgressStateChangedEvent, value);
            }
        }

        /// <summary>
        /// 表示当前 <c>RenamerWpf.BlurProgressBar</c> 的状态。
        /// </summary>
        public BlurProgressState ProgressState
        {
            get
            {
                return (BlurProgressState)GetValue(ProgressStateProperty);
            }
            set
            {
                SetValue(ProgressStateProperty, value);
            }
        }

        /// <summary>
        /// 标识 <c>RenamerWpf.BlurProgressBar.ProgressValue</c> 依赖项属性。
        /// </summary>
        public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register("ProgressValue", typeof(double), typeof(BlurProgressBar), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnProgressValueChanged));

        private static void OnProgressValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var test = (double)e.NewValue;
            if((double)e.NewValue > 1.0)
                d.SetValue(e.Property, 1.0);
            else if(test < 0.0 || double.IsNaN(test))
                d.SetValue(e.Property, 0.0);
            var pb = (BlurProgressBar)d;
            pb.Progress0.Width = new GridLength((double)d.GetValue(ProgressValueProperty), GridUnitType.Star);
            pb.Progress1.Width = new GridLength(1.0-(double)d.GetValue(ProgressValueProperty), GridUnitType.Star);
        }

        /// <summary>
        /// 表示当前的进度值，<c>0~1</c>。
        /// </summary>
        /// <value>当前的进度值，<c>0~1</c>。</value>
        public double ProgressValue
        {
            get
            {
                return (double)GetValue(ProgressValueProperty);
            }
            set
            {
                SetValue(ProgressValueProperty, value);
            }
        }
    }

    /// <summary>
    /// 指定进度指示器的状态。
    /// </summary>
    public enum BlurProgressState
    {
        /// <summary>
        /// 未显示进度指示器。
        /// </summary>
        None,
        /// <summary>
        /// 显示闪烁的进度指示器。
        /// </summary>
        Indeterminate,
        /// <summary>
        /// 显示进度指示器。
        /// </summary>
        Normal
    }

    /// <summary>
    /// 包含 <c>RenamerWpf.BlurProgressBar.ProgressStateChanged</c> 事件关联的状态信息和事件数据。
    /// </summary>
    public class ProgressStateChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// 初始化 <c>RenamerWpf.ProgressStateChangedEventArgs</c> 类的新实例。
        /// </summary>
        /// <param name="oldValue">更改前的属性值。</param>
        /// <param name="newValue">更改后的属性值。</param>
        public ProgressStateChangedEventArgs(BlurProgressState oldValue, BlurProgressState newValue)
        {
            base.RoutedEvent = BlurProgressBar.ProgressStateChangedEvent;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// 更改前的属性值。
        /// </summary>
        public BlurProgressState OldValue
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 更改后的属性值。
        /// </summary>
        public BlurProgressState NewValue
        {
            get;
            private set;
        }
    }
}
