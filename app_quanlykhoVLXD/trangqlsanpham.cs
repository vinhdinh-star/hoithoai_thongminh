using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Drawing;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class trangqlsanpham : Form
    {
        private Panel pnlContent;
        private Panel pnlRightDetail;
        private Panel pnlCenterContainer;
        private Panel pnlEmptyState;
        private DataGridView dgvProducts;

        private TextBox txtMaDon, txtNhaPhanPhoi, txtNgayNhap, txtSoSanPham, txtTrangThai;
        private Button btnAddProduct;

        public trangqlsanpham()
        {
            InitializeComponent();
            InitializeComponentCustom();
            LoadSampleData(false);
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Sản phẩm - Quản lý kho VLXD";
            this.BackColor = Color.FromArgb(220, 220, 220);

            // 1. KHUNG NỘI DUNG (không còn sidebar / header / chatbot - đã có ở Form1)
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(15);

            // 2. THANH CÔNG CỤ PHÍA TRÊN
            Panel pnlSubToolbar = new Panel();
            pnlSubToolbar.Dock = DockStyle.Top;
            pnlSubToolbar.Height = 45;

            Button btnTabSanPham = new Button() { Text = "Sản phẩm", Location = new Point(0, 5), Size = new Size(100, 30), BackColor = Color.LightGray };
            Button btnCancel = new Button() { Text = "Hủy", Size = new Size(80, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnCreateOrder = new Button() { Text = "Tạo & duyệt đơn hàng", Size = new Size(150, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnCreatePurchase = new Button() { Text = "Tạo đơn đặt hàng", Size = new Size(140, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            pnlSubToolbar.Controls.AddRange(new Control[] { btnTabSanPham, btnCancel, btnCreateOrder, btnCreatePurchase });
            pnlSubToolbar.Resize += (s, e) =>
            {
                int right = pnlSubToolbar.ClientSize.Width;
                btnCreatePurchase.Location = new Point(right - btnCreatePurchase.Width, 5);
                btnCreateOrder.Location = new Point(btnCreatePurchase.Left - btnCreateOrder.Width - 10, 5);
                btnCancel.Location = new Point(btnCreateOrder.Left - btnCancel.Width - 10, 5);
            };

            // 3. PANEL CHI TIẾT BÊN PHẢI
            pnlRightDetail = new Panel();
            pnlRightDetail.Dock = DockStyle.Right;
            pnlRightDetail.Width = 295;
            pnlRightDetail.BackColor = Color.FromArgb(245, 240, 240);

            txtMaDon = CreateDetailField(pnlRightDetail, "Mã đơn", 15);
            txtNhaPhanPhoi = CreateDetailField(pnlRightDetail, "Nhà phân phối", 90);
            txtNgayNhap = CreateDetailField(pnlRightDetail, "Ngày nhập hàng", 165);
            txtSoSanPham = CreateDetailField(pnlRightDetail, "Số sản phẩm", 240);
            txtTrangThai = CreateDetailField(pnlRightDetail, "Trạng thái", 315);

            // 4. KHUNG GIỮA
            pnlCenterContainer = new Panel();
            pnlCenterContainer.Dock = DockStyle.Fill;
            pnlCenterContainer.BackColor = Color.FromArgb(210, 210, 210);

            // --- Trạng thái trống ---
            pnlEmptyState = new Panel();
            pnlEmptyState.Dock = DockStyle.Fill;
            pnlEmptyState.BackColor = Color.FromArgb(210, 210, 210);

            Label lblTitle = new Label() { Text = "SẢN PHẨM", Location = new Point(30, 20), AutoSize = true, Font = new Font("Arial", 12, FontStyle.Bold) };
            TextBox txtSearchBox = new TextBox() { Text = "TÌM KIẾM", Location = new Point(30, 55), Size = new Size(400, 25), TextAlign = HorizontalAlignment.Center };
            Button btnLoaiSP = new Button() { Text = "Loại sản phẩm", Location = new Point(440, 53), Size = new Size(100, 28) };

            btnAddProduct = new Button();
            btnAddProduct.Text = "Thêm sản phẩm";
            btnAddProduct.Location = new Point(30, 100);
            btnAddProduct.Size = new Size(200, 35);
            btnAddProduct.BackColor = Color.White;
            btnAddProduct.Click += btnThemSanPham_Click;

            Panel pnlThanhToan = new Panel() { Location = new Point(30, 300), Size = new Size(250, 100), BackColor = Color.FromArgb(200, 200, 200), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            pnlThanhToan.Paint += (s, e) => e.Graphics.DrawString("Thanh toán", Font, Brushes.Black, 10, 10);

            Panel pnlGhiChu = new Panel() { Location = new Point(290, 300), Size = new Size(250, 100), BackColor = Color.FromArgb(200, 200, 200), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            pnlGhiChu.Paint += (s, e) => e.Graphics.DrawString("Ghi chú", Font, Brushes.Black, 10, 10);

            pnlEmptyState.Controls.AddRange(new Control[] { lblTitle, txtSearchBox, btnLoaiSP, btnAddProduct, pnlThanhToan, pnlGhiChu });

            Button btnThem = new Button()
            {
                Text = "+ Thêm sản phẩm",
                Location = new Point(110, 5),
                Size = new Size(130, 30),
                BackColor = Color.White
            };
            btnThem.Click += btnThemSanPham_Click;
            pnlSubToolbar.Controls.Add(btnThem);

            // --- Trạng thái có danh sách ---
            dgvProducts = new DataGridView();
            dgvProducts.Dock = DockStyle.Fill;
            dgvProducts.BackgroundColor = Color.FromArgb(210, 210, 210);
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.CellClick += DgvProducts_CellClick;

            pnlCenterContainer.Controls.Add(dgvProducts);
            pnlCenterContainer.Controls.Add(pnlEmptyState);

            // 5. GHÉP LẠI: Fill trước, rồi Right, rồi Top
            pnlContent.Controls.Add(pnlCenterContainer);
            pnlContent.Controls.Add(pnlRightDetail);
            pnlContent.Controls.Add(pnlSubToolbar);
            this.Controls.Add(pnlContent);


        }

        private TextBox CreateDetailField(Panel parent, string labelName, int topCoord)
        {
            Label lbl = new Label();
            lbl.Text = labelName;
            lbl.Location = new Point(10, topCoord);
            lbl.Size = new Size(270, 20);
            parent.Controls.Add(lbl);

            TextBox txt = new TextBox();
            txt.Location = new Point(10, topCoord + 22);
            txt.Size = new Size(275, 25);
            txt.BackColor = Color.FromArgb(255, 245, 245);
            parent.Controls.Add(txt);
            return txt;
        }
        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            using (sanpham frmSP = new sanpham())
            {
                frmSP.ShowDialog();   // chờ form sản phẩm đóng
            }
            LoadSampleData(true);     // nạp lại danh sách từ SQLite
        }

        private void LoadSampleData(bool forceLoad = true)
        {
            string connectionString = "Data Source=quan_ly_kho.db";
            DataTable? dt = new DataTable();

            try
            {
                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaVT AS [Mã SP], TenVT AS [Tên vật tư], DVT AS [ĐVT], SoLuongTon AS [Số lượng] FROM VatTu";

                    using (SqliteCommand cmd = new SqliteCommand(query, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            catch (Exception)
            {
                // Bảng chưa tồn tại hoặc lỗi kết nối -> hiện giao diện trống
                dt = null;
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                pnlEmptyState.Visible = false;
                dgvProducts.Visible = true;
                dgvProducts.DataSource = dt;
            }
            else
            {
                dgvProducts.Visible = false;
                pnlEmptyState.Visible = true;

                txtMaDon.Clear();
                txtNhaPhanPhoi.Clear();
                txtNgayNhap.Clear();
                txtSoSanPham.Clear();
                txtTrangThai.Clear();
            }
        }

        private void DgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
            txtMaDon.Text = Convert.ToString(row.Cells["Mã SP"].Value);
            txtNhaPhanPhoi.Text = Convert.ToString(row.Cells["Tên vật tư"].Value);
            txtNgayNhap.Text = DateTime.Now.ToString("dd/MM/yyyy");
            txtSoSanPham.Text = Convert.ToString(row.Cells["Số lượng"].Value);
            txtTrangThai.Text = "Đang lưu kho";
        }
    }
}