# Thiết kế Frontend (ASP.NET Core Razor Pages & SignalR)

## 1. Tổng quan

Frontend của ứng dụng nhắn tin sẽ được xây dựng bằng ASP.NET Core Razor Pages, kết hợp với JavaScript để xử lý tương tác người dùng động và tích hợp với SignalR client library để nhận/gửi thông điệp thời gian thực. Giao diện sẽ được thiết kế đơn giản, tập trung vào chức năng cốt lõi của ứng dụng nhắn tin.

## 2. Công nghệ và Thư viện

- **Framework:** ASP.NET Core Razor Pages
- **Ngôn ngữ:** C#, HTML, CSS, JavaScript
- **Real-time:** `@microsoft/signalr` JavaScript client library
- **Styling:** Bootstrap (hoặc CSS tùy chỉnh) để tạo giao diện nhanh chóng và đáp ứng.
- **JavaScript:** Vanilla JS hoặc một thư viện nhỏ gọn như jQuery (tùy chọn) để đơn giản hóa DOM manipulation và AJAX calls.

## 3. Cấu trúc Layout Chính (`_Layout.cshtml`)

Một layout chung sẽ được sử dụng cho các trang yêu cầu xác thực:

- **Header:** Thanh điều hướng trên cùng, có thể chứa logo, tên người dùng đăng nhập, nút đăng xuất, và có thể là ô tìm kiếm bạn bè nhanh.
- **Sidebar (Tùy chọn):** Có thể có một sidebar bên trái để hiển thị danh sách bạn bè, nhóm chat, và truy cập nhanh các chức năng.
- **Main Content Area:** Khu vực chính để hiển thị nội dung của từng trang (`@RenderBody()`).
- **Footer:** Thông tin cơ bản (nếu cần).

## 4. Các Trang (Pages) và Thành phần (Components) Chính

### 4.1. Trang Đăng nhập (`Pages/Account/Login.cshtml`)

- Form đăng nhập với các trường: Tên đăng nhập, Mật khẩu.
- Nút "Đăng nhập".
- Liên kết đến trang Đăng ký và Quên mật khẩu.
- Hiển thị thông báo lỗi nếu đăng nhập thất bại.

### 4.2. Trang Đăng ký (`Pages/Account/Register.cshtml`)

- Form đăng ký với các trường: Tên đăng nhập, Mật khẩu, Xác nhận mật khẩu, Tên hiển thị.
- Nút "Đăng ký".
- Liên kết đến trang Đăng nhập.
- Hiển thị thông báo lỗi hoặc thành công.

### 4.3. Trang Quên/Đặt lại Mật khẩu (`Pages/Account/ForgotPassword.cshtml`, `ResetPassword.cshtml`)

- Giao diện đơn giản để người dùng nhập tên đăng nhập/email (tùy theo logic backend) và sau đó nhập mật khẩu mới (nếu có token hợp lệ).

### 4.4. Trang Chính / Bảng điều khiển (`Pages/Index.cshtml` hoặc `Pages/Chat/Index.cshtml`)

Đây là giao diện cốt lõi sau khi đăng nhập, thường được chia thành nhiều phần:

- **Danh sách Bạn bè/Nhóm (Sidebar hoặc Panel riêng):**
    - Hiển thị danh sách bạn bè với tên và trạng thái online/offline (cập nhật real-time).
    - Hiển thị danh sách các nhóm chat người dùng tham gia.
    - Cho phép người dùng chọn một bạn bè hoặc nhóm để bắt đầu/tiếp tục trò chuyện.
    - Có thể có chỉ báo tin nhắn chưa đọc.
- **Khu vực Story:**
    - Hiển thị các story của bạn bè (ví dụ: dạng thumbnail tròn ở đầu trang hoặc khu vực riêng).
    - Cho phép click để xem story chi tiết (có thể mở modal hoặc trang riêng).
    - Nút để đăng story mới.
- **Khu vực Chat:**
    - Hiển thị lịch sử tin nhắn của cuộc trò chuyện đang được chọn (cá nhân hoặc nhóm).
    - Tin nhắn được hiển thị theo thứ tự thời gian, phân biệt tin nhắn của mình và của người khác.
    - Hiển thị tên người gửi (trong chat nhóm).
    - Hiển thị trạng thái tin nhắn (Đã gửi, Đã xem - cập nhật real-time).
    - Hiển thị chỉ báo "Đang nhập..." (cập nhật real-time).
    - Hiển thị các file đính kèm (hình ảnh thu nhỏ, link tải file).
    - Tự động cuộn xuống tin nhắn mới nhất.
- **Khu vực Nhập liệu Tin nhắn:**
    - Ô nhập văn bản.
    - Nút gửi tin nhắn.
    - Nút đính kèm file (mở dialog chọn file).
    - (Tùy chọn) Nút ghi âm tin nhắn thoại.

### 4.5. Trang Quản lý Bạn bè (`Pages/Friends/Index.cshtml`)

- **Tìm kiếm người dùng:** Ô tìm kiếm và nút tìm kiếm. Hiển thị kết quả tìm kiếm với nút "Gửi yêu cầu kết bạn".
- **Yêu cầu đang chờ:** Hiển thị danh sách yêu cầu kết bạn đã nhận với nút "Chấp nhận" và "Từ chối".
- **Yêu cầu đã gửi:** Hiển thị danh sách yêu cầu đã gửi với nút "Hủy yêu cầu".

