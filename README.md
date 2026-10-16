# 20261007GraduateProjectTest：魚缸軟糖餵食

Unity 2023.1.13f1 · URP 15（也支援 Built-in，以及 Unity 6 + URP 17）

> ## 📦 想用在自己的專案？先看 **[導入手冊 SETUP.md](Assets/Scripts/FishTank/SETUP.md)**
>
> | 你要 | 下載什麼 |
> |---|---|
> | **直接用在自己的 Unity 專案**（大多數人） | 到 **[Releases](../../releases/latest)** 下載 **`FishTank.unitypackage`** → Unity 裡 Assets > Import Package > Custom Package → 照 [SETUP.md](Assets/Scripts/FishTank/SETUP.md) 擺進場景 |
> | 看原始碼、改功能、自己重新打包 | Clone 整個 repo，用 Unity 2023.1.13f1 開啟，**Tools > FishTank** 選單打包 |
>
> 手冊分成：給程式的導入步驟、給美術的換素材方法、預計出現效果、架構、每個腳本的參數、常見問題。

點擊魚缸玻璃 → 在玻璃內側射出一顆發光軟糖（引力球）→ 附近小魚被吸過來圍著一口一口吃 → 軟糖縮小消失。
每團軟糖裡混一兩塊會閃的螢光小塊，魚咬到螢光塊會變成那個顏色並帶螢光。（v2 以前是「彈孔 → 魚穿出魚缸」）

- 功能程式碼：[`Assets/Scripts/FishTank/`](Assets/Scripts/FishTank/)
- **打包與新場景建置：[`Assets/Scripts/FishTank/SETUP.md`](Assets/Scripts/FishTank/SETUP.md)**
- 完整參考（參數、換模型、原理）：[`Assets/Scripts/FishTank/README.md`](Assets/Scripts/FishTank/README.md)

---

## 在本專案測試

1. 用 Unity 2023.1.13f1 開啟專案。
2. 開啟 `Assets/Scenes/SampleScene`，Hierarchy 裡應該有 `FishTankRoot`（掛 `TankBootstrap`）。
   沒有的話：Create Empty → 位置 (0,0,0) → Add Component → `TankBootstrap`。
3. 按 Play，點魚缸玻璃：
   - **左鍵點玻璃**：從鏡頭用力射出一團多色軟糖，啪一聲黏在玻璃內側（力道調 `InteractionManager` 的 `Launch Force`）。
   - 每團裡有 1–2 塊很亮、會閃的螢光小塊；咬到螢光塊的魚會變色並發光。
   - 射在既有軟糖附近會黏上去堆疊變大；場上最多 5 顆。
   - **左鍵按住垃圾**：夾住，拖曳移動、放開鬆手。游標下有垃圾時只會夾，不會發射軟糖。
   - 勾 `Build Beach`（預設）時，鏡頭從旁邊的簡易沙灘開始。沙灘上有一個縮小 3 倍的小魚缸和 3 個小垃圾：
     - 把沙灘的垃圾拖到小魚缸上方放開 → 掉進去的瞬間搬到魚缸世界、變大 3 倍，鏡頭跟著推進小魚缸、穿過玻璃到魚缸世界。
     - 在魚缸裡把大型垃圾拉出水面 → 縮小 3 倍，從小魚缸右邊拋到沙灘上，鏡頭拉回沙灘。
     - **Tab**：手動在沙灘和魚缸之間運鏡。
     - 大型垃圾（瓶子、塑膠袋、鋁罐）：跟手、可以往上拖出水面，帶出魚缸後消失。
     - 小型垃圾（彩色碎片）：跟不上、一直抖，夾子變紅、鏡頭震動，最後一定會滑掉沉回去。

`TankBootstrap` 會在 Play 時用 Primitive 自動生成魚缸（頂端開口）、15 隻魚、軟糖模板、大小垃圾、控制器與 Bloom 後製，並調整相機，不需要任何美術資產。

## 腳本

| 檔案 | 職責 | 是否隨套件帶走 |
|---|---|---|
| `FishController.cs` | 單隻魚狀態機：`FreeSwimming` → `SeekingJelly` → `Eating`；螢光變色、軟Q動作 | 是 |
| `Jelly.cs` | 發光軟糖：多塊黏成一團、彈出、漂浮、發光呼吸、吸引魚、被咬縮小與軟Q晃動、堆疊 | 是 |
| `SoftSpring.cs` | 阻尼彈簧小工具：軟Q的擠壓與回彈 | 是 |
| `FishPool.cs` | 物件池：生成、回收、延遲重生；Inspector 可設定魚 Prefab、數量、可游範圍 | 是 |
| `InteractionManager.cs` | 滑鼠點擊 → Raycast → 在玻璃內側發射軟糖（或堆疊到附近的軟糖） | 是 |
| `TrashItem.cs` | 單件垃圾：水中阻力與浮力、被夾時跟隨、小型垃圾的阻力／掙扎／滑脫、大型垃圾帶出魚缸 | 是 |
| `TrashGrabber.cs` | 夾子：滑鼠左鍵輸入、夾取判定、拖曳、震動事件與模擬震動 | 是 |
| `TrashSpawner.cs` | 開場把垃圾 Prefab 撒在魚缸底部（數量、隨機大小、隨機上色） | 是 |
| `WaterVolume.cs` | 水的範圍與水面（取代原本全域的水面高度），沙灘的海也能用 | 是 |
| `ScaleBridge.cs` | 沙灘小魚缸 ↔ 魚缸世界：物件掉進去放大搬過去、帶出來縮小搬回沙灘 | 是 |
| `DebugCameraSwitch.cs` | 測試用 Tab 在幾台相機之間硬切 | 是 |
| `TransferCameraMove.cs` | 可選的簡易運鏡：物件進出魚缸時推向玻璃、瞬移、移到停留視角；任何相機都能掛 | 是 |
| `PortalWindow.cs` / `.shader` | 可選：小魚缸玻璃顯示魚缸世界（鏡像相機 + RenderTexture），運鏡瞬移無縫 | 是 |
| `ViewCamera.cs` | 決定用哪台相機算點擊（不依賴 MainCamera Tag） | 是 |
| `InteractionCamera.cs` | 標記玩家操作用的相機：掛在相機群組的主鏡頭上 | 是 |
| `TankBootstrap.cs` | 僅本專案測試用，自動建場景 | 否 |

