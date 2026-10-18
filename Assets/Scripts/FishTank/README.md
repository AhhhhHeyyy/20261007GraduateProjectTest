# 魚缸軟糖餵食：配置步驟

點擊魚缸 → 從鏡頭用力射出一團多色軟糖，黏在玻璃內側 → 附近的小魚游過來圍著一口一口吃 → 軟糖縮小消失。
每團裡混一兩塊會閃的螢光小塊，魚咬到螢光塊會變成那個顏色並帶螢光。
左鍵按住垃圾 → 夾住拖曳：大型垃圾拉得出水面，小型垃圾一直滑掉（點在垃圾以外才會發射軟糖）。

> **只想知道怎麼打包、怎麼在新場景擺？看 [SETUP.md](SETUP.md)。** 這份 README 是完整參考（參數、換模型、原理）。

## 搬到別的專案

### 打包（在這個測試專案做）

1. **Tools > FishTank > 1. 建立 Prefab 與示範場景**
   ⚠️ 會重建 `Assets/FishTank/` 底下的 Prefab、材質和 `FishTankDemo` 場景。換過模型的 Prefab 先備份，或改用 Prefab Variant。
2. **Tools > FishTank > 2. 匯出 unitypackage**，存成 `FishTank.unitypackage`。

套件內容：

| 路徑 | 內容 |
|---|---|
| `Assets/Scripts/FishTank/` | 全部腳本、`PortalWindow.shader`、這份 README（**自動排除 `TankBootstrap.cs`**） |
| `Assets/FishTank/Prefabs/` | `FishTankSetup`（魚缸世界整組）、**`BeachTankSetup`（沙灘小魚缸整組）**、`Fish`、`Jelly`、`Piece_*`、`Trash_*` |
| `Assets/FishTank/Materials/` | 佔位材質、**`PortalWindow.mat`**（Build 時傳送門 shader 靠它保留） |
| `Assets/FishTank/FishTankBloom.asset` | Bloom 設定 |
| `Assets/FishTank/Scenes/` | `FishTankDemo`（只有魚缸）、**`BeachDemo`（沙灘 + 魚缸 + 運鏡）** |

**不會帶走**：`TankBootstrap.cs`（測試用自動建場景）、`Assets/Editor/FishTankPackageBuilder.cs`（打包工具）、`SampleScene`、你自己的場景。想把沙灘的示範場景一起帶走，先在 Unity 的 Project 視窗把它拖進 `Assets/FishTank/Scenes/`，再匯出。

### 新專案需要

- Unity 2023.1 + **URP 15**（其他版本沒測過；URP 版本差太多時 shader 可能要調）。
- **Edit > Project Settings > Player > Active Input Handling** 設成 **Both** 或 **Input Manager (Old)**。
- 不需要新增 Tag 或 Layer（隱形牆用內建的 Ignore Raycast 層）。

### 配置 A：只有魚缸

1. **Assets > Import Package > Custom Package**，匯入 `FishTank.unitypackage`。
2. 把 `FishTankSetup` 拖進場景。
3. 相機從魚缸 -Z 那一面往 +Z 看；相機上 Add Component → **InteractionCamera**；Rendering 勾 **Post Processing**。
4. 場景加一個 **Global Volume**，Profile 拖 `FishTankBloom`。
5. Play：點玻璃射軟糖、按住垃圾夾。

### 配置 C：沙灘 + 單一魚缸（最簡單，3 步）

只有一個魚缸，直接縮小擺在沙灘上，不用 `BeachTankSetup`、`ScaleBridge`，也沒有遠處的魚缸世界。示範場景：`BeachDemo_SingleTank`。

1. **`FishTankSetup`** 拖到沙灘上，**Scale 設 0.33**（三軸一樣），Position Y 設成缸底貼地（0.33 時約 **1.01**）。
2. 展開 `FishTankSetup/Water`，**WaterVolume** 勾 **Drop Outside**（大型垃圾拉出水面後丟到魚缸 +X 側外面，不會消失）。
3. 主鏡頭 Add Component → **InteractionCamera**；要自動運鏡再加 **TransferCameraMove**（場景沒有 ScaleBridge 時自動改用單一魚缸模式：垃圾掉進水裡就推到 `FishTankSetup/TankView`，帶出時拉回來）。

沙灘垃圾用 **`Trash_*_Beach`**（已是 0.33 的大小），直接丟進魚缸。

**縮放怎麼處理**：魚、軟糖、垃圾、夾子用公尺算的參數（速度、距離、浮沉、發射力道、光照範圍……）都會自動乘上魚缸的 Scale，重力在水裡也一起換算，所以 Scale 0.33 的魚缸手感和原尺寸一樣，Inspector 的數值不用改。Scale = 1 時和以前完全相同。

### 配置 B：沙灘 + 魚缸（4 步）

1. **`FishTankSetup`** 拖進場景，Position X 設 **1000**（離沙灘夠遠就好）。
2. **`BeachTankSetup`** 拖到沙灘上（中心點在缸底，直接放地面）。
3. 主鏡頭 Add Component → **InteractionCamera**；要自動運鏡再加 **TransferCameraMove**。
4. **`Trash_*`** 直接拖到沙灘上，**不用改 Scale**（開場自動縮成沙灘尺寸）。

