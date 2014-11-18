using System;
using System.Collections.Generic;
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
using RenamerWpf;

namespace RenamerWpf
{
    /// <summary>
    /// BlurProgressBar.xaml 的交互逻辑
    /// </summary>
    public partial class BlurProgressBar : UserControl
    {
        public BlurProgressBar()
        {
            InitializeComponent();
            VisualStateManager.GoToState(this,this.ProgressState.ToString(), true);
        }

        public static readonly DependencyProperty ProgressStateProperty = DependencyProperty.Register("ProgressState", typeof(BlurProgressState), typeof(BlurProgressBar),new FrameworkPropertyMetadata(BlurProgressState.Normal, FrameworkPropertyMetadataOptions.AffectsRender,OnProgressStateChanged));

        private static void OnProgressStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = (BlurProgressBar)d;
            pb.RaiseEvent(new ProgressStateChangedEventArgs((BlurProgressState)e.OldValue, (BlurProgressState)e.NewValue));
            VisualStateManager.GoToState(pb, e.NewValue.ToString(), true);
        }

        public static readonly RoutedEvent ProgressStateChangedEvent = EventManager.RegisterRoutedEvent("ProgressStateChanged", RoutingStrategy.Direct, typeof(EventHandler<ProgressStateChangedEventArgs>), typeof(BlurProgressBar));

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

    public class ProgressStateChangedEventArgs : RoutedEventArgs
    {
        public ProgressStateChangedEventArgs(BlurProgressState oldValue, BlurProgressState newValue)
        {
            base.RoutedEvent = BlurProgressBar.ProgressStateChangedEvent;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public BlurProgressState OldValue
        {
            get;
            private set;
        }

        public BlurProgressState NewValue
        {
            get;
            private set;
        }
    }
}
