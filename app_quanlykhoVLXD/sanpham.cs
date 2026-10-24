using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class sanpham : Form
    {
        
        private Panel pnlOverlay;

        public sanpham()
        {
           
            InitializeComponent();
            // Đặt 2 dòng này ở SAU InitializeComponent():
            pnlOverlay = new Panel(); // Khởi tạo panel
            pnlOverlay.BackColor = Color.FromArgb(100, 0, 0, 0);
            pnlOverlay.BringToFront();
            // Đưa lớp phủ lên trên nền
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
