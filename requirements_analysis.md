# Phân tích Yêu cầu Dự án Ứng dụng Nhắn tin

## 1. Tổng quan

Dự án này nhằm mục đích xây dựng một ứng dụng nhắn tin web đơn giản cho môn Lập trình Mạng căn bản. Ứng dụng sẽ được phát triển bằng C# với ASP.NET Core, sử dụng SQLite làm cơ sở dữ liệu và SignalR để hỗ trợ giao tiếp thời gian thực.

## 2. Công nghệ sử dụng

- **Ngôn ngữ lập trình:** C#
- **Framework Backend:** ASP.NET Core
- **Framework Frontend:** ASP.NET Core Razor Pages (hoặc MVC)
- **Cơ sở dữ liệu:** SQLite
- **Giao tiếp Real-time:** SignalR
- **Môi trường phát triển:** Visual Studio 2022

## 3. Yêu cầu Chức năng Chi tiết

Dựa trên thông tin từ người dùng và hình ảnh cung cấp, các chức năng chính của ứng dụng bao gồm:

### 3.1. Module Xác thực Người dùng
- **Đăng ký:** Cho phép người dùng mới tạo tài khoản với tên đăng nhập, mật khẩu, và có thể là thông tin cá nhân cơ bản (tên hiển thị).
- **Đăng nhập:** Xác thực người dùng hiện tại bằng tên đăng nhập và mật khẩu.
- **Quên mật khẩu/Đặt lại mật khẩu:** Cung cấp cơ chế đơn giản để người dùng khôi phục hoặc đặt lại mật khẩu (ví dụ: qua email - mặc dù email có thể phức tạp, có thể thay bằng câu hỏi bí mật hoặc cơ chế đơn giản hơn cho bài tập).

### 3.2. Module Quản lý Danh bạ
- **Tìm kiếm người dùng:** Cho phép người dùng tìm kiếm người dùng khác trong hệ thống bằng tên đăng nhập hoặc tên hiển thị.
- **Gửi yêu cầu kết bạn:** Người dùng có thể gửi lời mời kết bạn đến người dùng khác.
- **Quản lý yêu cầu kết bạn:** Người dùng có thể xem danh sách yêu cầu kết bạn đã nhận và chấp nhận hoặc từ chối.
- **Hủy yêu cầu kết bạn:** Người dùng có thể hủy yêu cầu kết bạn đã gửi.
- **Hiển thị danh sách bạn bè:** Hiển thị danh sách những người dùng đã kết bạn.
- **Hiển thị trạng thái online/offline:** Hiển thị trạng thái hoạt động (online/offline) của bạn bè trong danh sách.

### 3.3. Module Nhắn tin Cá nhân
- **Gửi/Nhận tin nhắn văn bản:** Cho phép hai người dùng đã kết bạn gửi và nhận tin nhắn văn bản cho nhau theo thời gian thực.
- **Hiển thị lịch sử trò chuyện:** Lưu trữ và hiển thị lịch sử tin nhắn giữa hai người dùng.
- **Hiển thị trạng thái tin nhắn:** Hiển thị trạng thái của tin nhắn đã gửi (ví dụ: "Đã gửi", "Đã xem").
- **Chỉ báo "Đang nhập...":** Hiển thị thông báo khi đối phương đang soạn tin nhắn.
- **Gửi/Nhận tin nhắn thoại:** (Tính năng nâng cao) Cho phép người dùng ghi âm và gửi tin nhắn thoại ngắn. Nhận và phát lại tin nhắn thoại.

### 3.4. Module Nhắn tin Nhóm
- **Tạo nhóm:** Người dùng có thể tạo một cuộc trò chuyện nhóm mới.
- **Đặt tên nhóm:** Cho phép đặt tên cho nhóm chat.
- **Thêm/Xóa thành viên:** Người dùng tạo nhóm (quản trị viên) có thể thêm hoặc xóa thành viên khỏi nhóm.
- **Phân quyền quản trị viên:** (Đơn giản) Người tạo nhóm là quản trị viên, có thể có thêm quyền quản lý thành viên.
- **Gửi/Nhận tin nhắn trong nhóm:** Tất cả thành viên trong nhóm có thể gửi và nhận tin nhắn văn bản.

### 3.5. Module Chia sẻ File & Phương tiện
- **Gửi file:** Cho phép người dùng gửi các loại file (hình ảnh, video, tài liệu) trong cuộc trò chuyện cá nhân và nhóm.
- **Giới hạn kích thước file:** Áp dụng giới hạn về kích thước tối đa cho mỗi file tải lên.
- **Hiển thị/Tải file:** Hiển thị hình ảnh/video thu nhỏ và cho phép tải về các file đính kèm.

### 3.6. Module Thông báo & Trạng thái
- **Thông báo tin nhắn mới:** Thông báo cho người dùng (có thể là thông báo trên giao diện web) khi có tin nhắn mới.
- **Thông báo yêu cầu kết bạn:** Thông báo khi có yêu cầu kết bạn mới.
- **Cập nhật trạng thái online/offline:** Tự động cập nhật và hiển thị trạng thái online/offline của người dùng khi họ đăng nhập/đăng xuất hoặc mất kết nối.

### 3.7. Module Gọi thoại/Video (Tính năng nâng cao)
- **Thực hiện cuộc gọi:** Cho phép người dùng bắt đầu cuộc gọi thoại hoặc video với một người bạn trong danh sách.
- **Nhận/Từ chối cuộc gọi:** Người nhận có thể chấp nhận hoặc từ chối cuộc gọi đến.
- **Lưu ý:** Tính năng này phức tạp, thường yêu cầu WebRTC. Đối với bài tập cơ bản, có thể chỉ cần mô phỏng giao diện hoặc bỏ qua.

### 3.8. Module Đăng/Xem Story
- **Đăng Story:** Người dùng có thể đăng nội dung (văn bản, hình ảnh) làm story, hiển thị cho bạn bè.
- **Tự động xóa Story:** Story sẽ tự động biến mất sau 24 giờ.
- **Xem Story của bạn bè:** Người dùng có thể xem danh sách story từ bạn bè của họ.

## 4. Yêu cầu Phi chức năng

- **Giao diện người dùng:** Đơn giản, dễ sử dụng, phù hợp với ứng dụng web.
- **Hiệu năng:** Phản hồi nhanh chóng đối với các tương tác cơ bản và tin nhắn thời gian thực (trong phạm vi bài tập).
- **Khả năng mở rộng:** Thiết kế cơ bản, không yêu cầu cao về khả năng mở rộng quy mô lớn.
- **Bảo mật:** Mức độ cơ bản, chủ yếu tập trung vào xác thực người dùng và quản lý phiên làm việc. Không yêu cầu mã hóa đầu cuối hoặc các biện pháp bảo mật phức tạp.
- **Cơ sở dữ liệu:** Sử dụng SQLite, phù hợp cho ứng dụng nhỏ và đơn giản, dễ dàng triển khai.