再加上配置 A 的 Bloom（Global Volume + `FishTankBloom`）、沙灘地面要有 Collider。可以直接打開 `Assets/FishTank/Scenes/BeachDemo` 對照。

**自動處理的事**（不用手動做）：
- 小魚缸自動找到魚缸世界（場景裡的 `FishTankSetup`），內部範圍自動對齊。
- 倍數 = 1 / `BeachTankSetup` 的 Scale。預設 0.33 → 魚缸世界大 3 倍；想換倍數只改這個 Scale。
- 帶出口 `ExitPoint`、傳送門窗戶、窗戶材質都在 Prefab 裡，跟著小魚缸移動。
- 運鏡進魚缸後的停留視角預設用 `FishTankSetup/TankView`，想換視角就移動它。
- 魚缸世界離沙灘太近（< 50）時 Console 會提醒。

**可選**：
- 沙灘的海：空物件掛 **WaterVolume**，範圍蓋住海，**Remove On Exit 取消勾選**。
- 不想自動縮沙灘垃圾：ScaleBridge 的 **Scale Beach Items** 取消勾選，自己設 Scale。

### 搬完的檢查

- [ ] Console 沒有紅字（特別是 `FishTank/PortalWindow` shader）
- [ ] 從沙灘看小魚缸，裡面看得到魚在游
- [ ] 沙灘垃圾丟進小魚缸 → 在魚缸世界變大出現（有運鏡的話鏡頭跟進去）
- [ ] 魚缸裡大型垃圾拉出水面 → 縮小拋到 ExitPoint
- [ ] 魚缸裡的彩色碎片拉不出來
- [ ] 點魚缸玻璃射軟糖、點沙灘不會射
- [ ] **Build 一次**確認傳送門在正式版還在（`BeachTankSetup` 已經帶著 `PortalWindow.mat`）

---

## 需要的東西

| 項目 | 說明 |
|---|---|
| `FishController.cs` | 單隻魚：自由遊動 / 游向軟糖 / 吃軟糖 / 螢光變色 |
| `FishPool.cs` | 物件池：生成、回收、重生 |
| `Jelly.cs` | 軟糖：多塊黏成一團、發光、吸引魚、被咬縮小與軟Q晃動、堆疊 |
| `SoftSpring.cs` | 彈簧小工具（魚與軟糖共用） |
| `InteractionManager.cs` | 點擊、發射或堆疊軟糖（游標下有垃圾時讓給夾子） |
| `TrashItem.cs` | 單件垃圾：水中阻力、被夾時跟隨、小型垃圾滑脫、大型垃圾帶出魚缸 |
| `TrashGrabber.cs` | 夾子：左鍵夾取、拖曳、震動事件 |
| `TrashSpawner.cs` | 開場把垃圾撒在魚缸底部 |
| `WaterVolume.cs` | 水的範圍與水面；垃圾在裡面才有水的手感 |
| `ScaleBridge.cs` | 和沙灘同場景時用：沙灘 ↔ 魚缸的搬移與縮放（見「沙灘 ↔ 魚缸架構」） |
| `DebugCameraSwitch.cs` | 測試用：Tab 在幾台相機之間硬切 |
| `TransferCameraMove.cs` | 可選：物件進出魚缸時的簡易運鏡（從任何位置轉向繞到小魚缸前 → 推進 → 瞬移；回來時回到出發位置），任何相機都能掛 |
| `ScaleTraveler.cs` | 掛上就代表「可以放進魚缸」：進小魚缸放大搬過去、在魚缸頂端上方縮小搬回沙灘。零設定，不改物品原本的物理和父子結構 |
| `PortalWindow.cs` + `PortalWindow.shader` | 可選：小魚缸的玻璃變成看進魚缸世界的窗戶，讓運鏡瞬移無縫 |
| `ViewCamera.cs` | 決定用哪台相機算點擊（不依賴 Tag），夾子和軟糖共用 |
| `InteractionCamera.cs` | 掛在玩家操作用的相機上（相機群組的主鏡頭） |
| 魚 Prefab | 你的魚模型 |
| 軟糖 Prefab | 空物件 + Jelly +（建議）Point Light |
| 軟糖小塊 Prefab | 一到多個小模型（軟糖塊、之後的 3D 物品），會被黏成一團 |
| 點擊面 | 一個隱形的 Box Collider |

`TankBootstrap.cs` 只是測試專案自動建場景用的，**不需要帶走**。

---

## 快速開始（用套件內附的 Prefab）

套件已附上用佔位模型做好的 Prefab，匯入後可以直接測試：

| 路徑 | 內容 |
|---|---|
| `Assets/FishTank/Prefabs/Fish` | 魚（FishController → `Visual` → `Model`） |
| `Assets/FishTank/Prefabs/Jelly` | 軟糖（Jelly + Point Light），Piece Variants 指向下面兩個 |
| `Assets/FishTank/Prefabs/Piece_Sphere`、`Piece_Capsule` | 軟糖小塊 |
| `Assets/FishTank/Prefabs/Trash_Bottle`、`Trash_Bag`、`Trash_Can` | 大型垃圾（TrashItem → `Model`） |
| `Assets/FishTank/Prefabs/Trash_Fragment` | 小型垃圾（塑膠碎片），由 TrashSpawner 隨機縮小、上色 |
| `Assets/FishTank/Prefabs/FishTankSetup` | 魚缸外觀 + 點擊面 + 可游範圍 + 擋垃圾的隱形牆 + 水（WaterVolume）+ FishPool + InteractionManager + TrashGrabber + TrashSpawner，欄位都已拖好 |
| `Assets/FishTank/FishTankBloom` | Bloom 的 Volume Profile |
| `Assets/FishTank/Scenes/FishTankDemo` | 示範場景 |

