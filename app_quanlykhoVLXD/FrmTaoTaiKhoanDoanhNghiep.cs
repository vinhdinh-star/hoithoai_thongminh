using System;
using System.Drawing;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class FrmTaoTaiKhoanDoanhNghiep : Form
    {
        private string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";

        // Các biến lưu trữ tạm thông tin doanh nghiệp được truyền từ Form trước sang
        private string _tenDN, _mst, _gmail, _sdt, _diaChi, _tenMien;

        public FrmTaoTaiKhoanDoanhNghiep(string tenDN, string mst, string gmail, string sdt, string diaChi, string tenMien)
        {
            InitializeComponent();
            _tenDN = tenDN;
            _mst = mst;
            _gmail = gmail;
            _sdt = sdt;
            _diaChi = diaChi;
            _tenMien = tenMien;

            SetupFormStyle();
        }

        private void SetupFormStyle()
        {
            this.Text = "Tạo tài khoản quản trị doanh nghiệp";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Tùy chỉnh riêng cho nút Hoàn tất nếu cần
            if (btnHoanTat != null)
            {
                btnHoanTat.BackColor = Color.FromArgb(46, 204, 113);
                btnHoanTat.ForeColor = Color.White;
                btnHoanTat.FlatStyle = FlatStyle.Flat;
            }
        }

        private void btnHoanTat_Click(object sender, EventArgs e)
        {
            string taiKhoan = txtTaiKhoan?.Text.Trim() ?? "";
            string matKhau = txtMatKhau?.Text.Trim() ?? "";
            string xacNhanMK = txtXacNhanMatKhau?.Text.Trim() ?? "";

            // 1. Kiểm tra không để trống tài khoản/mật khẩu
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Kiểm tra khớp mật khẩu
            if (matKhau != xacNhanMK)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp. Vui lòng kiểm tra lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 3. Thực hiện câu lệnh INSERT lưu toàn bộ thông tin doanh nghiệp + tài khoản mật khẩu vào SQL Server
            string query = "INSERT INTO DoanhNghiep (TenDoanhNghiep, MaSoThuHe, Gmail, SoDienThoai, DiaChi, TenMien, TaiKhoan, MatKhau, SoLanDangNhapSai, TrangThai) " +
                           "VALUES (@TenDN, @MST, @Gmail, @SDT, @DiaChi, @TenMien, @TaiKhoan, @MatKhau, 0, N'Hoạt động')";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDN", _tenDN);
                        cmd.Parameters.AddWithValue("@MST", _mst);
                        cmd.Parameters.AddWithValue("@Gmail", _gmail);
                        cmd.Parameters.AddWithValue("@SDT", _sdt);
                        cmd.Parameters.AddWithValue("@DiaChi", _diaChi);
                        cmd.Parameters.AddWithValue("@TenMien", _tenMien);
                        cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                        cmd.Parameters.AddWithValue("@MatKhau", matKhau);

                        int result = cmd.ExecuteNonQuery();
                        if (result > 0)
                        {
                            MessageBox.Show("✅ Đăng ký tài khoản và doanh nghiệp thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Chuyển thẳng vào giao diện quản lý chính của kho (Form1)
                            Form1 mainForm = new Form1(_tenDN);
                            mainForm.Show();
                            this.Hide();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi lưu cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}