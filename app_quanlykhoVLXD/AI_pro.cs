using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http; // Thêm thư viện để gọi API
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq; // Thêm thư viện đọc JSON (Nhớ cài NuGet Newtonsoft.Json)

namespace app_quanlykhoVLXD
{
    public partial class AI_pro : Form
    {
        private RichTextBox rtbChatHistory;
        private TextBox txtChatInput;
        private Button btnSend;
        private Button btnQuickStock;
        private Button btnQuickSuggest;

        public AI_pro()
        {
            InitializeComponent();
            SetupAIProLayout();
        }

        private void SetupAIProLayout()
        {
            this.Text = "✨ Trợ lý AI Pro - Phân tích Kho Thông Minh (Python API)";
            this.Size = new Size(450, 580);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(245, 246, 250);

            // Lịch sử chat dạng RichTextBox để phân biệt màu sắc
            rtbChatHistory = new RichTextBox();
            rtbChatHistory.Size = new Size(410, 420);
            rtbChatHistory.Location = new Point(12, 12);
            rtbChatHistory.Font = new Font("Segoe UI", 10);
            rtbChatHistory.ReadOnly = true;
            rtbChatHistory.BackColor = Color.White;
            rtbChatHistory.BorderStyle = BorderStyle.FixedSingle;
            rtbChatHistory.Text = "🤖 [AI Pro]: Chào bạn! Tôi là Trợ lý AI Pro phiên bản kết nối Python.\nTôi đã sẵn sàng nhận lệnh từ bạn.\n\n";
            this.Controls.Add(rtbChatHistory);

            // Ô nhập tin nhắn
            txtChatInput = new TextBox();
            txtChatInput.Size = new Size(310, 32);
            txtChatInput.Location = new Point(12, 490);
            txtChatInput.Font = new Font("Segoe UI", 11);
            txtChatInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnSend.PerformClick(); e.SuppressKeyPress = true; } };
            this.Controls.Add(txtChatInput);

            // Nút Gửi
            btnSend = new Button();
            btnSend.Text = "Gửi";
            btnSend.Size = new Size(90, 32);
            btnSend.Location = new Point(332, 489);
            btnSend.BackColor = Color.FromArgb(108, 92, 231); // Màu tím chủ đạo của AI Pro
            btnSend.ForeColor = Color.White;
            btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => { await SendMessageAsync(); };
            this.Controls.Add(btnSend);

            // Nút gợi ý nhanh 1: Tồn kho
            btnQuickStock = new Button();
            btnQuickStock.Text = "📊 Tồn kho thấp";
            btnQuickStock.Size = new Size(130, 28);
            btnQuickStock.Location = new Point(12, 445);
            StyleQuickButton(btnQuickStock);
            btnQuickStock.Click += async (s, e) => {
                txtChatInput.Text = "Kiểm tra danh sách tồn kho giúp tôi";
                await SendMessageAsync();
            };
            this.Controls.Add(btnQuickStock);

            // Nút gợi ý nhanh 2: Đề xuất nhập hàng
            btnQuickSuggest = new Button();
            btnQuickSuggest.Text = "💡 Gợi ý nhập hàng";
            btnQuickSuggest.Size = new Size(140, 28);
            btnQuickSuggest.Location = new Point(150, 445);
            StyleQuickButton(btnQuickSuggest);
            btnQuickSuggest.Click += async (s, e) => {
                txtChatInput.Text = "Phân tích và gợi ý vật tư cần nhập thêm";
                await SendMessageAsync();
            };
            this.Controls.Add(btnQuickSuggest);
        }

        private void StyleQuickButton(Button btn)
        {
            btn.BackColor = Color.FromArgb(223, 230, 233);
            btn.ForeColor = Color.FromArgb(45, 52, 54);
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        private async Task SendMessageAsync()
        {
            string userMessage = txtChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            AppendChat("👤 Bạn: ", userMessage, Color.FromArgb(9, 132, 227));
            txtChatInput.Clear();

            AppendChat("🤖 [AI Pro]: ", "Đang kết nối tới Python AI Server...", Color.Gray);
            string botReply = await GetAIProResponseAsync(userMessage);

            // Xóa dòng "Đang kết nối..." giả lập và in kết quả thật
            RemoveLastLineFromRichTextBox();
            AppendChat("🤖 [AI Pro]: ", botReply, Color.FromArgb(45, 52, 54));
        }

        private void AppendChat(string speaker, string message, Color color)
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;
            rtbChatHistory.SelectionColor = color;
            rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, speaker.Contains("Bạn") ? FontStyle.Bold : FontStyle.Regular);
            rtbChatHistory.AppendText(speaker + message + "\n\n");
            rtbChatHistory.ScrollToCaret();
        }

        private void RemoveLastLineFromRichTextBox()
        {
            int linesCount = rtbChatHistory.Lines.Length;
            if (linesCount > 2)
            {
                int startIndex = rtbChatHistory.GetFirstCharIndexFromLine(linesCount - 3);
                rtbChatHistory.Select(startIndex, rtbChatHistory.Text.Length - startIndex);
                rtbChatHistory.SelectedText = "";
            }
        }

        // ==========================================
        // PHẦN GỌI PYTHON API THAY CHO SQL TRỰC TIẾP
        // ==========================================
        private async Task<string> GetAIProResponseAsync(string prompt)
        {
            // Địa chỉ API Python đang chạy (ví dụ chạy trên cổng 8000)
            string url = $"http://127.0.0.1:8000/api/chat?prompt={Uri.EscapeDataString(prompt)}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();

                        // Đọc kết quả JSON trả về từ Python
                        JObject jsonResponse = JObject.Parse(jsonString);
                        string reply = jsonResponse["reply"]?.ToString();

                        return reply ?? "Không nhận được phản hồi từ AI.";
                    }
                    else
                    {
                        return "⚠️ Không thể kết nối tới máy chủ Python API. Hãy đảm bảo bạn đã bật file Python lên nhé!";
                    }
                }
                catch (Exception ex)
                {
                    return "⚠️ Lỗi kết nối Python API: " + ex.Message;
                }
            }
        }
    }
}