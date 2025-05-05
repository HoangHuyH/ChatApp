# Cấu trúc Dự án Chi tiết (ASP.NET Core Razor Pages)

## 1. Tổng quan

Cấu trúc dự án được tổ chức theo các lớp logic (Presentation, Business Logic, Data Access) và các tính năng của ứng dụng nhắn tin. Sử dụng ASP.NET Core Razor Pages làm nền tảng chính.

## 2. Cấu trúc Thư mục Gốc

```
ChatApp/
│
├── ChatApp.sln                   # Solution file
│
└── src/
    │
    └── ChatApp.Web/              # Dự án chính ASP.NET Core
        │
        ├── ChatApp.Web.csproj    # Project file
        ├── Program.cs            # (Hoặc Startup.cs) Cấu hình ứng dụng và dịch vụ
        ├── appsettings.json      # Cấu hình ứng dụng (chuỗi kết nối, etc.)
        ├── Properties/
        │   └── launchSettings.json # Cấu hình khởi chạy
        │
        ├── wwwroot/                # Thư mục chứa file tĩnh
        │   ├── css/                # Files CSS (e.g., site.css, bootstrap.min.css)
        │   ├── js/                 # Files JavaScript (e.g., site.js, chat.js, signalr.js)
        │   ├── lib/                # Thư viện frontend (e.g., bootstrap, jquery)
        │   └── uploads/            # Thư mục lưu trữ file tải lên (cần tạo)
        │       ├── images/
        │       ├── videos/
        │       ├── files/
        │       └── stories/
        │
        ├── Data/                   # Lớp Data Access (EF Core)
        │   ├── ApplicationDbContext.cs # DbContext
        │   └── Migrations/         # Thư mục chứa các migrations của EF Core
        │
        ├── Models/                 # ViewModels, DTOs, và có thể cả Entities nếu đơn giản
        │   ├── Entities/           # Các lớp thực thể (User, Message, Group, etc.)
        │   │   ├── User.cs
        │   │   ├── Friendship.cs
        │   │   ├── Message.cs
        │   │   ├── Group.cs
        │   │   ├── GroupMember.cs
        │   │   ├── Story.cs
        │   │   └── UserStatus.cs
        │   ├── ViewModels/         # Các lớp ViewModel cho Pages
        │   │   ├── Account/        # ViewModels cho Account Pages
        │   │   │   ├── LoginViewModel.cs
        │   │   │   └── RegisterViewModel.cs
        │   │   ├── Chat/           # ViewModels cho Chat Pages
        │   │   │   └── ChatViewModel.cs
        │   │   └── ... (Các ViewModels khác cho Friends, Groups, Stories)
        │   └── DTOs/               # Data Transfer Objects (nếu cần)
        │
        ├── Services/               # Lớp Business Logic
        │   ├── Interfaces/         # Interfaces cho Services
        │   │   ├── IAuthService.cs
        │   │   ├── IUserService.cs
        │   │   ├── IFriendshipService.cs
        │   │   ├── IChatService.cs
        │   │   ├── IGroupService.cs
        │   │   ├── IStoryService.cs
        │   │   └── IFileService.cs
        │   ├── AuthService.cs      # Implementations của Services
        │   ├── UserService.cs
        │   ├── FriendshipService.cs
        │   ├── ChatService.cs
        │   ├── GroupService.cs
        │   ├── StoryService.cs
        │   └── FileService.cs
        │
        ├── Hubs/                   # SignalR Hubs
        │   └── ChatHub.cs
        │
        ├── Pages/                  # Razor Pages (Presentation Layer)
        │   ├── _ViewStart.cshtml
        │   ├── _ViewImports.cshtml
        │   ├── Index.cshtml        # Trang chính (có thể là trang chat)
        │   ├── Index.cshtml.cs
        │   ├── Privacy.cshtml
        │   ├── Privacy.cshtml.cs
        │   ├── Error.cshtml
        │   ├── Error.cshtml.cs
        │   ├── Shared/             # Layouts và Partial Views dùng chung
        │   │   ├── _Layout.cshtml
        │   │   └── _LoginPartial.cshtml
        │   ├── Account/            # Pages cho quản lý tài khoản
        │   │   ├── Login.cshtml
        │   │   ├── Login.cshtml.cs
        │   │   ├── Register.cshtml
        │   │   ├── Register.cshtml.cs
        │   │   ├── Logout.cshtml
        │   │   ├── Logout.cshtml.cs
        │   │   └── ... (ForgotPassword, ResetPassword)
        │   ├── Chat/               # Pages cho chức năng chat
        │   │   ├── Index.cshtml    # Giao diện chat chính
        │   │   ├── Index.cshtml.cs
        │   │   └── _ChatHistoryPartial.cshtml # Partial view hiển thị lịch sử chat
        │   ├── Friends/            # Pages cho quản lý bạn bè
        │   │   ├── Index.cshtml
        │   │   ├── Index.cshtml.cs
        │   │   └── _FriendRequestPartial.cshtml
        │   ├── Groups/             # Pages cho quản lý nhóm
        │   │   ├── Index.cshtml
        │   │   ├── Index.cshtml.cs
        │   │   ├── Create.cshtml
        │   │   ├── Create.cshtml.cs
        │   │   └── Details.cshtml
        │   │   └── Details.cshtml.cs
        │   └── Stories/            # Pages cho quản lý story
        │       ├── Index.cshtml    # Xem story
        │       ├── Index.cshtml.cs
        │       ├── Create.cshtml
        │       └── Create.cshtml.cs
        │
        └── BackgroundServices/     # (Tùy chọn) Các dịch vụ chạy nền
            └── ExpiredStoryCleanupService.cs
```

