using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace CDKeyMiner
{
    static class Extensions
    {
        public static void AnimatedUpdate(this Label label, string content)
        {
            if ((string)label.Content != content)
            {
                var fadeIn = (Storyboard)label.FindResource("FadeIn");
                label.Opacity = 0;
                label.Content = content;
                fadeIn.Begin(label);
            }
        }
    }
}
