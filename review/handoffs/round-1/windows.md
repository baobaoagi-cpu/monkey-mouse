# Round 1 / ASUS

## 2026-09-13 Windows 獨立準備更新（最新狀態）

本節優先於下方歷史紀錄。父提交 a38de69209938dc5fd2864f2e87e53efd01c6cc3；程式基準仍為 6958594d61217aa47a61fe11649e3570283180fb。本次只有文件變更；Round 1 仍為 **BLOCKED**，未進入第二輪。使用者要求 Windows 先獨立完成能做的準備，本次執行既有工具版本查詢與唯讀 OS 顯示採集。

| Case | 主機／時間（+08:00） | 命令或方法 | 結果 | 狀態／本地證據 |
| --- | --- | --- | --- | --- |
| R1-W16 | ASUS；2026-09-13 14:45–14:48 | Codex bundled runtime discovery；既有 python.exe -I -B 查詢版本及標準函式庫；Git Bash 以程序限定 python3 function 查版本 | 已有 Python 3.12.14 AMD64；pathlib、json、xml.etree.ElementTree 可用；Bash 可呼叫。無新增安裝或全域 PATH 變更 | PASS；local-only/round-1-python-validation.json |
| R1-W17 | ASUS；2026-09-13 14:48 | 已讀 collector 的本地唯讀衍生採集器：專用執行緒 PMv2 EnumDisplayMonitors/GetMonitorInfoW；與 EnumDisplaySettingsExW current mode 比對 | 兩個啟用螢幕的像素位置與尺寸逐項相同；外接在左、筆電在右，水平邊界相接且垂直重疊，並非鏡像。current mode 是目前桌面模式，不等於面板最大原生解析度 | PASS；local-only/round-1-displays-independent.json；local-only/collect-display-independent.ps1 |
| R1-W18 | ASUS；2026-09-13 14:48 | EnumDisplayDevicesW EDD_GET_DEVICE_INTERFACE_NAME 與 WmiMonitorConnectionParams 完整 instance 配對 | 每個輸出恰好匹配一個 Active 裝置；INTERNAL 對應 WindowsLaptop，外接輸出對應 WindowsExternal。沒有以 Primary 猜角色 | PASS；同上及 local-only/round-1-monitor-connections.json |
| R1-W19 | ASUS；2026-09-13 14:48 | GetScaleFactorForMonitor 與 GetDpiForMonitor，返回碼皆成功 | 確認兩屏使用不同縮放；preliminary inventory 的座標空間缺口已在 OS 層補足。精確縮放／座標只留本地；不能自動換算成引擎 MouseScale | PASS（OS 層）；local-only/round-1-displays-independent.json |
| R1-W20 | ASUS；2026-09-13 14:48 | git check-ignore 本次採集器與三份本地證據 | 全部命中既有 local-only 排除規則 | PASS；本機命令結果 |

採集器只在新建的專用執行緒暫設 DPI awareness，finally 回復原 context；沒有修改顯示設定、解析度、縮放或 Registry。首次以 EnumDisplayDevices 預設 DeviceID 對應 WMI instance 時，檢查拒絕不同識別格式；改用 device-interface-name flag 後才完成完整 instance 配對，未以中間結果宣稱 PASS。

Python 使用 Codex 已有 bundled runtime，未下載第三方 Python。後續可在受控 Bash 程序定義 python3 function 指向該 executable，不修改 review/test.sh 或系統 PATH。函式只在版本驗證程序存在，未宣稱全系統已有 python3 命令。

### 剩餘工作與責任