1. 設定 **Active Input Handling**（見下方步驟 0 的第 2 點）。
2. 打開 `FishTankDemo` 按 Play；或把 `FishTankSetup` 拖進自己的場景，再照步驟 5 設定相機與 Bloom。

> `FishTankSetup` 的點擊面在魚缸 -Z 那一面，相機要從 -Z 往 +Z 看。點擊射線預設打所有 Layer；場景裡如果有其他 Collider 擋在前面，照步驟 3 把 `ClickSurface` 放到獨立 Layer，再設定 Glass Mask。

### 換成正式模型

腳本只認外殼上的元件，換模型不用改程式。想保留佔位版的話，先在 Prefab 上按右鍵 → **Create > Prefab Variant**，在 Variant 裡換。

| 要換的 | 做法 |
|---|---|
| 魚 | 打開 `Fish`，刪掉 `Visual/Model`，把新模型放到 `Visual` 底下：魚頭朝 +Z、移除 Collider。魚比較大時調大 `Mouth Offset`。有貼圖的魚把 `FishTankSetup` 上 FishPool 的 **Randomize Color** 關掉 |
| 軟糖小塊 | 把新物品存成 Prefab（URP Lit 勾 Emission、不要 Collider），拖進 `Jelly` 的 **Piece Variants**；依模型大小調 **Piece Radius**；有自己貼圖、不想被染色時關掉 **Tint Pieces** |
| 垃圾 | 見下方「換垃圾模型」 |
| 魚缸 | 刪掉 `FishTankSetup/Tank`，換成你的魚缸模型（**不要有 Collider**，或放到 Glass Mask 沒勾的 Layer）。再調 `ClickSurface`（正面）、`SwimArea`（缸內）、`TrashWalls`（擋垃圾的牆）、`Water`（水的範圍）的大小與位置 |

### 換垃圾模型

每個垃圾 Prefab 都是「外殼 + Model」：

```
Trash_Bottle      ← Rigidbody + TrashItem + Box Collider（程式和碰撞都在這層，不要動元件）
└─ Model          ← 佔位模型，換掉這個
```

1. 打開 Prefab（例如 `Trash_Bottle`），刪掉 `Model`。
2. 把新模型拖進來放在外殼底下，**移除模型上的 Collider**，位置歸零。
3. 選外殼，在 Box Collider 按 **Edit Collider**，把綠框調到大致包住模型。
   形狀很不規則時，也可以改用多個 Box / Capsule Collider，或在模型上加 **Mesh Collider 並勾 Convex**（不勾 Convex 會報錯）。
4. 外殼上 TrashItem 的 **Size** 決定手感：`Large` 拉得出水面，`Small` 一定滑掉。
5. 有貼圖的模型，在 `FishTankSetup` 的 TrashSpawner 裡，把那一筆的 **Random Tint** 關掉，不然會被染色。

**新增一種垃圾**：複製一個現有的 Prefab（Ctrl+D）照上面換模型，再到 `FishTankSetup` 的 TrashSpawner → **Entries** 加一筆，拖入 Prefab、填數量。
**不想隨機撒**：把 TrashSpawner 的 Entries 清空，直接把垃圾 Prefab 拖進場景擺好位置也可以，夾子一樣夾得到。

> 外殼的 Scale 保持 1，大小在 `Model` 上調。TrashSpawner 的 Scale Range 會乘在外殼上。
> Rigidbody 的 Mass 不影響被夾的手感（夾住時是直接帶速度），只影響被撞時的反應。

---

## 0. 匯入與專案設定

1. **Assets > Import Package > Custom Package...**，選 `.unitypackage`，按 Import。
2. **Edit > Project Settings > Player > Other Settings > Active Input Handling**
   設為 **Input Manager (Old)** 或 **Both**（程式使用 `Input.GetMouseButtonDown`）。

## 1. 魚 Prefab

1. 把魚模型放進場景，**魚頭朝 +Z（藍色軸）**。
   方向不對時，外面包一層空的父物件，再轉正裡面的模型。
2. **移除魚身上所有 Collider**（避免擋住點擊）。
3. 在最外層物件掛上 **FishController**。
4. 拖到 Project 視窗存成 Prefab，然後刪掉場景裡的這一隻。

> 螢光變色會改所有子 Renderer 的 `material.color`（URP Lit 的 Base Map 顏色）並開啟 Emission。如果魚身有貼圖，顏色會跟貼圖相乘。

## 2. 軟糖 Prefab

**小塊**（可以做好幾種）
1. 任意小模型：軟糖塊、糖果、之後的 3D 物品都行。**移除 Collider**。
2. 材質用 URP Lit，勾 **Emission**（顏色程式會覆寫）；想要半透明就把 Surface Type 改成 Transparent。
3. 各自存成 Prefab。