---

## 迭代進度

### v1：基本功能（2026-10-07）
- 依開發筆記實作 `InteractionManager`、`FishController`、`FishPool`、`TankBootstrap`。
- 修正開發筆記範例的問題：魚抵達洞口後，下一幀會被 `MoveTowards` 拉回洞口而卡住。
  改成三段狀態，抵達後切到 `ExitingTank`，沿點擊面的法線方向穿出。
- 不用 `FindObjectsByType`，改由 `FishPool.ActiveFish` 提供在場的魚。
- 決定：場景由程式自動生成、魚逃出後回收重生、彈孔永久保留。
- 在 `SampleScene` 加入 `FishTankRoot`（直接修改場景檔）。
- ✅ Unity 編輯器編譯通過（Editor.log 無 `error CS`）。

### v2：可移植化（2026-10-07）
- `FishPool` 改為可直接從 Inspector 設定並在 `Start()` 自行啟動：
  Fish Prefab、Fish Count、Respawn Delay、Randomize Color、Swim Area（BoxCollider）或 Swim Center / Size、Swim Margin。
  選取時會用 Gizmo 畫出可游範圍。
- 生成的魚先設為 inactive，再由 `Spawn()` 啟用，可接受 active 的 Prefab。
- `InteractionManager` 會自動尋找 FishPool，欄位缺漏時在 Console 顯示錯誤。
- 點擊面確定使用「關掉 Mesh、保留 Collider」的隱形物件，不需要可見的玻璃。
- 新增移植用 [`Assets/Scripts/FishTank/README.md`](Assets/Scripts/FishTank/README.md)。
- ⚠️ 這一版尚未確認編譯結果，也尚未在 Play 模式實測。

### v3：彈孔 → 軟糖引力球（2026-10-13）
團隊決定把彈孔換成「發射軟糖」，降低認知負擔，並加入色素軟糖讓魚變色。
- 新增 `Jelly.cs`：生成時回彈彈出、原地漂浮、自發光 + 點光源呼吸脈動；每 0.25 秒掃描半徑 4 內的空閒魚，由近到遠補到最多 6 隻來圍食；每被咬一口按體積縮小、光變暗，吃完淡出消失。
- `FishController` 改為 `FreeSwimming` / `SeekingJelly` / `Eating`：魚在軟糖周圍隨機挑一個偏水平的站位，抵達後往軟糖啄食，約每 0.6 秒咬一口。移除彈孔逃脫流程（`StartEscape`、`SwimmingToHole`、`ExitingTank`）。
- 色素：每吃一口色素軟糖，魚的目標顏色往色素色靠近 25%，身體顏色平滑過渡。
- `InteractionManager`：左鍵普通、右鍵色素（另有 `Pigment Chance` 讓左鍵也可能隨機出色素糖）；0.8 內已有軟糖時改為堆疊（+半份口數），場上上限 5 顆；生成位置夾在可游範圍內。
- `FishPool` 只新增 `SwimBounds` 屬性；`TankBootstrap` 改建軟糖模板，並在 runtime 加 Bloom Volume、開啟相機後製。
- ✅ 用 Unity 內建 Roslyn 對 Unity / URP DLL 編譯 `FishTank/*.cs` 通過；尚未在 Unity Play 模式實測。

### v3.1：軟糖黏成一團 + 軟Q手感（2026-10-13）
- 軟糖改成由 7 塊小塊（測試用圓球＋膠囊）黏成一團，小塊來源是 `Jelly` 的 `Piece Variants`，之後直接換成 3D 物品 Prefab。
- 生成時小塊依序彈出；堆疊時在表面多黏 3 塊（上限 20），並把大小補回去。慢慢縮小的效果保留。
- 每塊各有一個彈簧：閒置時輕微呼吸，被咬時靠近嘴的小塊被壓扁再彈回，整團也跟著抖。
- 魚加上軟Q動作（只動模型節點 `Visual`，不影響移動）：游越快身體拉越長、加減速會晃、擺尾隨速度變化、轉彎傾斜、抵達與每咬一口都會壓扁回彈。
- 新增 `SoftSpring.cs`；外部 Roslyn 編譯通過，尚未 Play 實測。

