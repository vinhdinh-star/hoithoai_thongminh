using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class Form1 : Form
    {
        // Chuỗi kết nối đến SQL Server của bạn
        private string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";

        private string tenDoanhNghiepHienTai = "";

        // 1. Constructor mặc định (nếu gọi trực tiếp không qua đăng nhập)
        public Form1() : this("Hệ thống Quản lý Kho VLXD") { }

        // 2. Constructor nhận tên doanh nghiệp được truyền từ Form đăng nhập sang
        public Form1(string tenDoanhNghiep)
        {
            tenDoanhNghiepHienTai = tenDoanhNghiep;
            InitializeCustomLayout();
            LoadSoLuongDonHang();
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

        // Hàm truy vấn SQL Server để đếm tổng số lượng đơn hàng
        private void LoadSoLuongDonHang()
        {
            string query = "SELECT COUNT(*) FROM DonHang";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        int tongDonHang = Convert.ToInt32(cmd.ExecuteScalar());

                        btnDonHang.Text = $"ĐƠN HÀNG ({tongDonHang})";
                        if (lblThongKeDonHang != null)
                        {
                            lblThongKeDonHang.Text = $"Tổng số đơn hàng trong hệ thống: {tongDonHang}";
                        }
                    }
                }
            }
            catch (Exception)
            {
                btnDonHang.Text = "ĐƠN HÀNG (0)";
            }
        }

        private void btnDonHang_Click(object sender, EventArgs e)
        {
            donhang frmDonHang = new donhang();
            frmDonHang.Show();
            this.Hide(); // Ẩn Form1 thay vì dùng this.Close() làm tắt app
        }

        private void btnChatBot_Click(object sender, EventArgs e)
        {
            FormChatBot frmChat = new FormChatBot();
            frmChat.TopMost = true;
            frmChat.Show();
            // Không gọi this.Close() ở đây để giữ Form1 hoạt động ngầm
        }
        private void MoTrangBaocao()
        {
            pnlMainContent.Controls.Clear();

            // Khởi tạo form báo cáo (baocao) dạng con để nhúng vào Panel
            baocao frm = new baocao();
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;

            // Thêm vào Panel nội dung chính và đưa nút ChatBot lên trên cùng
            pnlMainContent.Controls.Add(frm);
            pnlMainContent.Controls.Add(btnChatBot);
            btnChatBot.BringToFront();

            frm.Show();
        }
        private void MoTrangQLSanPham()
        {
            pnlMainContent.Controls.Clear();

            // Khởi tạo form quản lý sản phẩm dạng con (Embed vào Panel)
            trangqlsanpham frm = new trangqlsanpham();
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;

            // Thêm vào Panel và hiển thị bên trong Form hiện tại
            pnlMainContent.Controls.Add(frm);
            pnlMainContent.Controls.Add(btnChatBot);
            btnChatBot.BringToFront();

            frm.Show();
            // Đã xóa bỏ lệnh frm.Close() và this.Close() gây lỗi
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
            // Không gọi this.Close() ở đây
        }

        private void MoTrangNhapHang()
        {
            nhaphang trang = new nhaphang();

            trang.TaoDonDatHang += data =>
            {
                this.BeginInvoke(new Action(() => MoTrang(new Dathangnhap(data))));
            };

            MoTrang(trang);
            // Không gọi this.Close() ở đây
        }

        private void InitializeCustomLayout()
        {
            // 1. Cấu hình Form chính
            this.Text = "app_quanlykhoVLXD - Quản lý Kho";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(235, 238, 242);

            // 2. SIDEBAR (Menu bên trái)
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 200;
            pnlSidebar.BackColor = Color.FromArgb(82, 95, 122);

            Label lblSidebarTitle = new Label();
            lblSidebarTitle.Text = "Tổng quan";
            lblSidebarTitle.ForeColor = Color.White;
            lblSidebarTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblSidebarTitle.Location = new Point(15, 20);
            lblSidebarTitle.AutoSize = true;
            pnlSidebar.Controls.Add(lblSidebarTitle);

            string[] menuItems = {
                "Tổng quan",
                "✨ Trợ lý AI Pro",
                "Tồn kho",
                "Đặt hàng nhập",
                "Nhập hàng",
                "Trả hàng nhập",
                "chuyển kho",
                "Nhà cung cấp",
                "Vị trí lưu kho",
                "Báo cáo",
                "Cài đặt"
            };

            int startY = 65;
            for (int i = menuItems.Length - 1; i >= 0; i--)
            {
                Button btnMenu = new Button();
                string itemName = menuItems[i];
                btnMenu.Text = itemName;
                btnMenu.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                btnMenu.ForeColor = Color.White;
                btnMenu.FlatStyle = FlatStyle.Flat;
                btnMenu.FlatAppearance.BorderSize = 0;
                btnMenu.TextAlign = ContentAlignment.MiddleLeft;
                btnMenu.Padding = new Padding(10, 0, 0, 0);
                btnMenu.Size = new Size(200, 40);
                btnMenu.Location = new Point(0, startY + (i * 41));

                if (itemName.Contains("AI Pro"))
                {
                    btnMenu.BackColor = Color.FromArgb(108, 92, 231);
                    btnMenu.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                    btnMenu.Click += (s, e) =>
                    {
                        AI_pro frmAI = new AI_pro();
                        frmAI.TopMost = true;
                        frmAI.Show();
                    };

                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(128, 114, 235); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(108, 92, 231); };
                }

                else if (itemName == "Tồn kho")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrangQLSanPham(); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else if (itemName == "Nhập hàng")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrangNhapHang(); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else if (itemName == "Đặt hàng nhập")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrang(new Dathangnhap()); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else if (i == 0)
                {
                    btnMenu.BackColor = Color.FromArgb(145, 156, 175);
                }
                else if (itemName == "Nhà cung cấp")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrang(new nhacungcap()); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else if (itemName == "Báo cáo")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrangBaocao(); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else if (itemName == "Cài đặt")
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.Click += (s, e) => { MoTrang(new caidat()); };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }
                else
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }

                pnlSidebar.Controls.Add(btnMenu);
            }

            // 3. THANH TOPBAR
            pnlTopBar = new Panel();
            pnlTopBar.Location = new Point(200, 0);
            pnlTopBar.Size = new Size(900, 150);
            pnlTopBar.BackColor = Color.FromArgb(205, 210, 218);

            txtGlobalSearch = new TextBox();
            txtGlobalSearch.Font = new Font("Segoe UI", 11);
            txtGlobalSearch.Size = new Size(540, 32);
            txtGlobalSearch.Location = new Point(45, 25);
            txtGlobalSearch.Text = "Tìm kiếm";
            txtGlobalSearch.ForeColor = Color.Gray;
            txtGlobalSearch.Enter += (s, e) => { if (txtGlobalSearch.Text == "Tìm kiếm") { txtGlobalSearch.Text = ""; txtGlobalSearch.ForeColor = Color.Black; } };
            txtGlobalSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtGlobalSearch.Text)) { txtGlobalSearch.Text = "Tìm kiếm"; txtGlobalSearch.ForeColor = Color.Gray; } };
            pnlTopBar.Controls.Add(txtGlobalSearch);

            btnAdmin = new Button();
            btnAdmin.Text = tenDoanhNghiepHienTai; // Hiển thị tên doanh nghiệp lên góc trên
            btnAdmin.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAdmin.BackColor = Color.FromArgb(220, 225, 230);
            btnAdmin.FlatStyle = FlatStyle.Flat;
            btnAdmin.FlatAppearance.BorderColor = Color.FromArgb(170, 175, 185);
            btnAdmin.Size = new Size(160, 32);
            btnAdmin.Location = new Point(580, 25);
            pnlTopBar.Controls.Add(btnAdmin);

            btnExportFile = CreateTopButton("Xuất file", 440, 78);
            btnExportFile.BackColor = Color.FromArgb(215, 220, 228);
            pnlTopBar.Controls.Add(btnExportFile);

            btnImportFile = CreateTopButton("Nhập file", 560, 78);
            btnImportFile.BackColor = Color.FromArgb(215, 220, 228);
            pnlTopBar.Controls.Add(btnImportFile);

            // 4. KHUNG NỘI DUNG CHÍNH
            pnlMainContent = new Panel();
            pnlMainContent.Location = new Point(200, 150);
            pnlMainContent.Size = new Size(900, 520);
            pnlMainContent.BackColor = Color.FromArgb(185, 192, 203);

            btnDonHang = new Button();
            btnDonHang.Text = "ĐƠN HÀNG (0)";
            btnDonHang.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnDonHang.ForeColor = Color.FromArgb(40, 45, 55);
            btnDonHang.BackColor = Color.White;
            btnDonHang.FlatStyle = FlatStyle.Flat;
            btnDonHang.FlatAppearance.BorderColor = Color.FromArgb(150, 158, 168);
            btnDonHang.Size = new Size(160, 36);
            btnDonHang.Location = new Point(30, 20);
            pnlMainContent.Controls.Add(btnDonHang);
            btnDonHang.Click += new EventHandler(btnDonHang_Click);

            lblThongKeDonHang = new Label();
            lblThongKeDonHang.Text = "Đang tải dữ liệu đơn hàng...";
            lblThongKeDonHang.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            lblThongKeDonHang.ForeColor = Color.FromArgb(40, 45, 55);
            lblThongKeDonHang.Location = new Point(205, 27);
            lblThongKeDonHang.AutoSize = true;
            pnlMainContent.Controls.Add(lblThongKeDonHang);

            pnlGridContainer = new Panel();
            pnlGridContainer.Location = new Point(30, 68);
            pnlGridContainer.Size = new Size(840, 415);
            pnlGridContainer.BackColor = Color.FromArgb(228, 232, 238);

            txtSubSearch = new TextBox();
            txtSubSearch.Font = new Font("Segoe UI", 10);
            txtSubSearch.Size = new Size(390, 28);
            txtSubSearch.Location = new Point(25, 20);
            txtSubSearch.Text = "Tìm kiếm";
            txtSubSearch.ForeColor = Color.Gray;
            txtSubSearch.Enter += (s, e) => { if (txtSubSearch.Text == "Tìm kiếm") { txtSubSearch.Text = ""; txtSubSearch.ForeColor = Color.Black; } };
            txtSubSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSubSearch.Text)) { txtSubSearch.Text = "Tìm kiếm"; txtSubSearch.ForeColor = Color.Gray; } };
            pnlGridContainer.Controls.Add(txtSubSearch);

            cmbCategory = new ComboBox();
            cmbCategory.Font = new Font("Segoe UI", 10);
            cmbCategory.Size = new Size(130, 28);
            cmbCategory.Location = new Point(435, 20);
            cmbCategory.Text = "Loại sản phẩm";
            pnlGridContainer.Controls.Add(cmbCategory);

            dgvProducts = new DataGridView();
            dgvProducts.Location = new Point(25, 65);
            dgvProducts.Size = new Size(790, 325);
            dgvProducts.BackgroundColor = Color.FromArgb(254, 242, 242);
            dgvProducts.BorderStyle = BorderStyle.FixedSingle;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.RowHeadersVisible = true;
            dgvProducts.AllowUserToAddRows = false;

            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvProducts.EnableHeadersVisualStyles = false;

            dgvProducts.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "ColCheck", Width = 35 });
            dgvProducts.Columns.Add("ColProduct", "sản phẩm");
            dgvProducts.Columns.Add("ColSellable", "có thể bán");
            dgvProducts.Columns.Add("ColType", "loại");
            dgvProducts.Columns.Add("ColBrand", "Nhãn hiệu");
            dgvProducts.Columns.Add("ColDate", "ngày khởi tạo");

            pnlGridContainer.Controls.Add(dgvProducts);
            pnlMainContent.Controls.Add(pnlGridContainer);

            // Nút "chat bot" hình tròn ở góc dưới bên phải
            btnChatBot = new Button();
            btnChatBot.Text = "chat";
            btnChatBot.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnChatBot.ForeColor = Color.FromArgb(50, 60, 75);
            btnChatBot.BackColor = Color.White;
            btnChatBot.FlatStyle = FlatStyle.Flat;
            btnChatBot.FlatAppearance.BorderSize = 1;
            btnChatBot.FlatAppearance.BorderColor = Color.FromArgb(160, 168, 178);
            btnChatBot.Size = new Size(65, 65);
            btnChatBot.Location = new Point(pnlMainContent.Width - 90, pnlMainContent.Height - 85);

            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, btnChatBot.Width, btnChatBot.Height);
            btnChatBot.Region = new Region(path);

            btnChatBot.Click += btnChatBot_Click;

            pnlMainContent.Controls.Add(btnChatBot);
            btnChatBot.BringToFront();

            this.Controls.Add(pnlMainContent);
            this.Controls.Add(this.pnlTopBar);
            this.Controls.Add(this.pnlSidebar);
        }

        private Button CreateTopButton(string text, int x, int y)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(165, 172, 182);
            btn.Size = new Size(95, 32);
            btn.Location = new Point(x, y);
            return btn;
        }
        // Hàm dùng chung để quay về Form1 từ bất kỳ trang con nào bên trong Panel hoặc Form độc lập
        public static void QuayVeFormChinh(Form currentForm)
        {
            if (currentForm == null || currentForm.IsDisposed) return;

            // 1. Kiểm tra xem ứng dụng đã có instance của Form1 đang mở hay ẩn chưa, hoặc tạo mới
            Form1 mainForm = Application.OpenForms["Form1"] as Form1;
            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = new Form1("Doanh nghiệp VLXD"); // Truyền tên doanh nghiệp của bạn vào đây
            }

            mainForm.Show();
            mainForm.BringToFront();

            // 2. Nếu trang hiện tại được nhúng trong Panel của Form1 thì chỉ cần Clear Panel hoặc Dispose nó
            // Nếu nó mở dạng form độc lập thì ẩn/đóng nó đi
            currentForm.Hide();
            currentForm.Close();
        }
    }
}