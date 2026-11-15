using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            LoadSampleData(true);
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Sản phẩm - Quản lý kho VLXD";
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(220, 220, 220);

            // 1. KHUNG NỘI DUNG CHÍNH
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(15);

            // 2. THANH CÔNG CỤ PHÍA TRÊN
            Panel pnlSubToolbar = new Panel();
            pnlSubToolbar.Dock = DockStyle.Top;
            pnlSubToolbar.Height = 45;

            Button btnTabSanPham = new Button() { Text = "Sản phẩm", Location = new Point(0, 5), Size = new Size(100, 30), BackColor = Color.LightGray };
            Button btnCancel = new Button() { Text = "Quay về Form chính", Size = new Size(140, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCancel.Click += (s, e) => {
                if (this.IsDisposed) return;
                Form1 mainForm = new Form1("Doanh nghiệp VLXD");
                mainForm.Show();
                this.Hide();
                this.Close();
            };

            Button btnCreateOrder = new Button() { Text = "Tạo & duyệt đơn hàng", Size = new Size(150, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnCreatePurchase = new Button() { Text = "Tạo đơn đặt hàng", Size = new Size(140, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            pnlSubToolbar.Controls.AddRange(new Control[] { btnTabSanPham, btnCancel, btnCreateOrder, btnCreatePurchase });
            pnlSubToolbar.Resize += (s, e) =>
            {
                if (this.IsDisposed) return;
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

            txtMaDon = CreateDetailField(pnlRightDetail, "Mã SP", 15);
            txtNhaPhanPhoi = CreateDetailField(pnlRightDetail, "Tên vật tư", 90);
            txtNgayNhap = CreateDetailField(pnlRightDetail, "Ngày cập nhật", 165);
            txtSoSanPham = CreateDetailField(pnlRightDetail, "Số lượng tồn", 240);
            txtTrangThai = CreateDetailField(pnlRightDetail, "Trạng thái", 315);

            // 4. KHUNG GIỮA
            pnlCenterContainer = new Panel();
            pnlCenterContainer.Dock = DockStyle.Fill;
            pnlCenterContainer.BackColor = Color.FromArgb(210, 210, 210);

            // --- Trạng thái trống ---
            pnlEmptyState = new Panel();
            pnlEmptyState.Dock = DockStyle.Fill;
            pnlEmptyState.BackColor = Color.FromArgb(210, 210, 210);

            Label lblTitle = new Label() { Text = "QUẢN LÝ SẢN PHẨM & VẬT TƯ", Location = new Point(30, 20), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            TextBox txtSearchBox = new TextBox() { Text = "TÌM KIẾM...", Location = new Point(30, 55), Size = new Size(350, 28) };
            Button btnLoaiSP = new Button() { Text = "Lọc loại SP", Location = new Point(390, 53), Size = new Size(100, 30) };

            btnAddProduct = new Button();
            btnAddProduct.Text = "Thêm sản phẩm mới";
            btnAddProduct.Location = new Point(30, 100);
            btnAddProduct.Size = new Size(200, 38);
            btnAddProduct.BackColor = Color.FromArgb(46, 204, 113);
            btnAddProduct.ForeColor = Color.White;
            btnAddProduct.FlatStyle = FlatStyle.Flat;
            btnAddProduct.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAddProduct.Click += btnThemSanPham_Click;

            pnlEmptyState.Controls.AddRange(new Control[] { lblTitle, txtSearchBox, btnLoaiSP, btnAddProduct });

            // Nút thêm sản phẩm nhanh ở thanh công cụ trên
            Button btnThem = new Button()
            {
                Text = "+ Thêm sản phẩm",
                Location = new Point(110, 5),
                Size = new Size(130, 30),
                BackColor = Color.White
            };
            btnThem.Click += btnThemSanPham_Click;
            pnlSubToolbar.Controls.Add(btnThem);

            // --- Trạng thái có danh sách (DataGridView) ---
            dgvProducts = new DataGridView();
            dgvProducts.Dock = DockStyle.Fill;
            dgvProducts.BackgroundColor = Color.FromArgb(210, 210, 210);
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.CellClick += DgvProducts_CellClick;

            pnlCenterContainer.Controls.Add(dgvProducts);
            pnlCenterContainer.Controls.Add(pnlEmptyState);

            // 5. GHÉP CÁC PANEL VÀO NỘI DUNG CHÍNH
            pnlContent.Controls.Add(pnlCenterContainer);
            pnlContent.Controls.Add(pnlRightDetail);
            pnlContent.Controls.Add(pnlSubToolbar);
            this.Controls.Add(pnlContent);
        }

        private TextBox CreateDetailField(Panel parent, string labelName, int topCoord)
        {
            Label lbl = new Label();
            lbl.Text = labelName;
            lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lbl.Location = new Point(10, topCoord);
            lbl.Size = new Size(270, 20);
            parent.Controls.Add(lbl);

            TextBox txt = new TextBox();
            txt.Location = new Point(10, topCoord + 22);
            txt.Size = new Size(275, 25);
            txt.Font = new Font("Segoe UI", 9.5F);
            txt.BackColor = Color.FromArgb(255, 245, 245);
            parent.Controls.Add(txt);
            return txt;
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;
            using (sanpham frmSP = new sanpham())
            {
                frmSP.ShowDialog();
            }
            LoadSampleData(true);
        }

        private void LoadSampleData(bool forceLoad = true)
        {
            // Kiểm tra ngay đầu hàm: nếu form đã bị hủy hoặc chưa sẵn sàng thì thoát luôn tránh lỗi
            if (this.IsDisposed || !this.IsHandleCreated) return;

            string connectionString = "Data Source=quan_ly_kho.db";
            DataTable dt = new DataTable();

            try
            {
                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaVT AS [Mã SP], TenVT AS [Tên vật tư], DVT AS [ĐVT], SoLuongTon AS [Số lượng] FROM VatTu";

                    using (SqliteCommand cmd = new SqliteCommand(query, conn))
                    {
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                    }
                }
            }
            catch (Exception)
            {
                dt = null;
            }

            // Kiểm tra lại lần nữa trước khi can thiệp vào các Control trên giao diện
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (dt != null && dt.Rows.Count > 0)
            {
                if (pnlEmptyState != null) pnlEmptyState.Visible = false;
                if (dgvProducts != null)
                {
                    dgvProducts.Visible = true;
                    dgvProducts.DataSource = dt;
                }
            }
            else
            {
                if (dgvProducts != null) dgvProducts.Visible = false;
                if (pnlEmptyState != null) pnlEmptyState.Visible = true;

                if (txtMaDon != null && !txtMaDon.IsDisposed) txtMaDon.Clear();
                if (txtNhaPhanPhoi != null && !txtNhaPhanPhoi.IsDisposed) txtNhaPhanPhoi.Clear();
                if (txtNgayNhap != null && !txtNgayNhap.IsDisposed) txtNgayNhap.Clear();
                if (txtSoSanPham != null && !txtSoSanPham.IsDisposed) txtSoSanPham.Clear();
                if (txtTrangThai != null && !txtTrangThai.IsDisposed) txtTrangThai.Clear();
            }
        }

        private void DgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (this.IsDisposed) return;
            if (e.RowIndex < 0 || dgvProducts == null || dgvProducts.Rows[e.RowIndex].IsNewRow) return;

            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

            if (txtMaDon != null && !txtMaDon.IsDisposed)
                txtMaDon.Text = Convert.ToString(row.Cells["Mã SP"].Value);
            if (txtNhaPhanPhoi != null && !txtNhaPhanPhoi.IsDisposed)
                txtNhaPhanPhoi.Text = Convert.ToString(row.Cells["Tên vật tư"].Value);
            if (txtNgayNhap != null && !txtNgayNhap.IsDisposed)
                txtNgayNhap.Text = DateTime.Now.ToString("dd/MM/yyyy");
            if (txtSoSanPham != null && !txtSoSanPham.IsDisposed)
                txtSoSanPham.Text = Convert.ToString(row.Cells["Số lượng"].Value);
            if (txtTrangThai != null && !txtTrangThai.IsDisposed)
                txtTrangThai.Text = "Đang lưu kho";
        }
    }
}