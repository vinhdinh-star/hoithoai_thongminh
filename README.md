# tuong_tac_nguoi_may# quanlykho-vlxd-app
# quanlykho-vlxd-app

# Ứng dụng Quản lý Kho Vật liệu Xây dựng tích hợp AI (Qwen2.5)

Hệ thống quản lý kho Vật liệu Xây dựng thông minh viết trên nền tảng **.NET 8.0 WinForms**, tích hợp mô hình ngôn ngữ lớn cục bộ **Qwen2.5-1.5B** (thông qua LLamaSharp) để hỗ trợ tra cứu dữ liệu kho bằng ngôn ngữ tự nhiên, kết hợp backend cơ sở dữ liệu SQLite/SQL Server.

---

## 🚀 Tính năng nổi bật
* **Giao diện Desktop trực quan:** Xây dựng bằng C# Windows Forms đáp ứng thao tác nghiệp vụ nhanh chóng.
* **Trợ lý AI thông minh (Text-to-SQL):** Cho phép người dùng gõ câu lệnh tiếng Việt tự nhiên (ví dụ: *"Vật tư tồn kho thấp"*, *"Tìm xi măng"*), AI sẽ tự động phân tích từ khóa, truy vấn cơ sở dữ liệu và đưa ra phản hồi chính xác.
* **Chạy Offline hoàn toàn:** Sử dụng mô hình chạy cục bộ trên CPU, đảm bảo bảo mật dữ liệu tuyệt đối mà không cần kết nối API bên ngoài.

---

## 📦 Hướng dẫn Tải và Cài đặt sử dụng

Do file trọng lượng của mô hình AI (`.gguf`) vượt quá giới hạn lưu trữ thông thường của GitHub (>100MB), mã nguồn được chia sẻ trên repository này **chưa bao gồm file model**. Bạn cần thực hiện các bước sau để chạy ứng dụng:

### 1. Clone repository về máy
Mở Git Bash và chạy lệnh:
git clone [https://github.com/vinhdinh-star/tuong_tac_nguoi_may.git](https://github.com/vinhdinh-star/tuong_tac_nguoi_may.git)
``bash
2. Chuẩn bị file Model AI (.gguf)
Tải mô hình đã được lượng tử hóa tối ưu cho CPU: qwen2.5-1.5b-instruct-q5_k_m.gguf (dung lượng khoảng ~1.2 GB).

Tạo một thư mục tên là Models nằm cùng cấp với thư mục dự án hoặc bên trong thư mục chạy của file .exe.

Đặt file qwen2.5-1.5b-instruct-q5_k_m.gguf vào bên trong thư mục Models đó.

3. Biên dịch và Chạy ứng dụng
Mở solution (.sln) của dự án bằng phần mềm Visual Studio 2022.

Kiểm tra cấu hình kết nối cơ sở dữ liệu (Database Connection String) trong mã nguồn cho khớp với môi trường của bạn.

Nhấn Start (hoặc phím F5) để chạy thử, hoặc tiến hành chọn Publish nếu bạn muốn đóng gói thành file .exe độc lập để sử dụng trên các máy tính khác.

🛠️ Công nghệ sử dụng
Ngôn ngữ: C# (.NET 8.0 Windows Forms)

Trí tuệ nhân tạo: LLamaSharp (CPU backend), Qwen2.5-1.5B-Instruct GGUF

Quản lý dữ liệu: SQLite / SQL Server

Quản lý mã nguồn: Git & GitHub

`'