### v3.2：多色小塊 + 吃慢一點 + 發射黏上（2026-10-13）
- 普通軟糖每塊從 6 色糖果調色盤輪流挑（相鄰不重複）；色素軟糖用同一色的不同深淺。自發光強度 2.5 → 1.2、小塊不透明度提高、散布半徑加大，避免被 Bloom 糊成一坨。
- 吃慢一點：軟糖口數 12 → 30（`Jelly.Bites`），魚每口間隔 0.6 → 1 秒（`FishController.Bite Interval`）。
- 左右鍵都改成「發射」：從鏡頭下方射出，沿飛行方向拉長、帶一點弧度，撞到玻璃內側沿法線壓扁、每塊亂晃後黏住不漂。射在既有軟糖附近則落地時黏到那顆上。
- 力道 `Launch Force`（0.2–3）同時影響飛行速度、飛行拉長與撞擊壓扁。
- 黏在玻璃上的軟糖，魚只會在缸內那一側圍食，不會穿出玻璃。
- 外部 Roslyn 編譯通過，尚未 Play 實測。

### v3.3：螢光小塊混在軟糖裡（2026-10-13）
原本整顆色素軟糖（右鍵）在一團多色軟糖中看不出來。改成：
- 只剩左鍵。每團 7 塊裡混 1–2 塊**螢光小塊**（桃紅 / 螢光綠 / 青 / 螢光橘），自發光 6、會閃、比普通塊稍大，放在外圈且朝缸內那一側，魚咬得到。
- 普通小塊自發光降到 0.35，讓螢光塊明顯跳出來。
- 堆疊時新黏上的 3 塊裡有 1 塊螢光。
- 魚每咬一口，判定嘴邊最近的是哪一塊；是螢光塊才會變色。所以只有圍在螢光塊旁邊的魚會變色，有因果感。
- 魚變色的同時加上同色自發光，吃越多越亮（上限 2.5）並輕微呼吸，一眼看得出哪些魚吃過。
- 移除右鍵、`Pigment Chance`、`Pigment Colors`。
- 外部 Roslyn 編譯通過，尚未 Play 實測。

### v3.4：打包用 Prefab（2026-10-13）
- 新增 `Assets/Editor/FishTankPackageBuilder.cs`（只在本專案用，不打包），選單 **Tools > FishTank**：
  1. **建立 Prefab 與示範場景**：把 `TankBootstrap` 生成的東西存成 `Assets/FishTank/` 底下的 Prefab、材質、Bloom Profile 與 `FishTankDemo` 場景。
  2. **匯出 unitypackage**：打包 `Assets/FishTank/` 與 `Assets/Scripts/FishTank/`，自動排除 `TankBootstrap.cs`。
- Prefab 的外殼和模型分開（魚：`Fish → Visual → Model`；軟糖小塊：`Piece Variants`；魚缸：`FishTankSetup/Tank`），之後換模型不用改腳本。換法寫在 [`FishTank/README.md`](Assets/Scripts/FishTank/README.md) 的「換成正式模型」。
- 點擊面改成獨立的隱形 `ClickSurface`（魚缸正面），魚缸外觀沒有 Collider。
- 外部 Roslyn 編譯通過，尚未在 Unity 裡執行選單。

### v4：夾垃圾原型，大小垃圾的手感差異（2026-10-13）
先用滑鼠做，Joy-Con 之後以滑鼠模式操作（游標 + 左鍵），程式不用改。夾垃圾和發射軟糖統一用左鍵：`InteractionManager` 發射前先問 `TrashGrabber.FindTrash()`，游標下有垃圾（或正在夾）就不發射。
- 新增 `TrashItem.cs`：Rigidbody 垃圾，在水裡有阻力和部分浮力，會慢慢沉。被夾時用「速度追目標點」移動，所以仍會被玻璃擋住。
  - **大型**：跟手（`Follow Gain` 12），拉到水面上 `Remove Height` 就算帶出魚缸：往上彈一下、縮小消失，觸發 `TrashItem.Removed` 事件。
  - **小型**：`Resistance` 0.75，只用 1/4 的力道跟手，還會用 Perlin 雜訊亂抖、亂轉。以下任一條件成立就**滑掉**，往下彈開，0.5 秒內不能再夾：
    1. 夾住超過 0.8–1.8 秒（隨機）
    2. 拖太快，夾子和垃圾距離超過 `Slip Distance`
    3. 離水面剩 `Surface Slip Margin` 以內（保證小型垃圾永遠出不了水）
- 新增 `TrashGrabber.cs`：
  - 判定用 `SphereCastAll`（半徑 `Grab Radius`），穿過玻璃挑最近的垃圾，小碎片也夾得到。拖曳平面是過夾取點、面向鏡頭的平面。
  - 震動走靜態事件 `TrashGrabber.Rumble(strength 0–1, seconds)`：夾大型垃圾時短震一下；夾小型垃圾時持續震動，越吃力越強；滑掉時強震；帶出魚缸時中震。
  - 沒有手把時用**鏡頭與夾子抖動**模擬震動（`Simulate Rumble`），夾子會從白變紅表示吃力程度。鏡頭抖動是加減偏移，不會干擾之後的 zoom 運鏡。
  - 輸入集中在 `GripDown()`、`GripHeld()`、`PointerRay()` 三個 virtual 方法。Joy-Con 用滑鼠模式時不用動；震動另外訂閱 `Rumble` 事件。
- `TankBootstrap`：拿掉魚缸頂蓋（垃圾要拉得出去）；生成 3 個大型、12 個小型垃圾；水面在缸頂往下 0.3。
- 尚未處理：魚會直接穿過垃圾。（Prefab 已在 v4.1 補上）
- ✅ 外部 Roslyn 編譯通過；尚未在 Unity Play 模式實測。