**整團**
1. **Create Empty**，掛上 **Jelly**。
2. （建議）加一個子物件 **Point Light**，Range 約 3、Intensity 約 2、不投影。程式會讓它跟著軟糖顏色與剩餘量變化。
3. **Piece Variants** 拖入上面的小塊 Prefab（每塊隨機挑一種）。
4. **Piece Radius** 填小塊在 scale = 1 時的大約半徑（Unity Sphere 是 0.5），魚才會停在正確距離。
5. 小塊有自己的貼圖、不想被染色時，關掉 **Tint Pieces**。
6. 存成 Prefab。

> Piece Variants 留空時，會把這個物件自己的模型當成一整塊，只有整團晃動。

## 3. 點擊面（隱形玻璃）

1. **Create Empty**，命名為 `ClickSurface`，加上 **Box Collider**。
2. 調整大小和位置，蓋住魚缸正面（面向相機的那一面），厚度設薄一點，例如 Z = 0.05。
3. **不要勾 Is Trigger**（點擊射線會忽略 Trigger）。
4. 新增一個 Layer，例如 `TankGlass`，把 `ClickSurface` 設成這一層。

> 也可以用 Cube 並關掉 Mesh Renderer。不要用 Quad 的 Mesh Collider，它只有單面。

## 4. 控制物件

建一個空物件（例如 `FishTank`），同時掛上 **FishPool** 和 **InteractionManager**。

**FishPool**

| 欄位 | 設定 |
|---|---|
| Fish Prefab | 拖入步驟 1 的魚 |
| Fish Count | 魚的數量 |
| Swim Area | 可游範圍。二選一：拖入一個 Box Collider（**要勾 Is Trigger 或停用**），或留空改填下面兩欄 |
| Swim Center / Swim Size | Swim Area 留空時使用。中心點是相對於這個物件的位置 |

選取物件時，Scene 視窗會用藍色框顯示可游範圍。軟糖的生成位置也會被夾在這個範圍內。

**InteractionManager**

| 欄位 | 設定 |
|---|---|
| Jelly Prefab | 拖入步驟 2 的軟糖 |
| Fish Pool | 可留空，會自動尋找 |
| Glass Mask | **只勾 `TankGlass`** |

## 5. 相機與發光

- 相機位置在魚缸外面。**不需要特定 Tag**。在玩家操作用的相機上掛 **InteractionCamera**（見「沙灘 ↔ 魚缸架構」的相機說明）。
- 要有「發光球」效果需要 Bloom：
  1. 相機 Inspector 勾 **Rendering > Post Processing**。
  2. 場景裡要有 **Global Volume**，Profile 加 **Bloom**（Threshold 約 0.9、Intensity 約 1.2）。

## 6. 測試

按 Play：
- 左鍵點玻璃 → 一團多色軟糖從鏡頭射出，啪地黏在玻璃內側晃動，附近的魚圍過來啄食，軟糖越來越小、越來越暗，最後消失。
- 在同一處連點 → 新射的軟糖黏到原本那團上變大，不會多生一顆。
- 每團裡有一兩塊特別亮、會閃的螢光塊；圍在螢光塊旁邊吃的魚會慢慢變成該顏色並發光。
- 左鍵按住大型垃圾（瓶子、袋子、罐子）→ 跟著游標走，往上拖出水面後彈一下消失。
- 左鍵按住小型垃圾（彩色碎片）→ 拖不太動、一直抖，夾子變紅、鏡頭震，最後一定滑掉沉回去。

---

## 參數調整

**FishController（在魚 Prefab 上）**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Swim Speed | 1.5 | 自由遊動速度 |
| Waypoint Timeout | 6 | 多久沒到目標點就換一個 |
| Bob Amplitude / Frequency | 0.1 / 2 | 上下擺動幅度與頻率 |
| Seek Speed | 3 | 游向軟糖的速度 |
| Arrive Distance | 0.15 | 距離站位多近算抵達 |
| Mouth Offset | 0.3 | 魚身中心到嘴的距離；魚比較大時調大，避免魚身插進軟糖 |
| Bite Interval / Jitter | 1 / 0.25 | 每口間隔與隨機誤差；**越大吃越慢** |
| Peck Amplitude | 0.08 | 啄食時往前衝的距離 |
| Pigment Per Bite | 0.25 | 每咬一口螢光塊，身體往螢光色靠近的比例 |
| Glow Per Bite / Max Glow | 0.5 / 2.5 | 每咬一口螢光塊增加的自發光、上限 |
| Glow Pulse / Frequency | 0.2 / 3 | 魚身螢光的呼吸 |
| Color Change Speed | 2 | 變色過渡速度 |
| Turn Speed | 5 | 轉向速度 |
| Visual | 自動 | 做軟Q動作的模型節點；留空會自動把子物件包進 `Visual` |
| Stretch Per Speed | 0.05 | 每單位速度身體拉長多少 |
| Stretch Stiffness / Damping | 150 / 6 | 拉長彈簧；阻尼越小越 Q 越會晃 |
| Wiggle Angle / Frequency | 12 / 8 | 擺尾角度與頻率（隨速度變化） |
| Bank Per Turn / Max Bank | 0.15 / 35 | 轉彎傾斜 |
| Bite Squash / Arrive Squash | 2.5 / 2 | 咬一口、抵達時壓扁的力道 |