### 4.6. Trang Quản lý Nhóm (`Pages/Groups/Index.cshtml`, `Create.cshtml`, `Details.cshtml`)

- **Danh sách nhóm:** Hiển thị các nhóm người dùng tham gia.
- **Tạo nhóm mới (`Create.cshtml`):** Form nhập tên nhóm, chọn bạn bè để thêm vào nhóm ban đầu.
- **Chi tiết nhóm (`Details.cshtml`):** Hiển thị thông tin nhóm, danh sách thành viên. Cho phép quản trị viên thêm/xóa thành viên (nếu có quyền).

### 4.7. Trang Đăng Story (`Pages/Stories/Create.cshtml`)

- Form cho phép nhập nội dung text hoặc tải lên hình ảnh.
- Nút "Đăng Story".

## 5. Tích hợp SignalR Client (JavaScript)

- **Kết nối:** Trong file JavaScript chính (ví dụ: `site.js` hoặc `chat.js`), khởi tạo và bắt đầu kết nối đến `ChatHub` (`/chathub`) sau khi trang tải xong và người dùng đã xác thực.
  ```javascript
  const connection = new signalR.HubConnectionBuilder()
      .withUrl("/chathub")
      .build();

  connection.start().then(() => {
      console.log("SignalR Connected.");
      // Gọi các phương thức Hub khác nếu cần sau khi kết nối
  }).catch(err => console.error(err.toString()));
  ```
- **Gửi sự kiện:** Gắn các event listener vào các phần tử UI (nút gửi tin nhắn, ô nhập liệu) để gọi các phương thức trên Hub.
  ```javascript
  // Ví dụ: Gửi tin nhắn cá nhân
  document.getElementById("sendButton").addEventListener("click", event => {
      const receiverUserId = /* Lấy ID người nhận từ UI */;
      const message = document.getElementById("messageInput").value;
      connection.invoke("SendPrivateMessage", receiverUserId, message).catch(err => console.error(err.toString()));
      document.getElementById("messageInput").value = ""; // Xóa input
      event.preventDefault();
  });

  // Ví dụ: Thông báo đang nhập
  document.getElementById("messageInput").addEventListener("input", event => {
      const receiverUserId = /* Lấy ID người nhận */;
      connection.invoke("NotifyTyping", receiverUserId).catch(err => console.error(err.toString()));
  });
  ```
- **Nhận sự kiện:** Đăng ký các handler để xử lý các phương thức được gọi từ Hub (Server gọi Client).
  ```javascript
  // Ví dụ: Nhận tin nhắn cá nhân
  connection.on("ReceivePrivateMessage", (senderUserId, senderDisplayName, messageContent, sentAt, messageId) => {
      // Logic để hiển thị tin nhắn mới trong khu vực chat
      // Cập nhật UI...
      // Có thể gọi Hub để đánh dấu đã đọc nếu cửa sổ chat đang mở
  });

  // Ví dụ: Cập nhật trạng thái bạn bè
  connection.on("UpdateFriendStatus", (userId, isOnline) => {
      // Logic để cập nhật icon trạng thái của bạn bè trong danh sách
  });

  // Ví dụ: Nhận chỉ báo đang nhập
  connection.on("ReceiveTypingIndicator", (senderUserId) => {
      // Hiển thị chỉ báo "... đang nhập" cho senderUserId
      // Cần có logic để ẩn chỉ báo sau một khoảng thời gian ngắn
  });
  ```
- **Quản lý State:** Cần có logic JavaScript để quản lý trạng thái hiện tại của giao diện (ví dụ: cuộc trò chuyện nào đang được chọn) để hiển thị đúng thông tin và gửi đúng tham số khi gọi Hub.

## 6. Xử lý File

- **Upload:** Sử dụng thẻ `<input type="file">`. Khi người dùng chọn file, dùng JavaScript để lấy đối tượng `File`, có thể kiểm tra sơ bộ kích thước/loại file trên client, sau đó gửi file lên server thông qua một form POST đến một Action/Handler riêng biệt (không qua SignalR). Backend sẽ xử lý lưu file và có thể gửi thông báo tin nhắn chứa link file qua SignalR.
- **Download/Display:** Hiển thị link tải file hoặc thẻ `<img>` với đường dẫn đến file đã lưu trên server (ví dụ: `/uploads/images/abc.jpg`).

## 7. UI/UX Considerations

- **Responsive Design:** Sử dụng Bootstrap hoặc CSS media queries để đảm bảo giao diện hiển thị tốt trên các kích thước màn hình khác nhau.
- **Thông báo:** Sử dụng các thông báo tinh tế (ví dụ: toast notifications) cho các sự kiện như tin nhắn mới (khi cửa sổ chat không active), yêu cầu kết bạn mới.
- **Loading Indicators:** Hiển thị chỉ báo tải khi đang lấy dữ liệu (ví dụ: lịch sử chat) hoặc thực hiện hành động (gửi tin nhắn).
- **Real-time Feedback:** Cập nhật trạng thái tin nhắn (Đã gửi, Đã xem), trạng thái online, chỉ báo đang nhập một cách tức thì để người dùng cảm nhận được tính real-time.