### v4.1：垃圾做成 Prefab，美術直接換模型（2026-10-13）
- 垃圾改成「外殼 + Model」：外殼放 Rigidbody、TrashItem、Box Collider，`Model` 只是模型。換模型的步驟寫在 [`FishTank/README.md`](Assets/Scripts/FishTank/README.md) 的「換垃圾模型」。
- 新增 `TrashSpawner.cs`：依 Entries（Prefab、數量、Scale Range、Random Tint）把垃圾撒在範圍底部。`TankBootstrap` 也改用它。
- `TrashGrabber` 新增 `Water Surface`（Transform），水面跟著物件走，整組搬家不用改數字。
- `Tools > FishTank > 1` 現在會多產生 `Trash_Bottle`、`Trash_Bag`、`Trash_Can`、`Trash_Fragment` 四個 Prefab 與材質；`FishTankSetup` 多了 TrashGrabber、TrashSpawner、`TrashWalls`（Ignore Raycast 層的隱形牆，只擋垃圾、不擋點擊）、`WaterSurface`，魚缸外觀拿掉頂蓋。
- ✅ 外部 Roslyn 編譯通過（runtime 與 Editor 工具）；尚未在 Unity 裡執行選單與 Play。

### v5：沙灘 ↔ 魚缸的物件架構（2026-10-13）
鏡頭 zoom 由另一位組員負責，這版只確保**同一批物件在沙灘和魚缸都能用**，跨世界時自動換算大小。
- 決定：**魚缸世界維持原尺寸放在遠處，不縮小**。沙灘上只擺小魚缸外殼，兩邊用 `ScaleBridge` 換算（倍數 = 魚缸範圍寬 ÷ 小魚缸範圍寬）。原因：魚、軟糖、垃圾手感都是用世界單位調的，縮小會全部跑掉，也會出現極小尺度的物理問題。
- 新增 `WaterVolume`：水改成範圍，拿掉 `TrashItem.WaterSurfaceY` 與 `TrashGrabber` 的水面欄位。垃圾在水裡才有阻力、浮力、小型滑脫；在沙灘上大小都正常夾。
- 新增 `ScaleBridge`：沒被夾著的物件進到沙灘小魚缸範圍 → `Teleport` 到魚缸世界對應位置、放大；`TrashItem.Removed` → 縮小放到 ExitPoint。
- `TrashItem` 新增 `Teleport(位置, 縮放倍數, 速度)`；`Removed` 有人接手時不播消失動畫，沒有 ScaleBridge 時照舊消失。
- `InteractionManager` 新增 `Click Surface`：只有點到魚缸正面才發射軟糖，沙灘上點東西不會射。
- `TrashGrabber`：夾著的物件被搬走時自動鬆手；震動偏移記住是哪台相機，切換相機不會殘留。
- `TankBootstrap` 新增 `Build Beach`：在 X = 40 生成沙地、縮小 3 倍的小魚缸、ScaleBridge、3 個沙灘垃圾、沙灘相機，Tab 切換。
- 打包工具：`WaterSurface` 改成 `Water`（WaterVolume），InteractionManager 指定 ClickSurface。
- 相機不依賴 Tag（正式相機有自己的 Tag），且鏡頭組會同時開好幾台相機：新增 `ViewCamera.Current`，順序是 `ViewCamera.Override` → 掛了 `InteractionCamera` 的相機（Priority、Depth）→ 自動挑（畫到螢幕、非 Overlay、Depth 最高）。`TrashGrabber`、`InteractionManager` 改用它；測試場景和示範場景的相機都掛了 InteractionCamera。
- 鏡頭組的做法：一個相機群組（多台相機、以主鏡頭為準）在沙灘和魚缸之間移動。只在主鏡頭掛 InteractionCamera。群組進出魚缸時用 `ScaleBridge.BeachToTank()` / `TankToBeach()` 瞬移位置，移動速度與 Clip 乘除 `Factor`；群組自己控制鏡頭位置時，把 TrashGrabber 的 Camera Shake 設 0，改訂閱 `Rumble`。說明在 FishTank/README「給鏡頭組」。
- 把現有場景改成這個架構的步驟：[`FishTank/README.md` 的「沙灘 ↔ 魚缸架構」](Assets/Scripts/FishTank/README.md)。
- ✅ 外部 Roslyn 編譯通過（runtime 與 Editor 工具）；尚未 Play 實測。

### v5.1：簡易運鏡（2026-10-13）
- 新增 `TransferCameraMove`：掛在相機（或相機群組最外層）上，取消勾選就不運鏡。物件掉進小魚缸 → 推向玻璃 → 瞬移到魚缸世界同一構圖 → 移到 Tank View；大型垃圾帶出 → 反向回沙灘。Near / Far Clip 跟著乘除倍數。也可關掉事件觸發，自己呼叫 `GoToTank()` / `GoToBeach()`。
- `ScaleBridge` 新增事件 `MovedToTank`、`MovedToBeach` 與 `BeachTankCenter`。
- 修正：原本進魚缸是兩段（先推到玻璃前、瞬移、再移到 Tank View），中間會停在一個不是使用者指定的點。改成一段直達：推進終點 = Tank View 換算回沙灘的位置，瞬移後剛好就是 Tank View；出來時先瞬移回對應位置再拉回沙灘視角。`Push Time` / `Settle Time` 合併成 `Move Time`。
- `TankBootstrap`：沙灘測試改成同一台相機，從沙灘開始，用 TransferCameraMove 運鏡、Tab 手動切換（不再用 DebugCameraSwitch）。
- 掛到自己場景的步驟：[`FishTank/README.md` 的「簡易運鏡」](Assets/Scripts/FishTank/README.md)。
- ✅ 外部 Roslyn 編譯通過；尚未 Play 實測。

