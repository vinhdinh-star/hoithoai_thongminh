using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;

namespace app_quanlykhoVLXD
{
    public partial class caidat : Form
    {
        private const string ConnStr = "Data Source=quan_ly_kho.db";
        private static readonly Color MauTim = Color.FromArgb(108, 92, 231);
        private static readonly Color MauChu = Color.FromArgb(45, 52, 54);

        private DataGridView dgvFields = null!;
        private TextBox txtTenTruong = null!;
        private TextBox txtTenHienThi = null!;
        private ComboBox cmbKieuDuLieu = null!;
        private Button btnThem = null!;
        private Button btnSua = null!;
        private Button btnXoa = null!;

        private DataTable dtFields = new DataTable();
        private int selectedId = -1;

        public caidat()
        {
            BuildUI();
            KhoiTaoBangCauHinh();
            TaiDanhSachTruong();
        }

        private void BuildUI()
        {
            this.Text = "Cài đặt trường dữ liệu";
            this.BackColor = Color.FromArgb(236, 239, 244);
            this.Font = new Font("Segoe UI", 9.5F);
            this.AutoScroll = true;

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(236, 239, 244)
            };

            // Tiêu đề trang
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 55 };
            Label lblTitle = new Label
            {
                Text = "QUẢN LÝ & CÀI ĐẶT TRƯỜNG DỮ LIỆU",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = MauChu,
                AutoSize = true,
                Location = new Point(0, 5)
            };
            Label lblSub = new Label
            {
                Text = "Thêm, sửa, xóa các trường tùy chỉnh cho hệ thống kho",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(2, 30)
            };
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblSub);

            // Khung nhập liệu (Bên trái hoặc trên)
            Panel pnlForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            Label lbl1 = new Label { Text = "Mã trường (không dấu):", Location = new Point(15, 12), AutoSize = true };
            txtTenTruong = new TextBox { Location = new Point(15, 36), Size = new Size(160, 28) };

            Label lbl2 = new Label { Text = "Tên hiển thị:", Location = new Point(190, 12), AutoSize = true };
            txtTenHienThi = new TextBox { Location = new Point(190, 36), Size = new Size(180, 28) };

            Label lbl3 = new Label { Text = "Kiểu dữ liệu:", Location = new Point(385, 12), AutoSize = true };
            cmbKieuDuLieu = new ComboBox { Location = new Point(385, 36), Size = new Size(130, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbKieuDuLieu.Items.AddRange(new object[] { "TEXT", "INTEGER", "REAL" });
            cmbKieuDuLieu.SelectedIndex = 0;

            btnThem = TaoNut("Thêm trường", 110, MauTim, Color.White);
            btnThem.Location = new Point(530, 35);
            btnThem.Click += (s, e) => ThemTruong();

            btnSua = TaoNut("Cập nhật", 90, Color.FromArgb(9, 132, 227), Color.White);
            btnSua.Location = new Point(645, 35);
            btnSua.Click += (s, e) => SuaTruong();

            btnXoa = TaoNut("Xóa trường", 90, Color.FromArgb(214, 48, 49), Color.White);
            btnXoa.Location = new Point(740, 35);
            btnXoa.Click += (s, e) => XoaTruong();

            pnlForm.Controls.AddRange(new Control[] { lbl1, txtTenTruong, lbl2, txtTenHienThi, lbl3, cmbKieuDuLieu, btnThem, btnSua, btnXoa });

            // Bảng hiển thị danh sách trường
            dgvFields = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            dgvFields.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgvFields.ColumnHeadersDefaultCellStyle.ForeColor = MauChu;
            dgvFields.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvFields.EnableHeadersVisualStyles = false;
            dgvFields.RowTemplate.Height = 30;

            ThemCotDgv("Id", "ID", 10);
            ThemCotDgv("TenTruong", "Mã Trường", 35);
            ThemCotDgv("TenHienThi", "Tên Hiển Thị", 40);
            ThemCotDgv("KieuDuLieu", "Kiểu Dữ Liệu", 15);

            dgvFields.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    DataGridViewRow row = dgvFields.Rows[e.RowIndex];
                    selectedId = Convert.ToInt32(row.Cells["Id"].Value);
                    txtTenTruong.Text = row.Cells["TenTruong"].Value.ToString();
                    txtTenHienThi.Text = row.Cells["TenHienThi"].Value.ToString();
                    cmbKieuDuLieu.SelectedItem = row.Cells["KieuDuLieu"].Value.ToString();
                }
            };

            Panel spacer = new Panel { Dock = DockStyle.Top, Height = 12 };
            pnlContent.Controls.Add(dgvFields);
            pnlContent.Controls.Add(spacer);
            pnlContent.Controls.Add(pnlForm);
            pnlContent.Controls.Add(pnlTop);

            this.Controls.Add(pnlContent);
        }

        private Button TaoNut(string text, int width, Color bg, Color fg)
        {
            return new Button
            {
                Text = text,
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private void ThemCotDgv(string prop, string title, float weight)
        {
            dgvFields.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                Name = prop,
                HeaderText = title,
                FillWeight = weight
            });
        }

        private void KhoiTaoBangCauHinh()
        {
            using (SqliteConnection conn = new SqliteConnection(ConnStr))
            {
                conn.Open();
                string sql = "CREATE TABLE IF NOT EXISTS CauHinhTruong (Id INTEGER PRIMARY KEY AUTOINCREMENT, TenTruong TEXT, TenHienThi TEXT, KieuDuLieu TEXT)";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void TaiDanhSachTruong()
        {
            dtFields = new DataTable();
            dtFields.Columns.Add("Id", typeof(int));
            dtFields.Columns.Add("TenTruong", typeof(string));
            dtFields.Columns.Add("TenHienThi", typeof(string));
            dtFields.Columns.Add("KieuDuLieu", typeof(string));

            using (SqliteConnection conn = new SqliteConnection(ConnStr))
            {
                conn.Open();
                string sql = "SELECT Id, TenTruong, TenHienThi, KieuDuLieu FROM CauHinhTruong";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                using (SqliteDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        dtFields.Rows.Add(
                            Convert.ToInt32(r["Id"]),
                            Convert.ToString(r["TenTruong"]) ?? "",
                            Convert.ToString(r["TenHienThi"]) ?? "",
                            Convert.ToString(r["KieuDuLieu"]) ?? ""
                        );
                    }
                }
            }
            dgvFields.DataSource = dtFields;
            selectedId = -1;
            txtTenTruong.Clear();
            txtTenHienThi.Clear();
        }

        private void ThemTruong()
        {
            string ma = txtTenTruong.Text.Trim();
            string ten = txtTenHienThi.Text.Trim();
            string kieu = cmbKieuDuLieu.SelectedItem?.ToString() ?? "TEXT";

            if (string.IsNullOrEmpty(ma) || string.IsNullOrEmpty(ten))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mã trường và tên hiển thị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    string sql = "INSERT INTO CauHinhTruong (TenTruong, TenHienThi, KieuDuLieu) VALUES (@ma, @ten, @kieu)";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ma", ma);
                        cmd.Parameters.AddWithValue("@ten", ten);
                        cmd.Parameters.AddWithValue("@kieu", kieu);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Thêm trường thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TaiDanhSachTruong();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm trường: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SuaTruong()
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa từ bảng bên dưới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ten = txtTenHienThi.Text.Trim();
            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    string sql = "UPDATE CauHinhTruong SET TenHienThi = @ten WHERE Id = @id";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ten", ten);
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Cập nhật tên trường thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TaiDanhSachTruong();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void XoaTruong()
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xóa từ bảng bên dưới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Bạn có chắc chắn muốn xóa trường này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    string sql = "DELETE FROM CauHinhTruong WHERE Id = @id";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Đã xóa trường!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TaiDanhSachTruong();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}