using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using libModeSharp;

namespace ADSBSharp {
    public partial class MessageDisplay : Form {
        private AdsbBitDecoder _oldBitDecoder;

        public MessageDisplay() {
            InitializeComponent();
            _oldBitDecoder = new AdsbBitDecoder();
        }

        private void MessageDisplay_Load(object sender, EventArgs e) {

        }

        public void ReceiveOldFrame(byte[] frameData, int length) {
            var sb = new StringBuilder();
            
            for (var i = 0; i < length; i++) {
                sb.Append(string.Format("{0:X2}", frameData[i]));
            }

            txtOldDecode.AppendText(sb.ToString() + "\r\n");
        }

        public void ReceiveNewFrame() {

        }
    }
}