### v5.2：傳送門窗戶（2026-10-13）
- 問題：沙灘上的小魚缸是空殼，運鏡瞬移那一刻魚缸世界的東西會突然出現。比較三種做法（傳送門窗戶／氣泡閃白遮住／縮小複製品），選傳送門：從外面看是小魚缸、推進去是一整片海，尺度反差在這一刻最強，而且完全無縫。
- 新增 `PortalWindow.cs`：在魚缸世界自動建一台鏡像相機，每次畫面前（`beginCameraRendering`）對齊 `BeachToTank(玩家相機位置)`、同角度同 FOV、Clip 乘倍數，畫到 ARGBHalf RenderTexture（保留 HDR，軟糖透過窗戶仍有 Bloom）。玩家相機在魚缸世界時自動停畫。
- 新增 `PortalWindow.shader`（URP）：依螢幕座標取鏡像畫面，用鏡像的 alpha 混合後面的場景（`_CameraOpaqueTexture`，PortalWindow 會把玩家相機的 Opaque Texture 打開），所以空的水會透出後面的沙灘。`Blend Off` 直接覆寫，玻璃重疊不疊色。
- `ScaleBridge` 新增 `TankWorldCenter`。`TankBootstrap` 的沙灘小魚缸也掛上 PortalWindow。
- 使用者場景 `1013試做zoom in`：FishTankSetup 與 Trash_Fragment 移到 X + 1000、TankView 跟著移，小魚缸 `Tank` 掛 PortalWindow；沙灘鏡頭保留使用者擺的位置。
- ✅ C# 外部 Roslyn 編譯通過；**shader 只能在 Unity 裡編譯，尚未確認**；尚未 Play 實測。

### v5.3：搬家清單（2026-10-13）
- `FishTank/README.md` 最前面新增「搬到別的專案」：打包步驟、套件內容、新專案需求、配置 A（只有魚缸）／B（沙灘 + 魚缸）、搬完檢查清單。
- 修正：傳送門 shader 只靠 `Shader.Find` 取得，沒有素材引用，Build 時會被剔除。打包工具多產生 `Assets/FishTank/Materials/PortalWindow.mat`，正式場景的 PortalWindow 要拖這個材質。

### v5.4：自動化，沙灘 + 魚缸改成 4 步（2026-10-13）
使用者希望「盡量少步驟、少搬東西、自動化」。原本配置 B 要 12 步、手動拼小魚缸，容易擺錯（ExitPoint 放進小魚缸、移動後彼此對不上、窗戶材質忘了拖）。
- 新增 **`BeachTankSetup` Prefab**：小魚缸外觀 + TrashWalls + BeachTankArea + ExitPoint + ScaleBridge + PortalWindow（已帶 `PortalWindow.mat`），Scale 0.33，中心點在缸底，拖到沙灘就能用。
- `ScaleBridge` 自動化：Tank World 留空自動找場景裡 FishPool 的 SwimArea（`FishPool.SwimAreaCollider`）；`Match Tank Size` 自動對齊範圍，倍數 = 1 / Scale；`Scale Beach Items` 開場自動把沙灘上的垃圾縮成沙灘尺寸；魚缸世界太近時警告；提供 `TankView`。`Initialize()`（測試場景手動接線）會關掉兩個自動選項。
- `FishTankSetup` 內建 `TankView`；`TransferCameraMove` 沒指定 Tank View 時自動用它。`PortalWindow` 優先用父物件上的 ScaleBridge。
- 打包工具多產生 `BeachDemo` 場景（魚缸世界在 X = 1000、沙灘、BeachTankSetup、3 個沙灘垃圾、相機掛 InteractionCamera + TransferCameraMove）。
- README 配置 B 改成 4 步，原本的手動步驟改成「手動拼裝（參考）」。
- ✅ 外部 Roslyn 編譯通過（runtime 與 Editor）；尚未在 Unity 執行 Tools 與 Play。

### v5.5：Unity 6 相容（2026-10-13）
- 在 Unity 6 專案匯入套件時跳出 Script Updating Consent（`TrashItem.cs`）：Unity 6 把 Rigidbody 的 `velocity` / `drag` / `angularDrag` 改名成 `linearVelocity` / `linearDamping` / `angularDamping`。
- `TrashItem` 改用 `SetVelocity()` / `SetDamping()`，以 `#if UNITY_6000_0_OR_NEWER` 分版本，2023.1 和 Unity 6 都能直接編譯。
- ✅ 2023.1.13f1 + URP 15、6000.0.60f1 + URP 17 都外部編譯通過（Unity 6 那次把過時 API 警告 CS0618 / CS0619 / CS0612 當錯誤，沒有其他會觸發更新的寫法）。shader 尚未在 Unity 6 確認。
- `SETUP.md` 補上 Unity 6 說明與這個視窗的處理方式。

