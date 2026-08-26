# 📖 LacVietGiaPha — Project Status & Working Standard

> **File này là nguồn chuẩn duy nhất (single source of truth)** để bất kỳ trợ lý AI/dev nào tham gia dự án đọc trước khi làm việc.
> Mục đích: tránh phải mô tả lại cấu trúc dự án mỗi lần bắt đầu phiên làm việc mới.

---

## 🛠️ Công nghệ sử dụng

**Backend**
- ASP.NET Core 9
- EF Core 9.0.15 (đồng bộ bản cùng SDK hiện tại)
- MySQL (Pomelo.EntityFrameworkCore.MySql v9.0.0)
- Kiến trúc: Clean Architecture 3 tầng (API / Core / Infrastructure)

**Frontend**
- React ^19.2.8 + Vite ^8.2.0
- React Router DOM ^7.18.2
- Axios ^1.19.0 (HTTP client)
- Vanilla CSS + Custom design tokens (Không dùng TailwindCSS)

---

## 📂 Cấu trúc thư mục

`
LacVietGiaPha/
├── LacVietGiaPha-BE/                     # Backend Layer
│   ├── LacVietGenealogy.sln
│   ├── LacVietGenealogy.API/             # Presentation layer
│   │   ├── Program.cs                    # Entry point, JWT & RBAC Config
│   │   └── appsettings.json              # Connection string & JWT Secret
│   ├── LacVietGenealogy.Core/            # Domain layer
│   │   └── Entities/                     # Models & Relationships (Guid standard)
│   └── LacVietGenealogy.Infrastructure/  # Data access layer
│       └── Data/
│           ├── AppDbContext.cs           # Global Query Filters applied
│           └── DbSeeder.cs               # Seeder for Roles & Permissions
│
└── LacVietGiaPha-FE/                     # Frontend Layer
    └── src/
        ├── contexts/                     # AuthContext & FamilyTreeContext
        ├── components/                   # ProtectedRoute Guard
        ├── services/                     # Axios Client (api.js), authService, familyTreeService
        └── pages/                        # Login, Register, SelectTree, Dashboard
`

---

## 📍 Trạng thái hiện tại (Cập nhật liên tục)

**🔵 Đang ở: Giai đoạn 2 — Bàn giao & Chuẩn bị phát triển Cây gia phả trực quan**

### Việc đã hoàn thành
| Ngày | Giai đoạn.Bước | Đã làm gì | Trạng thái |
|---|---|---|---|
| 2026-08-27 | Giai đoạn 0 | Chuẩn hóa Entity Guid, cấu hình Global Query Filter, tích hợp JWT Authentication, cài đặt Authorization Policy theo permission, thiết lập AuthController và FamilyTreeController, seed dữ liệu quyền và vai trò mặc định, hoàn tất chạy Migration vào DB MySQL cục bộ. | ✅ Hoàn thành |
| 2026-08-27 | Giai đoạn 1 | Thiết lập Axios client có interceptor đính kèm JWT và tự động refresh token khi gặp lỗi 401. Tạo AuthContext và FamilyTreeContext quản lý state toàn cục. Thiết lập ProtectedRoute bảo vệ route theo đăng nhập và theo permission. Xây dựng giao diện Login, Register, SelectTree và Dashboard. | ✅ Hoàn thành |
| 2026-08-27 | Thiết kế UI/UX | Làm lại hoàn toàn giao diện Frontend theo phong cách Light Mode cổ điển ấm áp truyền thống (giấy bản, đỏ son, viền tinh tế), áp dụng chuẩn **UI/UX Pro Max**: thiết kế responsive, cải thiện accessibility (ARIA, semantic HTML), tối ưu phím tắt và hiệu ứng chuyển động vi mô (micro-interactions). | ✅ Hoàn thành |

### Kế hoạch tiếp theo (Dành cho Dev/AI tiếp theo)
1. **Phát triển Cây gia phả trực quan (Interactive Family Tree Component):**
   - Thiết kế hoặc tích hợp thư viện vẽ sơ đồ (như React Flow, D3.js hoặc SVG thuần) để hiển thị sơ đồ phả hệ.
   - Cho phép zoom, kéo thả, click vào thành viên để xem popup thông tin chi tiết.
   - Thêm nút thêm/sửa thành viên trực tiếp từ node trên cây.
2. **Thuật toán xưng hô (Kinship algorithm):**
   - Xây dựng thuật toán tính toán mối quan hệ họ hàng giữa hai người bất kỳ trên cây (VD: chú, bác, cô, dì, anh em họ, cháu đích tôn...) dựa trên khoảng cách thế hệ và nhánh.

---

## 📐 Quy tắc làm việc chung cho mọi trợ lý AI tham gia dự án

1. **Luôn đọc file này trước**, không yêu cầu người dùng mô tả lại cấu trúc dự án.
2. Từ "tenant/multi-tenant" chỉ dùng khi nói về **khái niệm kiến trúc**, không dùng làm tên class/bảng/biến thực tế (sử dụng FamilyTree).
3. Mọi entity nghiệp vụ mới **bắt buộc** có FamilyTreeId (tenant key), kiểu Guid, không nullable.
4. Không tự ý bỏ qua Global Query Filter trong DB Context.
5. Dùng Permission-based Authorization Policy, **không** hardcode kiểm tra role.
6. **Sau khi hoàn thành bất kỳ phần việc nào → cập nhật ngay mục "📍 Trạng thái hiện tại" ở trên**.