using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class FrmQuanLyDoanhNghiep : Form
    {
        // Chuỗi kết nối đến SQL Server của bạn
        private string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";

        public FrmQuanLyDoanhNghiep()
        {
            InitializeComponent();
            LoadDanhSachDoanhNghiep();
        }
        // Các thành phần giao diện chính
        private Panel pnlSidebar;
        private Panel pnlTopBar;
        private Panel pnlMainContent;

        // Controls trong TopBar
        private TextBox txtGlobalSearch;
        private Button btnAdmin;
        private Button btnExportFile;
        private Button btnImportFile;

        // Controls trong MainContent
        private Button btnDonHang;
        private Label lblThongKeDonHang; // Label hiển thị số lượng đơn hàng
        private Panel pnlGridContainer;
        private TextBox txtSubSearch;
        private ComboBox cmbCategory;
        private DataGridView dgvProducts;
        private Button btnChatBot;
        // 1. Hàm tải dữ liệu danh sách doanh nghiệp lên DataGridView
        private void LoadDanhSachDoanhNghiep()
        {
            string query = "SELECT * FROM DoanhNghiep";
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvDoanhNghiep.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 2. Sự kiện click vào dòng trên bảng để đổ dữ liệu ngược lên các ô TextBox
        private void dgvDoanhNghiep_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvDoanhNghiep.Rows[e.RowIndex];
                txtTenDN.Text = row.Cells["TenDoanhNghiep"].Value.ToString();
                txtMaSoThue.Text = row.Cells["MaSoThuHe"].Value.ToString();
                txtGmail.Text = row.Cells["Gmail"].Value.ToString();
                txtSDT.Text = row.Cells["SoDienThoai"].Value.ToString();
                txtDiaChi.Text = row.Cells["DiaChi"].Value.ToString();
                txtTenMien.Text = row.Cells["TenMien"].Value.ToString();
            }
        }
        private void btnDonHang_Click(object sender, EventArgs e)
        {
            donhang frmDonHang = new donhang();
            frmDonHang.Show();
            // Đóng hẳn form hiện tại
            
        }

        private void btnChatBot_Click(object sender, EventArgs e)
        {
            FormChatBot frmChat = new FormChatBot();
            frmChat.TopMost = true;
            frmChat.Show();
            // Đóng hẳn form hiện tại
            
        }

        private void MoTrangQLSanPham()
        {
            pnlMainContent.Controls.Clear();

            trangqlsanpham frm = new trangqlsanpham();
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;

            pnlMainContent.Controls.Add(frm);
            frm.Show();
            // Đóng hẳn form hiện tại
            
        }
        
        private Form currentPage;

        private void MoTrang(Form frm)
        {
            if (currentPage != null) { currentPage.Dispose(); currentPage = null; }
            pnlMainContent.Controls.Clear();

            currentPage = frm;
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;

            pnlMainContent.Controls.Add(frm);
            pnlMainContent.Controls.Add(btnChatBot);
            btnChatBot.BringToFront();

            frm.Show();
            // Đóng hẳn form hiện tại
            
        }

        private void MoTrangNhapHang()
        {
            nhaphang trang = new nhaphang();

            trang.TaoDonDatHang += data =>
            {
                this.BeginInvoke(new Action(() => MoTrang(new Dathangnhap(data))));
            };

            MoTrang(trang);
            // Đóng hẳn form hiện tại
            
        }
        private void btnQuayVe_Click(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;

            // Gọi trực tiếp hàm mở lại Form1
            Form1 mainForm = new Form1("Doanh nghiệp VLXD");
            mainForm.Show();

            this.Hide();
            this.Dispose(); // Giải phóng form con hiện tại
        }
        // 3. Chức năng: THÊM doanh nghiệp mới
        private void btnThem_Click(object sender, EventArgs e)
        {
            string query = "INSERT INTO DoanhNghiep (TenDoanhNghiep, MaSoThuHe, Gmail, SoDienThoai, DiaChi, TenMien) " +
                           "VALUES (@TenDN, @MST, @Gmail, @SDT, @DiaChi, @TenMien)";
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDN", txtTenDN.Text.Trim());
                        cmd.Parameters.AddWithValue("@MST", txtMaSoThue.Text.Trim());
                        cmd.Parameters.AddWithValue("@Gmail", txtGmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@SDT", txtSDT.Text.Trim());
                        cmd.Parameters.AddWithValue("@DiaChi", txtDiaChi.Text.Trim());
                        cmd.Parameters.AddWithValue("@TenMien", txtTenMien.Text.Trim());

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("✅ Thêm doanh nghiệp thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDanhSachDoanhNghiep();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 4. Chức năng: SỬA thông tin doanh nghiệp
        private void btnSua_Click(object sender, EventArgs e)
        {
            string query = "UPDATE DoanhNghiep SET TenDoanhNghiep = @TenDN, Gmail = @Gmail, SoDienThoai = @SDT, DiaChi = @DiaChi, TenMien = @TenMien " +
                           "WHERE MaSoThuHe = @MST";
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDN", txtTenDN.Text.Trim());
                        cmd.Parameters.AddWithValue("@MST", txtMaSoThue.Text.Trim()); // Dựa vào mã số thuế để cập nhật
                        cmd.Parameters.AddWithValue("@Gmail", txtGmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@SDT", txtSDT.Text.Trim());
                        cmd.Parameters.AddWithValue("@DiaChi", txtDiaChi.Text.Trim());
                        cmd.Parameters.AddWithValue("@TenMien", txtTenMien.Text.Trim());

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("✅ Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDanhSachDoanhNghiep();
                        }
                        else
                        {
                            MessageBox.Show("❌ Không tìm thấy mã số thuế để cập nhật!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 5. Chức năng: XÓA doanh nghiệp
        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtMaSoThue.Text))
            {
                MessageBox.Show("Vui lòng chọn doanh nghiệp cần xóa trên bảng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult dr = MessageBox.Show("Bạn có chắc chắn muốn xóa doanh nghiệp này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                string query = "DELETE FROM DoanhNghiep WHERE MaSoThuHe = @MST";
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@MST", txtMaSoThue.Text.Trim());
                            cmd.ExecuteNonQuery();

                            MessageBox.Show("✅ Xóa doanh nghiệp thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDanhSachDoanhNghiep();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

    }
}