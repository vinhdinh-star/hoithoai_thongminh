from fastapi import FastAPI
import pyodbc

app = FastAPI()

conn_str = (
    r"DRIVER={ODBC Driver 17 for SQL Server};"
    r"SERVER=(localdb)\MSSQLLocalDB;"
    r"DATABASE=app_quanlykhoVLXD;"
    r"Trusted_Connection=yes;"
)

@app.get("/api/chat")
def ai_process(prompt: str):
    prompt_lower = prompt.lower().strip()
    
    # 1. Xử lý các câu chào hỏi, giao tiếp ngoài lề
    if any(word in prompt_lower for in ["chào", "hi ", "hello", "bạn ơi", "xin chào"]):
        return {
            "status": "success", 
            "reply": "👋 Chào bạn! Tôi là Trợ lý AI Pro. Tôi có thể giúp gì cho việc quản lý kho vật liệu xây dựng của bạn hôm nay?"
        }
        
    elif any(word in prompt_lower for word in ["cảm ơn", "thanks", "thank you"]):
        return {
            "status": "success", 
            "reply": "😊 Không có gì! Rất vui được hỗ trợ bạn vận hành kho hiệu quả hơn."
        }
        
    elif any(word in prompt_lower for word in ["tên gì", "bạn là ai", "giới thiệu"]):
        return {
            "status": "success", 
            "reply": "🤖 Tôi là AI Pro - Trợ lý thông minh tích hợp trong hệ thống quản lý kho VLXD, giúp bạn tra cứu dữ liệu và phân tích kho hàng nhanh chóng."
        }

    # 2. Xử lý nghiệp vụ tra cứu tồn kho (có thể mở rộng thêm nhiều từ khóa)
    elif any(word in prompt_lower for word in ["tồn kho", "kiểm tra", "còn lại", "danh sách vật tư"]):
        try:
            conn = pyodbc.connect(conn_str)
            cursor = conn.cursor()
            cursor.execute("SELECT TOP 5 v.TenVT, k.TenKho, t.SoLuongTon, v.DonViTinh FROM TonKho t JOIN VatTu v ON t.MaVT = v.MaVT JOIN Kho k ON t.MaKho = k.MaKho")
            
            rows = cursor.fetchall()
            conn.close()
            
            if not rows:
                return {"status": "success", "reply": "📊 Hiện tại cơ sở dữ liệu chưa có dữ liệu tồn kho nào."}
                
            result_text = "📊 Dữ liệu tồn kho phân tích từ hệ thống:\n"
            for row in rows:
                result_text += f"\n• {row[0]} tại {row[1]}: còn lại {row[2]} {row[3]}"
            return {"status": "success", "reply": result_text}
            
        except Exception as e:
            return {"status": "error", "reply": f"⚠️ Lỗi kết nối CSDL từ Python: {str(e)}"}
            
    # 3. Xử lý nghiệp vụ gợi ý nhập hàng
    elif any(word in prompt_lower for word in ["gợi ý", "nhập hàng", "mua thêm", "bổ sung"]):
        return {
            "status": "success",
            "reply": "💡 **Phân tích chiến lược từ AI Pro:**\n"
                     "1. Các mặt hàng như Xi măng và Sắt thép đang có lượng tiêu thụ ổn định, nên lên lịch đặt hàng nhà cung cấp trước thứ 5 hàng tuần.\n"
                     "2. Cần kiểm tra lại khu vực kho chính để tối ưu không gian lưu trữ vật liệu nặng."
        }

    # 4. Phản hồi mặc định cho các câu hỏi ngoài lề khác mà AI chưa định nghĩa cụ thể
    else:
        return {
            "status": "success", 
            "reply": f"🤔 Tôi đã tiếp nhận câu hỏi: \"{prompt}\". Hiện tại tôi đang tập trung hỗ trợ quản lý kho VLXD, tra cứu tồn kho và gợi ý nhập hàng. Bạn có muốn tôi kiểm tra kho giúp không?"
        }