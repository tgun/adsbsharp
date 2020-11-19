using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterSDR.Controls {
    public partial class FrequencyEdit : UserControl {
        public bool EntryModeActive { get; set; }
        public FrequencyEdit() {
            InitializeComponent();
        }
    }
}
