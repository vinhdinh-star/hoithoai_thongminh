namespace app_quanlykhoVLXD
{
    partial class FrmQuanLyDoanhNghiep
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
            this.dgvDoanhNghiep = new System.Windows.Forms.DataGridView();
            this.txtTenDN = new System.Windows.Forms.TextBox();
            this.txtMaSoThue = new System.Windows.Forms.TextBox();
            this.txtGmail = new System.Windows.Forms.TextBox();
            this.txtSDT = new System.Windows.Forms.TextBox();
            this.txtDiaChi = new System.Windows.Forms.TextBox();
            this.txtTenMien = new System.Windows.Forms.TextBox();
            this.btnThem = new System.Windows.Forms.Button();
            this.btnSua = new System.Windows.Forms.Button();
            this.btnXoa = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDoanhNghiep)).BeginInit();
            this.SuspendLayout();

            // dgvDoanhNghiep
            this.dgvDoanhNghiep.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDoanhNghiep.Location = new System.Drawing.Point(30, 200);
            this.dgvDoanhNghiep.Name = "dgvDoanhNghiep";
            this.dgvDoanhNghiep.RowHeadersWidth = 51;
            this.dgvDoanhNghiep.Size = new System.Drawing.Size(740, 220);
            this.dgvDoanhNghiep.TabIndex = 0;
            this.dgvDoanhNghiep.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDoanhNghiep_CellClick);

            // txtTenDN
            this.txtTenDN.Location = new System.Drawing.Point(30, 30);
            this.txtTenDN.Name = "txtTenDN";
            this.txtTenDN.Size = new System.Drawing.Size(220, 22);

            // txtMaSoThue
            this.txtMaSoThue.Location = new System.Drawing.Point(290, 30);
            this.txtMaSoThue.Name = "txtMaSoThue";
            this.txtMaSoThue.Size = new System.Drawing.Size(220, 22);

            // txtGmail
            this.txtGmail.Location = new System.Drawing.Point(550, 30);
            this.txtGmail.Name = "txtGmail";
            this.txtGmail.Size = new System.Drawing.Size(220, 22);

            // txtSDT
            this.txtSDT.Location = new System.Drawing.Point(30, 80);
            this.txtSDT.Name = "txtSDT";
            this.txtSDT.Size = new System.Drawing.Size(220, 22);

            // txtDiaChi
            this.txtDiaChi.Location = new System.Drawing.Point(290, 80);
            this.txtDiaChi.Name = "txtDiaChi";
            this.txtDiaChi.Size = new System.Drawing.Size(220, 22);

            // txtTenMien
            this.txtTenMien.Location = new System.Drawing.Point(550, 80);
            this.txtTenMien.Name = "txtTenMien";
            this.txtTenMien.Size = new System.Drawing.Size(220, 22);

            // btnThem
            this.btnThem.Location = new System.Drawing.Point(290, 140);
            this.btnThem.Name = "btnThem";
            this.btnThem.Size = new System.Drawing.Size(90, 35);
            this.btnThem.Text = "Thêm";
            this.btnThem.UseVisualStyleBackColor = true;
            this.btnThem.Click += new System.EventHandler(this.btnThem_Click);

            // btnSua
            this.btnSua.Location = new System.Drawing.Point(400, 140);
            this.btnSua.Name = "btnSua";
            this.btnSua.Size = new System.Drawing.Size(90, 35);
            this.btnSua.Text = "Sửa";
            this.btnSua.UseVisualStyleBackColor = true;
            this.btnSua.Click += new System.EventHandler(this.btnSua_Click);

            // btnXoa
            this.btnXoa.Location = new System.Drawing.Point(510, 140);
            this.btnXoa.Name = "btnXoa";
            this.btnXoa.Size = new System.Drawing.Size(90, 35);
            this.btnXoa.Text = "Xóa";
            this.btnXoa.UseVisualStyleBackColor = true;
            this.btnXoa.Click += new System.EventHandler(this.btnXoa_Click);

            // FrmQuanLyDoanhNghiep
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnXoa);
            this.Controls.Add(this.btnSua);
            this.Controls.Add(this.btnThem);
            this.Controls.Add(this.txtTenMien);
            this.Controls.Add(this.txtDiaChi);
            this.Controls.Add(this.txtSDT);
            this.Controls.Add(this.txtGmail);
            this.Controls.Add(this.txtMaSoThue);
            this.Controls.Add(this.txtTenDN);
            this.Controls.Add(this.dgvDoanhNghiep);
            this.Name = "FrmQuanLyDoanhNghiep";
            this.Text = "Quản lý doanh nghiệp hệ thống";
            ((System.ComponentModel.ISupportInitialize)(this.dgvDoanhNghiep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvDoanhNghiep;
        private System.Windows.Forms.TextBox txtTenDN;
        private System.Windows.Forms.TextBox txtMaSoThue;
        private System.Windows.Forms.TextBox txtGmail;
        private System.Windows.Forms.TextBox txtSDT;
        private System.Windows.Forms.TextBox txtDiaChi;
        private System.Windows.Forms.TextBox txtTenMien;
        private System.Windows.Forms.Button btnThem;
        private System.Windows.Forms.Button btnSua;
        private System.Windows.Forms.Button btnXoa;
    }
}