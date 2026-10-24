using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace app_quanlykhoVLXD
{
    // Trang "Đặt hàng nhập" - nhận dữ liệu từ trang Nhập hàng (nếu có) để tạo đơn đặt hàng
    public partial class Dathangnhap : Form
    {
        private static readonly CultureInfo vi = new CultureInfo("vi-VN");

        private readonly BindingList<DongHang> dong = new BindingList<DongHang>();

        private DataGridView dgv = null!;
        private TextBox txtMaDon = null!;
        private ComboBox cmbNCC = null!;
        private DateTimePicker dtpNgayDat = null!;
        private DateTimePicker dtpDuKien = null!;
        private ComboBox cmbTrangThai = null!;
        private TextBox txtGhiChu = null!;
        private Label lblTong = null!;

        public Dathangnhap() : this(null)
        {
        }

        public Dathangnhap(DonNhapData? data)
        {
            BuildUI();
            EnsureTables();
            NapNhaCungCap();
            DatLaiForm();

            // Có dữ liệu từ trang Nhập hàng thì điền sẵn
            if (data != null)
            {
                cmbNCC.Text = data.NhaPhanPhoi;
                txtGhiChu.Text = data.GhiChu;
                foreach (DongHang d in data.Dong) dong.Add(d);
                CapNhatTong();
            }
        }

        // ================= GIAO DIỆN =================
        private void BuildUI()
        {
            this.Text = "Đặt hàng nhập";
            this.BackColor = Color.FromArgb(185, 192, 203);
            this.Font = new Font("Segoe UI", 9.5F);

            Panel pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // ---------- Thanh công cụ ----------
            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

            Button btnTab = new Button
            {
                Text = "Đơn đặt hàng nhập",
                Location = new Point(0, 6),
                Size = new Size(170, 34),
                BackColor = Color.FromArgb(225, 225, 225),
                FlatStyle = FlatStyle.Flat
            };

            Button btnThemDong = TaoNut("+ Thêm dòng", 120);
            btnThemDong.Location = new Point(180, 6);
            btnThemDong.Margin = new Padding(0);
            btnThemDong.Click += (s, e) =>
            {
                dong.Add(new DongHang());
                dgv.Focus();
            };

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.Transparent
            };

            Button btnHuy = TaoNut("Hủy", 80);
            btnHuy.Click += (s, e) => HuyDon();

            Button btnLuu = TaoNut("Lưu đơn đặt hàng", 160);
            btnLuu.Click += (s, e) => LuuDon();

            flow.Controls.Add(btnHuy);
            flow.Controls.Add(btnLuu);

            pnlToolbar.Controls.Add(btnTab);
            pnlToolbar.Controls.Add(btnThemDong);
            pnlToolbar.Controls.Add(flow);

            // ---------- Khung chi tiết bên phải ----------
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 270,
                BackColor = Color.FromArgb(245, 240, 240)
            };

            txtMaDon = new TextBox { BackColor = Color.FromArgb(255, 245, 245) };
            AddRow(pnlRight, "Mã đơn đặt hàng", txtMaDon, 12);

            cmbNCC = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, BackColor = Color.FromArgb(255, 245, 245) };
            AddRow(pnlRight, "Nhà phân phối", cmbNCC, 92);

            dtpNgayDat = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            AddRow(pnlRight, "Ngày đặt hàng", dtpNgayDat, 172);

            dtpDuKien = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            AddRow(pnlRight, "Ngày dự kiến nhận", dtpDuKien, 252);

            cmbTrangThai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(255, 245, 245) };
            cmbTrangThai.Items.AddRange(new object[] { "Chờ xác nhận", "Đã đặt hàng", "Đã nhận hàng", "Đã hủy" });
            AddRow(pnlRight, "Trạng thái", cmbTrangThai, 332);

            // ---------- Khung giữa ----------
            Panel pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 0) };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.RowTemplate.Height = 32;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(165, 165, 165);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(180, 180, 180);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(110, 120, 140);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            AddCol("TenVT", "Tên sản phẩm", 30, false, null);
            AddCol("MaVT", "Mã sản phẩm", 14, false, null);
            AddCol("DonVi", "Đơn vị", 11, false, null);
            AddCol("DonGia", "Đơn giá", 15, false, "N0");
            AddCol("SoLuong", "Số lượng", 11, false, "0.##");
            AddCol("ThanhTien", "Thành tiền", 17, true, "N0");

            dgv.DataSource = dong;
            dgv.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                CapNhatTong();
                dgv.InvalidateRow(e.RowIndex);
            };
            dgv.DataError += (s, e) =>
            {
                e.ThrowException = false;
                MessageBox.Show("Giá trị không hợp lệ. Đơn giá và số lượng phải là số.", "Sai định dạng",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            dong.ListChanged += (s, e) => CapNhatTong();

            // Phần dưới: Ghi chú + Tổng tiền
            TableLayoutPanel bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 140,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel pnlGhiChu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(205, 205, 205),
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8)
            };
            txtGhiChu = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            Label lblGhiChu = new Label
            {
                Text = "Ghi chú",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            pnlGhiChu.Controls.Add(txtGhiChu);
            pnlGhiChu.Controls.Add(lblGhiChu);

            Panel pnlTong = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(205, 205, 205),
                Margin = new Padding(6, 0, 0, 0)
            };
            Label lblTitleTong = new Label
            {
                Text = "Tổng giá trị đơn",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 8),
                AutoSize = true
            };
            lblTong = new Label
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(12, 50),
                AutoSize = true
            };
            pnlTong.Controls.Add(lblTitleTong);
            pnlTong.Controls.Add(lblTong);

            bottom.Controls.Add(pnlGhiChu, 0, 0);
            bottom.Controls.Add(pnlTong, 1, 0);

            pnlCenter.Controls.Add(dgv);
            pnlCenter.Controls.Add(bottom);

            pnlContent.Controls.Add(pnlCenter);
            pnlContent.Controls.Add(pnlRight);
            pnlContent.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlContent);
        }

        private Button TaoNut(string text, int width)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 34),
                Margin = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(150, 158, 168);
            return btn;
        }

        private static void AddRow(Panel parent, string label, Control c, int y)
        {
            Label lbl = new Label { Text = label, Location = new Point(12, y), AutoSize = true };
            c.Location = new Point(12, y + 22);
            c.Width = 245;
            parent.Controls.Add(lbl);
            parent.Controls.Add(c);
        }

        private void AddCol(string prop, string header, float weight, bool readOnly, string? format)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                Name = prop,
                HeaderText = header,
                FillWeight = weight,
                ReadOnly = readOnly
            };
            if (format != null)
            {
                col.DefaultCellStyle.Format = format;
                col.DefaultCellStyle.FormatProvider = vi;
            }
            dgv.Columns.Add(col);
        }

        // ================= DATABASE =================
        private void EnsureTables()
        {
            try
            {
                using (SqliteConnection conn = KhoDb.Open())
                {
                    string[] sqls =
                    {
                        @"CREATE TABLE IF NOT EXISTS DonDatHang (
                            MaDon       INTEGER PRIMARY KEY AUTOINCREMENT,
                            MaDonDH     TEXT,
                            NhaPhanPhoi TEXT,
                            NgayDat     TEXT,
                            NgayDuKien  TEXT,
                            TrangThai   TEXT,
                            TongTien    REAL,
                            GhiChu      TEXT)",
                        @"CREATE TABLE IF NOT EXISTS ChiTietDatHang (
                            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                            MaDon    INTEGER,
                            MaVT     TEXT,
                            TenVT    TEXT,
                            DonVi    TEXT,
                            DonGia   REAL,
                            SoLuong  REAL)"
                    };
                    foreach (string sql in sqls)
                    {
                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                            cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tạo được bảng đơn đặt hàng:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NapNhaCungCap()
        {
            cmbNCC.Items.Clear();
            cmbNCC.Items.AddRange(KhoDb.LayNhaCungCap().ToArray());
        }

        // ================= CÁC NÚT =================
        private static string TaoMaDon()
        {
            return "DH" + DateTime.Now.ToString("yyMMdd-HHmmss");
        }

        private void DatLaiForm()
        {
            dong.Clear();
            txtMaDon.Text = TaoMaDon();
            cmbNCC.Text = "";
            dtpNgayDat.Value = DateTime.Now;
            dtpDuKien.Value = DateTime.Now.AddDays(7);
            cmbTrangThai.SelectedIndex = 0;
            txtGhiChu.Clear();
            CapNhatTong();
        }

        private void CapNhatTong()
        {
            decimal tien = 0;
            foreach (DongHang d in dong) tien += d.ThanhTien;
            lblTong.Text = tien.ToString("N0", vi) + " VND";
        }

        private void HuyDon()
        {
            if (dong.Count > 0 &&
                MessageBox.Show("Hủy đơn đặt hàng đang soạn?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            DatLaiForm();
        }

        private void LuuDon()
        {
            dgv.EndEdit();

            if (string.IsNullOrWhiteSpace(cmbNCC.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập nhà phân phối.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbNCC.Focus();
                return;
            }
            if (dong.Count == 0)
            {
                MessageBox.Show("Đơn đặt hàng chưa có sản phẩm nào.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (DongHang d in dong)
            {
                if (d.SoLuong <= 0 || string.IsNullOrWhiteSpace(d.TenVT))
                {
                    MessageBox.Show("Mỗi dòng cần có tên sản phẩm và số lượng lớn hơn 0.", "Dữ liệu chưa hợp lệ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(txtMaDon.Text)) txtMaDon.Text = TaoMaDon();

            decimal tong = 0;
            foreach (DongHang d in dong) tong += d.ThanhTien;

            try
            {
                using (SqliteConnection conn = KhoDb.Open())
                using (SqliteTransaction tran = conn.BeginTransaction())
                {
                    long maDon;
                    using (SqliteCommand cmd = new SqliteCommand(
                        @"INSERT INTO DonDatHang (MaDonDH, NhaPhanPhoi, NgayDat, NgayDuKien, TrangThai, TongTien, GhiChu)
                          VALUES (@ma, @ncc, @ngay, @dukien, @tt, @tien, @gc);
                          SELECT last_insert_rowid();", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@ma", txtMaDon.Text.Trim());
                        cmd.Parameters.AddWithValue("@ncc", cmbNCC.Text.Trim());
                        cmd.Parameters.AddWithValue("@ngay", dtpNgayDat.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@dukien", dtpDuKien.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@tt", cmbTrangThai.Text);
                        cmd.Parameters.AddWithValue("@tien", (double)tong);
                        cmd.Parameters.AddWithValue("@gc", txtGhiChu.Text.Trim());
                        maDon = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    foreach (DongHang d in dong)
                    {
                        using (SqliteCommand cmd = new SqliteCommand(
                            @"INSERT INTO ChiTietDatHang (MaDon, MaVT, TenVT, DonVi, DonGia, SoLuong)
                              VALUES (@don, @ma, @ten, @dv, @gia, @sl)", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@don", maDon);
                            cmd.Parameters.AddWithValue("@ma", d.MaVT);
                            cmd.Parameters.AddWithValue("@ten", d.TenVT);
                            cmd.Parameters.AddWithValue("@dv", d.DonVi);
                            cmd.Parameters.AddWithValue("@gia", (double)d.DonGia);
                            cmd.Parameters.AddWithValue("@sl", (double)d.SoLuong);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }

                MessageBox.Show("Đã lưu đơn đặt hàng " + txtMaDon.Text.Trim() + ".", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DatLaiForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được đơn đặt hàng:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}