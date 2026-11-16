---
lesson: 1
lang: zh-tw
title: 讀 ROM 的 NitroFS 檔案系統
date: 2026-11-16
milestone: 1
tags: [rom, nitrofs, binary-parsing]
---

# 第一課：讀 ROM 的 NitroFS 檔案系統

## 目標

寫一個 Console 程式（還不用 SDL）：輸入美版 ROM 的路徑，列出 ROM 裡所有檔案的完整路徑與大小，最後印出總數。

這是之後所有東西的入口：背景（`.bbg`）、動畫（`.bac`）、關卡地圖（`.narc`）都是從這個檔案系統讀出來的。

## 背景：NDS 的 ROM 長什麼樣子

NDS 卡匣的 ROM 是一個大檔案，開頭是固定格式的**標頭**，後面放程式碼（ARM9／ARM7）、overlay，以及一個小型的唯讀檔案系統 **NitroFS**。NitroFS 由兩張表組成：

- **FAT**（File Allocation Table）：每個檔案的資料在 ROM 的哪一段。只有位置，沒有名字。
- **FNT**（File Name Table）：資料夾結構和檔名，把名字對應到檔案編號。

兩張表靠**檔案編號**連在一起：FNT 告訴你「`bg/sp_fix_base.bbg` 是第 N 號」，FAT 第 N 項告訴你「它在 ROM 的哪裡」。

所有整數都是 **little-endian**。

## 格式

### 1. 標頭（ROM 開頭）

| 位移 | 大小 | 內容 |
| --- | --- | --- |
| `0x40` | u32 | FNT 在 ROM 的位置 |
| `0x44` | u32 | FNT 大小 |
| `0x48` | u32 | FAT 在 ROM 的位置 |
| `0x4C` | u32 | FAT 大小 |

### 2. FAT：每個檔案 8 bytes

| 位移 | 大小 | 內容 |
| --- | --- | --- |
| +0 | u32 | 起點（ROM 內位置） |
| +4 | u32 | 終點（**不含**終點本身） |

第 N 項就是檔案編號 N，大小是 `終點 − 起點`。

### 3. FNT 主表：每個資料夾 8 bytes

放在 FNT 開頭，第 i 項是資料夾 `0xF000 + i`；根目錄是 `0xF000`（第 0 項）。

| 位移 | 大小 | 內容 |
| --- | --- | --- |
| +0 | u32 | 子表位移（**從 FNT 開頭算**，不是從 ROM 開頭） |
| +4 | u16 | 這個資料夾第一個檔案的編號 |
| +6 | u16 | 父資料夾編號；**根目錄這裡放的是資料夾總數** |

### 4. FNT 子表：資料夾的內容

一連串項目，每項：

1. 一個 byte：
   - `0x00`：這個資料夾結束。
   - 否則低 7 bit（`& 0x7F`）是名字長度；`0x80` 這個 bit 有設表示是**資料夾**。
2. 名字（ASCII，沒有結尾的 `0`）。
3. 如果是資料夾：再一個 u16，子資料夾編號（`0xF000 + 索引`）。

同一個資料夾裡的檔案，編號從主表的「第一個檔案編號」開始，**每遇到一個檔案就加 1**（資料夾不佔檔案編號）。

## C# 提示

- `File.ReadAllBytes` 整個讀進來，用 `ReadOnlySpan<byte>` 切片：`rom.Slice(fntOffset, fntSize)`。
- 讀整數：`BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0x40))`、`ReadUInt16LittleEndian(...)`（`System.Buffers.Binary`）。
- 從根目錄開始**遞迴**走訪子表最自然：走到資料夾項目就用子資料夾編號遞迴，路徑往下接。
- `ReadOnlySpan<T>` 是 `ref struct`：不能存在 class 的欄位、不能放進 lambda、`async` 方法或 `yield` iterator。要嘛用一般方法把 span 傳下去，要嘛把 `byte[]` 存起來、用到時再 `.AsSpan()`。
- 建議位置：之後的 `Rush.Core/Rom/`，例如一個 `NitroFs` 類別，對外提供「檔案清單」和「用路徑或編號取得檔案內容」。第二課就會用到後者。

## 自我檢查

- 美版要列出 **2245** 個檔案。
- 裡面要有：`narc/z11_map.narc`、`bg/sp_fix_base.bbg`、`ss/ss_1.sst`、`act/ac_spst.bac`。
- 數字對不上的話，想想「FAT 的項數」和「有名字的檔案數」是不是同一件事。

<details>
<summary>參考答案（寫完再看）</summary>

美版（遊戲代碼 `ASCE`）：

| 項目 | 值 |
| --- | --- |
| FNT | `0x1AF000`，大小 `0xA136` |
| FAT | `0x1B9200`，大小 `0x4638` → **2247 項** |
| 資料夾數 | 62 |
| 根目錄第一個檔案編號 | **2** |
| 有名字的檔案 | **2245** |

FAT 比有名字的檔案多 2 項：編號 0、1 是 **overlay**（ARM9 動態載入的程式碼），它們由標頭另一張 overlay 表指向，在 FNT 裡沒有名字。

| 檔案 | 編號 | 大小（bytes） |
| --- | --- | --- |
| `act/ac_spst.bac` | 203 | 65360 |
| `bg/sp_fix_base.bbg` | 997 | 5164 |
| `narc/z11_map.narc` | 1876 | 69132 |
| `ss/ss_1.sst` | 1999 | 6636 |

第一個檔案是 `act/ac_dec_algae.bac`（#2），最後一個是 `talk/talk_demo_zone_son7_spa.btk`（#2246）。

</details>

## 學到的東西

<!-- 寫完後由你補：遇到的坑、做法、之後想改的地方 -->
