# Tài Liệu Kỹ Thuật Tích Hợp Auto Combat & Naruto Mod
*(Dành cho Developer / AI Agent tiếp quản bảo trì và sửa lỗi)*

Tài liệu này ghi lại toàn bộ chi tiết kỹ thuật, các thay đổi mã nguồn, cơ chế xử lý và hướng dẫn khắc phục sự cố (debugging) khi tích hợp mod **Drafted Auto-Combat** với hệ sinh thái **Naruto**.

---

## 1. Thông Tin Tổng Quan & Các Mod Liên Quan

| Tên Mod | Package ID | Workshop ID | Vai Trò |
| :--- | :--- | :--- | :--- |
| **Drafted Auto-Combat** | `RedMattis.AutoCombat` | `3546270076` | Mod gốc cung cấp tính năng chiến đấu tự động khi Draft pawn |
| **World of Naruto** | `LifeisGame.WorldOfNaruto` | `3025338958` | Mod Naruto chính (Nhẫn thuật, Huyết kế, Vĩ thú,...) |
| **Taran Magic Framework (TMF)** | `Taranchuk.TaranMagicFramework` | `3527502876` | Framework nền tảng quản lý cây kỹ năng, chakra, cooldown |
| **Naruto Expanded (Karma Legacy)** | `Ghost.WorldOfNaruto.KarmaLegacy` | `3803570326` | Bản mở rộng (Rinnegan, Karma, Đất sét nổ C1-C4,...) |

---

## 2. Nguyên Tắc Kiến Trúc (Architecture Constraints)

1. **Decoupled 100% (Không Hard-Reference):**
   - Dự án `AutoCombat.csproj` **KHÔNG** tham chiếu trực tiếp file `.dll` của TaranMagicFramework, WorldOfNaruto hay KarmaLegacy.
   - Tất cả tương tác với TMF runtime đều được thực hiện qua **Dynamic Reflection & Harmony** trong class `TMFAutoCombatIntegration`.
   - **Mục đích:** Đảm bảo mod Auto Combat hoạt động độc lập, không bị crash game do lỗi `MissingMethodException` hay `TypeLoadException` nếu người chơi dùng save không cài Naruto.

---

## 3. Danh Sách Các File Đã Thay Đổi & Tạo Mới

### 3.1. `Source/BigAndSmall/TMFAutoCombatIntegration.cs` (TẠO MỚI - TRỌNG TÂM)
Đây là cầu nối trung tâm giữa Auto Combat và hệ thống Taran Magic Framework:
- **`Initialize()`:** Cache các `Type`, `MethodInfo`, `FieldInfo`, `PropertyInfo` của TMF (`CompAbilities`, `Ability`, `AbilityTierDef`, `TMagicUtils`, `AIAbilityWorker`,...).
- **`ResolveAbility(Command instance, Pawn contextPawn)`:** Phân giải một `Gizmo/Command` bất kỳ trên giao diện về đối tượng `TaranMagicFramework.Ability`:
  1. Kiểm tra Property `Ability` (công khai / ẩn).
  2. Kiểm tra Field `ability` / `Ability`.
  3. Kiểm tra Delegate closures (`Command_Action.action.Target` hoặc `Command_Toggle.toggleAction.Target`, đào sâu các field compiler-generated `<>c__DisplayClass` để trích xuất `Ability`).
  4. Fallback: So khớp `instance.defaultLabel` hoặc `instance.icon` với danh sách kỹ năng đã học trong `compAbilities.AllLearnedAbilities` của Pawn đang chọn.
- **`HandleGizmo(...)`:** Xử lý hiển thị icon vàng chữ **Auto** trên ô kỹ năng và bắt sự kiện Click chuột phải (`GizmoState.RightClicked`) để Toggle Auto-Cast.
- **`IsSelfBuffOrToggle(object ability)`:** Nhận diện các chiêu tự niệm / biến hình / triệu hồi / bùa chú dựa vào tên class (`Mode`, `Sage`, `Gates`, `Cloak`, `Susanoo`, `Summon`, `Clone`, `CurseMark`, `Mantle`,...) hoặc `TargetingParameters` (chỉ nhắm vào bản thân hoặc nhắm vào đá/vật phẩm).
- **`IsActive(object ability)`:** Kiểm tra trạng thái kích hoạt thực tế thông qua thuộc tính `ability.Active` và danh sách Hediff trên cơ thể Pawn (`pawn.health.hediffSet.HasHediff(...)`) để **chống spam hoặc tắt nhầm chiêu**.
- **`ActivateSelfBuff(object ability, Pawn pawn)`:** Thi triển chiêu thức biến hình / triệu hồi. Tự động xử lý vị trí xuất chiêu:
  - Nếu chiêu cần vật phẩm (ví dụ Golem Đá cần đá tảng `StoneChunks`), tự động quét khối đá gần nhất trong bán kính 25 ô.
  - Nếu chiêu cần ô đất trước mặt, tự động lấy ô trước mặt `pawn.Position + pawn.Rotation.FacingCell`.
  - Mặc định chọn chính bản thân Pawn.
- **`TryCastOffensiveAbility(...)`:** Thi triển chiêu công kích vào mục tiêu `enemyTarget`:
  1. Kiểm tra `AIAbilityWorker` của chiêu nếu có.
  2. Kiểm tra các `Verb` tầm xa / cận chiến của chiêu.
  3. Fallback: Kiểm tra cự ly (`effectRadius`), tầm nhìn (Line of Sight) và tạo Job thông qua `TMagicUtils.MakeJobAbility` hoặc gọi `DoCast(target)`.