**Jelly（在軟糖 Prefab 上）**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Bites | 30 | 一顆軟糖可以被咬幾口；**越多吃越久** |
| Min Scale | 0.35 | 剩一口時的大小比例 |
| Piece Count / Max Pieces | 7 / 20 | 一開始幾塊、堆疊後最多幾塊 |
| Pieces Per Stack | 3 | 堆疊一次多黏幾塊 |
| Cluster Radius | 0.22 | 小塊散布的半徑 |
| Piece Scale Range | 0.28–0.42 | 每塊隨機大小（乘上小塊 Prefab 本身的 Scale） |
| Piece Colors | 紅橙黃綠藍紫 | 普通軟糖每塊輪流挑的顏色 |
| Fluorescent Count | 1–2 | 每團混幾塊螢光小塊 |
| Fluorescent Per Stack | 1 | 堆疊時新黏上的螢光塊數 |
| Fluorescent Colors | 桃紅 / 螢光綠 / 青 / 螢光橘 | 螢光塊顏色池 |
| Fluorescent Emission | 6 | 螢光塊自發光強度 |
| Fluorescent Scale | 1.15 | 螢光塊比普通塊大多少 |
| Fluorescent Blink / Frequency | 0.5 / 6 | 螢光塊閃爍 |
| Hue Jitter | 0.03 | 每塊色相的隨機偏移 |
| Flight Stretch | 0.35 | 飛行時拉長多少（乘力道） |
| Impact Squash / Impact Scatter | 7 / 3 | 撞上玻璃整團壓扁、每塊亂晃的力道（乘力道） |
| Jiggle Stiffness / Damping | 220 / 9 | 小塊彈簧 |
| Bite Jiggle / Cluster Bite Jiggle | 4 / 1.5 | 被咬時小塊、整團的晃動力道 |
| Stack Jiggle | 3 | 堆疊時的晃動力道 |
| Idle Wobble / Frequency | 0.04 / 3 | 閒置呼吸 |
| Attract Radius | 4 | 吸引範圍 |
| Max Eaters | 6 | 同時圍食的魚數上限 |
| Scan Interval | 0.25 | 多久補一次位 |
| Pop In Time / Pop Stagger / Vanish Time | 0.35 / 0.04 / 0.3 | 每塊彈出時間、依序彈出間隔、消失時間 |
| Float Amplitude / Frequency | 0.08 / 1.2 | 漂浮幅度與頻率 |
| Glow Color | 暖白 | 普通軟糖點光源的顏色 |
| Emission Intensity | 0.35 | 普通小塊自發光；壓低才襯得出螢光塊 |
| Glow Pulse / Frequency | 0.3 / 2.5 | 發光呼吸幅度與頻率 |

**InteractionManager**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Max Ray Distance | 100 | 點擊射線長度 |
| Launch Force | 1 | **發射力道**（0.2–3）：飛行速度、拉長、撞擊壓扁一起變 |
| Launch Speed | 18 | 力道 1 時的飛行速度 |
| Launch Arc | 0.4 | 飛行弧度高度 |
| Launch Offset | (0, -0.6, 0.8) | 發射點相對鏡頭的位置（右、上、前） |
| Inset Depth | 0.4 | 軟糖黏在玻璃內側多深 |
| Stack Radius | 0.8 | 落點這個距離內已有軟糖就黏到那顆上 |
| Max Jellies | 5 | 場上軟糖上限 |

**TrashItem（在垃圾 Prefab 上）**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Size | — | `Large` 可帶出、`Small` 一定滑掉 |
| Water Drag / Angular Drag | 3 / 2 | 在水裡的阻力 |
| Buoyancy | 0.6 | 抵銷多少重力；1 = 懸浮 |
| Follow Gain / Max Follow Speed | 12 / 8 | 跟上夾子的速度 |
| Remove Height / Remove Time | 0.6 / 0.4 | 大型：拉到水面上多高算帶出、消失動畫長度 |
| Resistance | 0.75 | 小型：阻力，越大越拖不動 |
| Slip Time | 0.8–1.8 | 小型：夾住多久一定滑掉 |
| Slip Distance | 0.8 | 小型：拖太快（夾子和垃圾拉開這麼遠）就滑掉 |
| Surface Slip Margin | 0.8 | 小型：離水面剩這段距離一定滑掉 |
| Struggle Jitter / Frequency | 0.12 / 14 | 小型：被夾時亂抖 |
| Slip Kick / Regrab Delay | 1.5 / 0.5 | 滑掉時彈開的力道、多久才能再夾 |

**TrashGrabber（在 FishTankSetup 上）**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Grab Radius | 0.25 | 夾取判定半徑；小碎片點不到就調大，常誤夾就調小 |
| Mouse Button | 0 | 左鍵（和軟糖共用） |
| Grab Large / Small Rumble、Struggle、Slip、Remove Rumble | 0.5 / 0.3 / 0.35 / 1 / 0.6 | 各事件的震動強度 |
| Simulate Rumble / Camera Shake / Claw Shake | 開 / 0.06 / 0.08 | 沒接手把時用抖動代替震動 |

