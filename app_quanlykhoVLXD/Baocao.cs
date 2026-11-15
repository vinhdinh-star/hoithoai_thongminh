using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace app_quanlykhoVLXD
{
    // Trang "Báo cáo": hàng đã bán / đã xuất khỏi kho (nhúng vào pnlMainContent của Form1).
    // Đọc từ bảng PhieuXuat + ChiTietPhieuXuat, lấy tên vật tư từ bảng VatTu.
    public partial class baocao : Form
    {
        private const string ConnStr = "Data Source=quan_ly_kho.db";
        private static readonly CultureInfo vi = new CultureInfo("vi-VN");

        private static readonly Color MauTim = Color.FromArgb(108, 92, 231);
        private static readonly Color MauXanh = Color.FromArgb(9, 132, 227);
        private static readonly Color MauLuc = Color.FromArgb(0, 184, 148);
        private static readonly Color MauCam = Color.FromArgb(253, 150, 68);
        private static readonly Color MauChu = Color.FromArgb(45, 52, 54);

        private DateTimePicker dtpTu = null!;
        private DateTimePicker dtpDen = null!;
        private ComboBox cmbNhanh = null!;
        private ComboBox cmbChiSo = null!;
        private Button btnXem = null!;
        private Button btnCsv = null!;
        private Button btnMau = null!;
        private Label lblSoPhieu = null!;
        private Label lblSoLuong = null!;
        private Label lblGiaTri = null!;
        private Label lblTieuDeChart = null!;
        private BarChart chart = null!;
        private DataGridView dgv = null!;

        private DataTable dtChiTiet = new DataTable();
        private bool dangDatNgay;      // đang gán ngày bằng code (không coi là người dùng tự chọn)
        private bool chuaCoBang;       // chưa có bảng phiếu xuất trong database

        public baocao()
        {
            BuildUI();

            dangDatNgay = true;            // tránh việc đặt giá trị mặc định kích hoạt tải báo cáo nhiều lần
            cmbNhanh.SelectedIndex = 2;    // mặc định: 30 ngày qua
            cmbChiSo.SelectedIndex = 0;    // mặc định: theo số lượng
            dangDatNgay = false;

            DatNhanh(2);
            TaiBaoCao();
        }

        // ================= GIAO DIỆN =================
        private void BuildUI()
        {
            this.Text = "Báo cáo";
            this.BackColor = Color.FromArgb(236, 239, 244);
            this.Font = new Font("Segoe UI", 9.5F);

            // Bật cuộn chuột cho toàn bộ form báo cáo
            this.AutoScroll = true;

            // Panel gốc chứa toàn bộ nội dung, đặt độ rộng tối thiểu để không bị bóp méo
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Top,
                Height = 900, // Đủ chiều cao để chứa tất cả các thành phần và có không gian cuộn
                Padding = new Padding(16),
                BackColor = Color.FromArgb(236, 239, 244)


            };

            // ---------- Thanh trên: tiêu đề + bộ lọc ----------
            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55 }; // Giảm chiều cao thanh toolbar

            Label lblTitle = new Label
            {
                Text = "BÁO CÁO",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = MauChu,
                AutoSize = true,
                Location = new Point(0, 0) // Kéo sát lên đỉnh trên cùng của toolbar
            };
            Label lblSub = new Label
            {
                Text = "Hàng đã bán và đã ra khỏi kho",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(2, 40) // Đặt sát ngay dưới tiêu đề
            };

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 2, 0, 0), // Kéo các nút bộ lọc dịch lên trên
                BackColor = Color.Transparent
            };

            dtpTu = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 105, Margin = new Padding(4, 3, 4, 0) };
            dtpDen = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 105, Margin = new Padding(4, 3, 4, 0) };
            dtpTu.ValueChanged += (s, e) => KhiNguoiDungDoiNgay();
            dtpDen.ValueChanged += (s, e) => KhiNguoiDungDoiNgay();

            cmbNhanh = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Margin = new Padding(8, 3, 4, 0) };
            cmbNhanh.Items.AddRange(new object[] { "Hôm nay", "7 ngày qua", "30 ngày qua", "Tháng này", "Tháng trước", "Năm nay", "Tất cả", "Tùy chọn" });
            cmbNhanh.SelectedIndexChanged += (s, e) =>
            {
                if (dangDatNgay || cmbNhanh.SelectedIndex == 7) return;
                DatNhanh(cmbNhanh.SelectedIndex);
                TaiBaoCao();
            };

            btnXem = TaoNut("Xem báo cáo", 112, true);
            btnXem.Click += (s, e) => TaiBaoCao();

            btnCsv = TaoNut("Xuất CSV", 90, false);
            btnCsv.Click += (s, e) => XuatCsv();

            btnMau = TaoNut("Tạo dữ liệu mẫu", 130, false);
            btnMau.Visible = false;
            btnMau.Click += (s, e) => TaoDuLieuMau();

            flow.Controls.Add(NhanNho("Từ"));
            flow.Controls.Add(dtpTu);
            flow.Controls.Add(NhanNho("đến"));
            flow.Controls.Add(dtpDen);
            flow.Controls.Add(cmbNhanh);
            flow.Controls.Add(btnXem);
            flow.Controls.Add(btnCsv);
            flow.Controls.Add(btnMau);

            pnlToolbar.Controls.Add(lblTitle);
            pnlToolbar.Controls.Add(lblSub);
            pnlToolbar.Controls.Add(flow);

            // ---------- 3 thẻ số liệu tổng ----------
            TableLayoutPanel cards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 3,
                RowCount = 1
            };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel c1 = TaoThe("Số phiếu xuất", MauXanh, out lblSoPhieu);
            Panel c2 = TaoThe("Tổng số lượng đã xuất", MauLuc, out lblSoLuong);
            Panel c3 = TaoThe("Tổng giá trị đã xuất", MauCam, out lblGiaTri);
            c3.Margin = new Padding(0);
            cards.Controls.Add(c1, 0, 0);
            cards.Controls.Add(c2, 1, 0);
            cards.Controls.Add(c3, 2, 0);

            // ---------- Khung biểu đồ ----------
            Panel chartWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };

            Panel chartHeader = new Panel { Dock = DockStyle.Top, Height = 34 };
            lblTieuDeChart = new Label
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = MauChu,
                AutoSize = true,
                Location = new Point(0, 5)
            };
            cmbChiSo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            cmbChiSo.Items.AddRange(new object[] { "Theo số lượng", "Theo giá trị (VND)" });
            cmbChiSo.SelectedIndexChanged += (s, e) => CapNhatChart();
            chartHeader.Controls.Add(lblTieuDeChart);
            chartHeader.Controls.Add(cmbChiSo);
            chartHeader.Resize += (s, e) => cmbChiSo.Location = new Point(Math.Max(0, chartHeader.Width - cmbChiSo.Width), 3);

            chart = new BarChart { Dock = DockStyle.Fill };

            chartWrap.Controls.Add(chart);
            chartWrap.Controls.Add(chartHeader);

            // ---------- Bảng chi tiết ----------
            dgv = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 280, // Cố định chiều cao bảng để người dùng lăn chuột xem gọn gàng
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 238, 243),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            dgv.RowTemplate.Height = 30;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MauChu;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 246, 250);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 222, 250);
            dgv.DefaultCellStyle.SelectionForeColor = MauChu;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);

            ThemCot("NgayXuat", "Ngày xuất", 11, null, false);
            ThemCot("MaPX", "Mã phiếu", 11, null, false);
            ThemCot("NguoiNhan", "Người nhận", 18, null, false);
            ThemCot("TenVT", "Vật tư", 22, null, false);
            ThemCot("DonVi", "Đơn vị", 8, null, false);
            ThemCot("SoLuong", "Số lượng", 9, "#,##0.##", true);
            ThemCot("DonGia", "Đơn giá", 11, "N0", true);
            ThemCot("ThanhTien", "Thành tiền", 12, "N0", true);

            // Thêm khoảng cách giữa các phần tử cho thoáng giao diện
            Panel spacer3 = new Panel { Dock = DockStyle.Top, Height = 12 };
            Panel spacer2 = new Panel { Dock = DockStyle.Top, Height = 12 };
            Panel spacer1 = new Panel { Dock = DockStyle.Top, Height = 12 };

            // Thứ tự add vào pnlContent (từ dưới lên trên do cơ chế Dock.Top)
            pnlContent.Controls.Add(dgv);
            pnlContent.Controls.Add(spacer3);
            pnlContent.Controls.Add(chartWrap);
            pnlContent.Controls.Add(spacer2);
            pnlContent.Controls.Add(cards);
            pnlContent.Controls.Add(spacer1);
            pnlContent.Controls.Add(pnlToolbar);

            this.Controls.Add(pnlContent);
        }

        private Button TaoNut(string text, int width, bool noiBat)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                BackColor = noiBat ? MauTim : Color.White,
                ForeColor = noiBat ? Color.White : MauChu,
                Font = new Font("Segoe UI", 9.5F, noiBat ? FontStyle.Bold : FontStyle.Regular)
            };
            btn.FlatAppearance.BorderColor = noiBat ? MauTim : Color.FromArgb(190, 196, 206);
            return btn;
        }

        private Label NhanNho(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(6, 7, 0, 0)
            };
        }

        private Panel TaoThe(string tieuDe, Color mau, out Label lblSo)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };
            Panel vach = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = mau };
            Label t = new Label
            {
                Text = tieuDe,
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 12)
            };
            lblSo = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = mau,
                AutoSize = true,
                Location = new Point(18, 36)
            };
            card.Controls.Add(vach);
            card.Controls.Add(t);
            card.Controls.Add(lblSo);
            return card;
        }

        private void ThemCot(string prop, string tieuDe, float trongSo, string dinhDang, bool canPhai)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                Name = prop,
                HeaderText = tieuDe,
                FillWeight = trongSo
            };
            if (dinhDang != null)
            {
                col.DefaultCellStyle.Format = dinhDang;
                col.DefaultCellStyle.FormatProvider = vi;
            }
            if (canPhai)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            dgv.Columns.Add(col);
        }

        // ================= BỘ LỌC NGÀY =================
        private void DatNhanh(int chiSo)
        {
            DateTime hn = DateTime.Today;
            DateTime tu = hn;
            DateTime den = hn;

            switch (chiSo)
            {
                case 0: break;                          // Hôm nay
                case 1: tu = hn.AddDays(-6); break;     // 7 ngày qua
                case 2: tu = hn.AddDays(-29); break;    // 30 ngày qua
                case 3: tu = new DateTime(hn.Year, hn.Month, 1); break;     // Tháng này
                case 4:                                 // Tháng trước
                    tu = new DateTime(hn.Year, hn.Month, 1).AddMonths(-1);
                    den = new DateTime(hn.Year, hn.Month, 1).AddDays(-1);
                    break;
                case 5: tu = new DateTime(hn.Year, 1, 1); break;            // Năm nay
                case 6: tu = new DateTime(2000, 1, 1); break;               // Tất cả
            }

            dangDatNgay = true;
            dtpTu.Value = tu;
            dtpDen.Value = den;
            dangDatNgay = false;
        }

        private void KhiNguoiDungDoiNgay()
        {
            if (dangDatNgay) return;
            dangDatNgay = true;
            cmbNhanh.SelectedIndex = 7;   // "Tùy chọn"
            dangDatNgay = false;
        }

        // ================= ĐỌC DỮ LIỆU =================
        private static HashSet<string> LayCot(SqliteConnection conn, string bang)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(" + bang + ")", conn))
            using (SqliteDataReader r = cmd.ExecuteReader())
            {
                while (r.Read()) set.Add(Convert.ToString(r["name"]) ?? "");
            }
            return set;
        }

        private static double ToD(object o)
        {
            if (o == null || o is DBNull) return 0;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static DataTable TaoBangTrong()
        {
            DataTable t = new DataTable();
            t.Columns.Add("NgayXuat", typeof(string));
            t.Columns.Add("MaPX", typeof(string));
            t.Columns.Add("NguoiNhan", typeof(string));
            t.Columns.Add("MaVT", typeof(string));
            t.Columns.Add("TenVT", typeof(string));
            t.Columns.Add("DonVi", typeof(string));
            t.Columns.Add("SoLuong", typeof(double));
            t.Columns.Add("DonGia", typeof(double));
            t.Columns.Add("ThanhTien", typeof(double));
            return t;
        }

        private void TaiBaoCao()
        {
            DateTime tu = dtpTu.Value.Date;
            DateTime den = dtpDen.Value.Date;
            if (tu > den)
            {
                MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.", "Khoảng ngày chưa hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable bang = TaoBangTrong();
            HashSet<string> maPhieu = new HashSet<string>();
            int tongPhieuTrongDb = 0;
            chuaCoBang = false;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();

                    HashSet<string> cotPX = LayCot(conn, "PhieuXuat");
                    HashSet<string> cotCT = LayCot(conn, "ChiTietPhieuXuat");
                    bool coBang = cotPX.Contains("MaPX") && cotPX.Contains("NgayXuat") &&
                                  cotCT.Contains("MaPX") && cotCT.Contains("MaVT") && cotCT.Contains("SoLuong");

                    if (!coBang)
                    {
                        chuaCoBang = true;
                    }
                    else
                    {
                        using (SqliteCommand dem = new SqliteCommand("SELECT COUNT(*) FROM PhieuXuat", conn))
                            tongPhieuTrongDb = Convert.ToInt32(dem.ExecuteScalar());

                        HashSet<string> cotVT = LayCot(conn, "VatTu");
                        bool coVT = cotVT.Contains("MaVT") && cotVT.Contains("TenVT");
                        string tenVt = coVT ? "COALESCE(v.TenVT, c.MaVT)" : "c.MaVT";
                        string donVi = !coVT ? "''" : cotVT.Contains("DonViTinh") ? "v.DonViTinh" : cotVT.Contains("DVT") ? "v.DVT" : "''";
                        string join = coVT ? "LEFT JOIN VatTu v ON v.MaVT = c.MaVT" : "";
                        string nguoi = cotPX.Contains("NguoiNhan") ? "p.NguoiNhan" : "''";
                        string gia = cotCT.Contains("DonGia") ? "c.DonGia" : "0";

                        string sql =
                            "SELECT p.NgayXuat, p.MaPX, " + nguoi + " AS NguoiNhan, c.MaVT, " + tenVt + " AS TenVT, " +
                            donVi + " AS DonVi, c.SoLuong, " + gia + " AS DonGia " +
                            "FROM ChiTietPhieuXuat c JOIN PhieuXuat p ON p.MaPX = c.MaPX " + join + " " +
                            "WHERE substr(p.NgayXuat, 1, 10) BETWEEN @tu AND @den " +
                            "ORDER BY p.NgayXuat DESC, p.MaPX";

                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@tu", tu.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@den", den.ToString("yyyy-MM-dd"));

                            using (SqliteDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string ngayGoc = Convert.ToString(r["NgayXuat"]) ?? "";
                                    string ngayHienThi = ngayGoc;
                                    DateTime d;
                                    if (ngayGoc.Length >= 10 &&
                                        DateTime.TryParseExact(ngayGoc.Substring(0, 10), "yyyy-MM-dd",
                                            CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                                        ngayHienThi = d.ToString("dd/MM/yyyy");

                                    double sl = ToD(r["SoLuong"]);
                                    double dg = ToD(r["DonGia"]);
                                    string px = Convert.ToString(r["MaPX"]) ?? "";

                                    DataRow row = bang.NewRow();
                                    row["NgayXuat"] = ngayHienThi;
                                    row["MaPX"] = px;
                                    row["NguoiNhan"] = Convert.ToString(r["NguoiNhan"]) ?? "";
                                    row["MaVT"] = Convert.ToString(r["MaVT"]) ?? "";
                                    row["TenVT"] = Convert.ToString(r["TenVT"]) ?? "";
                                    row["DonVi"] = Convert.ToString(r["DonVi"]) ?? "";
                                    row["SoLuong"] = sl;
                                    row["DonGia"] = dg;
                                    row["ThanhTien"] = sl * dg;
                                    bang.Rows.Add(row);
                                    maPhieu.Add(px);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không đọc được dữ liệu báo cáo:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            dtChiTiet = bang;
            dgv.DataSource = dtChiTiet;

            double tongSl = 0, tongGt = 0;
            foreach (DataRow r in dtChiTiet.Rows)
            {
                tongSl += (double)r["SoLuong"];
                tongGt += (double)r["ThanhTien"];
            }
            lblSoPhieu.Text = maPhieu.Count.ToString("N0", vi);
            lblSoLuong.Text = tongSl.ToString("#,##0.##", vi);
            lblGiaTri.Text = tongGt.ToString("N0", vi) + " đ";

            CapNhatChart();

            btnMau.Visible = chuaCoBang || tongPhieuTrongDb == 0;
        }

        // ================= BIỂU ĐỒ =================
        private void CapNhatChart()
        {
            if (chart == null || cmbChiSo == null) return;

            bool theoSoLuong = cmbChiSo.SelectedIndex != 1;
            lblTieuDeChart.Text = theoSoLuong
                ? "Top vật tư xuất nhiều nhất (theo số lượng)"
                : "Top vật tư xuất nhiều nhất (theo giá trị)";

            Dictionary<string, double> tong = new Dictionary<string, double>();
            foreach (DataRow r in dtChiTiet.Rows)
            {
                string ten = Convert.ToString(r["TenVT"]) ?? "";
                if (ten == "") ten = "(không có tên)";
                double giaTri = theoSoLuong ? (double)r["SoLuong"] : (double)r["ThanhTien"];
                double cu;
                tong.TryGetValue(ten, out cu);
                tong[ten] = cu + giaTri;
            }

            List<KeyValuePair<string, double>> ds = new List<KeyValuePair<string, double>>(tong);
            ds.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (ds.Count > 8) ds = ds.GetRange(0, 8);

            string thongBao = chuaCoBang
                ? "Chưa có phiếu xuất kho nào.\nDữ liệu sẽ hiện ở đây khi có phiếu xuất (hoặc bấm “Tạo dữ liệu mẫu”)."
                : "Không có hàng xuất kho trong khoảng thời gian này.";

            Func<double, string> dinhDang = theoSoLuong
                ? new Func<double, string>(v => v.ToString("#,##0.##", vi))
                : new Func<double, string>(v => v.ToString("N0", vi) + " đ");

            chart.DatDuLieu(ds, dinhDang, thongBao);
        }

        // ================= XUẤT CSV =================
        private static string Q(object o)
        {
            string s = Convert.ToString(o) ?? "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string So(object o)
        {
            return ToD(o).ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void XuatCsv()
        {
            if (dtChiTiet.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Xuất CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.FileName = "bao_cao_xuat_kho_" + dtpTu.Value.ToString("yyyyMMdd") + "_" + dtpDen.Value.ToString("yyyyMMdd") + ".csv";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("sep=,");
                    sb.AppendLine("Ngày xuất,Mã phiếu,Người nhận,Mã vật tư,Vật tư,Đơn vị,Số lượng,Đơn giá,Thành tiền");
                    foreach (DataRow r in dtChiTiet.Rows)
                    {
                        sb.AppendLine(string.Join(",", new string[]
                        {
                            Q(r["NgayXuat"]), Q(r["MaPX"]), Q(r["NguoiNhan"]), Q(r["MaVT"]), Q(r["TenVT"]), Q(r["DonVi"]),
                            So(r["SoLuong"]), So(r["DonGia"]), So(r["ThanhTien"])
                        }));
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show("Đã xuất " + dtChiTiet.Rows.Count + " dòng ra file:\n" + sfd.FileName, "Xuất CSV",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không ghi được file:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ================= DỮ LIỆU MẪU =================
        private void TaoDuLieuMau()
        {
            if (MessageBox.Show(
                    "Tạo khoảng 15 phiếu xuất mẫu trong 30 ngày gần đây để xem thử báo cáo?\n" +
                    "Các phiếu mẫu có mã bắt đầu bằng PXM nên dễ xoá sau này.",
                    "Tạo dữ liệu mẫu", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(ConnStr))
                {
                    conn.Open();

                    HashSet<string> cotVT = LayCot(conn, "VatTu");
                    if (!cotVT.Contains("MaVT"))
                    {
                        MessageBox.Show("Chưa có bảng VatTu. Hãy thêm vài vật tư (trang Tồn kho) rồi tạo dữ liệu mẫu.",
                            "Chưa có vật tư", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string cotGia = cotVT.Contains("GiaGoc") ? "GiaGoc" : cotVT.Contains("DonGia") ? "DonGia" : cotVT.Contains("GiaTien") ? "GiaTien" : "0";
                    List<string> maVt = new List<string>();
                    List<double> giaVt = new List<double>();
                    using (SqliteCommand cmd = new SqliteCommand("SELECT MaVT, " + cotGia + " AS Gia FROM VatTu LIMIT 8", conn))
                    using (SqliteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            maVt.Add(Convert.ToString(r["MaVT"]) ?? "");
                            giaVt.Add(ToD(r["Gia"]));
                        }
                    }

                    if (maVt.Count == 0)
                    {
                        MessageBox.Show("Bảng VatTu đang trống. Hãy thêm vài vật tư (trang Tồn kho) rồi tạo dữ liệu mẫu.",
                            "Chưa có vật tư", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string[] khach =
                    {
                        "Công ty Xây dựng An Phát", "Cửa hàng Minh Tâm", "Anh Hùng (thợ xây)",
                        "Công ty TNHH Hoàng Gia", "Chị Lan (chủ nhà)"
                    };

                    Random rnd = new Random();
                    string dauMa = "PXM" + DateTime.Now.ToString("yyMMddHHmmss") + "-";

                    using (SqliteTransaction tran = conn.BeginTransaction())
                    {
                        using (SqliteCommand t1 = new SqliteCommand(
                            "CREATE TABLE IF NOT EXISTS PhieuXuat (MaPX TEXT PRIMARY KEY, NgayXuat TEXT, NguoiNhan TEXT, MaNV TEXT)", conn, tran))
                            t1.ExecuteNonQuery();
                        using (SqliteCommand t2 = new SqliteCommand(
                            "CREATE TABLE IF NOT EXISTS ChiTietPhieuXuat (MaPX TEXT, MaVT TEXT, SoLuong INTEGER, DonGia REAL, PRIMARY KEY (MaPX, MaVT))", conn, tran))
                            t2.ExecuteNonQuery();

                        for (int i = 1; i <= 15; i++)
                        {
                            string maPx = dauMa + i.ToString("00");
                            DateTime ngay = DateTime.Today.AddDays(-rnd.Next(0, 30));

                            using (SqliteCommand ins = new SqliteCommand(
                                "INSERT INTO PhieuXuat (MaPX, NgayXuat, NguoiNhan, MaNV) VALUES (@px, @ngay, @khach, '')", conn, tran))
                            {
                                ins.Parameters.AddWithValue("@px", maPx);
                                ins.Parameters.AddWithValue("@ngay", ngay.ToString("yyyy-MM-dd"));
                                ins.Parameters.AddWithValue("@khach", khach[rnd.Next(khach.Length)]);
                                ins.ExecuteNonQuery();
                            }

                            int soDong = Math.Min(rnd.Next(1, 4), maVt.Count);
                            HashSet<int> daChon = new HashSet<int>();
                            while (daChon.Count < soDong) daChon.Add(rnd.Next(maVt.Count));

                            foreach (int idx in daChon)
                            {
                                double gia = giaVt[idx] > 0 ? Math.Round(giaVt[idx] * 1.1 / 1000) * 1000 : rnd.Next(20, 300) * 1000;
                                using (SqliteCommand ct = new SqliteCommand(
                                    "INSERT INTO ChiTietPhieuXuat (MaPX, MaVT, SoLuong, DonGia) VALUES (@px, @vt, @sl, @gia)", conn, tran))
                                {
                                    ct.Parameters.AddWithValue("@px", maPx);
                                    ct.Parameters.AddWithValue("@vt", maVt[idx]);
                                    ct.Parameters.AddWithValue("@sl", rnd.Next(5, 121));
                                    ct.Parameters.AddWithValue("@gia", gia);
                                    ct.ExecuteNonQuery();
                                }
                            }
                        }
                        tran.Commit();
                    }
                }

                TaiBaoCao();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tạo được dữ liệu mẫu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =====================================================================
        // BIỂU ĐỒ CỘT NGANG TỰ VẼ (GDI+)
        // =====================================================================
        private class BarChart : Control
        {
            private class Muc
            {
                public string Ten = "";
                public double GiaTri;
            }

            private readonly List<Muc> items = new List<Muc>();
            private Func<double, string> dinhDang = v => v.ToString("N0");
            private string thongBaoTrong = "Chưa có dữ liệu";

            private double tienDo = 1.0;
            private DateTime batDau;
            private int hoverIndex = -1;

            private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 16 };
            private readonly ToolTip tip = new ToolTip();
            private readonly Font fontNho = new Font("Segoe UI", 8.5F);
            private readonly Font fontDam = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            public BarChart()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
                Font = new Font("Segoe UI", 9.5F);

                timer.Tick += (s, e) =>
                {
                    double t = (DateTime.Now - batDau).TotalMilliseconds / 650.0;
                    if (t >= 1.0)
                    {
                        tienDo = 1.0;
                        timer.Stop();
                    }
                    else
                    {
                        tienDo = 1.0 - Math.Pow(1.0 - t, 3);
                    }
                    Invalidate();
                };
            }

            public void DatDuLieu(List<KeyValuePair<string, double>> ds, Func<double, string> dinhDang, string thongBaoTrong)
            {
                items.Clear();
                foreach (KeyValuePair<string, double> kv in ds)
                    items.Add(new Muc { Ten = kv.Key, GiaTri = kv.Value });

                this.dinhDang = dinhDang;
                this.thongBaoTrong = thongBaoTrong;
                hoverIndex = -1;
                tip.SetToolTip(this, "");

                tienDo = 0;
                batDau = DateTime.Now;
                timer.Start();
                Invalidate();
            }

            private Rectangle TinhVungVe()
            {
                int trai = Math.Max(90, Math.Min(200, (int)(Width * 0.30)));
                int phai = 112;
                int tren = 6;
                int duoi = 26;
                return new Rectangle(trai, tren, Width - trai - phai, Height - tren - duoi);
            }

            private static string RutGon(double v)
            {
                double a = Math.Abs(v);
                if (a >= 1e9) return (v / 1e9).ToString("0.#", vi) + " tỷ";
                if (a >= 1e6) return (v / 1e6).ToString("0.#", vi) + " tr";
                if (a >= 1e3) return (v / 1e3).ToString("0.#", vi) + " k";
                return v.ToString("0.#", vi);
            }

            private static GraphicsPath RoundedRect(Rectangle r, int radius)
            {
                int d = Math.Max(2, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
                GraphicsPath path = new GraphicsPath();
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(BackColor);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (items.Count == 0)
                {
                    TextFormatFlags kieu = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
                    Size co = TextRenderer.MeasureText(g, thongBaoTrong, Font, new Size(Math.Max(60, Width - 40), int.MaxValue), kieu);
                    Rectangle giua = new Rectangle((Width - co.Width) / 2, (Height - co.Height) / 2, co.Width + 2, co.Height);
                    TextRenderer.DrawText(g, thongBaoTrong, Font, giua, Color.Gray, kieu | TextFormatFlags.HorizontalCenter);
                    return;
                }

                Rectangle plot = TinhVungVe();
                if (plot.Width < 40 || plot.Height < 40) return;

                double max = 0;
                foreach (Muc m in items) if (m.GiaTri > max) max = m.GiaTri;
                if (max <= 0) max = 1;

                using (Pen luoi = new Pen(Color.FromArgb(232, 235, 240)))
                {
                    for (int k = 0; k <= 4; k++)
                    {
                        int x = plot.X + plot.Width * k / 4;
                        g.DrawLine(luoi, x, plot.Y, x, plot.Bottom);
                        Rectangle nhan = new Rectangle(x - 40, plot.Bottom + 4, 80, 18);
                        TextRenderer.DrawText(g, RutGon(max * k / 4), fontNho, nhan, Color.Gray,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
                    }
                }

                int rowH = plot.Height / items.Count;
                int barH = Math.Max(10, (int)(rowH * 0.56));

                for (int i = 0; i < items.Count; i++)
                {
                    Muc m = items[i];
                    bool noi = (i == hoverIndex);
                    int rowTop = plot.Y + i * rowH;
                    int y = rowTop + (rowH - barH) / 2;
                    int w = (int)(plot.Width * (m.GiaTri / max) * tienDo);
                    if (m.GiaTri > 0 && w < 3) w = 3;

                    if (noi)
                    {
                        using (SolidBrush nen = new SolidBrush(Color.FromArgb(22, 108, 92, 231)))
                            g.FillRectangle(nen, 0, rowTop, Width, rowH);
                    }

                    Rectangle ten = new Rectangle(4, rowTop, plot.X - 14, rowH);
                    TextRenderer.DrawText(g, m.Ten, Font, ten, MauChu,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                    if (w > 0)
                    {
                        Color c1 = noi ? Color.FromArgb(142, 128, 255) : MauTim;
                        Color c2 = noi ? Color.FromArgb(70, 180, 245) : MauXanh;
                        Rectangle thanh = new Rectangle(plot.X, y, w, barH);
                        using (GraphicsPath path = RoundedRect(thanh, 8))
                        using (LinearGradientBrush lg = new LinearGradientBrush(
                                         new Rectangle(plot.X, y, Math.Max(w, 2), barH), c1, c2, LinearGradientMode.Horizontal))
                        {
                            g.FillPath(lg, path);
                        }
                    }

                    Rectangle so = new Rectangle(plot.X + w + 8, rowTop, 130, rowH);
                    TextRenderer.DrawText(g, dinhDang(m.GiaTri * tienDo), fontDam, so, MauChu,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);

                int idx = -1;
                if (items.Count > 0)
                {
                    Rectangle plot = TinhVungVe();
                    if (plot.Height > 0 && e.Y >= plot.Y && e.Y < plot.Bottom)
                    {
                        int rowH = plot.Height / items.Count;
                        if (rowH > 0)
                        {
                            idx = (e.Y - plot.Y) / rowH;
                            if (idx >= items.Count) idx = -1;
                        }
                    }
                }

                if (idx == hoverIndex) return;
                hoverIndex = idx;

                if (idx >= 0)
                {
                    double tong = 0;
                    foreach (Muc m in items) tong += m.GiaTri;
                    Muc chon = items[idx];
                    double phanTram = tong > 0 ? chon.GiaTri / tong * 100 : 0;
                    tip.SetToolTip(this, chon.Ten + "\n" + dinhDang(chon.GiaTri) + "  (" + phanTram.ToString("0.#", vi) + "% trong top)");
                }
                else
                {
                    tip.SetToolTip(this, "");
                }
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (hoverIndex != -1)
                {
                    hoverIndex = -1;
                    tip.SetToolTip(this, "");
                    Invalidate();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    timer.Dispose();
                    tip.Dispose();
                    fontNho.Dispose();
                    fontDam.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}