## 3. Giải thích Cấu trúc

- **`ChatApp.Web`:** Dự án chính chứa tất cả code của ứng dụng. Đối với một dự án nhỏ/bài tập, việc gộp tất cả vào một project là chấp nhận được. Trong các dự án lớn hơn, BLL và DAL có thể được tách thành các Class Library riêng biệt.
- **`wwwroot`:** Chứa các tài nguyên tĩnh như CSS, JavaScript, hình ảnh, và các thư viện frontend. Thư mục `uploads` được tạo thủ công hoặc bằng code để lưu file do người dùng tải lên.
- **`Data`:** Chứa `DbContext` và các migrations của Entity Framework Core, đại diện cho lớp truy cập dữ liệu.
- **`Models`:**
    - **`Entities`:** Các lớp POCO (Plain Old CLR Object) ánh xạ trực tiếp tới các bảng trong cơ sở dữ liệu.
    - **`ViewModels`:** Các lớp được thiết kế đặc biệt để truyền dữ liệu đến và đi từ các Razor Pages. Chúng có thể chứa dữ liệu từ nhiều Entities và logic trình bày cụ thể.
    - **`DTOs`:** (Data Transfer Objects) Tùy chọn, dùng để truyền dữ liệu giữa các lớp, đặc biệt là qua các ranh giới như API hoặc SignalR Hub, giúp tách biệt cấu trúc dữ liệu nội bộ và cấu trúc dữ liệu truyền đi.
- **`Services`:** Chứa các lớp logic nghiệp vụ. Mỗi service thường xử lý một lĩnh vực chức năng cụ thể (Auth, User, Chat...). Các interface được định nghĩa trong thư mục con `Interfaces` để tuân thủ Dependency Inversion Principle.
- **`Hubs`:** Chứa các lớp SignalR Hub, xử lý giao tiếp thời gian thực.
- **`Pages`:** Chứa các Razor Pages, là thành phần chính của lớp trình bày. Mỗi page bao gồm một file `.cshtml` (HTML và Razor syntax) và một file `.cshtml.cs` (PageModel chứa logic xử lý request và tương tác với Services).
    - **`Shared`:** Chứa các layout (`_Layout.cshtml`) và partial views (`_LoginPartial.cshtml`) được sử dụng lại trên nhiều trang.
    - Các thư mục con (`Account`, `Chat`, `Friends`, `Groups`, `Stories`) tổ chức các trang theo chức năng.
- **`BackgroundServices`:** (Tùy chọn) Chứa các dịch vụ chạy nền độc lập với request của người dùng, ví dụ như dọn dẹp dữ liệu cũ.

## 4. Luồng hoạt động cơ bản

1.  Người dùng truy cập một URL.
2.  ASP.NET Core Routing ánh xạ URL đến một Razor Page trong thư mục `Pages`.
3.  Phương thức Handler trong PageModel (`.cshtml.cs`) được thực thi (ví dụ: `OnGetAsync`, `OnPostAsync`).
4.  PageModel gọi các phương thức trong lớp `Services` tương ứng (được inject qua constructor) để thực hiện logic nghiệp vụ.
5.  Lớp `Services` sử dụng `ApplicationDbContext` (được inject) để truy vấn hoặc cập nhật dữ liệu trong SQLite.
6.  Nếu cần tương tác real-time (ví dụ: gửi tin nhắn), `Services` hoặc `PageModel` có thể lấy `IHubContext<ChatHub>` (được inject) để gọi các phương thức trên `ChatHub` hoặc trực tiếp đến các client.
7.  `ChatHub` xử lý các kết nối và giao tiếp hai chiều với các client JavaScript thông qua SignalR.
8.  PageModel chuẩn bị dữ liệu (thường là thông qua ViewModel) và trả về cho file `.cshtml`.
9.  File `.cshtml` render HTML cuối cùng để gửi về trình duyệt người dùng.
10. JavaScript trong trình duyệt (trong `wwwroot/js`) kết nối đến `ChatHub`, gửi sự kiện (ví dụ: người dùng gõ tin nhắn) và nhận sự kiện (ví dụ: có tin nhắn mới) để cập nhật UI động.