震動會從靜態事件 `TrashGrabber.Rumble(強度 0–1, 秒數)` 送出，接手把震動時訂閱它。
大型垃圾帶出魚缸時觸發 `TrashItem.Removed`，接轉場用。

**TrashSpawner（在 FishTankSetup 上）**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Entries | 瓶 / 袋 / 罐各 1、碎片 12 | 每筆：Prefab、數量、Scale Range、Random Tint |
| Area | `SwimArea` | 撒的範圍（只用底部） |
| Margin / Drop Height | 0.5 / 0.5 | 往內縮的距離、離底部多高內生成 |

**FishPool**

| 欄位 | 預設 | 說明 |
|---|---|---|
| Respawn Delay | 1 | 回收後幾秒重生 |
| Randomize Color | 關 | 每隻魚隨機上色（測試用） |
| Swim Margin | 0.3 | 可游範圍往內縮的距離 |

## 常見問題

| 症狀 | 檢查 |
|---|---|
| 點了沒反應／點的位置對不上 | Active Input Handling、主鏡頭有沒有掛 InteractionCamera（群組裡其他相機不要掛）、點擊面是否勾了 Is Trigger、Glass Mask 有沒有勾到點擊面的 Layer |
| 軟糖不會發光 / 沒有光暈 | 材質沒勾 Emission；相機沒開 Post Processing；場景沒有 Bloom Volume |
| 軟糖生成在玻璃外 | 點擊面不在魚和相機之間，或 Swim Area 沒設在缸內 |
| 魚倒著游 / 側著游 | 魚模型的頭沒有朝 +Z |
| 魚身插進軟糖 | 加大 FishController 的 Mouth Offset，或確認 Jelly 的 Piece Radius |
| 魚太晃 / 不夠 Q | 調 Stretch Damping（越小越晃）、Stretch Per Speed |
| 魚沒變色 | 魚要圍在螢光塊旁邊才會變；魚的材質 shader 要有主顏色（`_BaseColor` / `_Color`）與 Emission |
| 看不出螢光塊 | 調高 Fluorescent Emission、調低普通塊的 Emission Intensity，確認 Bloom 有開 |
| 魚貼著或穿過魚缸邊 | 縮小 Swim Area，或加大 Swim Margin |
| Console 顯示「尚未指定 Jelly Prefab」等錯誤 | FishPool / InteractionManager 的欄位沒拖 |
| 垃圾掉出魚缸 | 換魚缸模型後沒調 `TrashWalls`；或垃圾 Collider 太小 |
| 垃圾一直抖或卡在一起 | 外殼的 Collider 互相重疊太多，縮小一點；或模型上還留著 Collider |
| 點垃圾卻射出軟糖 | 垃圾外殼沒有 Collider 或沒掛 TrashItem；或 Grab Radius 太小 |
| 想射軟糖卻夾到垃圾 | 調小 TrashGrabber 的 Grab Radius |
| 大型垃圾拉不出去 | `Water` 的範圍太高，或魚缸模型頂端有 Collider 擋住 |
| 垃圾在沙灘上也像在水裡 | 某個 WaterVolume 的範圍蓋到沙灘了，選取 `Water` 看藍框 |
| 丟進小魚缸沒反應 | ScaleBridge 的三個欄位沒拖；BeachTankArea 太小；垃圾還被夾著（要放開才會掉進去） |
| 帶出的垃圾又掉回魚缸 | ExitPoint 在小魚缸裡面或太近 |
| 在沙灘點東西射出軟糖 | InteractionManager 的 Click Surface 沒指定 |
| 換模型後被染成奇怪顏色 | TrashSpawner 那一筆的 Random Tint 關掉 |

---

## 沙灘 ↔ 魚缸架構

> 只要一個魚缸、不需要「鏡頭穿進魚缸變大」的效果時，用上面的**配置 C** 就好，不需要這套架構。

這套架構是兩個世界：魚缸世界用原尺寸放在遠處，沙灘上只放一個縮小的外殼：

```
沙灘場景（人的尺度）
├─ 沙灘、海……
├─ 小魚缸外殼          ← 只是模型，頂端開口；牆可以有 Collider
│  └─ BeachTankArea   ← 停用的 Box Collider，標出小魚缸內部
├─ ExitPoint          ← 帶出的垃圾掉在這裡（藍色軸 = 拋出方向），要在小魚缸外面
├─ 沙灘上的垃圾        ← 和魚缸用同一批 Trash_* Prefab，Scale 縮小
└─ ScaleBridge        ← 拖入 BeachTankArea、SwimArea、ExitPoint

魚缸世界（放在遠處，例如 X = 1000，原尺寸不縮放）
└─ FishTankSetup      ← 魚、軟糖、垃圾、夾子、Water，全部不用改
```

**比例**：ScaleBridge 用「SwimArea 寬 ÷ BeachTankArea 寬」算出倍數 k（例如 3）。

| 發生什麼 | 物件怎麼走 |
|---|---|
| 沙灘的垃圾被丟進小魚缸 | 進到 BeachTankArea 的那一刻，搬到魚缸世界對應的位置，**放大 k 倍**。沙灘上的小瓶子在魚缸裡變成巨物 |
| 大型垃圾被拉出魚缸水面 | 觸發 `TrashItem.Removed`，ScaleBridge 把它**縮小 k 倍**放到 ExitPoint |
| 小型垃圾 | 永遠出不了水，所以永遠留在魚缸世界 |

