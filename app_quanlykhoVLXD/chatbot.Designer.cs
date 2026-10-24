using System;
using System.Drawing;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    // Thêm ": Form" vào đây:
    partial class chatbot : Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 500);
            this.Text = "chatbot";
        }
    }
}