### 3.2. `Source/BigAndSmall/DraftedActionData.cs` (CHỈNH SỬA)
- Thêm trường `public List<string> autocastTMFAbilities = new List<string>();` để lưu danh sách `defName` các nhẫn thuật được bật Auto Cast.
- Bổ sung các phương thức:
  - `AutoCastFor(string defName)`
  - `ToggleAutoCastFor(string defName)`
  - Sửa `ToggleAutoForAll(bool? force = null)` để gọi sang `TMFAutoCombatIntegration.ToggleAutoForAll()`.
- Bổ sung `Scribe_Collections.Look` trong `ExposeData()` để lưu danh sách kỹ năng tự động vào file save game của RimWorld.

### 3.3. `Source/BigAndSmall/DraftGizmos.cs` (CHỈNH SỬA)
- Trong `AddToggleAllAbilitiesButton()`: Cập nhật điều kiện hiển thị trạng thái nút **Auto Use All** nếu `autocastTMFAbilities.Count > 0`.
- Trong Harmony Patch `GizmoOnGUIPostfix` trên `Command.GizmoOnGUIInt`: Gọi sang `TMFAutoCombatIntegration.HandleGizmo()` để vẽ icon Auto và nhận diện sự kiện Click chuột phải.

### 3.4. `Source/BigAndSmall/JobGiver_AutoCombat.cs` (CHỈNH SỬA)
- Trong `ExtraTargetValidator`: Cho phép Pawn nhắm mục tiêu nếu có nhẫn thuật TMF hợp lệ.
- Trong `TryGetAttackVerb`: Cho phép lấy Verb nhẫn thuật của TMF khi Pawn chuẩn bị tấn công.
- Trong `GetAbilityJob`: Đưa `TMFAutoCombatIntegration.TryGetTMFAbilityJob()` lên đầu luồng xử lý trước các chiêu thức Psycast/Gene vanilla.

### 3.5. `About/About.xml` & `LoadFolders.xml` (CHỈNH SỬA)
- Thêm hỗ trợ các phiên bản RimWorld: `1.5`, `1.6`, `1.7`.
- Cấu hình thẻ `<loadAfter>`:
  - `Taranchuk.TaranMagicFramework`
  - `LifeisGame.WorldOfNaruto`
  - `Ghost.WorldOfNaruto.KarmaLegacy`

---

## 4. Hướng Dẫn Debug Nếu Gặp Sự Cố (Troubleshooting for Next Agent)

Nếu người dùng phản hồi có lỗi hoặc chiêu thức nào đó không hoạt động, hãy kiểm tra theo các tình huống sau:

### Tình huống 1: Kỹ năng không hiện icon "Auto" hoặc Click chuột phải không ăn
- **Nguyên nhân:** Gizmo của kỹ năng đó không phải là `Command_Ability` chuẩn mà là một lớp custom ghi đè `GetGizmos()` không chứa thuộc tính `Ability`.
- **Cách sửa:** Mở `TMFAutoCombatIntegration.cs`, kiểm tra hàm `ResolveAbility()`. Thêm điều kiện bắt lớp Command đó hoặc kiểm tra xem `instance.defaultLabel` có bị đổi tên động hay không.

### Tình huống 2: Kỹ năng Biến hình / Thể thức (Sage Mode, Gates, Susanoo, Cloak) bị spam dùng liên tục hoặc bật xong tự tắt
- **Nguyên nhân:** Hàm `IsActive()` chưa nhận diện được Hediff tương ứng của kỹ năng đó trên Pawn.
- **Cách sửa:**
  1. Kiểm tra Hediff mà kỹ năng đó gắn lên Pawn trong XML hoặc C#.
  2. Bổ sung tên Hediff hoặc cơ chế kiểm tra vào hàm `IsActive()` trong `TMFAutoCombatIntegration.cs`.

### Tình huống 3: Kỹ năng Triệu hồi (Summons / Clones) không chịu dùng
- **Nguyên nhân:** Kỹ năng đó bị xếp nhầm vào nhóm tấn công mục tiêu (`TryCastOffensiveAbility`) thay vì nhóm bổ trợ (`IsSelfBuffOrToggle`), dẫn đến việc kiểm tra tầm đánh vào kẻ địch bị thất bại do chiêu không nhắm vào địch.
- **Cách sửa:**
  1. Thêm từ khóa tên class của chiêu đó vào `IsSelfBuffOrToggle()`.
  2. Nếu chiêu cần vị trí đặt linh thú đặc biệt, cập nhật `ActivateSelfBuff()` để chỉ định `castTarget` hợp lệ.

### Tình huống 4: Kỹ năng chưởng xa (Fireball, Rasengan, Water Dragon,...) không tự bắn
- **Nguyên nhân:** Sai số về tầm đánh (`GetAbilityRange`) hoặc vướng kiểm tra Line of Sight (`GenSight.LineOfSight`).
- **Cách sửa:**
  1. Kiểm tra xem chiêu đó dùng `effectRadius` hay `VerbProperties.range`.
  2. Kiểm tra `requireLineOfSight`: một số chiêu xuyên tường hoặc diện rộng không yêu cầu LOS.

---

## 5. Hướng Dẫn Biên Dịch (Compilation)

Khi thực hiện chỉnh sửa trong thư mục `Source/`:
1. Mở terminal tại thư mục gốc của mod:
   ```powershell
   dotnet build "Source/AutoCombat.csproj" -c Release
   ```
2. Copy file DLL sau khi build:
   - Từ: `Source/bin/Release/net48/AutoCombat.dll`
   - Sang: `NotBigAndSmall/Assemblies/AutoCombat.dll`
3. Commit và push lên GitHub:
   ```powershell
   git add .
   git commit -m "Fix: mô tả nội dung sửa"
   git push origin main
   ```