物件本身不知道自己在哪個世界：在水裡（`WaterVolume` 範圍內）就有水的手感，在沙灘上就是普通物理，大小都正常夾。之後沙灘的海也可以放一個 WaterVolume，記得關掉 **Remove On Exit**。

**鏡頭不歸這套架構管，也不看相機的 Tag。** 夾子和軟糖透過 `ViewCamera.Current` 決定用哪台相機算點擊，依序：

1. `ViewCamera.Override`：鏡頭組的程式直接指定（例如 `ViewCamera.Override = beachCam;`）。
2. 掛了 **InteractionCamera** 而且開著的相機：同時有好幾台時，**Priority** 大的優先，一樣大再比 Depth。
3. 都沒有時自動挑：開著、畫到螢幕（沒有 Target Texture）、不是 URP Overlay、Depth 最高的那台。

我們的鏡頭是**一個相機群組（多台相機），以其中一台主鏡頭為準，整組在沙灘和魚缸之間移動**。所以：**只在主鏡頭上掛一個 InteractionCamera**，群組裡其他相機都不要掛。Priority 不用調，也不用設定 Override。

測試場景沒有相機群組，所以用兩台相機加 `DebugCameraSwitch`（Tab 硬切）代替，之後換成鏡頭組的群組。

### 給鏡頭組：相機在兩個世界之間移動

魚缸世界在遠處、而且大 k 倍，所以主鏡頭 zoom 進小魚缸時，要在穿過玻璃那一刻把整個相機群組**瞬移**到魚缸世界。ScaleBridge 提供換算：

```csharp
// 進魚缸：沙灘上的位置 → 魚缸世界的位置（旋轉不變）
rig.position = scaleBridge.BeachToTank(rig.position);
// 出魚缸：反過來
rig.position = scaleBridge.TankToBeach(rig.position);
// 倍數
float k = scaleBridge.Factor;
```

- **只換位置、不換旋轉**，畫面構圖就會接得上。瞬移那一幀用氣泡、水花或模糊遮住，比較看不出接縫。
- 進到魚缸世界後，一切都大了 k 倍：相機的**移動速度、和物件保持的距離、Near / Far Clip** 要乘 k（出來時除以 k），不然會覺得鏡頭突然變慢、變近。
- 夾子在沒有手把時會**抖動主鏡頭**模擬震動（`TrashGrabber` 的 Camera Shake）。如果相機群組每幀自己設定鏡頭位置，兩邊會打架：把 Camera Shake 設成 0，改由相機群組訂閱 `TrashGrabber.Rumble(強度, 秒數)` 自己做晃動。

### 手動拼裝（參考）

`BeachTankSetup` Prefab 就是下面這些東西包在一起。只有想自己做小魚缸外觀、或了解原理時才需要看：

- 小魚缸外觀（頂端開口）＋ `TrashWalls`（擋垃圾）＋ `BeachTankArea`（停用的 Box Collider，標出內部範圍）
- `ExitPoint`：帶出的垃圾放這裡，要在小魚缸**外面**，藍色軸 = 拋出方向
- 根物件掛 **ScaleBridge**（Beach Tank、Exit Point 指好；Tank World 留空會自動找）和 **PortalWindow**（Window Material 拖 `PortalWindow.mat`）
- 自己做的外觀：把 `BeachTankSetup/Body/Tank` 換成你的模型即可，其他不用動

| 欄位（ScaleBridge，在 `BeachTankSetup` 上） | 預設 | 說明 |
|---|---|---|
| Beach Tank | Prefab 已指定 | 小魚缸的內部範圍（停用的 Box Collider） |
| Tank World | 自動 | 魚缸世界的範圍；留空自動找 FishTankSetup 的 `SwimArea` |
| Exit Point | Prefab 已指定 | 帶出的物品放這裡，往藍色軸拋 |
| Exit Pop | 1.5 | 拋出的力道 |
| Enter Sink Speed | 1 | 掉進魚缸時的初速 |
| Transfer Cooldown | 1 | 搬過去後多久內不會再被搬 |
| Traveler Exit Height | 0.5 | ScaleTraveler 物品高於魚缸頂端多少就搬回沙灘（魚缸世界單位） |
| Scale Beach Items | 關 | 開場把沙灘上的 TrashItem 縮成沙灘尺寸。沙灘請用 `Trash_*_Beach`（所見即所得）；只有把魚缸尺寸的 `Trash_*` 放沙灘時才打開 |

### 簡易運鏡（TransferCameraMove，可選）

掛在相機上（相機群組就掛在最外層，或把 **Target** 指向最外層）。**取消勾選這個元件就不會運鏡**。

