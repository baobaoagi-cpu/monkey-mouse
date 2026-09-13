# Round 1 / MAC

- 狀態：BLOCKED（第一輪整體前置門檻未齊）；Mac 本輪只讀盤點已完成，沒有進入第二輪。
- 本報告提交 SHA：由同分支 decision.md 引用確切提交，避免自引用循環。
- 計畫／分支起點：`97a4fbfdfeb8943ce1d062b27547afce7b8d2490`，依 [THREE-ROUND-EXECUTION.md](../../THREE-ROUND-EXECUTION.md) 第5節執行。
- 受檢程式基準：`6958594d61217aa47a61fe11649e3570283180fb`；計畫提交只有文件變更。本輪沒有編譯或修改程式。
- 已讀 Windows 第一輪：`cec7d323d80ccbaa20173ba5ad73bcc530769577`；[確切報告](https://github.com/baobaoagi-cpu/monkey-mouse/blob/cec7d323d80ccbaa20173ba5ad73bcc530769577/review/handoffs/round-1/windows.md)。已由 Git fetch、commit parent、diff 核對；該提交以同一計畫 SHA 為父提交，只新增 Windows 報告，沒有新程式／測試結果。
- 工作分支：`codex/mac-round-1`；本次只提交 mac.md 及整合 decision.md，未合併 main、未修改 Windows 分支、未建立 PR。
- 使用者授權：2026-09-13 明確要求第一輪 Mac 只讀盤點、核對 Windows 交接並安全發布文件；嚴格停止在 Round 1。
- 主要螢幕採集時間：2026-09-13T13:40:52+08:00。
- 未執行：工具安裝／更新、restore/build/test、原生 Monkey Mouse 受保護儲存、身份／配對、socket／LAN listener、controller、鍵鼠／剪貼簿實測、ShareMouse 操作、系統權限／防火牆／顯示設定變更或重啟。

## 本機實際盤點

| 項目 | 本輪 Mac 只讀事實 | 限制 |
| --- | --- | --- |
| OS | macOS 26.6.2，Build 25G83 | sw_vers |
| CPU／架構 | Apple M4／arm64 | sysctl、uname |
| Git | 2.50.1（Apple Git-155） | git --version |
| Bash | 3.2.57 | 現有 shell；不新增工具 |
| Python | python3 3.9.6 | 現有命令可用；未执行專案 static check |
| .NET SDK | 10.0.401 | 使用先前已核對的隔離 SDK；目前一般 PATH 未找到 dotnet，不能讓另一端照抄私人路徑 |
| .NET Runtime | Microsoft.NETCore.App／Microsoft.AspNetCore.App 10.0.12 | 同一既有隔離 runtime 的 list-runtimes；未安裝 |
| 螢幕 | MacBook 內建與 BenQ 外接均在線，非鏡像 | OS 顯示器列舉與 engine PlatformId 本機交叉核對；舊的「BenQ 未接」狀態已過時 |
| 座標／排列 | 內建在左、BenQ在右，水平邊緣相接且有垂直重疊 | 已核對 raw 與 advertised 座標、尺寸、角色；精確值與 ID 僅在 local-only |
| CoordinateSpace | review CLI 回報 HydraReported | 不是跨機座標已對齊，也不是四屏測試 PASS |
| Scale | 兩屏 OS 像素／邏輯比例不同；engine default mouse scale 為 1.0 | OS pixel ratio 不等於 MouseScale；實際游標速度／縮放未校準，不填成實機驗證 PASS |
| 網路 | 本機 Wi-Fi 有系統可達狀態；可讀公開 GitHub | 不證明 ASUS 可達、同一可信 LAN 或雙機 TLS；未讀取／提交 SSID、IP到公開報告 |
| 防火牆 | 只讀查詢既有狀態，沒有更動 | 詳細安全設定僅本地記錄；不是安全配置或LAN運作已驗證，後續 preflight 仍需審查 |

螢幕採集使用先前已建置且來源可追溯至上述程式基準的 `MonkeyMouse.Tools`：只執行 `displays --host Mac`，未啟動 Hydra controller。現有 review CLI 的 SHA-256 為 `92d2d945bda3d6bcbee809419e63d0bedac25771371ac0dbdbae9f502c8a390e`（產物摘要，不是裝置指紋）。來源工作樹的相關程式沒有新增差異；上輪乾淨建置紀錄指向該產物。本輪沒有重新建置，也沒有將原生輸入權限授給它。

系統顯示器名稱、原生 DisplayID 與 engine PlatformId 在本機一一對照後，確認右方外接為 BenQ。原始 X/Y、AdvertisedX/Y、logical dimensions、physical pixels、OS 比例和原生 ID 均有本地記錄。重新連接或移動排列後必須再採集；不能沿用舊單螢幕名稱。兩個螢幕在線與座標列舉成功，不等於使用者曾以共享鍵鼠往返。

## 驗證表

| Case | 實際命令／步驟 | 結果 | 狀態 | 證據 |
| --- | --- | --- | --- | --- |
| R1-M01 | git ls-remote、fetch、show、diff；讀 Windows 精確SHA | Windows 第一輪 SHA 和父提交符合；只新增報告；同計畫基準 | PASS | 上述不可變 SHA／Git diff |
| R1-M02 | sw_vers、uname -m、sysctl CPU、git/python3/bash version | 完成本機環境盤點 | PASS | 本地採集結果；上表 |
| R1-M03 | 已有隔離 dotnet --list-sdks／--list-runtimes | 指定 SDK 及 runtime 可見；不在一般 PATH | PASS | 本地採集；未安裝／更新 |
| R1-M04 | system_profiler SPDisplaysDataType -json；既有 review CLI displays --host Mac | 內建與 BenQ均在線、非鏡像；角色可對照 | PASS | local-only/round-1/mac-displays-system.json、mac-displays-engine.json |
| R1-M05 | 讀取兩份 inventory，核對本機 ID、左右相接、垂直重疊、raw/advertised空間、OS與engine scale差別 | 本機列舉和幾何關係可解釋 | PASS | local-only/round-1/display-role-validation.json；不是實際滑鼠校準 |
| R1-M06 | scutil --nwi、networksetup -listallhardwareports、socketfilterfw --getglobalstate | 僅讀既有網路／防火牆狀態，不改設定 | PASS | 本地網路與安全設定記錄；非對端連通證据 |
| R1-M07 | 雙端完整 engine inventory／角色與Scale對齊 | Windows僅preliminary；Mac操作Scale未實測；尚無双端私下資料對齊 | BLOCKED | Windows R1-W06、R1-W08及本報告 |
| R1-M08 | 同一可信LAN確認、socket與實際路由 | 使用者尚未確認可信LAN；沒有listener或連通測試 | NOT_RUN | 不把Internet可達寫成LAN PASS |
| R1-M09 | git check-ignore；逐檔審查；noreply作者／提交者核對 | 原始inventory／網路資料留本機；只提交去識別化文件 | PASS | staged diff／ignore與Git提交metadata |
| R1-M10 | restore/build/test、native-store、配對、controller及H01–H11 | 第一輪以外全部未執行 | NOT_RUN | 無新測試／硬體驗收結果 |

## Windows 交接的解讀

Windows 文件自述兩個顯示器可被 OS collector 列出，但 AssignedRole 未指定、CoordinateSpace UNCONFIRMED，沒有 engine CLI 資料；不能把此項視為四角色已對齊。Windows 仍缺 SDK10.0.401及命令可見的 Python；只有 Runtime不構成建置環境。Windows Wi-Fi類別為Public，不能未經同意改類別或防火牆來通過測試。以上來自其提交，Mac沒有遠端操作ASUS或獨立重測其硬體。

661回歸／14靜態檢查是舊程式基準的既有Mac結果，本輪沒有重跑，也不是雙平台／原生Keychain/DPAPI／socket或實體四屏驗收。本輪新增的PASS僅限表中具體只讀動作。

## 改動、資料與回復

本輪產品程式變更為零。新增本機Mac工作分支、去識別化報告及被ignore的本地證據。所有本機識別資料、IP、SSID、原始系統輸出與安全設定細節未提交；未建立Monkey Mouse信任或寫入其Keychain。發布Git文件使用既有帳號登入，不改帳號設定；本輪author/committer使用已核對的GitHub noreply。

沒有系統變更需要回復。保留本地證據與分支，停止在第一輪；整體結論與唯一優先使用者決定見同目錄decision.md。
