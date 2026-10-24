using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace app_quanlykhoVLXD
{
    // Trang "Nhà cung cấp" - nhúng vào pnlMainContent của Form1.
    // Form thêm/sửa hiện đè lên ngay trong trang (giống mẫu thiết kế).
    public partial class nhacungcap : Form
    {
        private const string ConnStr = "Data Source=quan_ly_kho.db";

        private DataGridView dgv = null!;
        private ContextMenuStrip menuRow = null!;
        private NhaCungCapPanel? pnlForm;   // form thêm/sửa đang mở (nếu có)

        public nhacungcap()
        {
            BuildUI();
            EnsureTable();
            LoadData();
        }

        // ================= GIAO DIỆN DANH SÁCH =================
        private void BuildUI()
        {
            this.Text = "Nhà cung cấp";
            this.BackColor = Color.FromArgb(170, 170, 170);

            Panel pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25, 20, 25, 25) };

            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55 };

            Button btnTitle = new Button
            {
                Text = "DANH SÁCH NHÀ CUNG CẤP",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 220, 220),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(230, 38),
                Location = new Point(0, 0),
                Enabled = false
            };
            btnTitle.FlatAppearance.BorderSize = 0;

            FlowLayoutPanel flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            Button btnImport = CreateButton("Nhập file", 110);
            btnImport.Click += (s, e) => NhapFileCsv();

            Button btnAdd = CreateButton("+ Thêm nhà cung cấp", 180);
            btnAdd.Click += (s, e) => ThemNhaCungCap();

            flowRight.Controls.Add(btnImport);
            flowRight.Controls.Add(btnAdd);

            pnlToolbar.Controls.Add(btnTitle);
            pnlToolbar.Controls.Add(flowRight);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(215, 215, 215),
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            dgv.RowTemplate.Height = 40;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(160, 150, 150);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(110, 120, 140);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(175, 165, 165);

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) SuaNhaCungCap(); };
            dgv.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    dgv.ClearSelection();
                    dgv.Rows[e.RowIndex].Selected = true;
                    dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells[1];
                }
            };
            dgv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) XoaNhaCungCap(); };

            menuRow = new ContextMenuStrip();
            menuRow.Items.Add("Sửa", null, (s, e) => SuaNhaCungCap());
            menuRow.Items.Add("Xóa", null, (s, e) => XoaNhaCungCap());
            dgv.ContextMenuStrip = menuRow;

            pnlContent.Controls.Add(dgv);
            pnlContent.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlContent);
        }

        private Button CreateButton(string text, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 38),
                Margin = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(150, 158, 168);
            return btn;
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgv.Columns.Contains("Trạng thái hoạt động")) return;

            string status = Convert.ToString(dgv.Rows[e.RowIndex].Cells["Trạng thái hoạt động"].Value) ?? "";
            if (status == "Ngừng hợp tác")
            {
                e.CellStyle.BackColor = Color.FromArgb(253, 236, 236);
                e.CellStyle.ForeColor = Color.Black;
            }
        }

        // ================= MỞ / ĐÓNG FORM THÊM-SỬA (đè lên trang) =================
        private void MoForm(string title, string okText, NhaCungCapInfo? n, Action<NhaCungCapInfo> onSave)
        {
            DongForm();

            pnlForm = new NhaCungCapPanel(title, okText, n);
            pnlForm.Dock = DockStyle.Fill;
            pnlForm.Cancelled += DongForm;
            pnlForm.Confirmed += info =>
            {
                onSave(info);
                DongForm();
            };

            this.Controls.Add(pnlForm);
            pnlForm.BringToFront();
        }

        private void DongForm()
        {
            if (pnlForm == null) return;
            this.Controls.Remove(pnlForm);
            pnlForm.Dispose();
            pnlForm = null;
        }

        // ================= DATABASE =================
        // Các cột bổ sung (bảng cũ chưa có sẽ được tự thêm, dữ liệu cũ giữ nguyên)
        private static readonly string[] CotBoSung =
        {
            "MaNhaCungCap", "MaSoThue", "Website", "Fax", "QuocGia",
            "TinhThanh", "QuanHuyen", "PhuongXa", "DiaChi", "NguoiDaiDien", "ThongTinLienHe"
        };

        private void EnsureTable()
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();

                    string sql = @"CREATE TABLE IF NOT EXISTS NhaCungCap (
                                    MaNCC       INTEGER PRIMARY KEY AUTOINCREMENT,
                                    TenNCC      TEXT NOT NULL,
                                    SoDienThoai TEXT,
                                    Email       TEXT,
                                    TrangThai   TEXT DEFAULT 'Đang hợp tác',
                                    GhiChu      TEXT)";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        cmd.ExecuteNonQuery();

                    foreach (string col in CotBoSung)
                        ThemCotNeuThieu(conn, col);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tạo được bảng NhaCungCap:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ThemCotNeuThieu(SqliteConnection conn, string col)
        {
            bool daCo = false;
            using (SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(NhaCungCap)", conn))
            using (SqliteDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    if (string.Equals(Convert.ToString(r["name"]), col, StringComparison.OrdinalIgnoreCase))
                    {
                        daCo = true;
                        break;
                    }
                }
            }

            if (!daCo)
            {
                using (SqliteCommand alter = new SqliteCommand("ALTER TABLE NhaCungCap ADD COLUMN " + col + " TEXT", conn))
                    alter.ExecuteNonQuery();
            }
        }

        private void LoadData()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    string sql = @"SELECT MaNCC,
                                          TenNCC      AS [Tên nhà cung cấp],
                                          SoDienThoai AS [Số điện thoại],
                                          Email       AS [Gmail],
                                          TrangThai   AS [Trạng thái hoạt động],
                                          GhiChu      AS [Ghi chú]
                                   FROM NhaCungCap ORDER BY MaNCC DESC";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }

                dgv.DataSource = dt;
                if (dgv.Columns.Contains("MaNCC")) dgv.Columns["MaNCC"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không đọc được dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int? LayMaDangChon()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt32(dgv.CurrentRow.Cells["MaNCC"].Value);
        }

        // Đọc đầy đủ thông tin 1 nhà cung cấp để đưa lên form sửa
        private NhaCungCapInfo? DocChiTiet(int ma)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    using (SqliteCommand cmd = new SqliteCommand("SELECT * FROM NhaCungCap WHERE MaNCC=@ma", conn))
                    {
                        cmd.Parameters.AddWithValue("@ma", ma);
                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return null;
                            return new NhaCungCapInfo
                            {
                                Ma = Str(r, "MaNhaCungCap"),
                                Ten = Str(r, "TenNCC"),
                                Sdt = Str(r, "SoDienThoai"),
                                Email = Str(r, "Email"),
                                MaSoThue = Str(r, "MaSoThue"),
                                Website = Str(r, "Website"),
                                Fax = Str(r, "Fax"),
                                QuocGia = Str(r, "QuocGia"),
                                TinhThanh = Str(r, "TinhThanh"),
                                QuanHuyen = Str(r, "QuanHuyen"),
                                PhuongXa = Str(r, "PhuongXa"),
                                DiaChi = Str(r, "DiaChi"),
                                TrangThai = Str(r, "TrangThai"),
                                GhiChu = Str(r, "GhiChu"),
                                NguoiDaiDien = Str(r, "NguoiDaiDien"),
                                ThongTinLienHe = Str(r, "ThongTinLienHe")
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không đọc được thông tin nhà cung cấp:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static string Str(SqliteDataReader r, string col)
        {
            return Convert.ToString(r[col]) ?? "";
        }

        // ================= THÊM / SỬA / XÓA =================
        private void ThemNhaCungCap()
        {
            MoForm("Thêm nhà cung cấp", "Thêm", null, n =>
            {
                if (string.IsNullOrWhiteSpace(n.Ma))
                    n.Ma = "NCC" + DateTime.Now.ToString("yyMMddHHmmss");   // tự sinh mã nếu bỏ trống

                Luu(n, null);
                LoadData();
            });
        }

        private void SuaNhaCungCap()
        {
            int? ma = LayMaDangChon();
            if (ma == null) return;

            NhaCungCapInfo? cu = DocChiTiet(ma.Value);
            if (cu == null) return;

            MoForm("Sửa nhà cung cấp", "Lưu", cu, n =>
            {
                Luu(n, ma);
                LoadData();
            });
        }

        private void XoaNhaCungCap()
        {
            int? ma = LayMaDangChon();
            if (ma == null) return;

            string ten = Convert.ToString(dgv.CurrentRow!.Cells["Tên nhà cung cấp"].Value) ?? "";
            if (MessageBox.Show("Xóa nhà cung cấp \"" + ten + "\"?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    using (SqliteCommand cmd = new SqliteCommand("DELETE FROM NhaCungCap WHERE MaNCC=@ma", conn))
                    {
                        cmd.Parameters.AddWithValue("@ma", ma.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            LoadData();
        }

        // Thêm mới (ma == null) hoặc cập nhật (ma có giá trị)
        private void Luu(NhaCungCapInfo n, int? ma)
        {
            string sql = ma == null
                ? @"INSERT INTO NhaCungCap
                      (MaNhaCungCap, TenNCC, SoDienThoai, Email, MaSoThue, Website, Fax, QuocGia,
                       TinhThanh, QuanHuyen, PhuongXa, DiaChi, TrangThai, GhiChu, NguoiDaiDien, ThongTinLienHe)
                    VALUES
                      (@ma_ncc, @ten, @sdt, @email, @mst, @web, @fax, @quocgia,
                       @tinh, @quan, @phuong, @diachi, @tt, @gc, @nguoidd, @lienhe)"
                : @"UPDATE NhaCungCap SET
                      MaNhaCungCap=@ma_ncc, TenNCC=@ten, SoDienThoai=@sdt, Email=@email, MaSoThue=@mst,
                      Website=@web, Fax=@fax, QuocGia=@quocgia, TinhThanh=@tinh, QuanHuyen=@quan,
                      PhuongXa=@phuong, DiaChi=@diachi, TrangThai=@tt, GhiChu=@gc,
                      NguoiDaiDien=@nguoidd, ThongTinLienHe=@lienhe
                    WHERE MaNCC=@id";

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ma_ncc", n.Ma);
                        cmd.Parameters.AddWithValue("@ten", n.Ten);
                        cmd.Parameters.AddWithValue("@sdt", n.Sdt);
                        cmd.Parameters.AddWithValue("@email", n.Email);
                        cmd.Parameters.AddWithValue("@mst", n.MaSoThue);
                        cmd.Parameters.AddWithValue("@web", n.Website);
                        cmd.Parameters.AddWithValue("@fax", n.Fax);
                        cmd.Parameters.AddWithValue("@quocgia", n.QuocGia);
                        cmd.Parameters.AddWithValue("@tinh", n.TinhThanh);
                        cmd.Parameters.AddWithValue("@quan", n.QuanHuyen);
                        cmd.Parameters.AddWithValue("@phuong", n.PhuongXa);
                        cmd.Parameters.AddWithValue("@diachi", n.DiaChi);
                        cmd.Parameters.AddWithValue("@tt", n.TrangThai);
                        cmd.Parameters.AddWithValue("@gc", n.GhiChu);
                        cmd.Parameters.AddWithValue("@nguoidd", n.NguoiDaiDien);
                        cmd.Parameters.AddWithValue("@lienhe", n.ThongTinLienHe);
                        if (ma != null) cmd.Parameters.AddWithValue("@id", ma.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================= NHẬP FILE CSV =================
        // Cột theo thứ tự: Tên, Số điện thoại, Gmail, Trạng thái, Ghi chú
        private void NhapFileCsv()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV (*.csv)|*.csv";
                ofd.Title = "Chọn file danh sách nhà cung cấp";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                int dem = 0;
                try
                {
                    string[] lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);

                    using (SqliteConnection conn = new SqliteConnection(ConnStr))
                    {
                        conn.Open();
                        using (SqliteTransaction tran = conn.BeginTransaction())
                        {
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                                string[] p = lines[i].Split(',');
                                if (i == 0 && p[0].Trim().ToLower().StartsWith("tên")) continue;
                                if (string.IsNullOrWhiteSpace(p[0])) continue;

                                using (SqliteCommand cmd = new SqliteCommand(
                                    @"INSERT INTO NhaCungCap (MaNhaCungCap, TenNCC, SoDienThoai, Email, TrangThai, GhiChu, QuocGia)
                                      VALUES (@ma_ncc, @ten, @sdt, @email, @tt, @gc, 'Việt Nam')", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@ma_ncc", "NCC" + DateTime.Now.ToString("yyMMddHHmmss") + i);
                                    cmd.Parameters.AddWithValue("@ten", p[0].Trim());
                                    cmd.Parameters.AddWithValue("@sdt", p.Length > 1 ? p[1].Trim() : "");
                                    cmd.Parameters.AddWithValue("@email", p.Length > 2 ? p[2].Trim() : "");
                                    cmd.Parameters.AddWithValue("@tt", p.Length > 3 && p[3].Trim() != "" ? p[3].Trim() : "Đang hợp tác");
                                    cmd.Parameters.AddWithValue("@gc", p.Length > 4 ? p[4].Trim() : "");
                                    cmd.ExecuteNonQuery();
                                    dem++;
                                }
                            }
                            tran.Commit();
                        }
                    }

                    MessageBox.Show("Đã nhập " + dem + " nhà cung cấp.", "Nhập file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không nhập được file:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ================= DỮ LIỆU 1 NHÀ CUNG CẤP =================
        private class NhaCungCapInfo
        {
            public string Ma { get; set; } = "";
            public string Ten { get; set; } = "";
            public string Sdt { get; set; } = "";
            public string Email { get; set; } = "";
            public string MaSoThue { get; set; } = "";
            public string Website { get; set; } = "";
            public string Fax { get; set; } = "";
            public string QuocGia { get; set; } = "";
            public string TinhThanh { get; set; } = "";
            public string QuanHuyen { get; set; } = "";
            public string PhuongXa { get; set; } = "";
            public string DiaChi { get; set; } = "";
            public string TrangThai { get; set; } = "";
            public string GhiChu { get; set; } = "";
            public string NguoiDaiDien { get; set; } = "";
            public string ThongTinLienHe { get; set; } = "";
        }

        // ================= FORM THÊM / SỬA - HIỆN ĐÈ TRONG TRANG (giống mẫu) =================
        private class NhaCungCapPanel : Panel
        {
            private readonly TextBox txtTen, txtMa, txtSdt, txtEmail, txtMst, txtWeb, txtFax;
            private readonly TextBox txtQuocGia, txtTinh, txtQuan, txtPhuong, txtDiaChi;
            private readonly TextBox txtGhiChu, txtNguoiDD, txtLienHe;
            private readonly ComboBox cmbTrangThai;

            public event Action<NhaCungCapInfo>? Confirmed;
            public event Action? Cancelled;

            public NhaCungCapPanel(string title, string okText, NhaCungCapInfo? n)
            {
                n ??= new NhaCungCapInfo { QuocGia = "Việt Nam", TrangThai = "Đang hợp tác" };

                this.BackColor = Color.FromArgb(155, 155, 155);
                this.Font = new Font("Segoe UI", 9.5F);
                this.AutoScroll = true;

                // Thanh tiêu đề + đường kẻ
                Label lblTitle = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 13F, FontStyle.Regular),
                    Location = new Point(20, 10),
                    AutoSize = true
                };
                Panel line = new Panel
                {
                    Location = new Point(0, 44),
                    Size = new Size(2000, 1),
                    BackColor = Color.Black
                };
                Label lblHead = new Label
                {
                    Text = "Thông tin chung",
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
                    Location = new Point(25, 50),
                    AutoSize = true
                };
                this.Controls.Add(lblTitle);
                this.Controls.Add(line);
                this.Controls.Add(lblHead);

                // ----- Cột trái -----
                txtTen = AddField(this, "Tên nhà cung cấp *", 25, 76, 540, n.Ten);

                txtMa = AddField(this, "Mã nhà cung cấp", 25, 136, 255, n.Ma);
                txtSdt = AddField(this, "Số điện thoại", 290, 136, 275, n.Sdt);

                txtEmail = AddField(this, "Email", 25, 198, 255, n.Email);
                txtMst = AddField(this, "Mã số thuế", 290, 198, 275, n.MaSoThue);

                txtWeb = AddField(this, "Website", 25, 260, 255, n.Website);
                txtFax = AddField(this, "Số fax", 290, 260, 275, n.Fax);

                txtQuocGia = AddField(this, "Quốc gia", 25, 322, 255, n.QuocGia);
                txtTinh = AddField(this, "Tỉnh/Thành phố", 290, 322, 275, n.TinhThanh);

                txtQuan = AddField(this, "Quận/Huyện", 25, 384, 255, n.QuanHuyen);
                txtPhuong = AddField(this, "Phường/Xã", 290, 384, 275, n.PhuongXa);

                txtDiaChi = AddField(this, "Địa chỉ cụ thể", 25, 446, 540, n.DiaChi);

                // ----- Khung bên phải -----
                Panel pnlRight = new Panel
                {
                    Location = new Point(590, 76),
                    Size = new Size(215, 340),
                    BackColor = Color.FromArgb(205, 205, 205)
                };
                this.Controls.Add(pnlRight);

                txtGhiChu = AddField(pnlRight, "Ghi chú chi tiết", 10, 8, 195, n.GhiChu, true, 80);

                Label lblTT = new Label { Text = "Trạng thái hoạt động", Location = new Point(10, 112), AutoSize = true };
                cmbTrangThai = new ComboBox
                {
                    Location = new Point(10, 132),
                    Size = new Size(195, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(255, 245, 245)
                };
                cmbTrangThai.Items.AddRange(new object[] { "Đang hợp tác", "Ngừng hợp tác", "Bắt đầu hợp tác" });
                int idx = cmbTrangThai.Items.IndexOf(n.TrangThai);
                cmbTrangThai.SelectedIndex = idx >= 0 ? idx : 0;
                pnlRight.Controls.Add(lblTT);
                pnlRight.Controls.Add(cmbTrangThai);

                txtNguoiDD = AddField(pnlRight, "Người đại diện", 10, 176, 195, n.NguoiDaiDien);
                txtLienHe = AddField(pnlRight, "Thông tin liên hệ", 10, 240, 195, n.ThongTinLienHe);

                // ----- Nút Hủy / Thêm -----
                Button btnCancel = new Button
                {
                    Text = "Hủy",
                    Location = new Point(590, 440),
                    Size = new Size(100, 34),
                    BackColor = Color.FromArgb(225, 225, 225),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnCancel.Click += (s, e) => Cancelled?.Invoke();

                Button btnOk = new Button
                {
                    Text = okText,
                    Location = new Point(705, 440),
                    Size = new Size(100, 34),
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnOk.Click += (s, e) => XacNhan();

                this.Controls.Add(btnCancel);
                this.Controls.Add(btnOk);

                this.VisibleChanged += (s, e) => { if (this.Visible) txtTen.Focus(); };
            }

            private void XacNhan()
            {
                if (string.IsNullOrWhiteSpace(txtTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên nhà cung cấp.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTen.Focus();
                    return;
                }
                if (txtEmail.Text.Trim() != "" && !txtEmail.Text.Contains("@"))
                {
                    MessageBox.Show("Email không hợp lệ.", "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                Confirmed?.Invoke(new NhaCungCapInfo
                {
                    Ma = txtMa.Text.Trim(),
                    Ten = txtTen.Text.Trim(),
                    Sdt = txtSdt.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    MaSoThue = txtMst.Text.Trim(),
                    Website = txtWeb.Text.Trim(),
                    Fax = txtFax.Text.Trim(),
                    QuocGia = txtQuocGia.Text.Trim(),
                    TinhThanh = txtTinh.Text.Trim(),
                    QuanHuyen = txtQuan.Text.Trim(),
                    PhuongXa = txtPhuong.Text.Trim(),
                    DiaChi = txtDiaChi.Text.Trim(),
                    TrangThai = cmbTrangThai.Text,
                    GhiChu = txtGhiChu.Text.Trim(),
                    NguoiDaiDien = txtNguoiDD.Text.Trim(),
                    ThongTinLienHe = txtLienHe.Text.Trim()
                });
            }

            // Tạo 1 nhãn + 1 ô nhập
            private static TextBox AddField(Control parent, string label, int x, int y, int width, string value,
                                            bool multiline = false, int height = 28)
            {
                Label lbl = new Label { Text = label, Location = new Point(x, y), AutoSize = true };
                TextBox txt = new TextBox
                {
                    Location = new Point(x, y + 22),
                    Size = new Size(width, height),
                    Text = value,
                    Multiline = multiline,
                    BackColor = Color.FromArgb(255, 245, 245)
                };
                parent.Controls.Add(lbl);
                parent.Controls.Add(txt);
                return txt;
            }
        }
    }
}