- **Windows／WAITING_APPROVAL**：restore/build/離線測試尚未執行。SDK、Python 缺項已解除，但 Windows 編譯與回歸仍無 PASS；引擎 CLI 的螢幕 ID、啟動程序 DPI awareness 與 MouseScale 尚待驗證。目前只有 OS inventory 已核對。
- **Mac 整合／BLOCKED**：讀本次 Windows 提交與既有 Mac 4c55a644c4c9a45eeb47523da135ce84d1f76bba 報告，更新 decision，移除 Windows SDK／Python／OS 顯示角色與座標空間缺項；保留引擎層驗證、雙端私下四角色座標對齊及可信 LAN 確認。
- **雙端／NOT_RUN**：正式身份、native-store、socket/LAN listener、真實鍵鼠與剪貼簿均未執行。沒有調整 runtime gate、ShareMouse、配對密碼、加密、權限或防火牆，沒有重啟 Windows。
- **使用者**：目前不用向 Mac 轉貼長篇訊息。下一次通知 Mac 時，提供本次報告提交 SHA 即可；本節不是要求立即進入第二輪。

### 給 Mac 的唯一下一步

請讀本次完整提交 SHA 的 Windows 報告，更新同輪 decision，明列 Windows 已備妥的工具與 OS 雙屏資料；整理剩餘引擎層驗證及私下資料對齊的具體工作。不要重複要求安裝 Python 或 SDK，也不要把四屏連線標為 PASS。


## 2026-09-13 SDK 準備更新（目前狀態）

- 狀態仍為 **BLOCKED**，但 Windows SDK 缺項已解除。以下更新優先於後文保留的首次盤點；沒有進入第二輪。
- 已讀 Mac 報告 SHA：`4338610592b2779d71a64a492c68351858fe8933`；已讀整合 decision SHA：`4c55a644c4c9a45eeb47523da135ce84d1f76bba`。Mac 已確認內建與 BenQ 雙屏在線，舊的「BenQ 未接」不再列為現況。本端依 Git 報告確認，沒有遠端重測 Mac。
- 本次更新的父提交：`cec7d323d80ccbaa20173ba5ad73bcc530769577`；程式基準仍是 `6958594d61217aa47a61fe11649e3570283180fb`，產品程式未變更。本更新提交 SHA 由 Mac decision 引用。
- 使用者於 2026-09-13 在本機對「下載官方 .NET SDK 10.0.401 x64、驗證 SHA-512、隔離解壓與版本核對」明確回覆批准；不涵蓋其他工具安裝、restore/build/test、native store 或 listener。

| Case | 主機／時間 | 命令或步驟 | 實際結果 | 狀態 | 本地證據 |
| --- | --- | --- | --- | --- | --- |
| R1-W12 | ASUS；2026-09-13 14:40–14:41 +08:00 | git status、git check-ignore、Get-PSDrive、Test-Path；讀官方 metadata | 起始工作樹乾淨、目標目錄及ZIP不存在、磁碟空間足夠；工具目錄受ignore保護；官方版本與預期hash一致 | PASS | 本機前置查詢；未公開個人路徑 |
| R1-W13 | ASUS；2026-09-13T14:40:22+08:00 | Invoke-WebRequest 官方ZIP；Get-FileHash -Algorithm SHA512 | 300608304 bytes；實檔SHA-512與官方metadata及Mac批准卡一致，通過後才解壓 | PASS | local-only/round-1-sdk-download.json |
| R1-W14 | ASUS；2026-09-13T14:41:10+08:00 | Expand-Archive 至隔離目錄；明確路徑 dotnet.exe --list-sdks／--list-runtimes | SDK 10.0.401；NETCore／AspNetCore／WindowsDesktop Runtime 10.0.12 | PASS | local-only/round-1-sdk-validation.json |
| R1-W15 | ASUS；2026-09-13T14:41:10+08:00 | Get-AuthenticodeSignature 隔離dotnet.exe；系統dotnet --list-sdks；git check-ignore | dotnet.exe 簽章 Valid；系統原有dotnet仍無SDK；下載檔、解壓目錄與證據未納入Git | PASS | 同上；僅驗證dotnet.exe簽章，不宣稱逐檔簽章審計 |

官方來源：

- https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip
- 已驗證實檔 SHA-512：`24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430`（公開官方套件摘要，不是裝置指紋）。

