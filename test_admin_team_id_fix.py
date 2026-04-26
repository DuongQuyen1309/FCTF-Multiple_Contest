#!/usr/bin/env python
"""
Script để test xem fix team_id cho admin đã hoạt động chưa
"""

import sys
import os

# Add FCTF-ManagementPlatform to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'FCTF-ManagementPlatform'))

def test_getattr_with_default():
    """Test getattr với default value"""
    print("=" * 60)
    print("TEST 1: getattr với default value")
    print("=" * 60)
    
    class MockUser:
        def __init__(self):
            self.id = 1
            self.name = "admin"
            self.type = "admin"
            # Không có team_id attribute
    
    user = MockUser()
    
    # Test 1: Truy cập trực tiếp (sẽ lỗi)
    print("\n1. Truy cập trực tiếp user.team_id:")
    try:
        team_id = user.team_id
        print(f"   ❌ Không lỗi (không đúng): team_id = {team_id}")
    except AttributeError as e:
        print(f"   ✅ Lỗi như mong đợi: {e}")
    
    # Test 2: Dùng getattr với default
    print("\n2. Dùng getattr(user, 'team_id', None):")
    team_id = getattr(user, 'team_id', None)
    print(f"   ✅ Không lỗi: team_id = {team_id}")
    
    # Test 3: Dùng hasattr
    print("\n3. Dùng hasattr(user, 'team_id'):")
    has_team_id = hasattr(user, 'team_id')
    print(f"   ✅ Kết quả: {has_team_id}")
    
    # Test 4: Kết hợp hasattr và getattr
    print("\n4. Kết hợp hasattr và getattr:")
    team_id = getattr(user, 'team_id', None) if hasattr(user, 'team_id') and user.team_id is not None else -1
    print(f"   ✅ team_id = {team_id}")


def test_userattrs():
    """Test UserAttrs với admin user"""
    print("\n" + "=" * 60)
    print("TEST 2: UserAttrs với admin user")
    print("=" * 60)
    
    try:
        from CTFd.constants.users import UserAttrs
        
        print("\n1. UserAttrs fields:")
        print(f"   {UserAttrs._fields}")
        
        print("\n2. Tạo UserAttrs với team_id=None:")
        attrs = UserAttrs(
            id=1,
            oauth_id=None,
            name="admin",
            email="admin@test.com",
            type="admin",
            secret=None,
            website=None,
            affiliation=None,
            country=None,
            bracket_id=None,
            hidden=False,
            banned=False,
            verified=True,
            language=None,
            team_id=None,  # Admin không có team_id
            created=None
        )
        print(f"   ✅ Thành công: {attrs.name}, team_id={attrs.team_id}")
        
        print("\n3. Kiểm tra hasattr và getattr:")
        has_team_id = hasattr(attrs, 'team_id')
        team_id_value = getattr(attrs, 'team_id', None)
        print(f"   hasattr(attrs, 'team_id') = {has_team_id}")
        print(f"   getattr(attrs, 'team_id', None) = {team_id_value}")
        
        print("\n4. Kiểm tra điều kiện trong get_current_team_attrs:")
        if attrs and hasattr(attrs, 'team_id') and attrs.team_id:
            print(f"   ❌ Sẽ gọi get_team_attrs (không đúng cho admin)")
        else:
            print(f"   ✅ Không gọi get_team_attrs (đúng cho admin)")
            
    except ImportError as e:
        print(f"   ⚠️  Không thể import CTFd: {e}")
        print("   (Chạy script này từ thư mục gốc của project)")


def main():
    print("\n" + "=" * 60)
    print("TEST FIX ADMIN TEAM_ID ERROR")
    print("=" * 60)
    
    test_getattr_with_default()
    test_userattrs()
    
    print("\n" + "=" * 60)
    print("KẾT LUẬN")
    print("=" * 60)
    print("""
✅ Fix đã đúng nếu:
   1. getattr(user, 'team_id', None) trả về None thay vì lỗi
   2. hasattr(user, 'team_id') check được attribute có tồn tại không
   3. UserAttrs có thể tạo với team_id=None
   4. Điều kiện trong get_current_team_attrs không gọi get_team_attrs cho admin

📝 Bước tiếp theo:
   1. Khởi động lại ManagementPlatform: cd FCTF-ManagementPlatform && python serve.py
   2. Đăng nhập với admin tại http://localhost:8000/login
   3. Kiểm tra không còn lỗi AttributeError
    """)


if __name__ == "__main__":
    main()
