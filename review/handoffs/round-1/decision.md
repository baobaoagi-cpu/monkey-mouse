# Round 1 / 整合決策

- 決策：**STOP**。
- Round 1 exit gate：**BLOCKED**。兩端本輪報告交換已完成，但第一輪前置門檻未通過；不得宣稱 GO_NEXT_ROUND，更不進行第二輪實作／測試或第三輪實機操作。
- 使用者決定：只有下文的一項SDK準備動作為本次最優先待批准事項，狀態 **WAITING_APPROVAL**；尚未執行。
- 計畫起點：`97a4fbfdfeb8943ce1d062b27547afce7b8d2490`。
- 程式基準：`6958594d61217aa47a61fe11649e3570283180fb`；此次兩端都只有報告變更，沒有產品程式變更。
- Windows 報告：[`cec7d323d80ccbaa20173ba5ad73bcc530769577`](https://github.com/baobaoagi-cpu/monkey-mouse/blob/cec7d323d80ccbaa20173ba5ad73bcc530769577/review/handoffs/round-1/windows.md)。
- Mac 報告：[`4338610592b2779d71a64a492c68351858fe8933`](https://github.com/baobaoagi-cpu/monkey-mouse/blob/4338610592b2779d71a64a492c68351858fe8933/review/handoffs/round-1/mac.md)。
- 本decision在`codex/mac-round-1`公開，沒有PR或main合併。`integration/round-1`本輪未建立：依此次明確範圍，兩份Mac文件先放同一Mac工作分支，不擅自合併Windows分支。本decision引用不可變SHA完成可核對交换；整合分支未建立不是技術門檻已通過的理由。

## 可比性與已解除的缺口

Windows提交的父SHA與Mac分支起點均為同一計畫SHA。Windows只有報告，Mac本輪也只新增文件；兩端引用同一程式基準。Mac已直接讀取Windows完整報告；本decision完成Mac整合回覆，不能據此假稱Windows已讀回本decision或兩端已即時連線。

Mac重新採集確認內建與BenQ都在線、非鏡像，BenQ在右且邊緣相接、有垂直重疊；原先「BenQ未接」缺口已解除，不再要求使用者重接BenQ。Mac完整原生／引擎ID、raw/advertised座標、尺寸、OS像素比例只留本機。Mac取得HydraReported，但engine default MouseScale不是游標實測校準。Windows列出兩屏仍為preliminary，無AssignedRole、CoordinateSpace未確認、未跑engine CLI。

因此現在可以說「Mac實際確認雙屏；Windows報告雙屏列舉成功」，不能說「四個實體螢幕共享已通過」。本輪沒有H01–H11、socket、原生受保護儲存或剪貼簿實測。

## Exit gate 判定

| Gate | 證據 | 狀態 | Owner與最小解除條件 |
| --- | --- | --- | --- |
| 同計畫／程式基準與完整報告SHA | 上述两份不可變提交 | PASS | 已核對；後續程式變更須更新受測SHA |
| Mac雙屏在線與本機角色／座標來源可解釋 | Mac R1-M04／M05 | PASS | 僅列舉與幾何來源；非鍵鼠驗收 |
| Windows兩屏獨立角色／空間確認 | Windows R1-W05列舉成功，但W06仍preliminary | BLOCKED | Windows：只讀核對OS角色／DPI-awareness；CLI在獲准建置後再補engine資料 |
| 雙端四角色與縮放對齊 | 尚未完成私下核對；Mac default scale非校準值 | BLOCKED | 雙端：個人資料只留本地／經同意私下交換；不得公開inventory或套用示範數字 |
| Windows指定SDK與python3 | W04缺SDK10.0.401及命令可見Python；Mac既有SDK/Python可用 | BLOCKED | Windows／使用者：固定SDK準備待批准；Python缺項仍保留，本次不一併請求安裝 |
| 同一可信LAN確認 | Windows Public類別；MacWi-Fi可達；未有使用者同LAN確認 | BLOCKED | 使用者／雙端：之後明確確認可信環境；不得直接改類別／防火牆或開listener |
| 受控網路與安全preflight | 只有既有狀態只讀盤點，未做双機傳輸驗證 | NOT_RUN | 第二／三輪前按計畫另行檢查和批准；不假設現有配置已符合候選版需求 |
| 第二／三輪驗證 | 本次授權明確只做Round1 | NOT_RUN | 保持controller gate、native store與listener未啟動 |

工具與Windows資料門檻未達成，不能因報告已提交而把Round1標PASS。SDK準備完成也只解除其中一項，仍須更新第一輪報告與decision；不能直接跑review/test.sh。

## 唯一優先請使用者批准的動作

**請批准 ASUS Windows Codex 在使用者專用、Git忽略的 `local-only/tools/dotnet-10.0.401/` 準備官方 .NET SDK 10.0.401 x64：下載官方ZIP、驗證SHA-512後解壓，以該目錄的dotnet查詢SDK版本。**

批准範圍僅此SDK準備與版本核對。不修改全域PATH、不要求管理員或重啟、不安裝Python，不做NuGet restore／build／test、不寫DPAPI／Keychain、不配對、不啟動listener／controller、不改ShareMouse、權限或防火牆。執行前若出現額外系統變更需求就停止，不能擴大批准範圍。工作目錄位置由Windows本機核對，真實路徑不公開。

### 已核對的批准卡資料

- 官方版本／平台：Microsoft .NET SDK **10.0.401／win-x64 ZIP**。
- [官方release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)及[SDK ZIP](https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip)。Mac此次只讀metadata與HTTP HEAD，沒有下載或執行Windows套件。
- 伺服器回報壓縮大小：300608304 bytes；解壓後另占空間。Windows先檢查本地可用空間，不足就停止，不刪使用者檔案騰空間。
- 官方metadata的SHA-512：`24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430`。
- 安裝前置：下載後必須以Windows `Get-FileHash -Algorithm SHA512`核對實檔；目前只有官方預期hash，**尚未實際驗證下載檔**。不符即停止、標FAIL，不執行。
- 安裝方式：純解壓至上述本機隔離目录；如果已存在，先讀版本與內容，不覆蓋。使用明確dotnet路徑，不變更全域PATH；本次只執行版本列舉確認，不以缺Python為由額外安裝。
- 回復：若準備失敗，保留原因並只處理本次專用下載／解壓目錄；先核對不存在使用者既有檔案。無系統註冊、無服務、無防火牆規則需回復。

未獲批准前，Windows維持原狀。若獲批准並完成，只更新`review/handoffs/round-1/windows.md`的SDK結果／新SHA，再讓Mac更新同輪decision；Python、角色／座標與可信LAN仍各自標未完成，不一次要求使用者執行一串操作。

## 公開資料與執行邊界

本次Mac兩份文件不含本機IP／SSID／序號／原生顯示ID／完整螢幕座標、個人路徑、指紋、密碼、金鑰或raw log。Git author／committer使用經核對的GitHub noreply；不更改既有歷史。Windows cec7d3的兩個作者地址亦已核對為noreply。

本輪未安裝工具、未更改程式、未寫入Monkey Mouse受保護儲存，未修改Windows提交、未合併main。Mac工作停止在Round1文件交付；沒有第二輪工作或背景程序繼續。
