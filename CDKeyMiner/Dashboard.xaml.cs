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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        private Credentials creds;
        private bool mining = false;

        public Dashboard(Credentials credentials)
        {
            InitializeComponent();
            creds = credentials;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(this);
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!mining)
            {
                mining = true;
                buttonLbl.Content = "■";
                statusLbl.Content = "Mining...";
            }
            else
            {
                mining = false;
                buttonLbl.Content = "▶";
                statusLbl.Content = "Press to start mining.";
            }
        }

        private void buttonLbl_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerHighlight");
        }

        private void buttonLbl_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerGreen");
        }
    }
}
