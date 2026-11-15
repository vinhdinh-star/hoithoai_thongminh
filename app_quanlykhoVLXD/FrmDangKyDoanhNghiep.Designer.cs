namespace app_quanlykhoVLXD
{
    partial class FrmDangKyDoanhNghiep
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.txtTenDN = new System.Windows.Forms.TextBox();
            this.txtMaSoThue = new System.Windows.Forms.TextBox();
            this.txtGmail = new System.Windows.Forms.TextBox();
            this.txtSDT = new System.Windows.Forms.TextBox();
            this.txtDiaChi = new System.Windows.Forms.TextBox();
            this.txtTenMien = new System.Windows.Forms.TextBox();
            this.btnTiepTuc = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // txtTenDN
            this.txtTenDN.Location = new System.Drawing.Point(50, 50);
            this.txtTenDN.Name = "txtTenDN";
            this.txtTenDN.Size = new System.Drawing.Size(250, 22);

            // txtMaSoThue
            this.txtMaSoThue.Location = new System.Drawing.Point(400, 50);
            this.txtMaSoThue.Name = "txtMaSoThue";
            this.txtMaSoThue.Size = new System.Drawing.Size(250, 22);

            // txtGmail
            this.txtGmail.Location = new System.Drawing.Point(50, 120);
            this.txtGmail.Name = "txtGmail";
            this.txtGmail.Size = new System.Drawing.Size(250, 22);

            // txtSDT
            this.txtSDT.Location = new System.Drawing.Point(400, 120);
            this.txtSDT.Name = "txtSDT";
            this.txtSDT.Size = new System.Drawing.Size(250, 22);

            // txtDiaChi
            this.txtDiaChi.Location = new System.Drawing.Point(50, 190);
            this.txtDiaChi.Name = "txtDiaChi";
            this.txtDiaChi.Size = new System.Drawing.Size(250, 22);

            // txtTenMien
            this.txtTenMien.Location = new System.Drawing.Point(400, 190);
            this.txtTenMien.Name = "txtTenMien";
            this.txtTenMien.Size = new System.Drawing.Size(250, 22);

            // btnTiepTuc
            this.btnTiepTuc.Location = new System.Drawing.Point(550, 260);
            this.btnTiepTuc.Name = "btnTiepTuc";
            this.btnTiepTuc.Size = new System.Drawing.Size(100, 35);
            this.btnTiepTuc.Text = "Tiếp tục";
            this.btnTiepTuc.UseVisualStyleBackColor = true;
            this.btnTiepTuc.Click += new System.EventHandler(this.btnTiepTuc_Click);

            // FrmDangKyDoanhNghiep
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 350);
            this.Controls.Add(this.btnTiepTuc);
            this.Controls.Add(this.txtTenMien);
            this.Controls.Add(this.txtDiaChi);
            this.Controls.Add(this.txtSDT);
            this.Controls.Add(this.txtGmail);
            this.Controls.Add(this.txtMaSoThue);
            this.Controls.Add(this.txtTenDN);
            this.Name = "FrmDangKyDoanhNghiep";
            this.Text = "Đăng ký thông tin doanh nghiệp";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtTenDN;
        private System.Windows.Forms.TextBox txtMaSoThue;
        private System.Windows.Forms.TextBox txtGmail;
        private System.Windows.Forms.TextBox txtSDT;
        private System.Windows.Forms.TextBox txtDiaChi;
        private System.Windows.Forms.TextBox txtTenMien;
        private System.Windows.Forms.Button btnTiepTuc;
    }
}