- 沙灘的物件掉進小魚缸 → 等 Start Delay → 鏡頭一路推向「Tank View 換算回沙灘的位置」→ 到點瞬移，剛好就是魚缸世界的 Tank View。前後構圖相同，中間不會停在別的點。
- 大型垃圾被帶出 → 瞬移回沙灘上對應的位置（構圖不變）→ 一路拉回沙灘視角。
- 小魚缸掛了 **PortalWindow** 時，瞬移前後畫面一樣，看不出接縫（見下方「傳送門窗戶」）。沒掛的話小魚缸是空的，瞬移那一刻魚缸世界的東西會突然出現。
- 進魚缸時 Near / Far Clip 自動乘上倍數，出來除回去。
- 運鏡中、或已經在那一邊時，不會重複觸發。

**在你的場景裡用**：
1. 選 Main Camera → Add Component → **TransferCameraMove**。
2. 把 Main Camera 擺到沙灘視角（開場位置就是沙灘視角；或另外建一個空物件拖進 **Beach View**）。
3. 進魚缸後停的視角：**不用設定**，預設用 `FishTankSetup/TankView`，移動它就能換視角。要用別的物件再拖進 **Tank View**。
4. 測試時可以把 **Toggle Key** 設成 Tab，手動切換。

| 欄位 | 預設 | 說明 |
|---|---|---|
| Target | 自己 | 要移動的物件；相機群組拖最外層 |
| Bridge | 自動 | ScaleBridge |
| Follow Into Tank / Follow Out To Beach | 開 / 開 | 哪個事件要跟著運鏡 |
| Toggle Key | None | 測試用手動切換鍵 |
| Start In Tank | 關 | 開場就在魚缸世界 |
| Beach View | 空 | 回沙灘時停的視角；留空回到進魚缸前的位置 |
| Tank View | 空 | 進魚缸後停的視角；留空用 `FishTankSetup/TankView` |
| Approach Distance | 2 | 推進前先經過小魚缸正前方多遠（沙灘單位）；鏡頭不在小魚缸前面時靠它繞過來，0 = 直線 |
| Glass Distance | 1.2 | 沒有 Tank View 時，停的位置離小魚缸中心多遠（沙灘單位） |
| Start Delay / Move Time | 0.4 / 1.2 | 觸發後等多久、推進（拉回）一次的時間 |
| Scale Clip Planes | 開 | 進出魚缸時縮放 Near / Far Clip |

**給其他相機或鏡頭組用**：
- 直接掛到別的相機或群組上，調 Target 和兩個 View 就能用。
- 不想靠事件觸發：關掉兩個 Follow，在自己的程式呼叫 `GoToTank()` / `GoToBeach()`，用 `InTank`、`IsMoving` 查狀態。
- 完全自己寫運鏡：訂閱 `ScaleBridge.MovedToTank` / `MovedToBeach`（參數是被搬的物件），瞬移用 `BeachToTank()` / `TankToBeach()`。

### 傳送門窗戶（PortalWindow）

讓沙灘上的小魚缸**看起來裝著魚缸世界**：從沙灘看得到裡面的魚、軟糖、垃圾在動，鏡頭推進去瞬移時前後畫面一樣，看不出切換。

**原理**：魚缸世界有一台自動建立的鏡像相機，每次畫面前跟著玩家相機（`ViewCamera`）移到 `BeachToTank()` 換算的位置，畫到 RenderTexture。小魚缸的玻璃換成 `FishTank/PortalWindow` 材質，依螢幕座標取那張圖。鏡像畫面透明的地方（空的水、背景）會透出小魚缸後面的沙灘。

**用法**：`BeachTankSetup` 已經掛好，不用設定。自己拼小魚缸時，在外觀的根物件 Add Component → **PortalWindow**，底下所有 Renderer 會在 Play 時換成窗戶材質。

**前提**：
- 魚缸世界要離沙灘夠遠（例如 X = 1000），主鏡頭在沙灘上不能直接看到魚缸世界。
- 玩家相機會被自動打開 **Opaque Texture**（Rendering 裡的選項），窗戶才拿得到後面的沙灘。
- 鏡像相機會自己判斷：玩家相機在魚缸世界時就停止畫，不吃效能。

| 欄位 | 預設 | 說明 |
|---|---|---|
| Bridge | 自動 | ScaleBridge |
| Windows | 空 | 要變成窗戶的 Renderer；留空就用這個物件底下全部 |
| Window Material | 空 | **正式場景要拖 `Assets/FishTank/Materials/PortalWindow`**。留空時編輯器裡會自動建立，但 Build 出來 shader 會被剔除、窗戶失效 |
| Resolution Scale | 1 | 鏡像畫面解析度比例；效能不夠時調到 0.5 |

窗戶材質的參數：**Portal Tint**（魚缸畫面的色調）、**Glass Color**（玻璃反光色，A 是強度）、**Flip Y**（窗戶裡的畫面上下顛倒時勾）。

| 症狀 | 檢查 |
|---|---|
| 小魚缸是全黑或全透明 | Console 有沒有 shader 錯誤；魚缸世界是不是在鏡像相機的視野裡（ScaleBridge 的 Tank World 有沒有對到） |
| 窗戶裡的畫面上下顛倒 | 自訂一個窗戶材質勾 Flip Y，拖進 Window Material |
| 窗戶裡畫面和瞬移後的畫面對不上 | TankView 換算回沙灘的位置要在小魚缸外面；兩個世界不能有旋轉差 |
| 窗戶裡看得到沙灘的東西 | 魚缸世界離沙灘不夠遠 |
