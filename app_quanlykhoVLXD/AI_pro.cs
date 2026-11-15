using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace app_quanlykhoVLXD
{
    public partial class AI_pro : Form
    {
        // ⚠️ Sửa chuỗi kết nối cho đúng máy của bạn
        private const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";
        private const string ModelFileName = "Models\\qwen2.5-1.5b-instruct-q5_k_m.gguf";
        private const int MaxRowsShown = 30;
        private const double MatchThreshold = 0.75;   // chỉ dùng câu mẫu khi khá giống; còn lại để AI tự viết SQL
        private const int LowStockThreshold = 300;
        private const int QuestionBankCacheMinutes = 5;

        private const bool ExplainWithQwen = true;
        private const string QwenChatUrl = "http://127.0.0.1:8000/api/chat";
        private string lastResultText;   // kết quả bảng gần nhất, để Qwen có dữ liệu mà giải thích

        // true = hiện thêm bong bóng "🧾 SQL: ..." để kiểm tra câu nào được chọn
        private const bool ShowSql = false;

        // Model Python trên CPU rất chậm nên mặc định TẮT
        private const bool UseModelFallback = false;

        // ---- AI viết SQL (Claude API) cho những câu hỏi không có trong bảng CauHoiSQL ----
        private const bool UseClaudeApi = true;
        private const string ClaudeModel = "claude-sonnet-5-5";
        // Khuyến nghị đặt khóa trong biến môi trường ANTHROPIC_API_KEY. Có thể dán tạm vào đây để thử (đừng đưa lên GitHub).
        private const string ApiKeyInline = "";
        // true = câu hỏi AI trả lời thành công sẽ được lưu vào CauHoiSQL để lần sau dùng lại ngay
        private const bool AutoLearn = false;

        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        // ---- Giao diện chat ----
        private static readonly Font UiFont = new Font("Segoe UI", 10f);
        private static readonly Font MonoFont = new Font("Consolas", 9f);
        private static readonly Color ChatBackColor = Color.FromArgb(236, 239, 244);
        private static readonly CultureInfo ViVN = CultureInfo.GetCultureInfo("vi-VN");

        private ChatPanel pnlChat;
        private TextBox txtChatInput;
        private Button btnSend;
        private Button btnQuickStock;
        private Button btnQuickSuggest;
        private bool resizingBubbles;

        private static readonly string[][] ExplainRules =
{
    new[] { "giai thich|dien giai|y nghia|la sao|tai sao|vi sao",
            "Hãy giải thích ngắn gọn, dễ hiểu, như đang nói với người không chuyên." },
    new[] { "phan tich|nhan xet|danh gia|so sanh",
            "Hãy phân tích số liệu: nêu 2-3 điểm đáng chú ý nhất (cao nhất, thấp nhất, bất thường)." },
    new[] { "tom tat|tong ket|noi ngan gon",
            "Hãy tóm tắt trong tối đa 3 ý." },
    new[] { "tu van|de xuat|nen lam gi|cach xu ly|khac phuc",
            "Hãy đưa ra 2-3 đề xuất hành động cụ thể cho người quản lý kho." },
    new[] { "viet giup|soan giup|viet bao cao|soan thao",
            "Hãy soạn một đoạn văn bản/báo cáo ngắn, lịch sự, dùng được ngay." },
};

        private bool TryGetExplainTask(string userMessage, out string instruction)
        {
            instruction = null;
            string lower = RemoveDiacritics(userMessage);
            foreach (string[] rule in ExplainRules)
                foreach (string kw in rule[0].Split('|'))
                    if (Regex.IsMatch(lower, @"\b" + Regex.Escape(kw.Trim()) + @"\b"))
                    {
                        instruction = rule[1];
                        return true;
                    }
            return false;
        }

        private async Task HandleExplainAsync(string question, string instruction)
        {
            ChatBubble waiting = AddBotMessage("🤖 Qwen đang soạn câu trả lời, vui lòng chờ...");
            try
            {
                JObject body = new JObject();
                body["question"] = question;
                body["instruction"] = instruction;
                body["context"] = lastResultText ?? "";

                using (StringContent content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage resp = await httpClient.PostAsync(QwenChatUrl, content);
                    string json = await resp.Content.ReadAsStringAsync();
                    RemoveBubble(waiting); waiting = null;

                    if (!resp.IsSuccessStatusCode)
                    {
                        AddBotError("⚠️ Qwen trả lỗi HTTP " + (int)resp.StatusCode);
                        return;
                    }
                    string answer = (string)JObject.Parse(json)["answer"];
                    AddBotMessage(string.IsNullOrWhiteSpace(answer) ? "Qwen không trả về nội dung." : answer.Trim());
                }
            }
            catch (Exception ex)
            {
                if (waiting != null) RemoveBubble(waiting);
                AddBotError("⚠️ Chưa kết nối được Qwen (đã chạy app.py chưa?): " + ex.Message);
            }
        }
        // Các tên có thể là cùng một cột. So khớp không phân biệt hoa thường, bỏ dấu cách và gạch dưới
        // nên "Dien Thoai", "dien_thoai", "DienThoai" được coi là một.
        private static readonly string[][] ColumnSynonyms =
        {
    new[] { "DienThoai", "SoDienThoai", "SDT", "SoDT", "Phone", "Tel" },
    new[] { "DiaChi", "DiaChiNCC", "Address" },
    new[] { "HoTen", "TenNV", "TenNhanVien" },
    new[] { "ChucVu", "ChucDanh" },
};

        private static string NormIdent(string s)
        {
            return Regex.Replace(RemoveDiacritics(s), @"[\s_]", "");
        }

        private static string QuoteIdent(string s)
        {
            return Regex.IsMatch(s, @"^\w+$") ? s : "[" + s + "]";
        }

        // Cột nào trong câu SQL không tồn tại ở các bảng được nhắc tới thì thay bằng cột tương đương có thật
        private string FixColumnNames(string sql)
        {
            if (schema.Count == 0) return sql;

            var tables = schema.Where(kv =>
                Regex.IsMatch(sql, @"\b" + Regex.Escape(kv.Key) + @"\b", RegexOptions.IgnoreCase)).ToList();
            if (tables.Count == 0) return sql;

            foreach (string[] group in ColumnSynonyms)
            {
                HashSet<string> norms = new HashSet<string>(group.Select(NormIdent));

                // tên thật của cột trong các bảng đang dùng (nếu có)
                string actual = null;
                foreach (var t in tables)
                {
                    actual = t.Value.FirstOrDefault(c => norms.Contains(NormIdent(c)));
                    if (actual != null) break;
                }
                if (actual == null) continue;

                foreach (string name in group)
                {
                    if (name.Equals(actual, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!Regex.IsMatch(sql, @"\b" + Regex.Escape(name) + @"\b", RegexOptions.IgnoreCase)) continue;
                    sql = Regex.Replace(sql, @"\b" + Regex.Escape(name) + @"\b", QuoteIdent(actual).Replace("$", "$$"),
                                        RegexOptions.IgnoreCase);
                }
            }
            return sql;
        }
        // ---- Tên cột giá của bảng VatTu (tự dò khi chạy, tránh lỗi "Invalid column name") ----
        private static readonly string[] PriceColumnCandidates =
            { "DonGia", "GiaTien", "GiaBan", "Gia", "DonGiaBan", "GiaNhap" };
        private string priceColumn;          // null nếu bảng VatTu không có cột giá
        private bool schemaLoaded;

        // ---- Ngân hàng câu hỏi (bảng CauHoiSQL) ----
        private class QA
        {
            public int Id;
            public string Question;
            public string Sql;
            public HashSet<string> Tokens;
            public HashSet<string> Topics;
            public double Weight;
        }
        private class Candidate
        {
            public QA Item;
            public double Score;
        }
        private List<QA> questionBank;
        private DateTime questionBankLoadedAt = DateTime.MinValue;
        private Dictionary<string, double> idf;
        private double defaultIdf;

        public AI_pro()
        {
            InitializeComponent();
            SetupAIProLayout();
        }

        private void SetupAIProLayout()
        {
            this.Text = "✨ Trợ lý AI Pro - Phân tích Kho Thông Minh";
            this.ClientSize = new Size(480, 580);
            this.MinimumSize = new Size(360, 420);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(245, 246, 250);

            pnlChat = new ChatPanel();
            pnlChat.Location = new Point(12, 12);
            pnlChat.Size = new Size(456, 468);
            pnlChat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlChat.FlowDirection = FlowDirection.TopDown;
            pnlChat.WrapContents = false;
            pnlChat.AutoScroll = true;
            pnlChat.BackColor = ChatBackColor;
            pnlChat.BorderStyle = BorderStyle.FixedSingle;
            pnlChat.ClientSizeChanged += (s, e) => ResizeBubbles();
            this.Controls.Add(pnlChat);

            txtChatInput = new TextBox();
            txtChatInput.Size = new Size(356, 32);
            txtChatInput.Location = new Point(12, 531);
            txtChatInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtChatInput.Font = new Font("Segoe UI", 11);
            txtChatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { btnSend.PerformClick(); e.SuppressKeyPress = true; }
            };
            this.Controls.Add(txtChatInput);

            btnSend = new Button();
            btnSend.Text = "Gửi";
            btnSend.Size = new Size(90, 32);
            btnSend.Location = new Point(378, 529);
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.BackColor = Color.FromArgb(108, 92, 231);
            btnSend.ForeColor = Color.White;
            btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => { await SendMessageAsync(); };
            this.Controls.Add(btnSend);

            btnQuickStock = new Button();
            btnQuickStock.Text = "📊 Tồn kho thấp";
            btnQuickStock.Size = new Size(130, 28);
            btnQuickStock.Location = new Point(12, 490);
            btnQuickStock.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            StyleQuickButton(btnQuickStock);
            btnQuickStock.Click += async (s, e) =>
            {
                string sql =
                    "SELECT kho.TenKho, vt.TenVT, tk.SoLuongTon, vt.DonViTinh " +
                    "FROM TonKho tk JOIN VatTu vt ON tk.MaVT = vt.MaVT JOIN Kho kho ON tk.MaKho = kho.MaKho " +
                    "WHERE tk.SoLuongTon < " + LowStockThreshold + " ORDER BY tk.SoLuongTon";
                await SendMessageAsync("Vật tư tồn kho thấp (dưới " + LowStockThreshold + ")", sql);
            };
            this.Controls.Add(btnQuickStock);

            btnQuickSuggest = new Button();
            btnQuickSuggest.Text = "💡 Gợi ý nhập hàng";
            btnQuickSuggest.Size = new Size(140, 28);
            btnQuickSuggest.Location = new Point(150, 490);
            btnQuickSuggest.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            StyleQuickButton(btnQuickSuggest);
            btnQuickSuggest.Click += async (s, e) =>
            {
                string sql =
                    "SELECT vt.MaVT, vt.TenVT, ISNULL(SUM(tk.SoLuongTon), 0) AS TongTon, " +
                    "ISNULL((SELECT SUM(x.SoLuong) FROM ChiTietPhieuXuat x WHERE x.MaVT = vt.MaVT), 0) AS DaXuat " +
                    "FROM VatTu vt LEFT JOIN TonKho tk ON tk.MaVT = vt.MaVT " +
                    "GROUP BY vt.MaVT, vt.TenVT " +
                    "HAVING ISNULL(SUM(tk.SoLuongTon), 0) < " + LowStockThreshold + " ORDER BY TongTon";
                await SendMessageAsync("Gợi ý vật tư cần nhập thêm", sql);
            };
            this.Controls.Add(btnQuickSuggest);

            AddBotMessage("Chào bạn! Hãy hỏi về nhân viên, kho, nhà cung cấp, phiếu nhập/xuất...\n" +
                          "Ví dụ: “Tổng doanh thu xuất hàng”, “Danh sách nhà cung cấp”.");
        }

        private void StyleQuickButton(Button btn)
        {
            btn.BackColor = Color.FromArgb(223, 230, 233);
            btn.ForeColor = Color.FromArgb(45, 52, 54);
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        private void SetBusy(bool busy)
        {
            btnSend.Enabled = !busy;
            txtChatInput.Enabled = !busy;
            btnQuickStock.Enabled = !busy;
            btnQuickSuggest.Enabled = !busy;
            if (!busy) txtChatInput.Focus();
        }

        // ==========================================
        // KHUNG CHAT 2 BÊN (BONG BÓNG TIN NHẮN)
        // ==========================================
        private class ChatPanel : FlowLayoutPanel
        {
            public ChatPanel() { DoubleBuffered = true; }
        }

        private class ChatBubble : Control
        {
            private const int PadX = 12, PadY = 8, SideMargin = 8, VMargin = 4, Radius = 14;
            private const int GridPad = 8, GridRowH = 26, GridHeaderH = 30, MaxGridRows = 8, MinGridRows = 5;
            private static readonly TextFormatFlags Flags =
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                TextFormatFlags.Left | TextFormatFlags.Top;

            private readonly bool fromUser;
            private readonly Color bubbleColor;
            private Rectangle bubbleRect;
            private Rectangle textRect;
            private DataGridView grid;

            public ChatBubble(string text, bool fromUser, Color bubbleColor, Color textColor, Font font, Color rowBack)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

                this.fromUser = fromUser;
                this.bubbleColor = bubbleColor;
                Text = text;
                Font = font;
                ForeColor = textColor;
                BackColor = rowBack;
                Margin = Padding.Empty;

                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Sao chép", null, (s, e) =>
                {
                    try { Clipboard.SetText(LayNoiDungSaoChep()); } catch { }
                });
                ContextMenuStrip = menu;
            }

            public void SetGrid(DataGridView g)
            {
                grid = g;
                g.ContextMenuStrip = ContextMenuStrip;
                Controls.Add(g);
            }

            private string LayNoiDungSaoChep()
            {
                if (grid == null) return Text;

                StringBuilder sb = new StringBuilder();
                List<string> head = new List<string>();
                foreach (DataGridViewColumn c in grid.Columns) head.Add(c.HeaderText);
                sb.AppendLine(string.Join("\t", head));
                foreach (DataGridViewRow r in grid.Rows)
                {
                    List<string> cells = new List<string>();
                    foreach (DataGridViewCell c in r.Cells) cells.Add(Convert.ToString(c.Value) ?? "");
                    sb.AppendLine(string.Join("\t", cells));
                }
                return sb.ToString();
            }

            public void FitToWidth(int rowWidth)
            {
                if (grid != null) { FitGrid(rowWidth); return; }

                int maxBubble = (int)((rowWidth - 2 * SideMargin) * 0.85);
                int maxText = Math.Max(40, maxBubble - 2 * PadX);

                Size s = TextRenderer.MeasureText(Text, Font, new Size(maxText, int.MaxValue), Flags);
                int bw = s.Width + 2 * PadX + 2;
                int bh = s.Height + 2 * PadY;
                int x = fromUser ? rowWidth - SideMargin - bw : SideMargin;

                bubbleRect = new Rectangle(x, VMargin, bw, bh);
                textRect = new Rectangle(x + PadX, VMargin + PadY, s.Width + 2, s.Height);

                Width = rowWidth;
                Height = bh + 2 * VMargin;
                Invalidate();
            }

            // Bong bóng chứa bảng: rộng gần hết khung chat, cao tối thiểu 5 dòng, tối đa 8 dòng rồi cuộn
            private void FitGrid(int rowWidth)
            {
                int bw = (int)((rowWidth - 2 * SideMargin) * 0.96);
                int inner = bw - 2 * GridPad;
                bool cuonDoc = grid.Rows.Count > MaxGridRows;
                int avail = inner - (cuonDoc ? SystemInformation.VerticalScrollBarWidth : 0);

                bool cuonNgang = DatDoRongCot(grid, avail);

                int soDong = Math.Max(MinGridRows, Math.Min(grid.Rows.Count, MaxGridRows));
                int gh = GridHeaderH + soDong * GridRowH + 2 +
                         (cuonNgang ? SystemInformation.HorizontalScrollBarHeight : 0);

                if (cuonDoc && cuonNgang) grid.ScrollBars = ScrollBars.Both;
                else if (cuonDoc) grid.ScrollBars = ScrollBars.Vertical;
                else if (cuonNgang) grid.ScrollBars = ScrollBars.Horizontal;
                else grid.ScrollBars = ScrollBars.None;

                int bh = gh + 2 * GridPad;
                bubbleRect = new Rectangle(SideMargin, VMargin, bw, bh);
                grid.SetBounds(SideMargin + GridPad, VMargin + GridPad, inner, gh);

                Width = rowWidth;
                Height = bh + 2 * VMargin;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = RoundedRect(bubbleRect, Radius))
                using (SolidBrush brush = new SolidBrush(bubbleColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (grid == null)
                    TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, Flags);
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
        }

        // Thừa chỗ thì giãn đều cho vừa khung, thiếu chỗ thì giữ độ rộng thật và cuộn ngang
        private static bool DatDoRongCot(DataGridView g, int avail)
        {
            int n = g.Columns.Count;
            if (n == 0) return false;

            Font fontDau = g.ColumnHeadersDefaultCellStyle.Font ?? g.Font;
            int[] w = new int[n];
            for (int c = 0; c < n; c++)
            {
                int best = TextRenderer.MeasureText(g.Columns[c].HeaderText, fontDau).Width;
                foreach (DataGridViewRow r in g.Rows)
                {
                    int tw = TextRenderer.MeasureText(DinhDangO(r.Cells[c].Value), g.Font).Width;
                    if (tw > best) best = tw;
                }
                w[c] = Math.Max(60, Math.Min(280, best + 24));
            }

            int sum = 0;
            foreach (int x in w) sum += x;

            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            if (sum < avail)
            {
                int used = 0;
                for (int c = 0; c < n; c++)
                {
                    int cw = (int)((long)w[c] * avail / sum);
                    g.Columns[c].Width = cw;
                    used += cw;
                }
                g.Columns[n - 1].Width += avail - used;
                return false;
            }

            for (int c = 0; c < n; c++) g.Columns[c].Width = w[c];
            return true;
        }

        private static string DinhDangO(object v)
        {
            if (v == null || v is DBNull) return "";
            if (v is double || v is float || v is decimal) return ((IFormattable)v).ToString("#,##0.##", ViVN);
            if (v is DateTime) return ((DateTime)v).ToString("dd/MM/yyyy", ViVN);
            return Convert.ToString(v) ?? "";
        }

        private int RowWidth()
        {
            int w = pnlChat.ClientSize.Width;
            if (!pnlChat.VerticalScroll.Visible) w -= SystemInformation.VerticalScrollBarWidth;
            return Math.Max(120, w);
        }

        private void ResizeBubbles()
        {
            if (resizingBubbles || pnlChat == null) return;
            resizingBubbles = true;
            try
            {
                int w = RowWidth();
                pnlChat.SuspendLayout();
                foreach (Control c in pnlChat.Controls)
                {
                    ChatBubble b = c as ChatBubble;
                    if (b != null) b.FitToWidth(w);
                }
                pnlChat.ResumeLayout(true);
            }
            finally
            {
                resizingBubbles = false;
            }
        }

        private ChatBubble PushBubble(ChatBubble bubble)
        {
            bubble.FitToWidth(RowWidth());
            pnlChat.Controls.Add(bubble);
            pnlChat.PerformLayout();
            if (this.IsHandleCreated)
                this.BeginInvoke(new Action(() => pnlChat.ScrollControlIntoView(bubble)));
            return bubble;
        }

        private ChatBubble AddBubble(string text, bool fromUser, Color bubbleColor, Color textColor, Font font)
        {
            return PushBubble(new ChatBubble(text, fromUser, bubbleColor, textColor, font, ChatBackColor));
        }

        private ChatBubble AddUserMessage(string text)
        {
            return AddBubble(text, true, Color.FromArgb(9, 132, 227), Color.White, UiFont);
        }

        private ChatBubble AddBotMessage(string text)
        {
            return AddBubble(text, false, Color.White, Color.FromArgb(45, 52, 54), UiFont);
        }

        private ChatBubble AddBotSql(string text)
        {
            return AddBubble(text, false, Color.FromArgb(223, 230, 233), Color.FromArgb(45, 52, 54), MonoFont);
        }

        private ChatBubble AddBotError(string text)
        {
            return AddBubble(text, false, Color.FromArgb(255, 235, 235), Color.Firebrick, UiFont);
        }

        private ChatBubble AddBotGridTable(DataTable dt)
        {
            DataGridView g = new DataGridView();
            g.RowTemplate.Height = 26;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Color.FromArgb(235, 238, 243);
            g.Font = new Font("Segoe UI", 9f);
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 30;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(45, 52, 54);
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 246, 250);
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 52, 54);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 222, 250);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 52, 54);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
            g.DataSource = dt;

            foreach (DataGridViewColumn c in g.Columns)
            {
                Type t = c.ValueType;
                if (t == typeof(double) || t == typeof(decimal) || t == typeof(float))
                {
                    c.DefaultCellStyle.Format = "#,##0.##";
                    c.DefaultCellStyle.FormatProvider = ViVN;
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (t == typeof(int) || t == typeof(long) || t == typeof(short))
                {
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (t == typeof(DateTime))
                {
                    c.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }

            ChatBubble bubble = new ChatBubble("", false, Color.White, Color.FromArgb(45, 52, 54), UiFont, ChatBackColor);
            bubble.SetGrid(g);
            return PushBubble(bubble);
        }

        private void RemoveBubble(ChatBubble bubble)
        {
            if (bubble == null) return;
            pnlChat.Controls.Remove(bubble);
            bubble.Dispose();
        }

        private static DataTable CopyFirstRows(DataTable dt, int n)
        {
            DataTable copy = dt.Clone();
            for (int i = 0; i < n && i < dt.Rows.Count; i++) copy.ImportRow(dt.Rows[i]);
            return copy;
        }

        // ==========================================
        // LUỒNG CHÍNH
        // ==========================================
        private static string ApiKey
        {
            get
            {
                string k = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                return string.IsNullOrWhiteSpace(k) ? ApiKeyInline : k;
            }
        }

        private static bool HasApiKey
        {
            get { return UseClaudeApi && !string.IsNullOrWhiteSpace(ApiKey); }
        }
        // Chuyển bảng kết quả thành chuỗi chữ để gửi cho Qwen giải thích
        private string FormatTable(DataTable dt)
        {
            if (dt.Rows.Count == 0) return "Không có dữ liệu.";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join(" | ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            sb.AppendLine(new string('-', 40));

            foreach (DataRow row in dt.Rows)
            {
                List<string> cells = new List<string>();
                foreach (object v in row.ItemArray)
                    cells.Add(DinhDangO(v));          // dùng lại hàm định dạng số/ngày đã có
                sb.AppendLine(string.Join(" | ", cells));
            }
            return sb.ToString().TrimEnd();
        }
        private async Task SendMessageAsync(string overrideQuestion = null, string directSql = null)
        {
            string userMessage = overrideQuestion ?? txtChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            SetBusy(true);
            try
            {
                AddUserMessage(userMessage);
                if (overrideQuestion == null) txtChatInput.Clear();

                // 1. Xã giao / ngoài lề
                if (directSql == null && TryHandleSmallTalk(userMessage, out string smallTalkReply))
                {
                    AddBotMessage(smallTalkReply);
                    return;
                }
                if (directSql == null && ExplainWithQwen && TryGetExplainTask(userMessage, out string explainTask))
                {
                    await HandleExplainAsync(userMessage, explainTask);
                    return;
                }
                await Task.Run(() => EnsureSchemaInfo());

                string sql = directSql;
                bool fromAi = false;
                List<Candidate> ranked = null;

                if (sql == null)
                {
                    ranked = await Task.Run(() => RankQuestions(userMessage));
                    Candidate best = ranked.Count > 0 ? ranked[0] : null;

                    // 2. Câu hỏi mẫu giống gần như hoàn toàn -> chạy luôn SQL có sẵn
                    if (best != null && best.Score >= MatchThreshold)
                    {
                        AddBotMessage("Mình hiểu câu hỏi của bạn là:\n“" + best.Item.Question + "”");
                        sql = FixPriceColumn(best.Item.Sql);
                    }
                    else
                    {
                        // 3. AI tự viết SQL dựa trên cấu trúc DB + các câu mẫu giống nhất
                        if (HasApiKey)
                        {
                            ChatBubble waiting = AddBotMessage("🤖 Đang phân tích câu hỏi...");
                            try
                            {
                                sql = await AskClaudeForSqlAsync(userMessage, ranked, null, null);
                                fromAi = sql != null;
                            }
                            catch (Exception ex)
                            {
                                AddBotError("⚠️ Không gọi được AI: " + ex.Message);
                            }
                            finally
                            {
                                RemoveBubble(waiting);
                            }
                        }

                        // 4. Không có AI / AI thất bại -> bộ từ khóa
                        if (sql == null && TryGetSmartKeywordSql(userMessage, out string smartSql, out string smartExplanation))
                        {
                            AddBotMessage("💡 " + smartExplanation);
                            sql = smartSql;
                        }
                        else if (sql == null && UseModelFallback)
                        {
                            ChatBubble waiting2 = AddBotMessage("Đang hỏi model AI, vui lòng chờ...");
                            string botReply = await GetAIProResponseAsync(userMessage);
                            RemoveBubble(waiting2);
                            sql = ExtractSelectSql(botReply);
                            if (sql == null)
                            {
                                AddBotMessage(botReply);
                                return;
                            }
                        }

                        if (sql == null)
                        {
                            string hint = "Mình chưa hiểu câu hỏi này.";
                            if (!HasApiKey)
                                hint += "\n(Chưa có khóa API nên AI chưa hoạt động: đặt biến môi trường ANTHROPIC_API_KEY.)";
                            if (ranked.Count > 0)
                            {
                                hint += "\nBạn thử hỏi theo kiểu:";
                                foreach (Candidate c in ranked.Take(3))
                                    hint += "\n • " + c.Item.Question;
                            }
                            AddBotMessage(hint);
                            return;
                        }
                    }
                }

                sql = ConvertToSqlServer(sql);
                if (ShowSql) AddBotSql("🧾 SQL:\n" + sql);

                string error;
                if (!IsSafeSelect(sql, out error))
                {
                    AddBotError("⛔ Từ chối: " + error);
                    return;
                }

                DataTable dt;
                try
                {
                    dt = await Task.Run(() => RunSelect(sql));
                }
                catch (SqlException ex) when (fromAi)
                {
                    // AI viết sai tên cột/bảng -> gửi lại lỗi để AI tự sửa 1 lần
                    AddBotMessage("🔧 Câu SQL bị lỗi, mình thử sửa lại...");
                    string sql2 = await AskClaudeForSqlAsync(userMessage, ranked, sql, ex.Message);
                    if (sql2 == null) throw;
                    sql = ConvertToSqlServer(sql2);
                    if (ShowSql) AddBotSql("🧾 SQL (sửa):\n" + sql);
                    if (!IsSafeSelect(sql, out error))
                    {
                        AddBotError("⛔ Từ chối: " + error);
                        return;
                    }
                    dt = await Task.Run(() => RunSelect(sql));
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    bool biCat = dt.Rows.Count > MaxRowsShown;

                    AddBotGridTable(biCat ? CopyFirstRows(dt, MaxRowsShown) : dt);
                    lastResultText = FormatTable(CopyFirstRows(dt, 15));
                    if (biCat) AddBotMessage("… Chỉ hiển thị " + MaxRowsShown + " dòng đầu tiên.");
                    if (fromAi && AutoLearn)
                    {
                        string q = userMessage, s2 = sql;
                        await Task.Run(() => SaveLearned(q, s2));
                    }
                }
                else
                {
                    AddBotMessage("📋 Không tìm thấy dữ liệu phù hợp.");
                }
            }
            catch (SqlException ex)
            {
                AddBotError("⚠️ Lỗi SQL Server: " + ex.Message);
            }
            catch (Exception ex)
            {
                AddBotError("⚠️ Lỗi: " + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ==========================================
        // AI VIẾT SQL (Claude API)
        // ==========================================
        // Mô tả cấu trúc DB để đưa cho AI: Bang(Cot1, Cot2, ...)
        private string BuildSchemaText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<string>> kv in schema)
            {
                if (kv.Key.Equals("CauHoiSQL", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.StartsWith("sysdiagrams", StringComparison.OrdinalIgnoreCase)) continue;
                sb.AppendLine(kv.Key + "(" + string.Join(", ", kv.Value) + ")");
            }
            if (sb.Length == 0)
                sb.AppendLine("(không đọc được cấu trúc DB; dùng các bảng NhanVien, Kho, NhaCungCap, VatTu, TonKho, PhieuNhap, ChiTietPhieuNhap, PhieuXuat, ChiTietPhieuXuat)");
            return sb.ToString();
        }

        // Gửi câu hỏi + cấu trúc DB + vài câu mẫu giống nhất cho AI, nhận lại 1 câu SELECT.
        // badSql/error khác null = nhờ AI sửa câu SQL bị lỗi.
        private async Task<string> AskClaudeForSqlAsync(string question, List<Candidate> examples, string badSql, string error)
        {
            string system =
                "Bạn là chuyên gia viết T-SQL cho Microsoft SQL Server, phục vụ phần mềm quản lý kho vật liệu xây dựng.\n" +
                "Quy tắc:\n" +
                "- Chỉ trả về MỘT câu SELECT (hoặc WITH ... SELECT) duy nhất, không giải thích, không markdown, không chú thích, không dấu chấm phẩy.\n" +
                "- Chỉ dùng đúng tên bảng và tên cột trong cấu trúc DB được cung cấp. Không bịa cột.\n" +
                "- Dùng TOP n thay cho LIMIT. Dùng LIKE N'%...%' khi tìm văn bản; dữ liệu tiếng Việt trong DB có thể lưu không dấu.\n" +
                "- Đặt tên cột kết quả dễ hiểu (AS ...). Với thời gian dùng YEAR(), MONTH() hoặc CONVERT(VARCHAR(7), cột, 120).\n" +
                "- Nếu câu hỏi không thể trả lời bằng dữ liệu trong DB, trả về đúng một chữ: KHONG_THE";

            StringBuilder user = new StringBuilder();
            user.AppendLine("CẤU TRÚC DB:");
            user.AppendLine(BuildSchemaText());

            if (examples != null && examples.Count > 0)
            {
                user.AppendLine("CÁC VÍ DỤ CÂU HỎI VÀ SQL ĐÚNG (tham khảo cách đặt tên bảng/cột và cách nối bảng):");
                foreach (Candidate c in examples.Take(5))
                {
                    user.AppendLine("Hỏi: " + c.Item.Question);
                    user.AppendLine("SQL: " + c.Item.Sql);
                    user.AppendLine();
                }
            }

            user.AppendLine("CÂU HỎI CẦN TRẢ LỜI: " + question);
            if (badSql != null)
            {
                user.AppendLine();
                user.AppendLine("Lần trước bạn viết câu SQL sau nhưng SQL Server báo lỗi.");
                user.AppendLine("SQL: " + badSql);
                user.AppendLine("Lỗi: " + error);
                user.AppendLine("Hãy viết lại câu SQL đúng.");
            }

            JObject body = new JObject();
            body["model"] = ClaudeModel;
            body["max_tokens"] = 800;
            body["system"] = system;
            body["messages"] = new JArray(new JObject
            {
                { "role", "user" },
                { "content", user.ToString() }
            });

            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
            {
                req.Headers.Add("x-api-key", ApiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await httpClient.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new Exception("HTTP " + (int)resp.StatusCode + ": " + json);

                JObject o = JObject.Parse(json);
                string text = (string)o["content"]?[0]?["text"];
                return ExtractSelectSql(text);   // KHONG_THE hoặc văn bản thường -> null
            }
        }

        // Lưu câu hỏi + SQL AI vừa viết vào CauHoiSQL để lần sau dùng lại
        private void SaveLearned(string question, string sql)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM CauHoiSQL WHERE CauHoiTiengViet = @q) " +
                    "INSERT INTO CauHoiSQL (CauHoiTiengViet, SQLQuery) VALUES (@q, @s)", conn))
                {
                    cmd.Parameters.AddWithValue("@q", question);
                    cmd.Parameters.AddWithValue("@s", sql);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                questionBankLoadedAt = DateTime.MinValue;   // nạp lại ngân hàng câu hỏi ở lần sau
            }
            catch { }
        }

        // ==========================================
        // DÒ TÊN CỘT GIÁ CỦA BẢNG VatTu
        // ==========================================
        // Cấu trúc DB: tên bảng -> tập tên cột (đọc 1 lần từ INFORMATION_SCHEMA)
        private Dictionary<string, HashSet<string>> schema =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private void EnsureSchemaInfo()
        {
            if (schemaLoaded) return;
            try
            {
                DataTable dt = RunSelect("SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS");
                Dictionary<string, HashSet<string>> sc =
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in dt.Rows)
                {
                    string t = r[0].ToString();
                    if (!sc.ContainsKey(t)) sc[t] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    sc[t].Add(r[1].ToString());
                }
                schema = sc;
                HashSet<string> vt;
                if (sc.TryGetValue("VatTu", out vt))
                    priceColumn = PriceColumnCandidates.FirstOrDefault(c => vt.Contains(c));
                schemaLoaded = true;
            }
            catch
            {
                // Không đọc được schema thì thử lại ở lần sau
            }
        }

        // Chưa đọc được schema thì coi như có (giữ hành vi cũ)
        private bool HasTable(string table)
        {
            return schema.Count == 0 || schema.ContainsKey(table);
        }

        private bool HasCol(string table, string col)
        {
            if (schema.Count == 0) return true;
            HashSet<string> cols;
            return schema.TryGetValue(table, out cols) && cols.Contains(col);
        }

        // Ghép câu SQL xem phiếu nhập / phiếu xuất theo đúng cột đang có trong DB
        private string BuildPhieuQuery(bool nhap, int top)
        {
            string head = nhap ? "PhieuNhap" : "PhieuXuat";
            string ct = nhap ? "ChiTietPhieuNhap" : "ChiTietPhieuXuat";
            string id = nhap ? "MaPN" : "MaPX";
            string date = nhap ? "NgayNhap" : "NgayXuat";

            List<string> cols = new List<string>();
            cols.Add("p." + id);
            bool coNgay = HasCol(head, date);
            if (coNgay) cols.Add("p." + date);

            string khoJoin = "";
            if (HasTable("Kho"))
            {
                if (HasCol(head, "MaKho"))
                    khoJoin = " LEFT JOIN Kho k ON p.MaKho = k.MaKho";
                else if (HasCol(ct, "MaKho"))
                    khoJoin = " LEFT JOIN Kho k ON ct.MaKho = k.MaKho";
                if (khoJoin != "") cols.Add("k.TenKho");
            }

            string partnerJoin = "";
            if (nhap && HasTable("NhaCungCap") && HasCol(head, "MaNCC"))
            {
                partnerJoin = " LEFT JOIN NhaCungCap ncc ON p.MaNCC = ncc.MaNCC";
                cols.Add("ncc.TenNCC");
            }
            else if (!nhap && HasTable("NhanVien") && HasCol(head, "MaNV"))
            {
                partnerJoin = " LEFT JOIN NhanVien nv ON p.MaNV = nv.MaNV";
                cols.Add("nv.HoTen AS NhanVienXuat");
            }

            cols.Add("vt.TenVT");
            cols.Add("ct.SoLuong");
            if (HasCol(ct, "DonGia"))
            {
                cols.Add("ct.DonGia");
                cols.Add("(ct.SoLuong * ct.DonGia) AS ThanhTien");
            }

            return "SELECT TOP " + top + " " + string.Join(", ", cols) +
                   " FROM " + head + " p" +
                   " JOIN " + ct + " ct ON p." + id + " = ct." + id +
                   " JOIN VatTu vt ON ct.MaVT = vt.MaVT" +
                   khoJoin + partnerJoin +
                   " ORDER BY " + (coNgay ? "p." + date : "p." + id) + " DESC";
        }

        // ", DonGia" nếu có cột giá, rỗng nếu không có
        private string PriceSelect()
        {
            return priceColumn != null ? ", " + priceColumn : "";
        }

        // Sửa SQL lấy từ bảng CauHoiSQL: thay tên cột giá sai của VatTu bằng tên thật
        private string FixPriceColumn(string sql)
        {
            if (priceColumn == null || string.IsNullOrEmpty(sql)) return sql;
            foreach (string wrong in PriceColumnCandidates)
            {
                if (wrong.Equals(priceColumn, StringComparison.OrdinalIgnoreCase)) continue;
                // Chỉ sửa tên "GiaTien" (tên hay bị nhầm); DonGia của bảng chi tiết phiếu giữ nguyên
                if (wrong == "GiaTien")
                    sql = Regex.Replace(sql, @"\bGiaTien\b", priceColumn, RegexOptions.IgnoreCase);
            }
            return sql;
        }

        // ==========================================
        // TÌM CÂU HỎI GIỐNG NHẤT TRONG BẢNG CauHoiSQL (TF-IDF)
        // ==========================================
        private static string RemoveDiacritics(string s)
        {
            s = s.ToLowerInvariant().Replace('đ', 'd');
            string d = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString();
        }

        private static HashSet<string> Tokenize(string s)
        {
            return new HashSet<string>(
                Regex.Split(RemoveDiacritics(s), "[^a-z0-9]+").Where(t => t.Length > 0));
        }

        private static HashSet<string> DetectTopics(string plain)
        {
            HashSet<string> t = new HashSet<string>();
            if (Regex.IsMatch(plain, @"\b(nhan vien|nhan su|thu kho|tho)\b")) t.Add("nhanvien");
            if (Regex.IsMatch(plain, @"\b(phieu nhap|nhap hang|nhap kho|don nhap)\b")) t.Add("phieunhap");
            if (Regex.IsMatch(plain, @"\b(phieu xuat|xuat hang|xuat kho|ban hang|doanh thu)\b")) t.Add("phieuxuat");
            if (Regex.IsMatch(plain, @"\b(nha cung cap|ncc|doi tac)\b")) t.Add("ncc");
            if (Regex.IsMatch(plain, @"\b(vat tu|san pham|hang hoa)\b")) t.Add("vattu");
            if (Regex.IsMatch(plain, @"\b(ton kho|so luong ton|con lai)\b")) t.Add("tonkho");
            string riengKho = Regex.Replace(plain, @"\b(thu|ton|nhap|xuat) kho\b", " ");
            if (Regex.IsMatch(riengKho, @"\bkho\b")) t.Add("kho");
            return t;
        }

        private double WeightOf(HashSet<string> tokens)
        {
            double sum = 0;
            foreach (string t in tokens)
            {
                double w;
                sum += idf.TryGetValue(t, out w) ? w : defaultIdf;
            }
            return sum;
        }

        private void EnsureQuestionTable()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "IF OBJECT_ID(N'dbo.CauHoiSQL', N'U') IS NULL " +
                    "CREATE TABLE dbo.CauHoiSQL (" +
                    "ID INT IDENTITY(1,1) PRIMARY KEY, " +
                    "CauHoiTiengViet NVARCHAR(500) NOT NULL, " +
                    "SQLQuery NVARCHAR(MAX) NOT NULL)", conn))
                {
                    cmd.CommandTimeout = 15;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Không tạo được thì để SELECT bên dưới báo lỗi rõ ràng
            }
        }

        private void EnsureQuestionBank()
        {
            if (questionBank != null &&
                (DateTime.Now - questionBankLoadedAt).TotalMinutes < QuestionBankCacheMinutes) return;

            EnsureQuestionTable();

            DataTable dt = RunSelect("SELECT ID, CauHoiTiengViet, SQLQuery FROM CauHoiSQL");
            List<QA> bank = new List<QA>();
            foreach (DataRow r in dt.Rows)
            {
                QA qa = new QA();
                qa.Id = Convert.ToInt32(r["ID"]);
                qa.Question = r["CauHoiTiengViet"].ToString();
                qa.Sql = r["SQLQuery"].ToString();
                qa.Tokens = Tokenize(qa.Question);
                qa.Topics = DetectTopics(RemoveDiacritics(qa.Question));
                bank.Add(qa);
            }

            Dictionary<string, int> df = new Dictionary<string, int>();
            foreach (QA qa in bank)
                foreach (string t in qa.Tokens)
                {
                    int c;
                    df.TryGetValue(t, out c);
                    df[t] = c + 1;
                }

            int n = Math.Max(bank.Count, 1);
            Dictionary<string, double> newIdf = new Dictionary<string, double>();
            foreach (KeyValuePair<string, int> kv in df)
                newIdf[kv.Key] = Math.Log(1.0 + (double)n / kv.Value);

            idf = newIdf;
            defaultIdf = Math.Log(1.0 + n);

            foreach (QA qa in bank) qa.Weight = WeightOf(qa.Tokens);
            questionBank = bank;
            questionBankLoadedAt = DateTime.Now;
        }

        private List<Candidate> RankQuestions(string userQuestion)
        {
            EnsureQuestionBank();
            HashSet<string> tq = Tokenize(userQuestion);
            double wq = WeightOf(tq);
            List<Candidate> result = new List<Candidate>();
            if (wq <= 0) return result;

            HashSet<string> userTopics = DetectTopics(RemoveDiacritics(userQuestion));

            foreach (QA qa in questionBank)
            {
                double common = 0;
                foreach (string t in tq)
                    if (qa.Tokens.Contains(t)) common += idf[t];
                double score = qa.Weight > 0 ? common / Math.Sqrt(wq * qa.Weight) : 0;

                if (userTopics.Count > 0 && qa.Topics.Count > 0 && !qa.Topics.Overlaps(userTopics))
                    score *= 0.4;

                result.Add(new Candidate { Item = qa, Score = score });
            }
            return result.OrderByDescending(c => c.Score).Take(5).ToList();
        }

        private string ExtractSelectSql(string reply)
        {
            if (string.IsNullOrWhiteSpace(reply)) return null;
            string text = Regex.Replace(reply, @"```[a-zA-Z]*", "").Replace("```", "");
            Match m = Regex.Match(text, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!m.Success) return null;
            string sql = text.Substring(m.Index).Trim();
            int semi = sql.IndexOf(';');
            if (semi >= 0) sql = sql.Substring(0, semi);
            return sql.Trim();
        }

        private string ConvertToSqlServer(string sql)
        {
            sql = sql.Trim().TrimEnd(';').Trim();

            Match lim = Regex.Match(sql, @"\bLIMIT\s+(\d+)\s*$", RegexOptions.IgnoreCase);
            if (lim.Success)
            {
                string n = lim.Groups[1].Value;
                sql = sql.Substring(0, lim.Index).TrimEnd();
                sql = Regex.Replace(sql, @"^(\s*SELECT)(\s+DISTINCT)?", "$1$2 TOP " + n, RegexOptions.IgnoreCase);
            }

            sql = Regex.Replace(sql, @"strftime\(\s*'%Y-%m'\s*,\s*([^)]+)\)", "LEFT($1, 7)", RegexOptions.IgnoreCase);

            // LEFT(ngày, 7) trên kiểu DATE cho kết quả sai định dạng -> đổi sang yyyy-MM
            sql = Regex.Replace(sql, @"\bLEFT\(\s*([\w.]+)\s*,\s*7\s*\)", "CONVERT(VARCHAR(7), $1, 120)", RegexOptions.IgnoreCase);

            // Dữ liệu có thể lưu không dấu ("Quy Nhon") còn người dùng gõ có dấu ("Quy Nhơn"):
            // mỗi điều kiện LIKE N'%x%' được tìm thêm bằng bản không dấu của x
            sql = Regex.Replace(sql, @"([\w.]+)\s+LIKE\s+N'%([^']*)%'", m =>
            {
                string col = m.Groups[1].Value;
                string kw = m.Groups[2].Value;
                string plainKw = RemoveDiacritics(kw);
                if (plainKw == kw.ToLowerInvariant() && plainKw == kw) return m.Value;
                if (string.Equals(plainKw, kw, StringComparison.Ordinal)) return m.Value;
                return "(" + col + " LIKE N'%" + kw + "%' OR " + col + " LIKE N'%" + plainKw + "%')";
            }, RegexOptions.IgnoreCase);
            sql = FixColumnNames(sql);
            return sql;
        }

        private bool IsSafeSelect(string sql, out string error)
        {
            error = null;

            if (!Regex.IsMatch(sql, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase))
            { error = "Chỉ cho phép câu lệnh SELECT."; return false; }

            if (sql.Contains(";"))
            { error = "Không cho phép nhiều câu lệnh."; return false; }

            if (sql.Contains("--") || sql.Contains("/*"))
            { error = "Không cho phép chú thích trong SQL."; return false; }

            string noStrings = Regex.Replace(sql, @"'([^']|'')*'", "''");
            string banned = @"\b(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|MERGE|GRANT|REVOKE|DENY|INTO|OPENROWSET|OPENQUERY|OPENDATASOURCE|BULK|SHUTDOWN|WAITFOR|BACKUP|RESTORE|xp_\w*|sp_\w*)\b";
            Match bad = Regex.Match(noStrings, banned, RegexOptions.IgnoreCase);
            if (bad.Success)
            { error = "Câu lệnh chứa từ khóa không được phép: " + bad.Value.ToUpper(); return false; }

            return true;
        }

        // ==========================================
        // BỘ PHÂN TÍCH TỪ KHÓA THÔNG MINH (đã gộp, bỏ nhánh trùng, cụ thể trước - chung sau)
        // ==========================================
        private static readonly Dictionary<string, string> Provinces = new Dictionary<string, string>
        {
            { "ha noi", "Hà Nội" }, { "hn", "Hà Nội" },
            { "ho chi minh", "Hồ Chí Minh" }, { "hcm", "Hồ Chí Minh" }, { "sai gon", "Hồ Chí Minh" },
            { "da nang", "Đà Nẵng" }, { "can tho", "Cần Thơ" }, { "hai phong", "Hải Phòng" },
            { "vinh long", "Vĩnh Long" }, { "binh dinh", "Bình Định" }, { "quy nhon", "Quy Nhơn" },
            { "binh duong", "Bình Dương" }, { "dong nai", "Đồng Nai" }, { "gia lai", "Gia Lai" },
            { "tra vinh", "Trà Vinh" }
        };

        private static string FindProvince(string plain)
        {
            foreach (KeyValuePair<string, string> kv in Provinces)
                if (Regex.IsMatch(plain, @"\b" + Regex.Escape(kv.Key) + @"\b")) return kv.Value;
            return null;
        }

        private static string Esc(string s)
        {
            return s.Replace("'", "''");
        }

        private const string TonKhoSelect =
            "SELECT k.TenKho, v.TenVT, t.SoLuongTon, v.DonViTinh " +
            "FROM TonKho t JOIN VatTu v ON t.MaVT = v.MaVT JOIN Kho k ON t.MaKho = k.MaKho ";

        private bool TryGetSmartKeywordSql(string userMessage, out string sql, out string explanation)
        {
            sql = null;
            explanation = null;
            string lower = RemoveDiacritics(userMessage);
            string price = PriceSelect();
            // 4. TÌM KIẾM VẬT TƯ THEO TỪ KHÓA (Ví dụ: "tìm vật tư xi măng", "có sắt không",...)
            if (lower.Contains("vat tu") || lower.Contains("san pham") || lower.Contains("hang hoa") || lower.Contains("tim") || lower.Contains("co"))
            {
                // Bóc tách từ khóa nằm sau các từ chỉ định nghĩa tìm kiếm
                string keyword = ExtractKeywordAfter(userMessage, new[] { "vat tu", "san pham", "hang hoa", "tim", "kiem", "co", "la", "ten" });

                if (!string.IsNullOrEmpty(keyword))
                {
                    // Thực hiện truy vấn tìm kiếm vật tư có chứa từ khóa
                    sql = $"SELECT MaVT, TenVT, DonViTinh, DonGia FROM VatTu WHERE TenVT LIKE N'%{keyword.Replace("'", "''")}%'";

                    // Chạy thử câu lệnh để kiểm tra xem có dữ liệu trả về hay không
                    DataTable dtCheck = RunSelect(sql);

                    if (dtCheck == null || dtCheck.Rows.Count == 0)
                    {
                        // Nếu không tìm thấy vật tư nào khớp với từ khóa -> Báo kho không có
                        AddBotMessage($"Rất tiếc, kho không có vật tư nào tên là “{keyword}”.");
                        return true; // Dừng lại, không chạy tiếp phần hiển thị bảng nữa
                    }
                    else
                    {
                        // Nếu có, gán giải thích và tiếp tục đưa vào khung hiển thị bảng
                        explanation = $"Kết quả tìm kiếm vật tư với từ khóa: “{keyword}”";
                        // Bạn có thể trả về SQL trực tiếp để luồng chính xử lý render bảng
                        // (Hoặc nếu gọi trong TryGetSmartKeywordSql thì return true kèm câu lệnh sql bên dưới)
                        return true;
                    }
                }
            }
            // 1. ĐẾM SỐ LƯỢNG
            if (lower.Contains("bao nhieu vat tu") || lower.Contains("tong so vat tu") || lower.Contains("so luong mat hang"))
            {
                sql = "SELECT COUNT(*) AS TongSoLoaiVatTu FROM VatTu";
                explanation = "Tổng số loại vật tư hiện có trong danh mục:";
                return true;
            }
            if (lower.Contains("bao nhieu kho") || lower.Contains("tong so kho"))
            {
                sql = "SELECT COUNT(*) AS TongSoKho FROM Kho";
                explanation = "Tổng số kho hàng đang hoạt động:";
                return true;
            }
            if (lower.Contains("bao nhieu nhan vien") || lower.Contains("tong so nhan vien") || lower.Contains("so luong nhan su"))
            {
                sql = "SELECT COUNT(*) AS TongSoNhanVien FROM NhanVien";
                explanation = "Tổng số nhân viên trong hệ thống:";
                return true;
            }
            if (lower.Contains("bao nhieu nha cung cap") || lower.Contains("tong so ncc"))
            {
                sql = "SELECT COUNT(*) AS TongSoNhaCungCap FROM NhaCungCap";
                explanation = "Tổng số nhà cung cấp đối tác:";
                return true;
            }

            // 2. DANH SÁCH VẬT TƯ TRONG KHO (cụ thể, đặt trước nhánh "vật tư" chung)
            if (lower.Contains("vat tu trong kho") || lower.Contains("tat ca vat tu"))
            {
                sql = TonKhoSelect + "ORDER BY t.SoLuongTon DESC";
                explanation = "Danh sách chi tiết số lượng vật tư tồn trong các kho:";
                return true;
            }

            // 3. GIỚI HẠN SỐ LƯỢNG VẬT TƯ
            Match topN = Regex.Match(lower, @"\b(?:top\s+)?(\d{1,3})\s+vat tu\b|\btop\s+(\d{1,3})\b");
            if (topN.Success && !lower.Contains("ban chay") && !lower.Contains("dat nhat") && !lower.Contains("re nhat"))
            {
                string num = topN.Groups[1].Success ? topN.Groups[1].Value : topN.Groups[2].Value;
                sql = "SELECT TOP " + num + " MaVT, TenVT, DonViTinh" + price + " FROM VatTu";
                explanation = "Danh sách " + num + " vật tư trong hệ thống:";
                return true;
            }
            if (lower.Contains("nam vat tu")) { sql = "SELECT TOP 5 MaVT, TenVT, DonViTinh" + price + " FROM VatTu"; explanation = "Danh sách 5 vật tư trong hệ thống:"; return true; }
            if (lower.Contains("muoi vat tu")) { sql = "SELECT TOP 10 MaVT, TenVT, DonViTinh" + price + " FROM VatTu"; explanation = "Danh sách 10 vật tư trong hệ thống:"; return true; }

            // 4. GIÁ ĐẮT NHẤT / RẺ NHẤT
            bool dat = lower.Contains("dat nhat") || lower.Contains("gia cao nhat") || lower.Contains("mac nhat");
            bool re = lower.Contains("re nhat") || lower.Contains("gia thap nhat");
            if (dat || re)
            {
                if (priceColumn == null)
                {
                    sql = "SELECT TOP 10 MaVT, TenVT, DonViTinh FROM VatTu";
                    explanation = "Bảng VatTu không có cột giá nên mình chỉ liệt kê vật tư:";
                }
                else
                {
                    sql = "SELECT TOP 10 MaVT, TenVT, DonViTinh, " + priceColumn + " FROM VatTu ORDER BY " +
                          priceColumn + (dat ? " DESC" : " ASC");
                    explanation = dat ? "Top vật tư có đơn giá cao nhất:" : "Top vật tư có đơn giá thấp nhất:";
                }
                return true;
            }

            // 5. DOANH THU / XUẤT HÀNG
            if (lower.Contains("doanh thu") || lower.Contains("tien ban") || lower.Contains("tong tien xuat"))
            {
                sql = "SELECT ISNULL(SUM(SoLuong * DonGia), 0) AS TongDoanhThuXuatHang FROM ChiTietPhieuXuat";
                explanation = "Tổng doanh thu từ các phiếu xuất hàng:";
                return true;
            }
            if (lower.Contains("ban chay") || lower.Contains("xuat nhieu nhat") || lower.Contains("tieu thu nhieu"))
            {
                sql = "SELECT TOP 10 vt.MaVT, vt.TenVT, SUM(ct.SoLuong) AS TongSoLuongXuat, vt.DonViTinh " +
                      "FROM ChiTietPhieuXuat ct JOIN VatTu vt ON ct.MaVT = vt.MaVT " +
                      "GROUP BY vt.MaVT, vt.TenVT, vt.DonViTinh ORDER BY TongSoLuongXuat DESC";
                explanation = "Top vật tư được xuất/bán nhiều nhất:";
                return true;
            }
            if (lower.Contains("phieu xuat") || lower.Contains("xuat hang") || lower.Contains("lich su xuat") || lower.Contains("don xuat"))
            {
                sql = BuildPhieuQuery(false, 30);
                explanation = "Danh sách chi tiết phiếu xuất hàng gần nhất:";
                return true;
            }

            // 6. NHẬP HÀNG
            if (lower.Contains("tong tien nhap") || lower.Contains("chi phi nhap") || lower.Contains("tien mua hang"))
            {
                sql = "SELECT ISNULL(SUM(SoLuong * DonGia), 0) AS TongChiPhiNhapHang FROM ChiTietPhieuNhap";
                explanation = "Tổng chi phí đã nhập hàng vào kho:";
                return true;
            }
            if (lower.Contains("nhap them") || lower.Contains("moi nhap") || lower.Contains("vua nhap") ||
                lower.Contains("hom nay nhap") || lower.Contains("hang moi"))
            {
                sql = BuildPhieuQuery(true, 15);
                explanation = "Danh sách hàng hóa mới nhập vào kho gần đây:";
                return true;
            }
            if (lower.Contains("phieu nhap") || lower.Contains("nhap hang") || lower.Contains("lich su nhap") || lower.Contains("don nhap"))
            {
                sql = BuildPhieuQuery(true, 30);
                explanation = "Danh sách chi tiết phiếu nhập hàng gần nhất:";
                return true;
            }

            // 7. TỒN KHO
            if (lower.Contains("het hang") || lower.Contains("ton bang 0") || lower.Contains("khong con ton"))
            {
                sql = TonKhoSelect + "WHERE t.SoLuongTon <= 0";
                explanation = "Danh sách vật tư đã hết hàng trong kho:";
                return true;
            }
            if (lower.Contains("sap het") || lower.Contains("ton it") || lower.Contains("ton thap") || lower.Contains("can nhap"))
            {
                sql = TonKhoSelect + "WHERE t.SoLuongTon < " + LowStockThreshold + " ORDER BY t.SoLuongTon ASC";
                explanation = "Danh sách vật tư tồn kho thấp (dưới " + LowStockThreshold + "):";
                return true;
            }
            if (lower.Contains("ton nhieu nhat") || lower.Contains("ton cao nhat"))
            {
                sql = "SELECT TOP 10 k.TenKho, v.TenVT, t.SoLuongTon, v.DonViTinh " +
                      "FROM TonKho t JOIN VatTu v ON t.MaVT = v.MaVT JOIN Kho k ON t.MaKho = k.MaKho ORDER BY t.SoLuongTon DESC";
                explanation = "Top vật tư có số lượng tồn kho nhiều nhất:";
                return true;
            }
            if (lower.Contains("ton kho") || lower.Contains("so luong ton") || lower.Contains("con lai") || lower.Contains("kiem tra ton"))
            {
                string keyword = ExtractKeywordAfter(userMessage, new[] { "ton kho", "so luong ton", "cua", "vat tu", "hang", "trong", "tai" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    string kw = Esc(keyword);
                    sql = TonKhoSelect + "WHERE v.TenVT LIKE N'%" + kw + "%' OR k.TenKho LIKE N'%" + kw + "%'";
                    explanation = "Tra cứu tồn kho liên quan đến: “" + keyword + "”";
                }
                else
                {
                    sql = "SELECT TOP 30 k.TenKho, v.TenVT, t.SoLuongTon, v.DonViTinh " +
                          "FROM TonKho t JOIN VatTu v ON t.MaVT = v.MaVT JOIN Kho k ON t.MaKho = k.MaKho ORDER BY t.SoLuongTon DESC";
                    explanation = "Danh sách tồn kho vật tư trong các kho:";
                }
                return true;
            }


            // GỢI Ý NHẬP HÀNG ---------------------------------------------------
            if (lower.Contains("goi y nhap") || lower.Contains("nen nhap") ||
                lower.Contains("can nhap them") || lower.Contains("de xuat nhap") ||
                lower.Contains("nhap them gi"))
            {
                int nguong = LowStockThreshold;   // đổi ngưỡng tồn thấp ở đây (mặc định 300)
                int soThang = 3;                  // số tháng nhu cầu muốn dự trù, đổi tùy ý

                sql =
                    "SELECT vt.MaVT, vt.TenVT, vt.DonViTinh, " +
                    "ISNULL(SUM(tk.SoLuongTon), 0) AS TongTon, " +
                    "ISNULL((SELECT SUM(x.SoLuong) FROM ChiTietPhieuXuat x WHERE x.MaVT = vt.MaVT), 0) AS DaXuat, " +
                    // Gợi ý nhập = nhu cầu dự trù - tồn hiện có (không âm)
                    "CASE WHEN ISNULL((SELECT SUM(x.SoLuong) FROM ChiTietPhieuXuat x WHERE x.MaVT = vt.MaVT), 0) * " + soThang + " - ISNULL(SUM(tk.SoLuongTon), 0) > 0 " +
                    "THEN ISNULL((SELECT SUM(x.SoLuong) FROM ChiTietPhieuXuat x WHERE x.MaVT = vt.MaVT), 0) * " + soThang + " - ISNULL(SUM(tk.SoLuongTon), 0) " +
                    "ELSE " + nguong + " - ISNULL(SUM(tk.SoLuongTon), 0) END AS GoiYNhap " +
                    "FROM VatTu vt LEFT JOIN TonKho tk ON tk.MaVT = vt.MaVT " +
                    "GROUP BY vt.MaVT, vt.TenVT, vt.DonViTinh " +
                    "HAVING ISNULL(SUM(tk.SoLuongTon), 0) < " + nguong + " " +
                    "ORDER BY TongTon ASC";

                explanation = "Vật tư tồn dưới " + nguong + " và số lượng gợi ý nhập thêm:";
                return true;
            }

            // 8. NHÀ CUNG CẤP (theo tỉnh thành -> tìm ở DiaChi)
            if (lower.Contains("nha cung cap") || Regex.IsMatch(lower, @"\bncc\b") || lower.Contains("doi tac"))
            {
                string tinh = FindProvince(lower);
                if (tinh != null)
                {
                    sql = "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap WHERE DiaChi LIKE N'%" + tinh + "%' OR TenNCC LIKE N'%" + tinh + "%'";
                    explanation = "Danh sách nhà cung cấp tại khu vực: " + tinh;
                    return true;
                }
                string keyword = ExtractKeywordAfter(userMessage, new[] { "nha cung cap", "ncc", "doi tac", "ten", "tim" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    string kw = Esc(keyword);
                    sql = "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap WHERE TenNCC LIKE N'%" + kw + "%' OR DiaChi LIKE N'%" + kw + "%'";
                    explanation = "Tìm kiếm nhà cung cấp với từ khóa: “" + keyword + "”";
                }
                else
                {
                    sql = "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap";
                    explanation = "Danh sách tất cả nhà cung cấp:";
                }
                return true;
            }
            // NHÀ CUNG CẤP NÀY CUNG CẤP GÌ ---------------------------------------
            if ((lower.Contains("nha cung cap") || Regex.IsMatch(lower, @"\bncc\b")) &&
                Regex.IsMatch(lower, @"cung cap (nhung |cac )?(gi|cai gi|mat hang|vat tu|vat lieu)|\bban gi\b|\bnhap gi\b|cung ung"))
            {
                // Bỏ các cụm thừa để còn lại tên (hoặc mã) nhà cung cấp, ví dụ "nhà cung cấp Nam A cung cấp gì" -> "nam a"
                string ten = Regex.Replace(lower,
                    @"\b(cho toi|cho biet|hay|xem|giup|nha cung cap|ncc|cung cap|cung ung|nhung|cac|cai gi|mat hang nao|mat hang|vat tu|vat lieu|nao|gi|ban|nhap|la)\b|\?",
                    " ");
                ten = Regex.Replace(ten, @"\s+", " ").Trim();

                string where = "";
                if (ten.Length > 0)
                    where = "WHERE ncc.TenNCC LIKE N'%" + Esc(ten) + "%' OR ncc.MaNCC = N'" + Esc(ten) + "' ";

                sql =
                    "SELECT ncc.MaNCC, ncc.TenNCC, vt.MaVT, vt.TenVT, vt.DonViTinh, " +
                    "SUM(ct.SoLuong) AS TongSoLuongNhap, " +
                    "SUM(ct.SoLuong * ct.DonGia) AS TongGiaTri, " +
                    "MAX(pn.NgayNhap) AS LanNhapGanNhat " +
                    "FROM NhaCungCap ncc " +
                    "JOIN PhieuNhap pn ON pn.MaNCC = ncc.MaNCC " +
                    "JOIN ChiTietPhieuNhap ct ON ct.MaPN = pn.MaPN " +
                    "JOIN VatTu vt ON vt.MaVT = ct.MaVT " +
                    where +
                    "GROUP BY ncc.MaNCC, ncc.TenNCC, vt.MaVT, vt.TenVT, vt.DonViTinh " +
                    "ORDER BY ncc.TenNCC, TongGiaTri DESC";

                explanation = ten.Length > 0
                    ? "Các vật tư nhà cung cấp “" + ten + "” đã cung cấp:"
                    : "Các vật tư mà từng nhà cung cấp đã cung cấp:";
                return true;
            }
            // 9. NHÂN VIÊN (đặt trước "kho" vì có "thủ kho")
            if (lower.Contains("nhan vien") || lower.Contains("nhan su") || lower.Contains("chuc vu") ||
                Regex.IsMatch(lower, @"\b(tho|thu kho|quan ly)\b"))
            {
                string keyword = ExtractKeywordAfter(userMessage, new[] { "nhan vien", "nhan su", "chuc vu", "ten", "tim" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    string kw = Esc(keyword);
                    sql = "SELECT MaNV, HoTen, DienThoai, ChucVu FROM NhanVien WHERE HoTen LIKE N'%" + kw + "%' OR ChucVu LIKE N'%" + kw + "%'";
                    explanation = "Tìm kiếm nhân viên theo từ khóa: “" + keyword + "”";
                }
                else
                {
                    sql = "SELECT MaNV, HoTen, DienThoai, ChucVu FROM NhanVien";
                    explanation = "Danh sách nhân viên trong hệ thống:";
                }
                return true;
            }


            // 10. KHO HÀNG (theo tỉnh thành hoặc từ khóa)
            string lowerNoKhoPhrases = Regex.Replace(lower, @"\b(thu|ton|nhap|xuat) kho\b", " ");
            if (lower.Contains("kho hang") || lower.Contains("danh sach kho") || lower.Contains("cac kho") ||
                Regex.IsMatch(lowerNoKhoPhrases, @"\bkho\b"))
            {
                string tinh = FindProvince(lower);
                if (tinh != null)
                {
                    sql = "SELECT MaKho, TenKho, DiaChi FROM Kho WHERE DiaChi LIKE N'%" + tinh + "%' OR TenKho LIKE N'%" + tinh + "%'";
                    explanation = "Danh sách kho hàng tại khu vực: " + tinh;
                    return true;
                }
                string keyword = ExtractKeywordAfter(userMessage, new[] { "kho hang", "kho", "tai", "dia chi" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    string kw = Esc(keyword);
                    sql = "SELECT MaKho, TenKho, DiaChi FROM Kho WHERE TenKho LIKE N'%" + kw + "%' OR DiaChi LIKE N'%" + kw + "%'";
                    explanation = "Tìm kiếm kho hàng theo từ khóa: “" + keyword + "”";
                }
                else
                {
                    sql = "SELECT MaKho, TenKho, DiaChi FROM Kho";
                    explanation = "Danh sách các kho hàng:";
                }
                return true;
            }

            // 11. DANH MỤC VẬT TƯ / SẢN PHẨM (nhánh chung nhất, đặt cuối)
            if (lower.Contains("vat tu") || lower.Contains("san pham") || lower.Contains("hang hoa") ||
                lower.Contains("mat hang") || Regex.IsMatch(lower, @"\bgia\b"))
            {
                string keyword = ExtractKeywordAfter(userMessage, new[] { "vat tu", "san pham", "mat hang", "hang hoa", "gia", "ten", "chi tiet", "tim" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    sql = "SELECT MaVT, TenVT, DonViTinh" + price + " FROM VatTu WHERE TenVT LIKE N'%" + Esc(keyword) + "%'";
                    explanation = "Tìm kiếm vật tư chứa từ khóa: “" + keyword + "”";
                }
                else
                {
                    sql = "SELECT TOP 30 MaVT, TenVT, DonViTinh" + price + " FROM VatTu";
                    explanation = "Danh sách vật tư có trong hệ thống:";
                }
                return true;
            }

            return false;
        }



        // Lấy phần từ khóa đứng sau một "từ đánh dấu" (ví dụ: "vật tư xi măng" -> "xi măng")
        private string ExtractKeywordAfter(string message, string[] markers)
        {
            string msg = message.Normalize(NormalizationForm.FormC);
            string plain = RemoveDiacritics(msg);
            if (plain.Length != msg.Length) msg = plain;   // phòng lệch độ dài: dùng bản không dấu

            foreach (string marker in markers)
            {
                Match m = Regex.Match(plain, @"\b" + Regex.Escape(marker) + @"\b");
                if (!m.Success) continue;

                int start = m.Index + m.Length;
                string subOriginal = msg.Substring(start);
                string subPlain = plain.Substring(start);

                Match filler = Regex.Match(subPlain, @"^(?:[\s\?,]+|\b(?:la gi|la|cua|goi|nhu|thong tin|trong|nao|gi|ma toi co|khong|)\b)+");
                int cut = filler.Success ? filler.Length : 0;

                string keyword = subOriginal.Substring(cut).Trim().TrimEnd('?', '.', '!', ' ');
                if (keyword.Length > 1) return keyword;
            }


            return null;
        }


        // ==========================================
        // XÃ GIAO (SMALL TALK)
        // ==========================================
        private bool TryHandleSmallTalk(string userMessage, out string reply)
        {
            reply = null;
            string lower = RemoveDiacritics(userMessage.Trim());

            if (lower == "chao" || lower.Contains("xin chao") || lower.Contains("chao ban") ||
                Regex.IsMatch(lower, @"^(chao|hi|hello|hey|alo)\b"))
            {
                reply = "👋 Chào bạn! Mình là Trợ lý AI Pro. Mình có thể giúp gì cho bạn về kho hàng, vật tư, nhà cung cấp hoặc trò chuyện cùng bạn hôm nay?";
                return true;
            }

            if (lower.Contains("ban ten gi") || lower.Contains("ban la ai") || lower.Contains("cau la ai") || lower.Contains("gioi thieu ve ban"))
            {
                reply = "🤖 Mình là Trợ lý AI Pro, một trợ lý thông minh được tích hợp trực tiếp vào phần mềm Quản lý Kho VLXD để giúp bạn tra cứu dữ liệu và phân tích nhanh chóng.";
                return true;
            }

            if (lower.Contains("cam on") || Regex.IsMatch(lower, @"\b(thanks|thank you)\b") || lower.Contains("tuyet voi"))
            {
                reply = "😊 Không có chi! Nếu bạn cần tra cứu số liệu hay hỗ trợ thêm gì cứ nói với mình nhé.";
                return true;
            }

            if (lower.Contains("ban khoe khong") || lower.Contains("the nao roi") || lower.Contains("hom nay the nao"))
            {
                reply = "🚀 Hệ thống đang hoạt động ổn định và mình luôn sẵn sàng 100% công suất để hỗ trợ bạn!";
                return true;
            }

            if (lower.Contains("tam biet") || Regex.IsMatch(lower, @"\bbye\b") || lower.Contains("chuc ngu ngon"))
            {
                reply = "👋 Tạm biệt bạn! Chúc bạn làm việc hiệu quả, hẹn gặp lại bạn sau nhé.";
                return true;
            }

            if (lower.Contains("neu can") || lower.Contains("giup do"))
            {
                reply = "Nếu cần hỗ trợ thêm thông tin gì, bạn cứ nhắn mình nhé!";
                return true;
            }

            return false;
        }

        // Để SqlException thoát ra nguyên bản (SendMessageAsync sẽ bắt và hiện lỗi cho người dùng)
        private DataTable RunSelect(string sql)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 15;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        // Gọi dịch vụ Python (app.py): POST /api/query
        private async Task<string> GetAIProResponseAsync(string prompt)
        {
            string url = "http://127.0.0.1:8000/api/query";
            try
            {
                JObject body = new JObject(new JProperty("question", prompt));
                using (StringContent content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        JObject jsonResponse = JObject.Parse(jsonString);
                        return jsonResponse["sql"]?.ToString() ?? "Không nhận được câu lệnh SQL từ mô hình.";
                    }
                    return "⚠️ Lỗi HTTP " + (int)response.StatusCode + ": " + jsonString;
                }
            }
            catch (Exception ex)
            {
                return "⚠️ Chi tiết lỗi kết nối C#: " + ex.Message;
            }
        }
    }
}