# Sửa lỗi "Unknown column 'u.TeamId'" - Entity Framework Configuration Issue

## Vấn đề phát hiện (Root Cause)

Backend báo lỗi:
```
Unknown column 'u.TeamId' in 'SELECT'
```

**Nguyên nhân**: Cấu hình Entity Framework sai trong `AppDbContext.cs`

### Phân tích chi tiết:

1. **Database schema đúng**:
   - Bảng `users` KHÔNG có cột `TeamId`
   - Relationship User ↔ Team là **many-to-many** qua bảng `users_teams`
   - Bảng `teams` có cột `captain_id` (FK đến `users`)

2. **Model C# đúng**:
   - `User.Teams` là `ICollection<Team>` (many-to-many)
   - `Team.Users` là `ICollection<User>` (many-to-many)
   - `Team.Captain` là `User` (many-to-one)
   - `Team.CaptainId` là `int?` (FK)

3. **Cấu hình EF Core SAI**:
   ```csharp
   // SAI - Dòng 908 trong AppDbContext.cs
   entity.HasOne(d => d.Captain).WithMany(p => p.Teams)
       .HasForeignKey(d => d.CaptainId)
   ```
   
   Cấu hình này nói với EF Core rằng:
   - `Team.Captain` → `User` (đúng)
   - `User.Teams` là inverse navigation của `Team.Captain` (SAI!)
   
   EF Core hiểu nhầm rằng `User.Teams` là one-to-many relationship qua `CaptainId`, và cố tạo shadow property `TeamId` trong bảng `users`.

## Giải pháp đã áp dụng

### 1. Sửa cấu hình Team.Captain (dòng ~908)

**Trước:**
```csharp
entity.HasOne(d => d.Captain).WithMany(p => p.Teams)
    .HasForeignKey(d => d.CaptainId)
    .OnDelete(DeleteBehavior.SetNull)
    .HasConstraintName("team_captain_id");
```

**Sau:**
```csharp
entity.HasOne(d => d.Captain).WithMany()  // Không map vào User.Teams
    .HasForeignKey(d => d.CaptainId)
    .OnDelete(DeleteBehavior.SetNull)
    .HasConstraintName("team_captain_id");
```

### 2. Cấu hình many-to-many relationship User ↔ Team (cuối file, sau UserTeam entity)

**Thêm:**
```csharp
// Configure many-to-many relationship between User and Team via UserTeam
modelBuilder.Entity<User>()
    .HasMany(u => u.Teams)
    .WithMany(t => t.Users)
    .UsingEntity<UserTeam>(
        j => j.HasOne(ut => ut.Team)
              .WithMany()
              .HasForeignKey(ut => ut.TeamId),
        j => j.HasOne(ut => ut.User)
              .WithMany()
              .HasForeignKey(ut => ut.UserId),
        j =>
        {
            j.HasKey(ut => new { ut.UserId, ut.TeamId });
            j.ToTable("users_teams");
        });
```

## Các bước thực hiện

### 1. Dừng backend
```powershell
# Trong terminal đang chạy backend, nhấn Ctrl+C
```

### 2. Restart backend
```powershell
cd ControlCenterAndChallengeHostingServer\ContestantBE
dotnet run
```

### 3. Kiểm tra backend log
Backend phải khởi động thành công và hiển thị:
```
Now listening on: http://localhost:5069
```

### 4. Thử đăng nhập lại
- Mở browser: `http://localhost:5173`
- Username: `student1`
- Password: `test123`

## Kết quả mong đợi

✅ **Thành công**: Đăng nhập thành công, nhận được token

❌ **Nếu vẫn lỗi**: Kiểm tra backend log để xem lỗi mới

## Giải thích kỹ thuật

### Tại sao cấu hình cũ sai?

EF Core có 2 loại relationship:

1. **One-to-Many với FK trong database**:
   ```csharp
   // Team có CaptainId → User
   entity.HasOne(d => d.Captain).WithMany(p => p.Teams)
   ```
   EF Core hiểu: Bảng `teams` có cột `captain_id`, và `User.Teams` là collection của các team mà user làm captain.
   
   **Vấn đề**: EF Core cố tìm FK trong bảng `users` để map ngược lại → tạo shadow property `TeamId`

2. **Many-to-Many với junction table**:
   ```csharp
   // User ↔ Team qua users_teams
   modelBuilder.Entity<User>()
       .HasMany(u => u.Teams)
       .WithMany(t => t.Users)
       .UsingEntity<UserTeam>(...)
   ```
   EF Core hiểu: Relationship qua bảng `users_teams`, không cần FK trong `users` hay `teams`.

### Tại sao cần cả 2 cấu hình?

Trong hệ thống này:
- **Team.Captain**: One-to-many (một user có thể là captain của nhiều team)
- **User.Teams / Team.Users**: Many-to-many (một user có thể thuộc nhiều team, một team có nhiều user)

Đây là 2 relationship KHÁC NHAU:
- `Team.Captain` → User làm captain
- `Team.Users` → Tất cả members trong team (bao gồm cả captain)

## Lưu ý quan trọng

1. **Không cần migration database**: Database schema đã đúng từ đầu
2. **Chỉ sửa code C#**: Thay đổi cấu hình EF Core để khớp với database
3. **Không ảnh hưởng dữ liệu**: Không có thay đổi nào đến database

## Kiểm tra thêm

Nếu muốn verify relationship đã đúng:

```powershell
# Kiểm tra user có team nào
docker exec -it fctf-mariadb mysql -u fctf_user -pfctf_password ctfd -e "SELECT ut.user_id, ut.team_id, u.name as user_name, t.name as team_name FROM users_teams ut JOIN users u ON ut.user_id = u.id JOIN teams t ON ut.team_id = t.id WHERE u.name='student1';"
```

## Tham khảo

- [EF Core Many-to-Many Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)
- [EF Core Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
