using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class FrmDangNhap : Form
    {
        private string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";

        public FrmDangNhap()
        {
            InitializeComponent();
        }

        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            string taiKhoan = txtTaiKhoan.Text.Trim();
            string matKhau = txtMatKhau.Text.Trim();

            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Kiểm tra Admin
                    string queryAdmin = "SELECT COUNT(1) FROM AdminAccounts WHERE Username = @User AND Password = @Pass";
                    using (SqlCommand cmdAdmin = new SqlCommand(queryAdmin, conn))
                    {
                        cmdAdmin.Parameters.AddWithValue("@User", taiKhoan);
                        cmdAdmin.Parameters.AddWithValue("@Pass", matKhau);
                        int isAdmin = Convert.ToInt32(cmdAdmin.ExecuteScalar());

                        if (isAdmin > 0)
                        {
                            MessageBox.Show("✅ Đăng nhập thành công với quyền Quản trị viên (Admin)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            FrmQuanLyDoanhNghiep adminForm = new FrmQuanLyDoanhNghiep();
                            adminForm.Show();
                            this.Hide();
                            return;
                        }
                    }

                    // Kiểm tra Doanh Nghiệp
                    string queryDN = "SELECT TenDoanhNghiep, MatKhau, TrangThai FROM DoanhNghiep WHERE TaiKhoan = @TaiKhoan OR Gmail = @TaiKhoan";
                    using (SqlCommand cmdDN = new SqlCommand(queryDN, conn))
                    {
                        cmdDN.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                        using (SqlDataReader reader = cmdDN.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string trangThai = reader["TrangThai"]?.ToString() ?? "Hoạt động";
                                string dbMatKhau = reader["MatKhau"]?.ToString();
                                string tenDoanhNghiep = reader["TenDoanhNghiep"]?.ToString() ?? "Doanh nghiệp";

                                if (trangThai == "Bị khóa")
                                {
                                    MessageBox.Show("❌ Tài khoản của bạn đã bị khóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                if (dbMatKhau == matKhau)
                                {
                                    reader.Close();
                                    MessageBox.Show($"✅ Xin chào, {tenDoanhNghiep}!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    Form1 mainForm = new Form1(tenDoanhNghiep);
                                    mainForm.Show();
                                    this.Hide();
                                }
                                else
                                {
                                    MessageBox.Show("❌ Mật khẩu không chính xác!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("❌ Không tìm thấy tài khoản trong hệ thống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối CSDL: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🌟 NÚT ĐĂNG KÝ: Mở form đăng ký doanh nghiệp và ẩn form đăng nhập
        private void btnDangKy_Click(object sender, EventArgs e)
        {
            FrmDangKyDoanhNghiep frmDangKy = new FrmDangKyDoanhNghiep();
            frmDangKy.Show();
            this.Hide();
        }
    }
}