### v5.6：自己的物品也能換場、鏡頭從任何位置出發（2026-10-13）
- 新增 `ScaleTraveler`：任何物品掛上就套用換場規則，不用改原本的腳本。沙灘上掉進小魚缸 → 放大搬到魚缸世界；在魚缸世界被拿到頂端上方放開 → 縮小搬到 ExitPoint。被拿著時不搬，判斷方式：`Held` 旗標、Rigidbody 是 Kinematic、或被設成別的物件的子物件（相容常見的抓取寫法）。物品放在沙灘多大就多大，不自動縮。
- `ScaleBridge`：同時處理 TrashItem 與 ScaleTraveler（同時掛兩個的交給 TrashItem）；新增 `Traveler Exit Height`；事件 `MovedToTank` / `MovedToBeach` 的參數改成 `Transform`。補回套件 README 的 ScaleBridge 參數表。
- `TransferCameraMove`：鏡頭不必一開始就在小魚缸前面。進魚缸走二次貝茲弧線：從當下位置出發、前半段先轉向、經過小魚缸正前方 `Approach Distance` 的點、直直推進；回沙灘時往後拉出，回到**進魚缸前的位置**（或指定的 Beach View）。拿掉「開場位置 = 沙灘視角」的假設。
- `SETUP.md` 新增「讓你自己的物品也套用換場規則」與「鏡頭不在小魚缸前面時」。
- ✅ 2023.1 + URP 15、Unity 6 + URP 17 外部編譯通過；尚未 Play 實測。

### v5.7：ScaleTraveler 改成零設定（2026-10-13）
- 使用者回饋：新專案的物品有自己的物理規則和父子結構，希望「只拉一個新腳本上去，說這個可以拉到魚缸裡」就好。上一版用「Rigidbody 是 Kinematic」或「父物件改變」判斷被拿著，會被這些物品原本的設定誤判。
- `ScaleTraveler` 拿掉被拿著的猜測，規則改成跟 TrashItem 一樣：沙灘上進到小魚缸 → 搬過去；在魚缸世界高於頂端 → 搬回沙灘（不論有沒有被拿著）。只動自己的位置和大小，Rigidbody 速度依倍數換算（Kinematic 不碰）。換世界時若父物件和開場不同（被夾子抓著），放回原本的父物件。`Held` 只留作可選的暫停開關。
- 在哪個世界改成每次即時判斷（`ScaleBridge.IsInTankWorld()`，PortalWindow 也改用它），不記狀態，中途生成的物品也正確。
- ✅ 2023.1 / Unity 6 外部編譯通過；尚未 Play 實測。

### v5.8：導入手冊分三區、窗戶只換玻璃（2026-10-13）
- `SETUP.md` 改成「FishTank 導入手冊」，分三區：**一、導入與引用（給整合的程式）**：打包、匯入、更新、4 步建場景、ScaleTraveler、相機、程式介面表、檢查清單、常見問題；**二、替換素材（給美術）**：Prefab Variant 原則、魚／軟糖／垃圾／魚缸外觀／窗戶材質／Bloom 的換法、不要動的物件；**三、架構**：兩個世界示意圖、換場流程、元件分工、設計原則。
- 修正：`PortalWindow` 原本把 `BeachTankSetup` 底下**所有** Renderer 換成窗戶，美術加的框、裝飾也會變成窗戶。新增 `Window Root`，`BeachTankSetup` 指向 `Body/Tank`：玻璃和缸底變窗戶，`Body` 底下其他東西保持原材質。
- 修正打包工具：建 `BeachDemo` 時出現 `MissingReferenceException: TrashItem has been destroyed`。前面存檔後素材資料庫重新整理，先前拿到的 Prefab 參照失效。改成存檔後、建示範場景時一律用路徑 `AssetDatabase.LoadAssetAtPath` 重新載入（新增 `Load<T>()`）。
- ✅ 2023.1 / Unity 6 外部編譯通過。

### v5.9：沙灘垃圾所見即所得（2026-10-13）
- 問題：沙灘上的 `Trash_*` 編輯時很大，按 Play 才被 `Scale Beach Items` 縮成 1/3，擺位置時對不準。
- 打包工具新增 `Trash_Bottle_Beach` / `Trash_Bag_Beach` / `Trash_Can_Beach`：原 Prefab 的 Variant，Scale = 0.33（= BeachTankSetup 的 Scale）。換原 Prefab 的模型會跟著變。`BeachDemo` 改用它們。
- `ScaleBridge.Scale Beach Items` 預設改為關閉，規則和 ScaleTraveler 統一：放在沙灘多大就多大，進魚缸才放大。
- ✅ 外部編譯通過。

### v5.10：進魚缸後的大小可自訂（2026-10-13）
- `ScaleTraveler`、`TrashItem` 新增 **Custom Tank Size**（勾選）與 **Tank Size**（0.05–3，預設 0.5；沒勾時等於 1，照比例）：勾起來後進魚缸的倍數 = 比例 × Tank Size；帶出時除以同一個值，回到沙灘一定是原本大小。
- `ScaleBridge.Match Tank Size` 從 Inspector 隱藏：它是讓倍數剛好 = 1 / Scale 的內部設定，關掉會讓換場位置算錯，使用者不需要碰。
- `SETUP.md` 參數表與常見問題同步更新。
- ✅ 2023.1 / Unity 6 外部編譯通過。

- `TrashGrabber`：夾住那一下的震動（Grab Large / Small Rumble）預設改為 0，不震；強度 0 的震動不送出事件、不晃鏡頭。掙扎、滑掉、帶出的震動不變。

