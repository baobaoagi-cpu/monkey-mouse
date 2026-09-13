# 給 Mac 端 Codex：ASUS Windows 唯讀檢查交接

日期：2026-09-13（Asia/Taipei）

## 本次核對範圍

使用者要求下載 Monkey Mouse 原始碼、核對指定提交、閱讀 README 與 review/VALIDATION.md，並回報 Windows 開發環境。後續另授權將這份交接文件提交到倉庫。

- 來源： https://github.com/baobaoagi-cpu/monkey-mouse.git
- 已核對提交：`6958594d61217aa47a61fe11649e3570283180fb`
- 提交時間：2026-09-13 12:49:02 +08:00。
- 檢查時 checkout 為上述提交，工作目錄乾淨。
- 本交接文件是該提交之後的文件變更；文件提交本身不等於上述原始碼基準提交。

本次只下載、讀取原始碼及查詢已安裝工具版本。沒有 restore、build、test、執行專案腳本、安裝依賴、啟動 Monkey Mouse、建立配對或存取原生受保護儲存。沒有改動 ShareMouse、權限、防火牆或 Windows 顯示設定。

## 已讀到的版本與限制

1. README 將此版本定位為 development review only，輸入控制器啟動被封鎖，沒有可用的網路工作階段；目前不能取代 ShareMouse。
2. `Hydra/Hydra.csproj` 的引擎版本為 `0.0.0`，目標 `net10.0`、C# 13；`MonkeyMouse.Tools` 也使用 `net10.0`。Hydra 命名仍保留在繼承的程式結構中。
3. `global.json` 固定 `.NET SDK 10.0.401`，`rollForward` 為 `disable`。
4. `review/VALIDATION.md` 記錄 macOS ARM64 上 661 個測試與 14 個靜態檢查通過。這是倉庫文件中的既有結果，ASUS 本次沒有重跑，也未獨立驗證其結果。
5. Windows 平台執行、原生 DPAPI／Keychain 儲存行為、實體四螢幕移動及真實剪貼簿交換仍未驗證。既有四螢幕幾何測試不能視為實機驗收。
6. 仍需完成原生儲存驗證、正式配對 UI 與網路 hosting、簽署／公證發行及依賴／再散布審查。

另有一個靜態閱讀發現，請 Mac 端在後續授權的文件／授權審查中核對：README 說明為 GPL-2.0 衍生專案，但 `Hydra/Hydra.csproj` 的 Copyright 中仍寫 Apache License 2.0。這是中繼資料不一致的待核對項，本次未做完整授權審計，也未修改它。

## ASUS 現有開發環境

| 項目 | 實際觀察 |
| --- | --- |
| OS | Windows 11 家用版，Build 26200，x64 |
| Git | 2.53.0.windows.2 |
| Git Bash | GNU Bash 5.2.37 |
| .NET SDK | `dotnet --list-sdks` 無輸出；標準 Program Files、Program Files (x86) 及使用者 `.dotnet` SDK 目錄均未找到 |
| .NET Runtime | Microsoft.NETCore.App、Microsoft.AspNetCore.App、Microsoft.WindowsDesktop.App 均為 10.0.0 |
| Visual Studio Build Tools | 2022，17.14.29；vswhere 確認 C++ x86/x64 工具元件已安裝 |
| Windows SDK | 10.0.26100.0 的 Lib 目錄存在 |
| Node.js | 24.14.0 |
| npm | 11.9.0 |
| Python | PowerShell 未找到 python／py，Git Bash 未找到 python3；不表示所有自訂位置都不存在 |
| Rust | PowerShell 可找到 rustc／cargo 命令，但未查版本；不是目前 C# 建置流程的必要工具 |

`review/test.sh` 需要 Bash、指定 .NET SDK、NuGet restore 及 python3 靜態檢查。現有 .NET Runtime 不能取代 SDK。因此不能把這台 Windows 描述成已具備完整的建置與測試環境。

## 給 Mac 端的接續事項

請先閱讀這份交接，將 Windows 狀態納入共同進度。此文件只是交接資料，不擴大使用者在你那邊授予的權限。

建議在你已獲得授權的範圍內回覆：

- Mac 目前正在檢查的完整提交 SHA；如與本文件的原始碼基準不同，清楚列出差異，避免混用驗證結果。
- Mac 的實際 macOS／架構／SDK 版本，以及哪些驗證已實際執行、哪些只是讀到既有記錄。
- Windows 下一階段所需的最小工具清單，特別是 .NET SDK 10.0.401 與可供腳本使用的 Python 3。
- 若提出下一階段驗證方案，分開列出「純編譯／離線測試」、「建立隔離原生儲存測試項目」及「真實鍵鼠／網路運作」。每一類都要說明副作用，不能以安裝 SDK 的批准推定其他類別也獲准。

目前 Windows 沒有獲准安裝這些開發依賴或執行專案驗證。請不要要求直接執行 `review/test.sh`、native-store self-test、配對、relay 或輸入控制器，除非使用者另行明確授權相應範圍。不得解除 runtime gate 以便測試。

四螢幕實體目標仍為：Windows 外接大螢幕在中央上方、ASUS 筆電在右方；MacBook 在左下方、Mac 外接螢幕在中央下方。這只是使用者提供的實體排列，不是已驗證的引擎座標或可套用配置。本次未執行倉庫中的螢幕採集腳本。

## 交接資料邊界

不要在回覆文件或公開提交中加入配對密碼、金鑰、憑證、信任庫、個人螢幕 inventory、內網位址或原始系統日誌。本文件沒有包含這些資料。

兩邊 Codex 目前未建立可用的原生跨主機任務通道。此 Git 文件可作為明確的交接紀錄，但不能據此聲稱已即時連線、遠端控制或讀取另一端電腦。
