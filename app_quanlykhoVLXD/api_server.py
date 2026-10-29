from fastapi import FastAPI, Query
from transformers import AutoModelForCausalLM, AutoTokenizer
from peft import PeftModel
import torch, time
import os
torch.set_num_threads(os.cpu_count())

app = FastAPI()

BASE_MODEL = "Qwen/Qwen2.5-Coder-1.5B-Instruct"
ADAPTER_PATH = r"D:\dai_hoc_quy_nhon\tuong_tac_nguoi_may\app_quanlykhoVLXD\app_quanlykhoVLXD\qwen_lora_adapter"

use_cuda = torch.cuda.is_available()
dtype = torch.float16 if use_cuda else torch.float32   # CPU phải dùng float32
print("CUDA:", use_cuda)

tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL)
base_model = AutoModelForCausalLM.from_pretrained(
    BASE_MODEL,
    torch_dtype=dtype,
    device_map="auto" if use_cuda else None,
)
model = PeftModel.from_pretrained(base_model, ADAPTER_PATH)
model = model.merge_and_unload()   # gộp adapter vào model -> suy luận nhanh hơn
model.eval()
print("Device:", model.device, "- Server sẵn sàng.")

# Dùng def (không async) để FastAPI chạy trong threadpool, không chặn server
@app.get("/api/chat")
def chat_with_ai(prompt: str = Query(...)):
    try:
        t0 = time.time()
        system_prompt = "Bạn là trợ lý AI quản lý kho vật liệu xây dựng. Hãy chuyển câu hỏi sau thành câu lệnh SQLite hoặc trả lời phân tích kho phù hợp."
        full_input = f"{system_prompt}\nCâu hỏi: {prompt}\nSQL/Trả lời:"

        inputs = tokenizer(full_input, return_tensors="pt").to(model.device)
        with torch.no_grad():
            outputs = model.generate(
                **inputs,
                max_new_tokens=64,
                do_sample=False,
                pad_token_id=tokenizer.eos_token_id,
            )

        # Chỉ decode phần token mới sinh, không cần split chuỗi
        new_tokens = outputs[0][inputs["input_ids"].shape[1]:]
        reply = tokenizer.decode(new_tokens, skip_special_tokens=True).strip()

        print(f"Xong trong {time.time() - t0:.1f}s")
        return {"reply": reply}
    except Exception as e:
        return {"reply": f"Lỗi xử lý bên Python: {str(e)}"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)