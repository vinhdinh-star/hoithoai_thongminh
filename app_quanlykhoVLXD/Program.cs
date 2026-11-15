using System;
using System.Windows.Forms;

namespace app_quanlykhoVLXD
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 🌟 Đặt FrmDangNhap làm màn hình xuất hiện đầu tiên của ứng dụng
            Application.Run(new FrmDangNhap());
        }
    }
}