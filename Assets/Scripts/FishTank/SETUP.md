# FishTank 導入手冊

程式 → [一](#一導入給程式)　效果 → [預計出現效果](#預計出現效果)　美術 → [二](#二換素材給美術)　原理 → [三](#三架構)　參數 → [四](#四腳本參考)　出問題 → [常見問題](#常見問題)

---

# 一、導入（給程式）

## 打包（在 20261007GraduateProjectTest）
1. **Tools > FishTank > 1. 建立 Prefab 與示範場景**
2. **Tools > FishTank > 2. 匯出 unitypackage**

## 匯入新專案（URP，Unity 2023.1 或 6）
1. 從 GitHub **Releases** 下載 `FishTank.unitypackage`（或自己打包）
2. **Assets > Import Package > Custom Package** → 全選 → Import
3. **Project Settings > Player > Active Input Handling** → **Both**
4. 打開 `Assets/FishTank/Scenes/BeachDemo` 按 Play 確認能玩

更新版本時一樣重新匯入；美術直接改過的套件 Prefab 會被蓋掉，匯入時取消勾選它們。

## 放進現有的沙灘場景
1. 拖入 **`FishTankSetup`**，放到場景**空曠的遠處**（例如 X = 1000，不要和地形重疊）
2. 拖入 **`BeachTankSetup`**，放在沙灘地面上（Scale 0.33 = 魚缸世界大 3 倍）
3. 主鏡頭加 **`InteractionCamera`**（要自動運鏡再加 **`TransferCameraMove`**）
4. 要光暈：主鏡頭勾 Post Processing，加 Global Volume，Profile 用 `FishTankBloom`

## 讓物品可以放進魚缸
- 現有物品：加 **`ScaleTraveler`**，不用設定，不影響它原本的物理和父子結構
- 進魚缸預設照比例放大（從沙灘看起來一樣大）；想要進去後變小或變大，勾 **Custom Tank Size** 再調 **Tank Size**
- 垃圾：沙灘上用 **`Trash_*_Beach`**（已是沙灘尺寸，看到多大玩的時候就多大）；`Trash_*` 是魚缸尺寸，給魚缸裡用

## 要知道的
- 相機不看 Tag，看哪台掛了 `InteractionCamera`（相機群組只掛主鏡頭）
- `PortalWindow` 會打開主鏡頭的 **Opaque Texture**
- Joy-Con 用滑鼠模式即可；震動訂閱 `TrashGrabber.Rumble`
- 自己接程式：見[程式介面](#程式介面)

交件前 **Build 一次**確認窗戶正常。

---

# 預計出現效果

按 Play 後，一切正常的話應該看到：

## 開場
- 從沙灘看小魚缸，**裡面看得到魚在游**，缸底有瓶子、袋子、罐子和彩色碎片
- 沙灘上的 `Trash_*_Beach` 大小和編輯時一樣

## 軟糖（點魚缸玻璃）
- 一團多色軟糖從鏡頭下方射出，撞到玻璃壓扁、黏住、發光
- 附近的魚圍過來一口一口吃，軟糖被咬時晃動、慢慢縮小，吃完消失
- 每團有 1–2 塊**會閃的螢光塊**；咬到螢光塊的魚會慢慢變成那個顏色並發光
- 在同一處連點，新軟糖黏上去變大；場上最多 5 團

## 夾垃圾（在魚缸裡按住左鍵）
| 垃圾 | 效果 |
|---|---|
| 大型（瓶、袋、罐） | 跟著游標移動、會被玻璃擋住；拉出水面 → 往上一彈，縮小出現在沙灘、從小魚缸旁拋出（中等震動） |
| 小型（彩色碎片） | 拖不太動、一直抖，夾子由白變紅、持續震動；1–2 秒內或接近水面**一定滑掉**沉回去（強震） |

夾住那一下不震動。沒接手把時，震動用鏡頭和夾子的輕微抖動表示。

## 換場（沙灘 ↔ 魚缸）
- 沙灘的垃圾或掛了 `ScaleTraveler` 的物品，丟進小魚缸 → 在魚缸世界**放大 3 倍**出現（勾了 Custom Tank Size 就照設定的大小）
- 掛了 `ScaleTraveler` 的物品在魚缸裡被拿到缸頂上方 → 縮小出現在沙灘
- 進出一趟，大小完全還原；魚缸裡的碎片永遠拿不出來

## 運鏡（主鏡頭有掛 TransferCameraMove）
- 物品掉進小魚缸約 0.4 秒後，鏡頭轉向小魚缸、一路推進、穿過玻璃，**畫面連續、看不出切換**，停在魚缸前的 TankView
- 大型垃圾帶出後，鏡頭從魚缸拉出，回到進去前的位置

---

# 二、換素材（給美術）

**規則**：用 **Prefab Variant** 換（右鍵 Prefab → Create > Prefab Variant）；只換 `Model` 層；模型不要帶 Collider。

| 素材 | 換哪裡 | 注意 |
|---|---|---|
| 魚 | `Fish/Visual/Model` | 魚頭朝 +Z；有貼圖時關掉 `FishTankSetup` FishPool 的 **Randomize Color** |
| 軟糖 | 新小塊拖進 `Jelly` 的 **Piece Variants** | 材質勾 Emission；有貼圖時關 **Tint Pieces** |
| 垃圾 | `Trash_*/Model` | 換完在外殼按 **Edit Collider** 包住模型；有貼圖時關 TrashSpawner 該筆的 **Random Tint**。`Trash_*_Beach` 會自動跟著換 |
| 魚缸 | `FishTankSetup/Tank` | 尺寸改變時請程式調範圍 |
| 沙灘小魚缸 | `BeachTankSetup/Body/Tank` | 這層會變成窗戶；框和裝飾放在 `Body` 底下其他地方 |
| 窗戶效果 | `Materials/PortalWindow` | Portal Tint、Glass Color |
| 光暈 | `FishTankBloom` | Intensity、Threshold |

**不要動**：`ClickSurface`、`SwimArea`、`TrashWalls`、`Water`、`BeachTankArea`

---

# 三、架構

```
沙灘場景                               魚缸世界（遠處，原尺寸）
BeachTankSetup（Scale 1/k）  ── ×k ──→  FishTankSetup
 └ 玻璃 = 看進魚缸世界的窗戶   ←── ÷k ──   魚、軟糖、垃圾、夾子
```

- 魚缸世界不縮小，手感和物理才不會跑掉；沙灘上只放縮小的外殼
- 物品掉進小魚缸 → 放大 k 倍搬過去；大型垃圾拉出水面或物品被拿到缸頂上方 → 縮小搬回沙灘
- 小魚缸的玻璃顯示魚缸世界的即時畫面，所以鏡頭推進去時看不出切換

---

# 四、腳本參考

## 腳本在哪、誰呼叫誰

```
FishTankSetup（魚缸世界）
 ├ FishPool ──生成──→ Fish（FishController）
 ├ InteractionManager ──點玻璃發射──→ Jelly ──吸引──→ FishController
 ├ TrashGrabber ──夾──→ TrashItem ──拉出水面──→ TrashItem.Removed
 ├ TrashSpawner ──開場撒──→ TrashItem
 └ Water（WaterVolume）←──查詢在不在水裡── TrashItem

BeachTankSetup（沙灘）
 ├ ScaleBridge ←──Removed── TrashItem
 │   ├ 搬移 TrashItem、ScaleTraveler
 │   └ 發出 MovedToTank / MovedToBeach ──→ TransferCameraMove
 └ PortalWindow（鏡像相機 + 窗戶材質）

主鏡頭：InteractionCamera（標記）、TransferCameraMove（可選）
ViewCamera（靜態）：TrashGrabber、InteractionManager、PortalWindow 用它找主鏡頭
```

以下只列 Inspector 上會調的欄位；括號是預設值。

## ScaleBridge　沙灘 ↔ 魚缸的換算與搬移（在 `BeachTankSetup`）
| 欄位 | 功能 |
|---|---|
| Beach Tank | 小魚缸內部範圍（Prefab 已指定） |
| Tank World | 魚缸世界範圍；留空自動找 `FishTankSetup/SwimArea` |
| Exit Point | 帶出的東西放這裡，往藍色軸拋 |
| Exit Pop (1.5) | 垃圾被帶出時拋出的力道 |
| Enter Sink Speed (1) | 垃圾掉進魚缸時的下沉初速 |
| Transfer Cooldown (1) | 搬過去後幾秒內不會再被搬 |
| Traveler Exit Height (0.5) | ScaleTraveler 物品高於缸頂多少就搬回沙灘 |
| Scale Beach Items (關) | 開場把沙灘上的 `Trash_*` 縮成沙灘尺寸（用 `_Beach` 時不需要） |

## ScaleTraveler　標記「可以放進魚缸」（掛在任何物品）
| 欄位 | 功能 |
|---|---|
| Held | 設 true 時暫停換場（一般不用） |
| Custom Tank Size (關) | 不勾 = 照比例（等於 1）；勾起來改用 Tank Size |
| Tank Size (0.5) | 進魚缸後的大小：1 = 照比例、0.5 = 一半、2 = 兩倍。帶出來會還原 |

## PortalWindow　小魚缸的窗戶（在 `BeachTankSetup`）
| 欄位 | 功能 |
|---|---|
| Window Root | 底下的 Renderer 變成窗戶（Prefab 指向 `Body/Tank`） |
| Window Material | 窗戶材質 `PortalWindow.mat`（Build 必須有） |
| Resolution Scale (1) | 窗戶畫面解析度；卡頓時調 0.5 |

材質 `PortalWindow.mat`：**Portal Tint**（窗內色調）、**Glass Color**（玻璃反光，A = 強度）、**Flip Y**（畫面顛倒時勾）

## TransferCameraMove　進出魚缸的運鏡（主鏡頭，可選）
| 欄位 | 功能 |
|---|---|
| Target | 要移動的物件；留空 = 自己，相機群組拖主鏡頭或最外層 |
| Follow Into Tank / Out To Beach (開) | 物品進／出魚缸時自動運鏡 |
| Toggle Key (None) | 測試用手動切換鍵 |
| Start In Tank (關) | 開場就在魚缸世界 |
| Beach View | 回沙灘停的位置；留空 = 進魚缸前的位置 |
| Tank View | 進魚缸停的位置；留空 = `FishTankSetup/TankView` |
| Glass Distance (1.2) | 沒有 Tank View 時停在離小魚缸多遠 |
| Approach Distance (2) | 推進前先經過小魚缸正前方多遠；0 = 直線 |
| Start Delay (0.4) | 觸發後等多久才動 |
| Move Time (1.2) | 推進／拉回一次的秒數 |
| Scale Clip Planes (開) | 進魚缸時 Near / Far Clip 跟著乘倍數 |

## InteractionCamera　標記操作用的相機（主鏡頭）
| 欄位 | 功能 |
|---|---|
| Priority (0) | 有好幾台標記相機同時開著時，大的優先 |

## TrashGrabber　夾子（在 `FishTankSetup`）
| 欄位 | 功能 |
|---|---|
| Grab Radius (0.25) | 夾取判定範圍；夾不到碎片調大，誤夾調小 |
| Mouse Button (0) | 0 左鍵（和發射軟糖共用） |
| Grab Large / Small Rumble (0 / 0) | 夾住那一下的震動；0 = 不震 |
| Struggle Rumble (0.35) | 夾著小垃圾時持續震動 |
| Slip / Remove Rumble (1 / 0.6) | 滑掉、帶出水面的震動 |
| Simulate Rumble (開) | 沒手把時用抖動模擬震動 |
| Camera Shake / Claw Shake (0.06 / 0.08) | 模擬震動的鏡頭／夾子抖動量 |
| Claw Size / Idle Color / Strain Color | 夾子球的大小與顏色（越吃力越紅） |

## TrashItem　垃圾手感（在每個 `Trash_*`）
| 欄位 | 功能 |
|---|---|
| Size | `Large` 拉得出水面／`Small` 一定滑掉 |
| Custom Tank Size / Tank Size (關 / 0.5) | 同 ScaleTraveler：進魚缸改用自訂大小，帶出來會還原。只設在 `Trash_*_Beach` 上 |
| Water Drag / Angular Drag (3 / 2) | 水中阻力 |
| Buoyancy (0.6) | 浮力，1 = 懸浮 |
| Follow Gain / Max Follow Speed (12 / 8) | 跟手的速度 |
| Remove Height / Time (0.6 / 0.4) | 大型：拉到水面上多高算帶出、消失動畫秒數 |
| Resistance (0.75) | 小型：阻力，越大越拖不動 |
| Slip Time (0.8–1.8) | 小型：夾住多久一定滑掉 |
| Slip Distance (0.8) | 小型：拖太快就滑掉 |
| Surface Slip Margin (0.8) | 小型：離水面多近一定滑掉 |
| Struggle Jitter / Frequency (0.12 / 14) | 小型：被夾時亂抖 |
| Slip Kick / Regrab Delay (1.5 / 0.5) | 滑掉的彈開力道、多久能再夾 |

## TrashSpawner　開場撒垃圾（在 `FishTankSetup`）
| 欄位 | 功能 |
|---|---|
| Entries | 每筆：Prefab、Count（數量）、Scale Range（隨機大小）、Random Tint（隨機上色） |
| Area | 撒的範圍（預設 `SwimArea`） |
| Margin / Drop Height (0.5 / 0.5) | 離邊界多遠、離缸底多高內生成 |

## WaterVolume　水的範圍（`FishTankSetup/Water`）
| 欄位 | 功能 |
|---|---|
| Center / Size | 範圍，頂面就是水面 |
| Remove On Exit (開) | 大型垃圾拉出水面算帶出；沙灘的海要關 |

## FishPool　魚的生成（在 `FishTankSetup`）
| 欄位 | 功能 |
|---|---|
| Fish Prefab / Fish Count (15) | 魚的 Prefab、數量 |
| Respawn Delay (1) | 回收後幾秒重生 |
| Randomize Color (開) | 佔位魚隨機上色；有貼圖的魚關掉 |
| Swim Area / Swim Margin (0.6) | 可游範圍、往內縮多少 |

## FishController　單隻魚（在 `Fish`）
| 欄位 | 功能 |
|---|---|
| Swim Speed / Seek Speed (1.5 / 3) | 平常游速／游向軟糖的速度 |
| Mouth Offset (0.3) | 魚身中心到嘴的距離；魚大時調大 |
| Bite Interval / Jitter (1 / 0.25) | 每口間隔；越大吃越慢 |
| Pigment Per Bite (0.25) | 每咬一口螢光塊，身體往螢光色靠近的比例 |
| Glow Per Bite / Max Glow (0.5 / 2.5) | 吃螢光塊後的發光量、上限 |
| Turn Speed (5) | 轉向速度 |
| Stretch / Wiggle / Bank 系列 | 軟Q動作：游快拉長、擺尾、轉彎傾斜 |
| Bite Squash / Arrive Squash (2.5 / 2) | 咬一口、抵達時壓扁的力道 |

## InteractionManager　點玻璃發射軟糖（在 `FishTankSetup`）
| 欄位 | 功能 |
|---|---|
| Click Surface | 只有點到它才發射（Prefab 已指定 `ClickSurface`） |
| Launch Force (1) | 發射力道：飛行速度、拉長、撞擊壓扁一起變 |
| Launch Speed / Arc (18 / 0.4) | 飛行速度、弧度 |
| Stack Radius (0.8) | 落點附近已有軟糖就黏上去 |
| Max Jellies (5) | 場上軟糖上限 |

## Jelly　軟糖（在 `Jelly`）
| 欄位 | 功能 |
|---|---|
| Bites (30) | 一顆能被咬幾口；越多吃越久 |
| Piece Variants / Piece Count (7) | 小塊的模型、一團幾塊 |
| Piece Radius (0.5) | 小塊在 scale 1 時的半徑（換模型時調） |
| Tint Pieces (開) | 小塊依調色盤上色；有貼圖時關 |
| Fluorescent Count (1–2) / Emission (6) | 每團幾塊螢光塊、螢光亮度 |
| Attract Radius / Max Eaters (4 / 6) | 吸引魚的範圍、最多幾隻一起吃 |
| Emission Intensity (0.35) | 普通小塊亮度；壓低才襯得出螢光塊 |
| Jiggle / Pop / Float 系列 | 軟Q晃動、彈出、漂浮的動作 |

## 測試用（不會打包）
`TankBootstrap`：本專案 `SampleScene` 自動建場景。`DebugCameraSwitch`：Tab 在幾台相機之間硬切。

## 程式介面

| 用途 | 介面 |
|---|---|
| 物品進／出魚缸 | `ScaleBridge.MovedToTank`、`MovedToBeach`（`Action<Transform>`） |
| 兩個世界座標換算 | `ScaleBridge.BeachToTank()`、`TankToBeach()`、`Factor`、`IsInTankWorld()` |
| 大型垃圾被帶出 | `TrashItem.Removed`（靜態） |
| 手把震動 | `TrashGrabber.Rumble(強度 0–1, 秒數)`（靜態） |
| 觸發運鏡 | `TransferCameraMove.GoToTank()`、`GoToBeach()`、`InTank`、`IsMoving` |
| 指定操作相機 | `ViewCamera.Override`；目前用的 `ViewCamera.Current` |

---

# 常見問題

## 匯入
| 狀況 | 處理 |
|---|---|
| 跳出 Script Updating Consent | 按 **Yes, just for these files** |
| 一點擊就報 Input 錯 | Active Input Handling 設 Both |
| 重新匯入後換過的模型不見了 | 直接改了套件 Prefab 被蓋掉；改用 Prefab Variant |
| Console：找不到 ScaleBridge | 場景沒放 `BeachTankSetup` |
| Console：找不到 Tank World | 場景沒放 `FishTankSetup` |
| Console：魚缸世界離沙灘太近 | `FishTankSetup` 移到更遠處 |

## 畫面
| 狀況 | 處理 |
|---|---|
| 小魚缸看不到魚 | Console 看 `PortalWindow` shader 錯誤 |
| 窗戶畫面上下顛倒 | `Materials/PortalWindow` 勾 **Flip Y** |
| 窗戶裡看到地形或怪東西 | 魚缸世界和場景其他東西重疊，移到空曠處 |
| 小魚缸的框也變成窗戶 | 框放到 `Body` 底下、`Tank` 以外 |
| 軟糖不發光 | 主鏡頭沒勾 Post Processing，或沒有 Bloom Volume |
| 魚吃了螢光塊沒變色 | 要圍在螢光塊旁邊吃；魚的材質要有主顏色與 Emission |
| 窗戶畫面卡頓 | PortalWindow 的 **Resolution Scale** 調 0.5 |

## 互動
| 狀況 | 處理 |
|---|---|
| 點擊位置不對 | 主鏡頭沒掛 `InteractionCamera` |
| 在沙灘點東西射出軟糖 | InteractionManager 的 Click Surface 被清掉，拖回 `ClickSurface` |
| 夾不到小碎片 | TrashGrabber 的 **Grab Radius** 調大 |
| 想射軟糖卻夾到垃圾 | **Grab Radius** 調小 |
| 小垃圾太容易／太難滑掉 | TrashItem 的 **Resistance**、**Slip Time** |
| 震動太強或和相機群組打架 | TrashGrabber 的 **Camera Shake** 設 0 |

## 換場
| 狀況 | 處理 |
|---|---|
| 沙灘上的垃圾太大 | 換成 `Trash_*_Beach`，或 Scale 設 0.33 |
| 垃圾 Play 時突然變小 | ScaleBridge 的 **Scale Beach Items** 被打開了，關掉並改用 `_Beach` |
| 丟進小魚缸沒反應 | 要放開、從**上方開口**掉進去；物品要有 Collider 和 Rigidbody |
| 物品太大掉不進小魚缸 | 調大 `BeachTankSetup` 的 Scale |
| 物品進魚缸後太大／太小 | 物品上勾 **Custom Tank Size**，調 **Tank Size** |
| 帶出來的東西掉進虛空 | 移動 `BeachTankSetup/ExitPoint` 到地面上方 |
| ScaleTraveler 物品不換場 | 確認 `Held` 沒被設成 true |

## 鏡頭
| 狀況 | 處理 |
|---|---|
| 物品進魚缸鏡頭沒跟 | 主鏡頭沒掛 TransferCameraMove，或 **Follow Into Tank** 沒勾 |
| 瞬移前後畫面對不上 | **Target** 改拖主鏡頭本身 |
| 進魚缸後停的位置不對 | 移動 `FishTankSetup/TankView` |
| 回沙灘停的位置不對 | 指定 **Beach View** |
| 推進路線怪 | 調 **Approach Distance** |
