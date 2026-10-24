using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    public partial class donhang : Form
    {
        private Panel pnlSidebar;
        private Panel pnlTopBar;
        private Panel pnlMainContent;

        // TopBar controls
        private TextBox txtGlobalSearch;
        private Button btnAdmin;

        // MainContent controls
        private Label lblPageTitleHeader; // Dòng chữ "Tạo đơn hàng" ngoài cùng góc trên
        private Button btnDanhSachSanPham;
        private Panel pnlCenterCard;
        private Button btnNhapFile;
        private Button btnTaoDonHang;
        private Button btnChatBot;

        public donhang()
        {
            InitializeComponent();
            InitializeCustomLayout();
        }

        private void btnChatBot_Click(object sender, EventArgs e)
        {
            FormChatBot botForm = new FormChatBot();
            botForm.TopMost = true;
            botForm.Show(); // Mở cửa sổ chat bot nổi lên trên màn hình
        }

        private void btnThemSanPham_Click(object sender, EventArgs e)
        {
            sanpham frmSP = new sanpham();
            frmSP.Show(); // Mở form sản phẩm lên mà vẫn giữ nguyên form đơn hàng hiện tại
        }

        private void InitializeCustomLayout()
        {
            // 1. Cấu hình Form chính
            this.Text = "app_quanlykhoVLXD - Tạo đơn hàng";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(235, 238, 242);

            // Dòng chữ tiêu đề "Tạo đơn hàng" ở ngoài cùng góc trên bên trái
            lblPageTitleHeader = new Label();
            lblPageTitleHeader.Text = "Tạo đơn hàng";
            lblPageTitleHeader.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblPageTitleHeader.ForeColor = Color.FromArgb(150, 155, 165);
            lblPageTitleHeader.Location = new Point(15, 12);
            lblPageTitleHeader.AutoSize = true;
            this.Controls.Add(lblPageTitleHeader);

            // 2. SIDEBAR (Menu bên trái)
            pnlSidebar = new Panel();
            pnlSidebar.Location = new Point(0, 45); // Chừa chỗ cho tiêu đề phía trên
            pnlSidebar.Size = new Size(200, 615);
            pnlSidebar.BackColor = Color.FromArgb(82, 95, 122);

            Label lblSidebarTitle = new Label();
            lblSidebarTitle.Text = "Tổng quan";
            lblSidebarTitle.ForeColor = Color.White;
            lblSidebarTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblSidebarTitle.Location = new Point(15, 20);
            lblSidebarTitle.AutoSize = true;
            pnlSidebar.Controls.Add(lblSidebarTitle);

            // Đưa nút AI Pro trực tiếp vào mảng menuItems
            // Đưa nút AI Pro trực tiếp vào mảng menuItems
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
      "Báo cáo"
  };

            int startY = 65;
            for (int i = 0; i < menuItems.Length; i++)
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

                // Nếu là mục AI Pro thì đổi màu tím nổi bật và gọi form AI_pro mới
                if (itemName.Contains("AI Pro"))
                {
                    btnMenu.BackColor = Color.FromArgb(108, 92, 231); // Màu tím AI hiện đại
                    btnMenu.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    btnMenu.Click += (s, e) => {
                        AI_pro frmAI = new AI_pro(); // Gọi form AI_pro độc lập
                        frmAI.TopMost = true;
                        frmAI.Show();
                    };
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(128, 114, 235); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(108, 92, 231); };
                }
                else if (i == 0)
                {
                    btnMenu.BackColor = Color.FromArgb(145, 156, 175);
                }
                else
                {
                    btnMenu.BackColor = Color.FromArgb(82, 95, 122);
                    btnMenu.MouseHover += (s, e) => { btnMenu.BackColor = Color.FromArgb(105, 118, 143); };
                    btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(82, 95, 122); };
                }

                pnlSidebar.Controls.Add(btnMenu);
            }

            // 3. THANH TOPBAR (Phía trên bên phải)
            pnlTopBar = new Panel();
            pnlTopBar.Location = new Point(200, 45);
            pnlTopBar.Size = new Size(885, 95);
            pnlTopBar.BackColor = Color.FromArgb(205, 210, 218);

            txtGlobalSearch = new TextBox();
            txtGlobalSearch.Font = new Font("Segoe UI", 11);
            txtGlobalSearch.Size = new Size(530, 32);
            txtGlobalSearch.Location = new Point(45, 30);
            txtGlobalSearch.Text = "Tìm kiếm";
            txtGlobalSearch.ForeColor = Color.Gray;
            txtGlobalSearch.Enter += (s, e) => { if (txtGlobalSearch.Text == "Tìm kiếm") { txtGlobalSearch.Text = ""; txtGlobalSearch.ForeColor = Color.Black; } };
            txtGlobalSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtGlobalSearch.Text)) { txtGlobalSearch.Text = "Tìm kiếm"; txtGlobalSearch.ForeColor = Color.Gray; } };
            pnlTopBar.Controls.Add(txtGlobalSearch);

            btnAdmin = new Button();
            btnAdmin.Text = "ADIMN";
            btnAdmin.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAdmin.BackColor = Color.FromArgb(220, 225, 230);
            btnAdmin.FlatStyle = FlatStyle.Flat;
            btnAdmin.FlatAppearance.BorderColor = Color.FromArgb(170, 175, 185);
            btnAdmin.Size = new Size(100, 32);
            btnAdmin.Location = new Point(610, 30);
            pnlTopBar.Controls.Add(btnAdmin);

            // 4. KHUNG NỘI DUNG CHÍNH
            pnlMainContent = new Panel();
            pnlMainContent.Location = new Point(200, 140);
            pnlMainContent.Size = new Size(885, 520);
            pnlMainContent.BackColor = Color.FromArgb(185, 192, 203);

            // Nút "DANH SÁCH ĐƠN HÀNG" màu trắng ở góc trên trái phần nội dung
            btnDanhSachSanPham = new Button();
            btnDanhSachSanPham.Text = "DANH SÁCH ĐƠN HÀNG";
            btnDanhSachSanPham.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnDanhSachSanPham.ForeColor = Color.FromArgb(40, 45, 55);
            btnDanhSachSanPham.BackColor = Color.White;
            btnDanhSachSanPham.FlatStyle = FlatStyle.Flat;
            btnDanhSachSanPham.FlatAppearance.BorderColor = Color.FromArgb(150, 158, 168);
            btnDanhSachSanPham.Size = new Size(185, 38);
            btnDanhSachSanPham.Location = new Point(45, 25);
            pnlMainContent.Controls.Add(btnDanhSachSanPham);

            // Khung thẻ chữ nhật màu trắng nhạt ở giữa chứa nút Nhập file và Tạo đơn hàng
            pnlCenterCard = new Panel();
            pnlCenterCard.Location = new Point(45, 80);
            pnlCenterCard.Size = new Size(795, 360);
            pnlCenterCard.BackColor = Color.FromArgb(225, 229, 235);

            // Nút "Nhập file" bên trong khung
            btnNhapFile = new Button();
            btnNhapFile.Text = "Nhập file";
            btnNhapFile.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btnNhapFile.ForeColor = Color.FromArgb(40, 45, 55);
            btnNhapFile.BackColor = Color.White;
            btnNhapFile.FlatStyle = FlatStyle.Flat;
            btnNhapFile.FlatAppearance.BorderColor = Color.FromArgb(160, 168, 178);
            btnNhapFile.Size = new Size(115, 42);
            btnNhapFile.Location = new Point(245, 155);
            pnlCenterCard.Controls.Add(btnNhapFile);

            btnTaoDonHang = new Button();
            btnTaoDonHang.Text = "+ Tạo đơn hàng";
            btnTaoDonHang.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnTaoDonHang.ForeColor = Color.FromArgb(40, 45, 55);
            btnTaoDonHang.BackColor = Color.White;
            btnTaoDonHang.FlatStyle = FlatStyle.Flat;
            btnTaoDonHang.FlatAppearance.BorderColor = Color.FromArgb(160, 168, 178);
            btnTaoDonHang.Size = new Size(145, 42);
            btnTaoDonHang.Location = new Point(375, 155);

            // ĐĂNG KÝ SỰ KIỆN: Khi bấm vào nút này sẽ mở form sản phẩm
            btnTaoDonHang.Click += (s, e) => {
                sanpham frmSP = new sanpham();
                frmSP.Show();
            };

            pnlCenterCard.Controls.Add(btnTaoDonHang);
            pnlMainContent.Controls.Add(pnlCenterCard);

            // Nút "chat bot" hình tròn ở góc dưới bên phải
            btnChatBot = new Button();
            btnChatBot.Text = "chat bot";
            btnChatBot.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnChatBot.ForeColor = Color.FromArgb(50, 60, 75);
            btnChatBot.BackColor = Color.White;
            btnChatBot.FlatStyle = FlatStyle.Flat;
            btnChatBot.FlatAppearance.BorderSize = 1;
            btnChatBot.FlatAppearance.BorderColor = Color.FromArgb(160, 168, 178);
            btnChatBot.Size = new Size(65, 65);
            btnChatBot.Location = new Point(pnlMainContent.Width - 90, 380);

            // Bo tròn nút chat bot
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, btnChatBot.Width, btnChatBot.Height);
            btnChatBot.Region = new Region(path);

            // Đăng ký sự kiện khi bấm vào nút
            btnChatBot.Click += new EventHandler(btnChatBot_Click);

            pnlMainContent.Controls.Add(btnChatBot);
            btnChatBot.BringToFront(); // Đẩy nút lên lớp hiển thị cao nhất

            this.Controls.Add(pnlMainContent);
            this.Controls.Add(pnlTopBar);
            this.Controls.Add(pnlSidebar);
        }
    }
}