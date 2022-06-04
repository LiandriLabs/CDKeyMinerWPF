using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace CDKeyMiner
{
    public partial class Chart : UserControl, INotifyPropertyChanged
    {
        PointCollection chartPoints;
        List<double> chartValues = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        double max = 0;
        string unit = "";
        Brush chartFill;

        public Chart()
        {
            InitializeComponent();
            this.ChartPoints = new PointCollection();
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PointCollection ChartPoints
        {
            get
            {
                return this.chartPoints;
            }
            set
            {
                if (this.chartPoints != value)
                {
                    this.chartPoints = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("ChartPoints"));
                    }
                }
            }
        }

        public string Unit
        {
            get
            {
                return this.unit;
            }
            set
            {
                if (this.unit != value)
                {
                    this.unit = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("MaxText"));
                    }
                }
            }
        }

        public double Max
        {
            get
            {
                return this.max;
            }
            set
            {
                if (value > this.max)
                {
                    this.max = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("MaxText"));
                    }
                }
            }
        }

        public string MaxText
        {
            get
            {
                return $"{this.max:N0} {this.unit}";
            }
        }

        public Brush ChartFill
        {
            get
            {
                return this.chartFill;
            }
            set
            {
                if (this.chartFill != value)
                {
                    this.chartFill = value;
                    if (this.PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("ChartFill"));
                    }
                }
            }
        }

        public void AddValue(double val)
        {
            {
                var i = 0;
                for (i = 0; i < (chartValues.Count - 1); i++)
                {
                    chartValues[i] = chartValues[i + 1];
                }
                chartValues[i] = val;
            }

            var h = theCanvas.ActualHeight;
            var w = theCanvas.ActualWidth;

            this.Max = val;
            var hScale = (h - 25) / this.Max;
            var wScale = w / (chartValues.Count - 1);
            var pc = new PointCollection();

            for (int i = 0; i < chartValues.Count; i++)
            {
                pc.Add(new Point(i * wScale, h - (chartValues[i] * hScale)));
            }

            pc.Add(new Point(w, h));
            pc.Add(new Point(0, h));

            this.ChartPoints = pc;
            this.Max = max;
        }
    }
}
