using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;          // .NET 5+ thì đổi thành Microsoft.Data.SqlClient
using System.Drawing;
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
        // ⚠️ SỬA CHUỖI KẾT NỐI CHO ĐÚNG MÁY CỦA BẠN (tên server trong SSMS)
        private const string ConnectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=app_quanlykhoVLXD;Trusted_Connection=True;";
        private const int MaxRowsShown = 30;          // tối đa số dòng hiện trong khung chat
        private const double MatchThreshold = 0.5;    // điểm tối thiểu để coi là "hiểu câu hỏi"
        private const int LowStockThreshold = 300;    // dưới mức này coi là tồn kho thấp

        // Model Python trên CPU rất chậm (~15 phút/câu) nên mặc định TẮT.
        // Đổi thành true nếu sau này có GPU.
        private const bool UseModelFallback = false;

        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        private RichTextBox rtbChatHistory;
        private TextBox txtChatInput;
        private Button btnSend;
        private Button btnQuickStock;
        private Button btnQuickSuggest;

        // ---- Ngân hàng câu hỏi (nạp từ bảng CauHoiSQL) ----
        private class QA
        {
            public int Id;
            public string Question;
            public string Sql;
            public HashSet<string> Tokens;
            public double Weight;
        }
        private class Candidate
        {
            public QA Item;
            public double Score;
        }
        private List<QA> questionBank;
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
            this.Size = new Size(450, 580);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(245, 246, 250);

            rtbChatHistory = new RichTextBox();
            rtbChatHistory.Size = new Size(410, 420);
            rtbChatHistory.Location = new Point(12, 12);
            rtbChatHistory.Font = new Font("Segoe UI", 10);
            rtbChatHistory.ReadOnly = true;
            rtbChatHistory.BackColor = Color.White;
            rtbChatHistory.BorderStyle = BorderStyle.FixedSingle;
            rtbChatHistory.Text = "🤖 [AI Pro]: Chào bạn! Hãy hỏi về nhân viên, kho, nhà cung cấp, phiếu nhập/xuất...\nVí dụ: \"Tổng doanh thu xuất hàng\", \"Danh sách nhà cung cấp\".\n\n";
            this.Controls.Add(rtbChatHistory);

            txtChatInput = new TextBox();
            txtChatInput.Size = new Size(310, 32);
            txtChatInput.Location = new Point(12, 490);
            txtChatInput.Font = new Font("Segoe UI", 11);
            txtChatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { btnSend.PerformClick(); e.SuppressKeyPress = true; }
            };
            this.Controls.Add(txtChatInput);

            btnSend = new Button();
            btnSend.Text = "Gửi";
            btnSend.Size = new Size(90, 32);
            btnSend.Location = new Point(332, 489);
            btnSend.BackColor = Color.FromArgb(108, 92, 231);
            btnSend.ForeColor = Color.White;
            btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => { await SendMessageAsync(); };
            this.Controls.Add(btnSend);

            // Nút 1: tồn kho thấp -> chạy thẳng SQL viết sẵn
            btnQuickStock = new Button();
            btnQuickStock.Text = "📊 Tồn kho thấp";
            btnQuickStock.Size = new Size(130, 28);
            btnQuickStock.Location = new Point(12, 445);
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

            // Nút 2: gợi ý nhập hàng -> chạy thẳng SQL viết sẵn
            btnQuickSuggest = new Button();
            btnQuickSuggest.Text = "💡 Gợi ý nhập hàng";
            btnQuickSuggest.Size = new Size(140, 28);
            btnQuickSuggest.Location = new Point(150, 445);
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
        // LUỒNG CHÍNH
        // overrideQuestion + directSql: dùng cho 2 nút gợi ý (bỏ qua bước tìm câu hỏi)
        // ==========================================
        private async Task SendMessageAsync(string overrideQuestion = null, string directSql = null)
        {
            string userMessage = overrideQuestion ?? txtChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            SetBusy(true);
            try
            {
                AppendChat("👤 Bạn: ", userMessage, Color.FromArgb(9, 132, 227));
                if (overrideQuestion == null) txtChatInput.Clear();

                string sql = directSql;

                if (sql == null)
                {
                    List<Candidate> ranked = await Task.Run(() => RankQuestions(userMessage));
                    Candidate best = ranked.Count > 0 ? ranked[0] : null;

                    if (best != null && best.Score >= MatchThreshold)
                    {
                        AppendChat("🤖 [AI Pro]: ", "Mình hiểu câu hỏi của bạn là:\n\"" + best.Item.Question + "\"",
                                   Color.FromArgb(45, 52, 54));
                        sql = best.Item.Sql;
                    }
                    else if (UseModelFallback)
                    {
                        int start = rtbChatHistory.TextLength;
                        AppendChat("🤖 [AI Pro]: ", "Đang hỏi model AI, vui lòng chờ...", Color.Gray);
                        string botReply = await GetAIProResponseAsync(userMessage);
                        RemoveFrom(start);
                        sql = ExtractSelectSql(botReply);
                        if (sql == null)
                        {
                            AppendChat("🤖 [AI Pro]: ", botReply, Color.FromArgb(45, 52, 54));
                            return;
                        }
                    }
                    else
                    {
                        string hint = "Mình chưa hiểu câu hỏi này. Bạn thử hỏi theo kiểu:";
                        foreach (Candidate c in ranked.Take(3))
                            hint += "\n • " + c.Item.Question;
                        AppendChat("🤖 [AI Pro]: ", hint, Color.FromArgb(45, 52, 54));
                        return;
                    }
                }

                sql = ConvertToSqlServer(sql);
                AppendChat("🧾 SQL: ", sql, Color.FromArgb(99, 110, 114), "Consolas", 9);

                string error;
                if (!IsSafeSelect(sql, out error))
                {
                    AppendChat("⛔ Từ chối: ", error, Color.Firebrick);
                    return;
                }

                try
                {
                    DataTable dt = await Task.Run(() => RunSelect(sql, MaxRowsShown + 1));
                    AppendChat("📋 Kết quả: ", FormatTable(dt), Color.FromArgb(45, 52, 54), "Consolas", 9);
                }
                catch (SqlException ex)
                {
                    AppendChat("⚠️ Lỗi SQL Server: ", ex.Message, Color.Firebrick);
                }
            }
            catch (Exception ex)
            {
                AppendChat("⚠️ Lỗi: ", ex.Message, Color.Firebrick);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ==========================================
        // TÌM CÂU HỎI GIỐNG NHẤT TRONG BẢNG CauHoiSQL (TF-IDF đơn giản)
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

        private void EnsureQuestionBank()
        {
            if (questionBank != null) return;

            DataTable dt = RunSelect("SELECT ID, CauHoiTiengViet, SQLQuery FROM CauHoiSQL", 0);
            List<QA> bank = new List<QA>();
            foreach (DataRow r in dt.Rows)
            {
                QA qa = new QA();
                qa.Id = Convert.ToInt32(r["ID"]);
                qa.Question = r["CauHoiTiengViet"].ToString();
                qa.Sql = r["SQLQuery"].ToString();
                qa.Tokens = Tokenize(qa.Question);
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
            idf = new Dictionary<string, double>();
            foreach (KeyValuePair<string, int> kv in df)
                idf[kv.Key] = Math.Log(1.0 + (double)n / kv.Value);
            defaultIdf = Math.Log(1.0 + n);

            foreach (QA qa in bank) qa.Weight = WeightOf(qa.Tokens);
            questionBank = bank;
        }

        private List<Candidate> RankQuestions(string userQuestion)
        {
            EnsureQuestionBank();
            HashSet<string> tq = Tokenize(userQuestion);
            double wq = WeightOf(tq);
            List<Candidate> result = new List<Candidate>();
            if (wq <= 0) return result;

            foreach (QA qa in questionBank)
            {
                double common = 0;
                foreach (string t in tq)
                    if (qa.Tokens.Contains(t)) common += idf[t];
                double score = qa.Weight > 0 ? common / Math.Sqrt(wq * qa.Weight) : 0;
                result.Add(new Candidate { Item = qa, Score = score });
            }
            return result.OrderByDescending(c => c.Score).Take(3).ToList();
        }

        // ==========================================
        // (Tùy chọn) LẤY SELECT TỪ CÂU TRẢ LỜI CỦA MODEL
        // ==========================================
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

        // Đổi các cú pháp SQLite phổ biến sang T-SQL
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
            return sql;
        }

        // ==========================================
        // CHỈ CHO PHÉP 1 CÂU SELECT
        // ==========================================
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

        // maxRows = 0 nghĩa là lấy hết
        private DataTable RunSelect(string sql, int maxRows)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 15;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(0, maxRows, dt);
                }
            }
            return dt;
        }

        private string FormatTable(DataTable dt)
        {
            if (dt.Rows.Count == 0) return "Không có dữ liệu.";

            bool truncated = dt.Rows.Count > MaxRowsShown;
            int rowsToShow = truncated ? MaxRowsShown : dt.Rows.Count;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join(" | ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            sb.AppendLine(new string('-', 40));

            CultureInfo vi = CultureInfo.GetCultureInfo("vi-VN");
            for (int i = 0; i < rowsToShow; i++)
            {
                List<string> cells = new List<string>();
                foreach (object v in dt.Rows[i].ItemArray)
                {
                    if (v == null || v is DBNull) cells.Add("NULL");
                    else if (v is double || v is float || v is decimal)
                        cells.Add(((IFormattable)v).ToString("#,##0.##", vi));
                    else cells.Add(v.ToString());
                }
                sb.AppendLine(string.Join(" | ", cells));
            }

            if (truncated) sb.AppendLine("... (chỉ hiển thị " + MaxRowsShown + " dòng đầu)");
            else sb.AppendLine("(" + rowsToShow + " dòng)");
            return sb.ToString().TrimEnd();
        }

        private void AppendChat(string speaker, string message, Color color,
                                string fontName = "Segoe UI", float fontSize = 10)
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;
            rtbChatHistory.SelectionColor = color;
            rtbChatHistory.SelectionFont = new Font(fontName, fontSize,
                speaker.Contains("Bạn") ? FontStyle.Bold : FontStyle.Regular);
            rtbChatHistory.AppendText(speaker + message + "\n\n");
            rtbChatHistory.ScrollToCaret();
        }

        private void RemoveFrom(int start)
        {
            rtbChatHistory.Select(start, rtbChatHistory.TextLength - start);
            rtbChatHistory.SelectedText = "";
        }

        // Chỉ dùng khi UseModelFallback = true
        private async Task<string> GetAIProResponseAsync(string prompt)
        {
            string url = $"http://127.0.0.1:8000/api/chat?prompt={Uri.EscapeDataString(prompt)}";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                string jsonString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResponse = JObject.Parse(jsonString);
                    return jsonResponse["reply"]?.ToString() ?? "Không nhận được phản hồi từ AI.";
                }
                return $"⚠️ Lỗi HTTP {(int)response.StatusCode}: {jsonString}";
            }
            catch (Exception ex)
            {
                return "⚠️ Chi tiết lỗi kết nối C#: " + ex.Message;
            }
        }
    }
}