### v6：同時支援 Built-in 與 URP（2026-10-16）
- 新增 `FishTank.asmdef`：裝了 URP 時自動定義 `FISHTANK_URP`（versionDefines），URP 專用程式只在這時編譯；沒裝 URP 的專案也能編譯。
- 新增 `FishTankRender`：判斷目前管線（`GraphicsSettings.currentRenderPipeline`），建立 URP Lit／Standard 材質（含透明、自發光）。TankBootstrap、打包工具改用它。
- `PortalWindow`：同時訂閱 `RenderPipelineManager.beginCameraRendering`（URP）與 `Camera.onPreCull`（Built-in）對齊鏡像相機；URP 相機設定與 Opaque Texture 包在 `FISHTANK_URP` 且非 Built-in 時才做。`ViewCamera` 的 Overlay 判斷同樣處理。
- `PortalWindow.shader` 改成兩套 SubShader，都只用 UnityCG：URP 讀 `_CameraOpaqueTexture`，Built-in 用具名 GrabPass。不 include URP 檔案，沒裝 URP 也不會編譯失敗。
- 新增套件內的 Editor 工具 `FishTankMaterialConverter`（`FishTank.Editor.asmdef`）：開啟專案或匯入後自動把 `Assets/FishTank` 的材質轉成目前管線的 Lit（讀存檔中的顏色、貼圖、透明、自發光，shader 不存在時也能轉）；另有選單 **Tools > FishTank > 材質轉成目前的渲染管線**。
- Built-in 沒有光暈（要另外裝 Post Processing Stack v2）；示範場景的 URP 相機元件、Volume 在 Built-in 會顯示 Missing Script，不影響功能。
- ✅ 外部編譯通過：2023.1 + URP 15、Unity 6 + URP 17、2023.1 Built-in（無 URP / Core RP）、Unity 6 Built-in、套件 Editor 工具、打包工具。**shader 與材質轉換只能在 Unity 裡確認，尚未在 Built-in 專案實測。**

### v6.1：同時支援舊版 Input Manager 與新版 Input System（2026-10-16）
- 在 Unity 6 Built-in 測試專案（Active Input Handling = 新版 Input System only）出現 `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`。
- 新增 `FishTankInput`：`ENABLE_LEGACY_INPUT_MANAGER` 時用 `Input`；只開新版時改用 Input System 的 `Mouse` / `Keyboard`（KeyCode 對應 Key，數字鍵、Enter 特別處理）。`TrashGrabber`、`InteractionManager`、`TransferCameraMove`、`DebugCameraSwitch` 全部改用它；asmdef 加 `Unity.InputSystem` 參照。
- 不用再改 Active Input Handling，SETUP 拿掉這一步。
- ✅ 外部編譯通過：Unity 6 Built-in + 新版 Input only（測試專案的實際設定）、Unity 6 URP + Both、Unity 6 URP + 新版 only、2023 URP + 舊版、2023 Built-in + 舊版、套件 Editor 工具。

### v6.2：示範場景不再出現 Missing Script（2026-10-16）
- Built-in 專案打開示範場景出現 3 條 `The referenced script (Unknown) on this Behaviour is missing!`：示範場景是在 URP 建的，相機帶 `UniversalAdditionalCameraData`、方向光帶 `UniversalAdditionalLightData`、光暈用 `Volume`。
- 打包工具存示範場景前呼叫 `StripPipelineComponents()`，拿掉 `UnityEngine.Rendering.*` 命名空間的元件（URP 執行時會自動補回相機與燈光的 Additional Data）。
- 新增 `FishTankPostFX`：開場時才用程式建立 Bloom Volume、打開操作相機的 Post Processing（只在 URP；Built-in 不做事）。示範場景改用它，SETUP 的「要光暈」步驟也改成掛這個元件。
- ✅ 外部編譯通過（Unity 6 / 2023、Built-in / URP、新舊 Input 共 5 種組合與打包工具）。

#### 手感調整入口
| 想要 | 調哪裡 |
|---|---|
| 小垃圾更難夾／更容易滑 | `TrashItem` 的 `Resistance` ↑、`Slip Time` ↓、`Slip Distance` ↓ |
| 小垃圾掙扎更明顯 | `Struggle Jitter`、`Struggle Frequency` ↑ |
| 大垃圾更重、更慢 | `Follow Gain` ↓、`Max Follow Speed` ↓ |
| 震動太吵／太弱 | `TrashGrabber` 的各項 Rumble 強度、`Camera Shake` |
| 小碎片點不到 | `Grab Radius` ↑ |

---

## 狀態與待辦

