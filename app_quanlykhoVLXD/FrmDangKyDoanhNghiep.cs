using System;
using System.Drawing;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class FrmDangKyDoanhNghiep : Form
    {
        private string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";

        public FrmDangKyDoanhNghiep()
        {
            InitializeComponent();
            SetupLabelsAndLayout(); // Tự động thêm các nhãn (Label) hướng dẫn
        }

        private void SetupLabelsAndLayout()
        {
            this.Text = "Đăng ký thông tin doanh nghiệp";
            this.Size = new Size(650, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Tạo và gán tên hiển thị cho các nhãn tương ứng với từng TextBox có sẵn
            AddLabel("Tên doanh nghiệp:", 30, 25);
            if (txtTenDN != null) { txtTenDN.Location = new Point(30, 48); txtTenDN.Size = new Size(270, 28); }

            AddLabel("Mã số thuế:", 340, 25);
            if (txtMaSoThue != null) { txtMaSoThue.Location = new Point(340, 48); txtMaSoThue.Size = new Size(250, 28); }

            AddLabel("Gmail:", 30, 95);
            if (txtGmail != null) { txtGmail.Location = new Point(30, 118); txtGmail.Size = new Size(270, 28); }

            AddLabel("Số điện thoại:", 340, 95);
            if (txtSDT != null) { txtSDT.Location = new Point(340, 118); txtSDT.Size = new Size(250, 28); }

            AddLabel("Địa chỉ:", 30, 165);
            if (txtDiaChi != null) { txtDiaChi.Location = new Point(30, 188); txtDiaChi.Size = new Size(270, 28); }

            AddLabel("Tên miền (.quanly.com):", 340, 165);
            if (txtTenMien != null) { txtTenMien.Location = new Point(340, 188); txtTenMien.Size = new Size(250, 28); }

            // Định vị lại nút "Tiếp tục" cho cân đối góc dưới bên phải
            if (btnTiepTuc != null)
            {
                btnTiepTuc.Text = "Tiếp tục";
                btnTiepTuc.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                btnTiepTuc.BackColor = Color.FromArgb(52, 152, 219);
                btnTiepTuc.ForeColor = Color.White;
                btnTiepTuc.FlatStyle = FlatStyle.Flat;
                btnTiepTuc.Size = new Size(130, 38);
                btnTiepTuc.Location = new Point(460, 270);
            }
        }

        // Hàm phụ trợ tạo nhanh Label
        private void AddLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(40, 50, 70);
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            this.Controls.Add(lbl);
        }

        private void btnTiepTuc_Click(object sender, EventArgs e)
        {
            string tenDN = txtTenDN?.Text.Trim() ?? "";
            string mst = txtMaSoThue?.Text.Trim() ?? "";
            string gmail = txtGmail?.Text.Trim() ?? "";
            string sdt = txtSDT?.Text.Trim() ?? "";
            string diaChi = txtDiaChi?.Text.Trim() ?? "";
            string tenMienInput = txtTenMien?.Text.Trim() ?? "";
            string tenMien = string.IsNullOrEmpty(tenMienInput) ? "" : tenMienInput + ".quanly.com";

            // 1. Kiểm tra dữ liệu bắt buộc không được để trống
            if (string.IsNullOrEmpty(tenDN) || string.IsNullOrEmpty(mst) || string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(sdt))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ các thông tin bắt buộc của doanh nghiệp!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Chuyển sang form tạo tài khoản & mật khẩu, truyền kèm thông tin doanh nghiệp vừa nhập
            FrmTaoTaiKhoanDoanhNghiep frmTaoTK = new FrmTaoTaiKhoanDoanhNghiep(tenDN, mst, gmail, sdt, diaChi, tenMien);
            frmTaoTK.Show();
            this.Hide();
        }
    }
}