新增的一般本機檔案只在 `local-only/tools/` 與 `local-only/round-1-sdk-*.json`。SDK 目錄為 `local-only/tools/dotnet-10.0.401/`；未覆蓋既有目錄。僅在版本查詢程序設定 CLI telemetry／首次體驗／開發憑證產生抑制變數及本地 CLI_HOME，不改全域PATH，不更動系統.NET，不需要管理員或重啟。未安裝Python、未restore/build/test、未執行專案CLI、未寫DPAPI／Keychain、未配對、未啟動controller／listener、未改ShareMouse／權限／防火牆。新增目錄保留供後续已授權工作使用，無系統變更需回復。

剩餘阻礙：Python命令尚未準備、Windows顯示角色／DPI空間獨立核對、雙端資料私下對齊與可信LAN确认仍未完成。SDK準備通過不等於Round1通過；Mac decision的STOP仍有效。

給 Mac 的唯一下一步：請讀取本次 Windows 更新的完整提交 SHA，更新同輪 `decision.md`，將 SDK 缺項改為已解除，保留上述其餘阻礙，提出下一個具體準備動作；不要進入第二輪或把本輪記成 PASS。

## 首次盤點紀錄（2026-09-13 13:14；下列缺項以更新節為準）

- 狀態：BLOCKED（工具與雙端資料前置門檻未完成）；作者隱私確認已完成，可發布本報告。
- 本報告提交 SHA：由整合 decision 引用本報告的完整提交 SHA，避免自引用 SHA 循環。
- 本輪輸入／計畫 SHA：`97a4fbfdfeb8943ce1d062b27547afce7b8d2490`。
- 受檢原始碼基準 SHA：`6958594d61217aa47a61fe11649e3570283180fb`。本輪未建置任何產物。
- 原 Windows 交接 SHA：`98db7f228723804ca4122d15fdfe92b688d07a93`，原分支保留，未合併或覆寫。
- 工作分支：[`codex/windows-round-1`](https://github.com/baobaoagi-cpu/monkey-mouse/tree/codex/windows-round-1)；無 PR。
- 整合分支／PR：無；本次 fetch 後未找到 `origin/integration/round-1`，待 Mac 建立。
- 執行主機：ASUS；採集時間：2026-09-13T13:14:09+08:00。
- 使用者批准範圍：2026-09-13 明確要求依 THREE-ROUND-EXECUTION.md 從第一輪執行並提交回報。據此執行第 5 節的唯讀環境盤點，以及先閱讀完整內容後執行該節明列的唯讀 collector。此授權不涵蓋第 3 節要求分別確認的工具安裝、restore/build/test、native store、listener 或輸入控制。
- 未批准／未執行：安裝工具、NuGet restore、build、所有專案測試、review CLI、DPAPI self-test、身份／配對、socket／LAN listener、鍵鼠控制、權限／防火牆變更、ShareMouse 操作及重啟。未進入第二或第三輪。

## 環境與最小工具準備

| 項目 | ASUS 本輪實測結果 | 限制 |
| --- | --- | --- |
| OS／架構 | Windows 11 家用版，Build 26200，x64 | 本機 CIM 查詢 |
| Git | 2.53.0.windows.2 | 現有工具 |
| Git Bash | 5.2.37 | 現有工具 |
| .NET SDK | `dotnet --list-sdks` 無輸出 | 目前 dotnet 可見範圍沒有 SDK；未聲稱搜遍所有自訂位置 |
| .NET Runtime | NETCore、AspNetCore、WindowsDesktop 均為 10.0.0 | Runtime 不能取代 SDK |
| Python | Get-Command 未找到 python、py、python3 | 此輪命令路徑查詢；不是全磁碟搜尋 |
| 顯示器 | OS API 成功列出兩個啟用中的顯示器 | preliminary；角色對映與 DPI 座標空間尚未完成獨立核對 |
| 網路 | 現有 Wi-Fi 有 Internet connectivity，類別 Public | 不等於兩端可信 LAN／UDP／socket 驗證 |
| 防火牆 | Domain／Private／Public 均啟用 | 未改規則，未開 listener |

第二輪前的最小工具準備方案（WAITING_APPROVAL，尚未下載或安裝）：

1. 官方 Microsoft .NET SDK **10.0.401 x64**；保持 global.json 不變。準備階段先核對官方實際可取得套件、來源與完整性，再提出使用者範圍的隔離工具目錄方案；優先僅在專用程序指定 DOTNET_ROOT／PATH，不修改全域 PATH，不預設要求管理員或重啟。精確來源、大小與雜湊須在安裝批准卡補齊，缺此資料不執行。
2. 可從 Git Bash 呼叫的 Python 3。先核對現有可信工具位置；若確實缺少，再從 python.org 核對正式 Windows x64 版本及 user-scope 安裝方式，提出明確版本與 PATH／磁碟影響後取得批准。本輪未選定或下載 Python 套件，不捏造已驗證版本。
3. 保留現有 Git／Bash。Node、npm、Rust、C++ 不列入這條 C# review 流程的新增必要工具。
4. 工具準備批准不等於 restore/build/test 批准。另行說明 NuGet 網路下載、repo-local cache／產物、scratch lock 與測試子程序，再執行受控入口；native store 與 socket 測試另行處理。

## 第一輪驗證表

以下實測均在 ASUS，輸入 SHA 與時間見報告開頭；精確採集時間、命令輸出及個人資料只保留於 local-only。引用項不標成 ASUS PASS。

| Case | 命令或手動步驟 | 預期 | 實際 | 狀態 | 證據 |
| --- | --- | --- | --- | --- | --- |
| R1-W01 | git status --porcelain；git fetch origin；git rev-parse origin/main；git diff --name-only 基準 origin/main | 乾淨、確切基準、新變更可解釋 | 起始乾淨；main 僅新增 THREE-ROUND-EXECUTION.md；沒有新程式變更 | PASS | 本輪來源 SHA；原始碼 diff 僅 1 個計畫文件 |
| R1-W02 | git switch -c codex/windows-round-1 計畫SHA | 保留既有分支並建立本輪分支 | 成功；沒有 reset、合併或覆写 handoff | PASS | 本機分支紀錄 |
| R1-W03 | git --version；bash --version；dotnet --list-sdks／--list-runtimes；Get-Command | 更新真實工具盤點 | 已取得上表；SDK／Python 缺項確認於命令可見範圍 | PASS | local-only/round-1-environment.json |
| R1-W04 | 檢查 SDK10.0.401 與 python3 是否可用 | 第二輪具備工具 | 指定 SDK 與 python3 目前不可用；未安裝 | BLOCKED | R1-W03；工具準備待批准 |
| R1-W05 | 阅读後執行 review/four-screen/collect-windows-readonly.ps1 | 成功唯讀列舉目前顯示器 | 列出 2 個顯示器；腳本正常完成 | PASS | local-only/round-1-displays.json；此 PASS 僅列舉成功 |
| R1-W06 | 獨立核對角色、原生來源座標、DPI awareness、引擎 ID 與 scale | 可解釋且可對齊的資料 | collector 明列 coordinateSpace UNCONFIRMED，AssignedRole 未指定；未跑 engine CLI；尚不能宣稱 engine inventory 驗證通過 | NOT_RUN | preliminary inventory；不得用 Primary 或 DPI/96 推定角色／MouseScale |
| R1-W07 | Get-NetConnectionProfile；Get-NetFirewallProfile | 唯讀盤點 | Wi-Fi 已連線、Public；全部防火牆啟用 | PASS | local-only/round-1-environment.json；不是連通性 PASS |
| R1-W08 | 使用者確認可信 LAN；雙端四角色資料對齊 | 四屏皆在線且雙端基準一致 | 本輪未取得 Mac 新報告；未取得可信 LAN 確認或私下資料對齊 | BLOCKED | 等待 Mac round-1 報告及使用者確認 |
| R1-W09 | git check-ignore local-only/round-1-displays.json local-only/round-1-environment.json | 個人資料排除公開提交 | 兩檔均被既有 ignore 規則涵蓋 | PASS | check-ignore 命中；僅 stage 報告 |
| R1-W10 | 使用者核對 GitHub Settings → Emails；git config --local；git var GIT_AUTHOR_IDENT／GIT_COMMITTER_IDENT | 符合第 4.2 節才提交 | 使用者於本輪後續明確確認自己的 noreply 並授權本次作者／提交者使用；已套用 repo-local 設定並核對兩者一致 | PASS | 使用者確認與本機比對；不在報告刊出地址 |
| R1-W11 | restore／build／test／native-store／loopback／LAN／H01–H11 | 需各自授權及前置門檻 | 本輪全部未執行 | NOT_RUN | 無執行結果、無產物 |

## 引用而非本機驗證

- 計畫文件說明 Mac 最近未偵測到 BenQ；本輪 Windows 沒有直接讀取 Mac，不能確認其最新顯示器狀態。
- VALIDATION.md 的 661 回歸／14 靜態檢查是 Mac 既有記錄，不是 Windows 測試，也不是實體四屏驗收。
- 控制器仍封鎖啟動。本輪沒有變更 gate 或執行任何專案二進位。

## 改動、公開審查與停止門檻

- 實際改動：新增這份去識別化報告；建立本機分支與被忽略的 local-only 證據檔。沒有修改產品程式。
- 實際工具／網路／儲存／權限變更：只使用既有工具、Git fetch 與本機一般證據檔；未安裝、未修改受保護儲存或任何系統／ShareMouse 設定。
- 公開資料審查：報告不含內網 IP、SSID、序號、hostname、使用者絕對路徑、指紋、密碼或 raw inventory。使用者後續已確認 noreply，作者／提交者均核對一致；僅提交此報告，不納入 local-only 證據。repo-local Git 作者設定已按授權更新，全域設定未變更。
- 本地證據：local-only/round-1-environment.json、local-only/round-1-displays.json；不公開原始內容。
- 回復狀態：無系統變更需回復；本機工作分支、報告及忽略的證據檔保留。

| Blocker | Owner | 最小解除條件 | 使用者決定 |
| --- | --- | --- | --- |
| 指定 SDK／python3 缺少 | Windows／使用者 | 完成精確官方工具准备批准卡並取得安裝授權；若找到既有可信工具則先核對 | 安裝需批准 |
| 角色／座標／縮放尚為 preliminary | Windows | 以唯讀 OS API 獨立核對顯示來源與 DPI 座標空間；CLI 在獲准建置後再驗證 | 後續 build 另批 |
| Mac BenQ 現況與同一基準未對齊 | Mac／使用者 | Mac 提交本輪真實報告，四屏在線及角色來源可解釋 | 接妥螢幕及必要確認 |
| integration/round-1 尚未建立、缺 Mac decision | Mac | 建立整合分支、引用雙方完整報告 SHA，發布 GO_NEXT_ROUND 或 STOP | 不推定 main 合併授權 |
| 可信 LAN 尚未取得明確確認 | 使用者／雙端 | 確認兩台在同一可信 LAN，不修改網路類別或防火牆 | 需要 |

## 給 Mac 的唯一下一步

請讀取 Windows 回報的完整 40 位提交 SHA（不要只依分支名），確認四屏資料缺口與工具準備授權，提交本輪 Mac 報告並發布 `review/handoffs/round-1/decision.md`。目前 Windows 為 BLOCKED，不可視為第一輪通過或開始第二／三輪；如果缺任一端報告、基準共識或必要批准，decision 應 STOP 並列 owner 與解除條件。