| 項目 | 狀態 |
|---|---|
| v1 編譯 | ✅ 通過 |
| v2 編譯 | ✅ 隨 v3 一起編譯通過 |
| v3 編譯 | ✅ 外部 Roslyn 編譯通過；切回 Unity 再看一次 Console |
| 本專案 Play 測試 | ⬜ 待測：左鍵軟糖一團彈出、魚圍食、咬的時候軟Q晃動、軟糖縮小消失、連點黏上新塊、螢光塊明顯、咬到螢光塊的魚變色發光、魚游動的拉長擺尾、Bloom 有作用 |
| `SampleScene` 出現 `FishTankRoot` | ⬜ 待確認（需重新開啟場景，選 Don't Save） |
| 建立 Prefab（Tools > FishTank > 1） | ⬜ 工具已寫好，待在 Unity 執行 |
| 匯出 `.unitypackage`（Tools > FishTank > 2） | ⬜ 未做 |
| 新專案依 README 從頭配置一次 | ⬜ 未做 |
| v4 夾垃圾 Play 測試 | ⬜ 待測：左鍵夾大型垃圾跟手、拉出水面消失；夾小型垃圾會抖、夾子變紅、鏡頭震、一定滑掉；點在垃圾上不會射出軟糖、點在其他地方照常發射 |
| v5 沙灘 ↔ 魚缸 Play 測試 | ⬜ 待測：丟進小魚缸後鏡頭跟進去、帶出後鏡頭拉回沙灘、Tab 手動運鏡、沙灘垃圾丟進小魚缸後在魚缸變大、大型垃圾拉出後在沙灘變小、沙灘點擊不射軟糖 |
| 自己的場景改成兩個世界的架構 | ⬜ 照 FishTank/README「沙灘 ↔ 魚缸架構」 |
| 重新執行 Tools > FishTank > 1 產生垃圾 Prefab | ⬜ 待在 Unity 執行（會覆蓋既有 Prefab） |
| 接 Joy-Con | ⬜ 等打包檔：以滑鼠模式操作即可；震動需訂閱 `TrashGrabber.Rumble` |

### 之後可以考慮
- 軟糖發射軌跡（從鏡頭飛進缸內）與落點濺開的粒子、泡泡。
- 軟糖改用 Shader Graph 做果凍感（菲涅耳、內部散射、表面抖動）。
- 吃飽的魚暫時不再被吸引；螢光累積到某個程度觸發其他反應；螢光隨時間慢慢褪色。
- Joy-Con 抓垃圾：手感原型已完成（v4），Joy-Con 以滑鼠模式操作，等打包檔進來後接震動。
- 改用新版 Input System（目前依賴舊版 `Input.GetMouseButtonDown`）。

---

## 夾垃圾的延伸建議（給團隊討論）

這部分的核心是**尺度反差做在操作上**：大的看起來嚴重，其實清得掉；小的不起眼，卻永遠清不乾淨。以下建議都是為了讓這一點更明顯。

### 和整體設定對齊
- **「大／小」以魚缸尺度來定義。** 魚缸裡對鯨魚來說很大的東西，其實就是人丟的完整瓶罐，可以帶出去；碎到在魚缸裡都很小的，就是微塑膠，帶不出去。和美術的視覺邏輯一致，評圖時比較好講。
- **大型垃圾帶出魚缸要接到海灘。** `TrashItem.Removed` 事件已經留好。建議轉場時在海灘鏡頭裡看到**同一個物件**變成手指捏著的小東西（match cut），這就是「原來只是這樣」的揭露時刻。物件共享的話，用比例係數換算大小即可。
- **魚缸內部不要真的縮小來做。** 極小尺度下 Rigidbody 會穿透、抖動、掉太快，會直接毀掉抓取手感。建議魚缸內部用正常比例做在另一個位置，靠鏡頭與轉場製造尺度感。

### 讓小垃圾的「拿不出來」更有意義
- **越夾越碎**：小型垃圾被夾住滑掉時，有機率裂成 2–3 塊更小的碎片。玩家越努力，碎片反而越多，直接講出微塑膠的問題。程式上只是 `Slip()` 時生成幾個縮小的副本。
- **滑掉時鏡頭推近一下**：讓那顆小碎片在畫面裡突然變大（需要和負責鏡頭的組員對一下）。
- **大的清完、小的還在**：所有大型垃圾都帶出去後，畫面停在滿地的彩色碎片上，可以當作這段體驗的收尾。
- **和軟糖連動**：小魚把彩色碎片誤認成軟糖去啃，吃了也會變色。和「人工色素軟糖」的設定是同一套因果，玩家會自己意識到兩者的相似。

### Joy-Con 震動設計
Joy-Con 是 HD 震動，可以分高低頻，比單純強弱更能表達「重」和「滑」：

| 事件 | 建議 | 感覺 |
|---|---|---|
| 夾住大型垃圾 | 低頻、短、一下 | 「咚」，抓穩了、有重量 |
| 拖動大型垃圾 | 低頻、很弱、持續 | 拖著重物 |
| 夾住小型垃圾 | 高頻、弱 | 滑滑的、沒抓實 |
| 小型垃圾掙扎 | 高頻、隨吃力程度變強、不規則 | 一直在溜 |
| 小型垃圾滑掉 | 高頻強震後突然停 | 「啊，掉了」 |
| 帶出魚缸 | 低頻中震 + 短暫停頓 | 完成感 |

目前 `Rumble` 事件只送強度與秒數。接 Joy-Con 時，如果套件支援高低頻，建議把事件改成 `(lowFreqAmp, highFreqAmp, seconds)`，再照上表分配。

### 操作手感
- 目前拖曳平面固定在夾取點的深度，沒辦法前後移動。Joy-Con 用滑鼠模式也一樣；如果之後需要，可以加滾輪（或 Joy-Con 搖桿對應的滾輪）控制前後。
- 夾取判定的半徑（`Grab Radius`）可以讓小碎片「其實很好夾到」，失敗只發生在拉出來的過程。這樣玩家知道自己有夾到，挫折感會指向「它太小、太滑」，而不是「我操作不好」。
