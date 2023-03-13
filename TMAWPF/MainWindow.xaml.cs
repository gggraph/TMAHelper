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
using System.Windows.Media.Animation;

namespace WPFtest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public void FadeElement(
            System.Windows.FrameworkElement c, 
            float targetOpacity = 1, 
            double beginTime = 0, 
            double duration = 1)
        {
            var animation = new DoubleAnimation
            {
                To = targetOpacity,
                BeginTime = TimeSpan.FromSeconds(beginTime),
                Duration = TimeSpan.FromSeconds(duration),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, a) => c.Opacity = targetOpacity;

            c.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        /*
         <BeginStoryboard>
                <Storyboard BeginTime="0" Duration="Forever">
                    <DoubleAnimation Storyboard.TargetName="mainWindow" Storyboard.TargetProperty="(Window.Width)" From="0" To="300"  BeginTime="0:0:0" Duration="0:0:1" />
                    <DoubleAnimation Storyboard.TargetName="mainWindow" Storyboard.TargetProperty="(Window.Height)" From="0" To="400" BeginTime="0:0:0" Duration="0:0:1" />
                </Storyboard>
            </BeginStoryboard>
         */
       

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FadeElement(startButton,0);
            FadeElement(secretInput,0);
            FadeElement(titleLabel,0);
            FadeElement(subtitleLabel,0);
            FadeElement(waveBubble, 1);
        }
    }
}
