using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace app_quanlykhoVLXD
{
    // ================= DỮ LIỆU DÙNG CHUNG (nhập hàng + đặt hàng nhập) =================
    public class DongHang
    {
        public string TenVT { get; set; } = "";
        public string MaVT { get; set; } = "";
        public string DonVi { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal DonGia { get; set; }
        public decimal SoLuong { get; set; } = 1;
        public decimal ThanhTien => DonGia * SoLuong;

        public override string ToString()
        {
            return TenVT + " (" + MaVT + ")" + (DonVi != "" ? " - " + DonVi : "");
        }
    }

    // Dữ liệu chuyển từ trang Nhập hàng sang trang Đặt hàng nhập
    public class DonNhapData
    {
        public string NhaPhanPhoi { get; set; } = "";
        public string GhiChu { get; set; } = "";
        public List<DongHang> Dong { get; set; } = new List<DongHang>();
    }

    // Các hàm truy cập database dùng chung
    internal static class KhoDb
    {
        public const string ConnStr = "Data Source=quan_ly_kho.db";

        public static SqliteConnection Open()
        {
            SqliteConnection conn = new SqliteConnection(ConnStr);
            conn.Open();
            return conn;
        }

        // Danh sách tên cột của 1 bảng (rỗng nếu bảng chưa tồn tại)
        public static HashSet<string> LayCot(SqliteConnection conn, string bang)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(" + bang + ")", conn))
            using (SqliteDataReader r = cmd.ExecuteReader())
            {
                while (r.Read()) set.Add(Convert.ToString(r["name"]) ?? "");
            }
            return set;
        }

        public static decimal ToDec(object? o)
        {
            if (o == null || o is DBNull) return 0;
            try { return Convert.ToDecimal(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        // Tìm sản phẩm trong bảng VatTu (theo tên / mã / barcode)
        public static List<DongHang> TimSanPham(string q, string loai)
        {
            List<DongHang> ds = new List<DongHang>();
            try
            {
                using (SqliteConnection conn = Open())
                {
                    HashSet<string> cot = LayCot(conn, "VatTu");
                    if (!cot.Contains("MaVT") || !cot.Contains("TenVT")) return ds;

                    string dvt = cot.Contains("DVT") ? "DVT" : "''";
                    string bar = cot.Contains("Barcode") ? "Barcode" : "''";
                    string gia = cot.Contains("DonGia") ? "DonGia" : "0";

                    string sql = "SELECT MaVT, TenVT, " + dvt + " AS DVT, " + bar + " AS Barcode, " + gia + " AS DonGia " +
                                 "FROM VatTu WHERE (TenVT LIKE @q OR MaVT LIKE @q" +
                                 (cot.Contains("Barcode") ? " OR Barcode LIKE @q" : "") + ")";

                    bool locLoai = loai != "" && cot.Contains("Loai");
                    if (locLoai) sql += " AND Loai=@loai";
                    sql += " LIMIT 20";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@q", "%" + q + "%");
                        if (locLoai) cmd.Parameters.AddWithValue("@loai", loai);

                        using (SqliteDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                ds.Add(new DongHang
                                {
                                    MaVT = Convert.ToString(r["MaVT"]) ?? "",
                                    TenVT = Convert.ToString(r["TenVT"]) ?? "",
                                    DonVi = Convert.ToString(r["DVT"]) ?? "",
                                    Barcode = Convert.ToString(r["Barcode"]) ?? "",
                                    DonGia = ToDec(r["DonGia"]),
                                    SoLuong = 1
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Chưa có bảng VatTu -> không có gợi ý, người dùng vẫn nhập tay được
            }
            return ds;
        }

        public static List<string> LayNhaCungCap()
        {
            List<string> ds = new List<string>();
            try
            {
                using (SqliteConnection conn = Open())
                {
                    if (!LayCot(conn, "NhaCungCap").Contains("TenNCC")) return ds;
                    using (SqliteCommand cmd = new SqliteCommand("SELECT TenNCC FROM NhaCungCap ORDER BY TenNCC", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) ds.Add(Convert.ToString(r[0]) ?? "");
                    }
                }
            }
            catch { }
            return ds;
        }

        public static List<string> LayLoai()
        {
            List<string> ds = new List<string>();
            try
            {
                using (SqliteConnection conn = Open())
                {
                    if (!LayCot(conn, "VatTu").Contains("Loai")) return ds;
                    using (SqliteCommand cmd = new SqliteCommand(
                        "SELECT DISTINCT Loai FROM VatTu WHERE Loai IS NOT NULL AND Loai <> '' ORDER BY Loai", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) ds.Add(Convert.ToString(r[0]) ?? "");
                    }
                }
            }
            catch { }
            return ds;
        }
    }

    // ================= TRANG NHẬP HÀNG =================
    public partial class nhaphang : Form
    {
        private static readonly CultureInfo vi = new CultureInfo("vi-VN");

        // Bấm "Tạo đơn đặt hàng" -> Form1 sẽ chuyển sang trang Đặt hàng nhập
        public event Action<DonNhapData>? TaoDonDatHang;

        private readonly BindingList<DongHang> dong = new BindingList<DongHang>();

        private Panel pnlCenter = null!;
        private DataGridView dgv = null!;
        private TextBox txtTimKiem = null!;
        private ComboBox cmbLoai = null!;
        private ListBox lstGoiY = null!;

        private TextBox txtMaLo = null!;
        private ComboBox cmbNCC = null!;
        private DateTimePicker dtpNgay = null!;
        private TextBox txtSoLuong = null!;
        private ComboBox cmbTrangThai = null!;

        private CheckBox chkThanhToan = null!;
        private Label lblThanhTien = null!;
        private TextBox txtGhiChu = null!;

        public nhaphang()
        {
            BuildUI();
            EnsureTables();
            NapNhaCungCap();
            NapLoai();
            DatLaiForm();
        }

        // ================= GIAO DIỆN =================
        private void BuildUI()
        {
            this.Text = "Nhập hàng";
            this.BackColor = Color.FromArgb(185, 192, 203);
            this.Font = new Font("Segoe UI", 9.5F);

            Panel pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // ---------- Thanh công cụ ----------
            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 48 };

            Button btnTab = new Button
            {
                Text = "Sản phẩm",
                Location = new Point(0, 6),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(225, 225, 225),
                FlatStyle = FlatStyle.Flat
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
            btnHuy.Click += (s, e) => HuyPhieu();

            Button btnTaoDuyet = TaoNut("Tạo & duyệt đơn hàng", 170);
            btnTaoDuyet.Click += (s, e) => TaoVaDuyet();

            Button btnTaoDonDat = TaoNut("Tạo đơn đặt hàng", 150);
            btnTaoDonDat.Click += (s, e) => ChuyenSangDatHang();

            flow.Controls.Add(btnHuy);
            flow.Controls.Add(btnTaoDuyet);
            flow.Controls.Add(btnTaoDonDat);

            pnlToolbar.Controls.Add(btnTab);
            pnlToolbar.Controls.Add(flow);

            // ---------- Khung chi tiết bên phải ----------
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 270,
                BackColor = Color.FromArgb(245, 240, 240)
            };

            txtMaLo = new TextBox { BackColor = Color.FromArgb(255, 245, 245) };
            AddRow(pnlRight, "Mã lô hàng", txtMaLo, 12);

            cmbNCC = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, BackColor = Color.FromArgb(255, 245, 245) };
            AddRow(pnlRight, "Nhà phân phối", cmbNCC, 92);

            dtpNgay = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            AddRow(pnlRight, "Ngày nhập hàng", dtpNgay, 172);

            txtSoLuong = new TextBox { ReadOnly = true, BackColor = Color.FromArgb(255, 245, 245) };
            AddRow(pnlRight, "Số lượng", txtSoLuong, 252);

            cmbTrangThai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(255, 245, 245) };
            cmbTrangThai.Items.AddRange(new object[] { "Đã nhận hàng", "Chờ nhận hàng" });
            AddRow(pnlRight, "Trạng thái", cmbTrangThai, 332);

            // ---------- Khung giữa ----------
            pnlCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 0) };

            // Bảng sản phẩm
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

            AddCol("TenVT", "Tên sản phẩm", 28, false, null);
            AddCol("MaVT", "Mã sản phẩm", 14, false, null);
            AddCol("DonVi", "Đơn vị", 11, false, null);
            AddCol("Barcode", "Barcode", 17, false, null);
            AddCol("DonGia", "Đơn giá", 14, false, "N0");
            AddCol("SoLuong", "Số lượng", 10, false, "0.##");
            AddCol("ThanhTien", "Thành tiền", 16, true, "N0");

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

            // Hàng tìm kiếm phía trên bảng
            TableLayoutPanel header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(210, 210, 210)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label lblSp = new Label
            {
                Text = "SẢN PHẨM",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(12, 0, 10, 0)
            };

            txtTimKiem = new TextBox
            {
                PlaceholderText = "TÌM KIẾM (tên, mã hoặc barcode)",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 10, 0)
            };

            cmbLoai = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 10, 0)
            };

            header.Controls.Add(lblSp, 0, 0);
            header.Controls.Add(txtTimKiem, 1, 0);
            header.Controls.Add(cmbLoai, 2, 0);

            // Phần dưới: Thanh toán + Ghi chú
            TableLayoutPanel bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 140,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel pnlThanhToan = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(205, 205, 205),
                Margin = new Padding(0, 0, 6, 0)
            };
            Label lblTT = new Label
            {
                Text = "Thanh toán cho nhà cung cấp",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 8),
                AutoSize = true
            };
            chkThanhToan = new CheckBox { Text = "Đã thanh toán", Location = new Point(14, 36), AutoSize = true };
            lblThanhTien = new Label
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(14, 72),
                AutoSize = true
            };
            pnlThanhToan.Controls.Add(lblTT);
            pnlThanhToan.Controls.Add(chkThanhToan);
            pnlThanhToan.Controls.Add(lblThanhTien);

            Panel pnlGhiChu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(205, 205, 205),
                Margin = new Padding(6, 0, 0, 0),
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

            bottom.Controls.Add(pnlThanhToan, 0, 0);
            bottom.Controls.Add(pnlGhiChu, 1, 0);

            // Danh sách gợi ý khi gõ tìm kiếm (nổi lên trên bảng)
            lstGoiY = new ListBox
            {
                Visible = false,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstGoiY.MouseClick += (s, e) => ChonGoiY();
            lstGoiY.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { ChonGoiY(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { lstGoiY.Visible = false; txtTimKiem.Focus(); }
            };
            lstGoiY.Leave += (s, e) => lstGoiY.Visible = false;

            txtTimKiem.TextChanged += (s, e) => CapNhatGoiY();
            txtTimKiem.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ThemTuOTimKiem();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Down && lstGoiY.Visible && lstGoiY.Items.Count > 0)
                {
                    lstGoiY.Focus();
                    lstGoiY.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    lstGoiY.Visible = false;
                }
            };
            txtTimKiem.Leave += (s, e) => BeginInvoke(new Action(() =>
            {
                if (!lstGoiY.Focused && !txtTimKiem.Focused) lstGoiY.Visible = false;
            }));

            // Ghép: Fill trước, rồi Bottom, rồi Top
            pnlCenter.Controls.Add(dgv);
            pnlCenter.Controls.Add(bottom);
            pnlCenter.Controls.Add(header);
            pnlCenter.Controls.Add(lstGoiY);

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

        // ================= TÌM / THÊM SẢN PHẨM =================
        private string LoaiDangChon()
        {
            return cmbLoai.SelectedIndex > 0 ? cmbLoai.Text : "";
        }

        private void CapNhatGoiY()
        {
            string q = txtTimKiem.Text.Trim();
            if (q.Length == 0) { lstGoiY.Visible = false; return; }

            List<DongHang> kq = KhoDb.TimSanPham(q, LoaiDangChon());

            lstGoiY.Items.Clear();
            foreach (DongHang d in kq) lstGoiY.Items.Add(d);

            if (kq.Count == 0) { lstGoiY.Visible = false; return; }

            Point p = pnlCenter.PointToClient(
                txtTimKiem.Parent!.PointToScreen(new Point(txtTimKiem.Left, txtTimKiem.Bottom)));
            lstGoiY.Location = p;
            lstGoiY.Width = txtTimKiem.Width;
            lstGoiY.Height = Math.Min(kq.Count * lstGoiY.ItemHeight + 4, 200);
            lstGoiY.Visible = true;
            lstGoiY.BringToFront();
        }

        private void ChonGoiY()
        {
            if (lstGoiY.SelectedItem is DongHang d) ThemDong(d);
        }

        // Nhấn Enter ở ô tìm kiếm: thêm kết quả đầu tiên, không có thì thêm 1 dòng trống mang tên đã gõ
        private void ThemTuOTimKiem()
        {
            string q = txtTimKiem.Text.Trim();
            if (q.Length == 0) return;

            List<DongHang> kq = KhoDb.TimSanPham(q, LoaiDangChon());
            if (kq.Count > 0) ThemDong(kq[0]);
            else ThemDong(new DongHang { TenVT = q });
        }

        private void ThemDong(DongHang d)
        {
            DongHang? daCo = null;
            if (d.MaVT != "")
            {
                foreach (DongHang x in dong)
                {
                    if (x.MaVT == d.MaVT) { daCo = x; break; }
                }
            }

            if (daCo != null)
            {
                daCo.SoLuong += 1;   // sản phẩm đã có thì cộng thêm 1
            }
            else
            {
                dong.Add(new DongHang
                {
                    TenVT = d.TenVT,
                    MaVT = d.MaVT,
                    DonVi = d.DonVi,
                    Barcode = d.Barcode,
                    DonGia = d.DonGia,
                    SoLuong = 1
                });
            }

            lstGoiY.Visible = false;
            txtTimKiem.Clear();
            txtTimKiem.Focus();
            dgv.Refresh();
            CapNhatTong();
        }

        private void CapNhatTong()
        {
            decimal tien = 0, sl = 0;
            foreach (DongHang d in dong)
            {
                tien += d.ThanhTien;
                sl += d.SoLuong;
            }
            txtSoLuong.Text = sl.ToString("0.##", vi);
            lblThanhTien.Text = "Thành tiền: " + tien.ToString("N0", vi) + " VND";
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
                        @"CREATE TABLE IF NOT EXISTS PhieuNhap (
                            MaPhieu     INTEGER PRIMARY KEY AUTOINCREMENT,
                            MaLo        TEXT,
                            NhaPhanPhoi TEXT,
                            NgayNhap    TEXT,
                            TrangThai   TEXT,
                            DaThanhToan INTEGER,
                            TongTien    REAL,
                            GhiChu      TEXT)",
                        @"CREATE TABLE IF NOT EXISTS ChiTietNhap (
                            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                            MaPhieu  INTEGER,
                            MaVT     TEXT,
                            TenVT    TEXT,
                            DonVi    TEXT,
                            Barcode  TEXT,
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
                MessageBox.Show("Không tạo được bảng phiếu nhập:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NapNhaCungCap()
        {
            cmbNCC.Items.Clear();
            cmbNCC.Items.AddRange(KhoDb.LayNhaCungCap().ToArray());
        }

        private void NapLoai()
        {
            cmbLoai.Items.Clear();
            cmbLoai.Items.Add("Loại sản phẩm");
            foreach (string loai in KhoDb.LayLoai()) cmbLoai.Items.Add(loai);
            cmbLoai.SelectedIndex = 0;
        }

        // ================= CÁC NÚT =================
        private static string TaoMaLo()
        {
            return "LH" + DateTime.Now.ToString("yyMMdd-HHmmss");
        }

        private void DatLaiForm()
        {
            dong.Clear();
            txtMaLo.Text = TaoMaLo();
            cmbNCC.Text = "";
            dtpNgay.Value = DateTime.Now;
            cmbTrangThai.SelectedIndex = 0;
            chkThanhToan.Checked = false;
            txtGhiChu.Clear();
            txtTimKiem.Clear();
            CapNhatTong();
        }

        private void HuyPhieu()
        {
            if (dong.Count > 0 &&
                MessageBox.Show("Hủy phiếu nhập đang soạn?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            DatLaiForm();
        }

        private void TaoVaDuyet()
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
                MessageBox.Show("Chưa có sản phẩm nào trong phiếu nhập.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTimKiem.Focus();
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

            if (string.IsNullOrWhiteSpace(txtMaLo.Text)) txtMaLo.Text = TaoMaLo();

            decimal tong = 0;
            foreach (DongHang d in dong) tong += d.ThanhTien;

            try
            {
                using (SqliteConnection conn = KhoDb.Open())
                {
                    // Chỉ cộng tồn kho khi đã nhận hàng và bảng VatTu có cột SoLuongTon
                    bool capNhatKho = cmbTrangThai.Text == "Đã nhận hàng";
                    if (capNhatKho)
                    {
                        HashSet<string> cot = KhoDb.LayCot(conn, "VatTu");
                        capNhatKho = cot.Contains("MaVT") && cot.Contains("SoLuongTon");
                    }

                    using (SqliteTransaction tran = conn.BeginTransaction())
                    {
                        long maPhieu;
                        using (SqliteCommand cmd = new SqliteCommand(
                            @"INSERT INTO PhieuNhap (MaLo, NhaPhanPhoi, NgayNhap, TrangThai, DaThanhToan, TongTien, GhiChu)
                              VALUES (@lo, @ncc, @ngay, @tt, @ttoan, @tien, @gc);
                              SELECT last_insert_rowid();", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@lo", txtMaLo.Text.Trim());
                            cmd.Parameters.AddWithValue("@ncc", cmbNCC.Text.Trim());
                            cmd.Parameters.AddWithValue("@ngay", dtpNgay.Value.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@tt", cmbTrangThai.Text);
                            cmd.Parameters.AddWithValue("@ttoan", chkThanhToan.Checked ? 1 : 0);
                            cmd.Parameters.AddWithValue("@tien", (double)tong);
                            cmd.Parameters.AddWithValue("@gc", txtGhiChu.Text.Trim());
                            maPhieu = Convert.ToInt64(cmd.ExecuteScalar());
                        }

                        foreach (DongHang d in dong)
                        {
                            using (SqliteCommand cmd = new SqliteCommand(
                                @"INSERT INTO ChiTietNhap (MaPhieu, MaVT, TenVT, DonVi, Barcode, DonGia, SoLuong)
                                  VALUES (@p, @ma, @ten, @dv, @bc, @gia, @sl)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@p", maPhieu);
                                cmd.Parameters.AddWithValue("@ma", d.MaVT);
                                cmd.Parameters.AddWithValue("@ten", d.TenVT);
                                cmd.Parameters.AddWithValue("@dv", d.DonVi);
                                cmd.Parameters.AddWithValue("@bc", d.Barcode);
                                cmd.Parameters.AddWithValue("@gia", (double)d.DonGia);
                                cmd.Parameters.AddWithValue("@sl", (double)d.SoLuong);
                                cmd.ExecuteNonQuery();
                            }

                            if (capNhatKho && d.MaVT != "")
                            {
                                using (SqliteCommand cmd = new SqliteCommand(
                                    "UPDATE VatTu SET SoLuongTon = COALESCE(SoLuongTon, 0) + @sl WHERE MaVT = @ma", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@sl", (double)d.SoLuong);
                                    cmd.Parameters.AddWithValue("@ma", d.MaVT);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();
                    }
                }

                MessageBox.Show("Đã tạo và duyệt phiếu nhập " + txtMaLo.Text.Trim() + ".", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DatLaiForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được phiếu nhập:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // "Tạo đơn đặt hàng": chuyển sang trang Đặt hàng nhập, mang theo nhà phân phối, ghi chú và danh sách hàng
        private void ChuyenSangDatHang()
        {
            dgv.EndEdit();

            DonNhapData data = new DonNhapData
            {
                NhaPhanPhoi = cmbNCC.Text.Trim(),
                GhiChu = txtGhiChu.Text.Trim()
            };
            foreach (DongHang d in dong)
            {
                data.Dong.Add(new DongHang
                {
                    TenVT = d.TenVT,
                    MaVT = d.MaVT,
                    DonVi = d.DonVi,
                    Barcode = d.Barcode,
                    DonGia = d.DonGia,
                    SoLuong = d.SoLuong
                });
            }

            if (TaoDonDatHang == null)
            {
                MessageBox.Show("Chưa gắn trang Đặt hàng nhập trong Form1.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            TaoDonDatHang.Invoke(data);
        }
    }
}