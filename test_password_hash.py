#!/usr/bin/env python3
"""
Script test password hash với passlib
Chạy: python test_password_hash.py
"""

from passlib.hash import bcrypt_sha256

# Password hash từ database của bạn
# Thay đổi giá trị này bằng password hash THỰC TẾ trong database
password_hash_from_db = "$bcrypt-sha256$v=2,t=2a,r=10$ThT1wMMFTDO3h/Lq9Ai6q.$lANTGUUYOs8N/oAtH.oJDRtCb0fyPHi"

# Password hash MỚI (đầy đủ)
password_hash_new = "$bcrypt-sha256$v=2,t=2a,r=10$JzVngLhmDKSMGxQSPVCns.$xmHuuZRH2o/4SFqN566qTkivGGcVdr9K"

# Passwords để test
test_passwords = ["Admin@123", "admin123", "test123", "password123"]

print("=" * 60)
print("TEST PASSWORD HASH")
print("=" * 60)
print()

# Test với hash từ database
print("1. Test với hash TỪ DATABASE:")
print(f"   Hash: {password_hash_from_db}")
print(f"   Length: {len(password_hash_from_db)}")
print()

try:
    for pwd in test_passwords:
        try:
            result = bcrypt_sha256.verify(pwd, password_hash_from_db)
            if result:
                print(f"   ✅ Password '{pwd}' ĐÚNG!")
                break
        except Exception as e:
            print(f"   ❌ Error với '{pwd}': {e}")
    else:
        print(f"   ❌ Không có password nào đúng!")
except Exception as e:
    print(f"   ❌ LỖI: {e}")

print()
print("-" * 60)
print()

# Test với hash MỚI
print("2. Test với hash MỚI (đầy đủ):")
print(f"   Hash: {password_hash_new}")
print(f"   Length: {len(password_hash_new)}")
print()

try:
    for pwd in test_passwords:
        try:
            result = bcrypt_sha256.verify(pwd, password_hash_new)
            if result:
                print(f"   ✅ Password '{pwd}' ĐÚNG!")
                break
        except Exception as e:
            print(f"   ❌ Error với '{pwd}': {e}")
    else:
        print(f"   ❌ Không có password nào đúng!")
except Exception as e:
    print(f"   ❌ LỖI: {e}")

print()
print("-" * 60)
print()

# Tạo hash mới
print("3. Tạo password hash MỚI:")
print()

for pwd in ["Admin@123", "test123"]:
    try:
        new_hash = bcrypt_sha256.using(rounds=10).hash(pwd)
        print(f"   Password: {pwd}")
        print(f"   Hash: {new_hash}")
        print(f"   Length: {len(new_hash)}")
        
        # Verify ngay
        verify_result = bcrypt_sha256.verify(pwd, new_hash)
        print(f"   Verify: {'✅ OK' if verify_result else '❌ FAIL'}")
        print()
    except Exception as e:
        print(f"   ❌ Error: {e}")
        print()

print("=" * 60)
print("HƯỚNG DẪN:")
print("=" * 60)
print()
print("1. Copy password hash MỚI từ phần 3 ở trên")
print("2. Chạy SQL trong DBeaver:")
print()
print("   UPDATE Users")
print("   SET Password = '<paste_hash_here>'")
print("   WHERE Username = 'admin';")
print()
print("3. Test login với password tương ứng")
print()
print("=" * 60)
