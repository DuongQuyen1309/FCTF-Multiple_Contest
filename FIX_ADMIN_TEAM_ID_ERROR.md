# 🔧 FIX LỖI: AttributeError 'Admins' object has no attribute 'team_id'

## ❌ VẤN ĐỀ
Sau khi đăng nhập thành công với tài khoản admin vào ManagementPlatform (http://localhost:8000/login), hệ thống báo lỗi:

```
AttributeError: 'Admins' object has no attribute 'team_id'
```

## 🔍 NGUYÊN NHÂN
1. **Kiến trúc database**: 
   - Bảng `Users` KHÔNG có cột `team_id` trực tiếp
   - Chỉ có bảng `ContestParticipants` mới có `team_id`
   - Admin users (type='admin') không thuộc team nào cả

2. **Lỗi trong code**:
   - File `CTFd/utils/user/__init__.py` có hàm `get_user_attrs()` cố gắng lấy tất cả các thuộc tính từ `UserAttrs` namedtuple
   - `UserAttrs` có field `team_id` nhưng admin users không có thuộc tính này
   - Khi code cố truy cập `user.team_id` cho admin → AttributeError

3. **Các file bị ảnh hưởng**:
   - `FCTF-ManagementPlatform/CTFd/utils/user/__init__.py` (2 chỗ)
   - `FCTF-ManagementPlatform/CTFd/DeployHistory.py` (4 chỗ)

## ✅ GIẢI PHÁP ĐÃ ÁP DỤNG

### 1. Fix `CTFd/utils/user/__init__.py`

#### Chỗ 1: Hàm `get_user_attrs()`
**Trước:**
```python
@cache.memoize(timeout=300)
def get_user_attrs(user_id):
    user = Users.query.filter_by(id=user_id).first()
    if user:
        d = {}
        for field in UserAttrs._fields:
            d[field] = getattr(user, field)  # ❌ Lỗi ở đây
        return UserAttrs(**d)
    return None
```

**Sau:**
```python
@cache.memoize(timeout=300)
def get_user_attrs(user_id):
    user = Users.query.filter_by(id=user_id).first()
    if user:
        d = {}
        for field in UserAttrs._fields:
            # ✅ Dùng getattr với default None cho các thuộc tính có thể không tồn tại
            d[field] = getattr(user, field, None)
        return UserAttrs(**d)
    return None
```

#### Chỗ 2: Hàm `get_current_team_attrs()`
**Trước:**
```python
def get_current_team_attrs():
    if authed():
        try:
            user = get_user_attrs(user_id=session["id"])
        except TypeError:
            clear_user_session(user_id=session["id"])
            user = get_user_attrs(user_id=session["id"])
        if user and user.team_id:  # ❌ Không check hasattr
            return get_team_attrs(team_id=user.team_id)
    return None
```

**Sau:**
```python
def get_current_team_attrs():
    if authed():
        try:
            user = get_user_attrs(user_id=session["id"])
        except TypeError:
            clear_user_session(user_id=session["id"])
            user = get_user_attrs(user_id=session["id"])
        # ✅ Check hasattr trước khi truy cập team_id
        if user and hasattr(user, 'team_id') and user.team_id:
            return get_team_attrs(team_id=user.team_id)
    return None
```

### 2. Fix `CTFd/DeployHistory.py`

**Trước (4 chỗ giống nhau):**
```python
team_id = user.team_id if user.team_id is not None else -1  # ❌ Lỗi
```

**Sau:**
```python
team_id = getattr(user, 'team_id', None) if hasattr(user, 'team_id') and user.team_id is not None else -1  # ✅
```

Đã fix ở 4 functions:
- `get_pods_logs()`
- `get_pods_logs_api()`
- `get_request_logs()`
- `get_request_logs_api()`

## 📋 CÁCH KIỂM TRA

### Bước 1: Khởi động lại ManagementPlatform
```bash
cd FCTF-ManagementPlatform
python serve.py
```

### Bước 2: Đăng nhập với tài khoản admin
1. Mở trình duyệt: http://localhost:8000/login
2. Nhập:
   - Username: `admin`
   - Password: `Admin@123` (hoặc password bạn đã set)
3. Click "Login"

### Bước 3: Kiểm tra kết quả
- ✅ **THÀNH CÔNG**: Được redirect đến trang admin challenges (http://localhost:8000/admin/challenges)
- ❌ **THẤT BẠI**: Vẫn thấy lỗi AttributeError

## 🎯 KẾT QUẢ MONG ĐỢI
- Admin có thể đăng nhập thành công
- Không còn lỗi `AttributeError: 'Admins' object has no attribute 'team_id'`
- Admin có thể truy cập các trang quản trị
- Các chức năng liên quan đến deploy history hoạt động bình thường

## 📝 GHI CHÚ KỸ THUẬT

### Tại sao admin không có team_id?
- Admin là người quản trị hệ thống, không tham gia thi đấu
- Chỉ có contestants (type='user') mới thuộc team
- Team membership được quản lý qua bảng `ContestParticipants`

### Cách xử lý an toàn với team_id:
```python
# ✅ ĐÚNG - Check hasattr trước
if hasattr(user, 'team_id') and user.team_id:
    # Xử lý team_id

# ✅ ĐÚNG - Dùng getattr với default
team_id = getattr(user, 'team_id', None)

# ❌ SAI - Truy cập trực tiếp
team_id = user.team_id  # Lỗi nếu user là admin
```

## 🔗 FILES ĐÃ SỬA
1. `FCTF-ManagementPlatform/CTFd/utils/user/__init__.py`
2. `FCTF-ManagementPlatform/CTFd/DeployHistory.py`

---
**Ngày fix**: 2026-04-26
**Người fix**: Kiro AI Assistant
**Status**: ✅ HOÀN THÀNH
