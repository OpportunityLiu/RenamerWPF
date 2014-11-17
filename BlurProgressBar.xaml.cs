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
        }

        public static readonly DependencyProperty ProgressStateProperty = DependencyProperty.Register("ProgessState", typeof(BlurProgressState), typeof(BlurProgressBar));

        public BlurProgressState ProgressState
        {
            get;
            set;
        }

        public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register("ProgessValue", typeof(double), typeof(BlurProgressBar), new PropertyMetadata(0.0, OnProgressValueChanged));

        private static void OnProgressValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if((double)e.NewValue > 1.0)
                d.SetValue(e.Property, 1.0);
            else if((double)e.NewValue < 0.0 || ((double)e.NewValue) == double.NaN)
                d.SetValue(e.Property, 0.0);
            ((BlurProgressBar)d).Progress0.Width = new GridLength((double)d.GetValue(ProgressValueProperty), GridUnitType.Star);
            ((BlurProgressBar)d).Progress1.Width = new GridLength(1.0-(double)d.GetValue(ProgressValueProperty), GridUnitType.Star);
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
}
