using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MessagesFinder
{
    public partial class ProgressBarForm : Form
    {
        public ProgressBarForm()
        {
            InitializeComponent();
        }

        public void setMax(int max)
        {
            progressBar1.Maximum = max;
            progressBar1.Value = 0;
        }

        public void Step()
        {
            progressBar1.Value++;
        }
    }
}
