using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace app_quanlykhoVLXD
{
    public partial class FormChatBot : Form
    {
        private TextBox txtChatInput;
        private Button btnSend;
        private ListBox lstChatHistory;

        // Thay "API_KEY_CUA_BAN" bằng mã API Gemini thực tế của bạn (lấy miễn phí tại Google AI Studio)
        private static readonly string apiKey = "AQ.Ab8RN6IsdWsnqenPbpxGwP8wmOmsLd3lWidyxZ4XIefyo3wo2w";

        public FormChatBot()
        {
            InitializeComponent();
            SetupChatBotCustomLayout();
        }

        private void SetupChatBotCustomLayout()
        {
            this.Text = "Chatbot AI";
            this.Size = new Size(350, 480);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;

            lstChatHistory = new ListBox();
            lstChatHistory.Size = new Size(310, 350);
            lstChatHistory.Location = new Point(12, 12);
            lstChatHistory.HorizontalScrollbar = true;   // câu dài cuộn ngang được
            lstChatHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstChatHistory.Items.Add("Bot: Xin chào! Tôi có thể giúp gì cho bạn?");
            this.Controls.Add(lstChatHistory);

            txtChatInput = new TextBox();
            txtChatInput.Size = new Size(220, 30);
            txtChatInput.Location = new Point(12, 380);
            txtChatInput.Font = new Font("Segoe UI", 10);
            txtChatInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtChatInput);

            btnSend = new Button();
            btnSend.Text = "Gửi";
            btnSend.Size = new Size(80, 30);
            btnSend.Location = new Point(242, 379);
            btnSend.BackColor = Color.FromArgb(82, 95, 122);
            btnSend.ForeColor = Color.White;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            btnSend.Click += async (s, e) =>
            {
                string userMessage = txtChatInput.Text.Trim();
                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    lstChatHistory.Items.Add("Bạn: " + userMessage);
                    txtChatInput.Clear();

                    string botReply = await GetAIResponseAsync(userMessage);
                    lstChatHistory.Items.Add("Bot: " + botReply);

                    lstChatHistory.TopIndex = lstChatHistory.Items.Count - 1;
                }
            };
            this.Controls.Add(btnSend);
        }

        // Hàm kết nối tới AI API
        /* private async Task<string> GetAIResponseAsync(string prompt)
         {
             try
             {
                 using (HttpClient client = new HttpClient())
                 {
                     string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                     var requestBody = new
                     {
                         contents = new[]
                         {
                     new
                     {
                         parts = new[]
                         {
                             new { text = prompt }
                         }
                     }
                 }
                     };

                     string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                     HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                     HttpResponseMessage response = await client.PostAsync(url, content);
                     string jsonResponse = await response.Content.ReadAsStringAsync();

                     if (response.IsSuccessStatusCode)
                     {
                         JObject parsedData = JObject.Parse(jsonResponse);
                         string reply = parsedData["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                         return string.IsNullOrEmpty(reply) ? "Bot không trả về nội dung." : reply.Trim();
                     }
                     else
                     {
                         return $"Lỗi API ({response.StatusCode}): Kiểm tra lại Key.";
                     }
                 }
             }
             catch (Exception ex)
             {
                 return "Lỗi ngoại lệ: " + ex.Message;
             }
         }*/
        // Hàm trả lời thông minh dựa trên từ khóa (Phục vụ vấn đáp / demo offline)
        private async Task<string> GetAIResponseAsync(string prompt)
        {
            // Giả lập độ trễ nhỏ (0.5 giây) cho giống đang suy nghĩ thật
            await Task.Delay(500);

            string query = prompt.ToLower().Trim();

            // 1. Chào hỏi
            if (query.Contains("chào") || query.Contains("hi") || query.Contains("hello"))
            {
                return "Xin chào bạn! Tôi là trợ lý ảo tích hợp trong hệ thống Quản lý kho VLXD. Tôi có thể giúp gì cho bạn?";
            }
            // 2. Về sản phẩm / vật liệu
            else if (query.Contains("sản phẩm") || query.Contains("vật liệu") || query.Contains("hàng hóa"))
            {
                return "Hệ thống hiện quản lý các loại vật liệu xây dựng đa dạng như: xi măng, sắt thép, gạch ống, cát, đá xây dựng và các thiết bị phần cứng khác.";
            }
            // 3. Về tồn kho
            else if (query.Contains("tồn kho") || query.Contains("kho") || query.Contains("số lượng"))
            {
                return "Bạn có thể theo dõi số lượng hàng tồn kho, hàng 'có thể bán' trực tiếp tại bảng danh sách ở màn hình chính hoặc mục 'Tồn kho' trên thanh menu bên trái.";
            }
            // 4. Về đơn hàng / tạo đơn
            else if (query.Contains("đơn hàng") || query.Contains("tạo đơn") || query.Contains("bán hàng"))
            {
                return "Để tạo đơn hàng mới, bạn hãy bấm vào nút '+ Tạo đơn hàng' màu trắng ở màn hình chính, hệ thống sẽ mở giao diện quản lý sản phẩm tương ứng.";
            }
            // 5. Về chức năng phần mềm / tác giả
            else if (query.Contains("chức năng") || query.Contains("phần mềm") || query.Contains("ứng dụng"))
            {
                return "Ứng dụng 'app_quanlykhoVLXD' hỗ trợ quản lý toàn diện: Quản lý danh mục sản phẩm, theo dõi tồn kho, nhập/xuất file dữ liệu và hỗ trợ tra cứu thông minh qua Chatbot AI.";
            }
            // Câu trả lời mặc định nếu không khớp từ khóa nào
            else
            {
                return $"Tôi đã ghi nhận câu hỏi: \"{prompt}\". Trong phiên bản quản lý kho VLXD này, tôi hỗ trợ bạn tra cứu thông tin sản phẩm, kiểm tra tồn kho và hướng dẫn thao tác tạo đơn hàng.";
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 431);
            this.Name = "FormChatBot";
            this.ResumeLayout(false);
        }
    }
}