---
name: session-handoff
description: >-
  Tổng hợp toàn bộ kiến thức của cuộc hội thoại hiện tại thành một file bàn giao
  (handoff) có cấu trúc, để một cuộc hội thoại MỚI có thể đọc lại và tiếp tục
  công việc đúng chỗ đã dừng. Use this skill whenever the user wants to save,
  summarize, snapshot, or hand off the current conversation's progress so a new
  session can continue — OR when starting/resuming work and there is an existing
  handoff file to read first. Bắt buộc kích hoạt khi người dùng nói các kiểu:
  "tổng hợp lại", "lưu lại context / tiến độ", "tạo file bàn giao / handoff",
  "ghi nhớ để conversation sau làm tiếp", "chốt phiên", "tiếp tục công việc
  trước", "đọc lại tiến độ", "save progress", "session handoff", "resume",
  "continue where we left off", "pick up from last time" — kể cả khi họ không
  dùng đúng chữ "handoff".
---

# Session Handoff — Bộ nhớ liên-phiên cho agent

Mục tiêu của skill: vì mỗi cuộc hội thoại mới KHÔNG nhớ gì từ hội thoại cũ, ta
duy trì **một file bàn giao** ghi lại đầy đủ "đang làm gì / đã làm tới đâu /
việc tiếp theo / các quyết định đã chốt". File này là nguồn sự thật để phiên sau
đọc và làm tiếp mà không phải hỏi lại từ đầu.

Skill có **2 chế độ**. Xác định đúng chế độ trước khi làm:

| Chế độ | Khi nào | Việc cần làm |
|---|---|---|
| **WRITE** (ghi/cập nhật) | Người dùng muốn lưu/tổng hợp/chốt tiến độ; hoặc cuối một phiên làm việc | Đọc toàn bộ hội thoại → viết/cập nhật file handoff |
| **RESUME** (đọc lại) | Bắt đầu phiên mới, muốn tiếp tục việc cũ; hoặc nhắc tới "tiến độ trước" | Đọc file handoff trước tiên → tóm tắt hiểu biết → xác nhận bước tiếp theo |

---

## Vị trí & tên file (quy ước)

- File chính (living document, **ghi đè/cập nhật tại chỗ**):
  `.agents/memory/SESSION_HANDOFF.md`
- Nhật ký tóm tắt mỗi phiên (tùy chọn, **append-only**, không xóa lịch sử):
  `.agents/memory/HANDOFF_LOG.md`

Nếu thư mục `.agents/memory/` chưa tồn tại thì tạo mới. Nếu dự án đã có quy ước
khác (vd. `docs/`), hỏi/tuân theo quy ước đó nhưng giữ nguyên cấu trúc nội dung.

---

## Chế độ WRITE — tạo / cập nhật file bàn giao

### Nguyên tắc cốt lõi
> Tiêu chuẩn chất lượng: **một agent hoàn toàn không có ký ức, chỉ đọc file này,
> phải đủ thông tin để tiếp tục công việc mà không cần hỏi lại.** Nếu chưa đạt,
> file còn thiếu.

### Quy trình
1. **Đọc TOÀN BỘ hội thoại hiện tại**, không chỉ vài tin nhắn cuối. Trích ra
   *kiến thức bền vững* (durable), bỏ qua phần tán gẫu/lặp:
   - Mục tiêu & phạm vi công việc.
   - **Các quyết định đã chốt + LÝ DO** (quan trọng nhất — đừng làm mất).
   - Những gì đã hoàn thành, những gì đang dở.
   - File/artefact đã tạo hoặc sửa và vai trò của chúng.
   - Việc tiếp theo (next steps) theo thứ tự ưu tiên.
   - Ràng buộc, quy ước, tech stack, lệnh hay dùng, kiến thức nghiệp vụ.
   - Câu hỏi còn mở / điểm cần người dùng xác nhận.
2. **Nếu đã có** `SESSION_HANDOFF.md`: đọc trước, rồi **cập nhật tại chỗ** —
   gộp thông tin mới, thay thế phần đã lỗi thời, đánh dấu việc đã xong. KHÔNG
   tạo file v2/v3 song song.
3. **Viết file** theo đúng cấu trúc mẫu ở `references/HANDOFF_TEMPLATE.md`
   (đọc file đó để lấy khung chuẩn). Cập nhật dòng "Cập nhật lần cuối" với ngày
   giờ hiện tại và tăng số "Phiên bản".
4. **Ghi 1 dòng tóm tắt phiên** vào cuối `HANDOFF_LOG.md` (nếu dùng):
   `- YYYY-MM-DD HH:mm — <tóm tắt 1 câu việc đã làm trong phiên này>`
5. **Giữ file gọn:** mục tiêu/quyết định/trạng thái/next-steps phải luôn cập
   nhật và chính xác. Việc đã hoàn thành từ lâu có thể nén lại 1 dòng hoặc dời
   xuống Change Log để mục "Trạng thái" không phình to.
6. Sau khi ghi xong, báo cho người dùng đường dẫn file và tóm tắt ngắn những
   mục đã cập nhật.

### Những điều PHẢI tránh
- Đừng chỉ tóm tắt tin nhắn cuối — phải tổng hợp cả hành trình.
- Đừng đánh mất "quyết định đã chốt" và lý do của chúng.
- Đừng bịa thông tin chưa từng xuất hiện trong hội thoại. Không chắc → ghi vào
  mục "Câu hỏi mở".
- Đừng nhồi bí mật nhạy cảm (mật khẩu, token, khóa API, secret) vào file.

---

## Chế độ RESUME — đọc lại để làm tiếp

1. Kiểm tra `.agents/memory/SESSION_HANDOFF.md`. Nếu có → **đọc trước khi làm
   bất cứ việc gì khác.**
2. Tóm tắt lại cho người dùng (3–6 gạch đầu dòng): mục tiêu, trạng thái hiện
   tại, các quyết định đã chốt cần tôn trọng, và **bước tiếp theo đề xuất**.
3. Hỏi xác nhận ngắn gọn ("Tiếp tục từ [bước X] đúng không?") rồi bắt tay làm.
4. Coi các "Quyết định đã chốt" là ràng buộc — không tự ý đảo ngược; nếu thấy
   cần đổi, nêu rõ và xin xác nhận.
5. Trong/cuối phiên, khi có tiến triển đáng kể → chuyển sang chế độ WRITE để cập
   nhật lại file.

---

## File trong skill này
- `references/HANDOFF_TEMPLATE.md` — **khung chuẩn** của file bàn giao. Luôn đọc
  file này khi viết/cập nhật ở chế độ WRITE.
- `examples/EXAMPLE_HANDOFF.md` — một ví dụ đã điền hoàn chỉnh (dự án thật) để
  tham khảo mức độ chi tiết phù hợp.

## Mẹo dùng (cho người dùng)
- Cuối mỗi phiên, chỉ cần nói "tổng hợp lại tiến độ" → skill chạy chế độ WRITE.
- Đầu phiên mới, nói "đọc tiến độ và làm tiếp" → skill chạy chế độ RESUME.
- Có thể để file `SESSION_HANDOFF.md` được agent tự đọc đầu phiên bằng cách
  tham chiếu nó trong `AGENTS.md`/rule của workspace nếu muốn tự động hóa.
