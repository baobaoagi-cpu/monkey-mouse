# Monkey Mouse：三輪雙端交接執行計畫

**狀態：PLAN／尚未啟動本計畫。這份文件的發布不代表允許安裝、開啟網路服務、寫入信任庫或操作鍵鼠。最多三輪是挑戰目標，不是成功保證；任何退出門檻未達成就停止，公開回報 BLOCKED，不以解除保護或省略驗證趕進度。**

版本：2026-09-13。產品 Monkey Mouse；Hydra GPL-2.0 衍生。保留 [LICENSE](../LICENSE)、[MODIFICATIONS.md](../MODIFICATIONS.md) 與原作者署名。本次只新增計畫文件，沒有改程式、安裝依賴或新增測試通過結果。

## 給 ASUS Windows Codex 的直接指令

你正在接手公開倉庫 `baobaoagi-cpu/monkey-mouse`。請把本文件當作完整交接規格；不用請使用者重述 Mac 的技術要求。目標是一組 ASUS 鍵鼠自然跨四個實體螢幕，指定跨機邊界為 Windows 大螢幕底邊↔BenQ頂邊，且能接續 BenQ↔MacBook；不是再做一份開源軟體推薦。

目前 main 程式基準是 `6958594d61217aa47a61fe11649e3570283180fb`；你先前的 `98db7f228723804ca4122d15fdfe92b688d07a93` 只新增 Windows 交接文字，尚無 Windows build/test 或螢幕座標。Mac 的 661/14 不能填進你的實測欄；Mac 最近沒有偵測到 BenQ。輸入控制器仍封鎖啟動，原生儲存與真實網路控制尚未完成。

**收到使用者要求依此文件開始後，第一輪立即從以下工作進行：**

1. 先讀本文件全部章節，再依第4.1節讀取倉庫、核對乾淨工作目錄、最新 main 與上述基準的差異。不要覆蓋原來的 handoff 分支或未提交資料。回報目前精確SHA，未看懂的新程式變更先停止執行。
2. 執行第5節列出的只讀版本／命令可用性查詢，更新 Windows 工具表；用OS顯示設定確認外接與筆電螢幕各自在線。執行顯示採集腳本或CLI前，確認已獲相應只讀腳本執行授權；沒有就標 NOT_RUN，不能安裝工具或啟動 controller 代替採集。
3. 明列 SDK10.0.401、python3 的缺項與最小準備方案，所有安裝、restore/build/test、native-store、自動或手動LAN listener、輸入權限、防火牆和重啟都按第3節分別確認。不要一口氣跑第二、三輪。
4. 將第一輪去識別化報告準備成 `review/handoffs/round-1/windows.md`，使用第9節模板，逐項分清實測／引用／未測。先核對repo限定的作者noreply與staged diff；在使用者已授權交接提交的範圍內放到 `codex/windows-round-1`，提交並回報完整SHA。若整合分支尚未建立，先交自己的分支URL並標待Mac建立；不要自行合併main或把未完成項目標PASS。
5. 給 Mac 的下一步只需在同一報告列「請讀此SHA、確認四屏資料缺口與工具準備授權，發布Round1 decision」。公開不要放實際IP、序號、路徑、指紋或原始inventory。使用者只需看到文件/分支URL和必要的批准問題，不需轉貼兩端的長篇技術交談。

你對另一端的進度只能根據Git報告確認；不得宣稱已連線讀取Mac。任何阻礙都在報告寫owner與最小解除條件，保留使用者的既有共享環境。以下各節定義完整的三輪責任、交付與停止門檻。

## 1. 任務、指揮關係與成功定義

Mac Codex 為整合負責人，ASUS Windows Codex 為 Windows 實作與實測負責人。兩者只操作各自獲授權的本機；Git 文件、提交與 PR 是非同步交接通道，不代表具備跨主機控制權。使用者負責接妥四螢幕、確認系統變更與完成親手操作驗收。代理只能引用實際取得的證據；不能將另一端文字自述寫成自己完成的測試。

一「輪」是一次完整、可核對的雙邊交換：整合端發布同一個輸入 SHA 與驗收清單 → 兩端各完成任務並提交去識別化報告 → 互讀對方的確切 SHA → 整合端發布輪次結論。單方發訊息、增加提交、等候使用者都不算完成一輪。該輪內可迭代修正；結案後若需重新交换新基準，記為下一輪或明示追加輪次，不把第四輪改名成第三輪。

必須達到的實體排列與路徑：

```text
上排：[Windows 外接大螢幕] —— [ASUS 筆電螢幕]
                 ⇅ 指定跨機邊界
下排：[MacBook 螢幕]      —— [BenQ 螢幕]
```

示意不按比例。Windows 外接大螢幕底邊 → BenQ 頂邊；BenQ 往左 → MacBook；反向 MacBook → BenQ → Windows 大螢幕 → ASUS 筆電也成立。同機螢幕由各作業系統正常移動。跨機僅開放已確認的邊界，不推導額外的 ASUS 筆電↔MacBook 邊界。位置、尺寸、DPI 必須以實際採集確認，不能把示意圖當成座標。

最終 PASS 必須同時滿足：一組接在 ASUS 的鍵鼠自然跨四屏，正常切换無快捷鍵；鍵盤跟隨目前控制目標；雙向純文字剪貼簿；雙端核准、獨立裝置身份與加密；未核准或撤銷後立即拒絕控制。禁止檔案傳輸、遠端鎖定／睡眠／喚醒、螢幕保護同步。不得降低防火牆、Gatekeeper、SIP、憑證驗證或其他系統保護。受測電腦由使用者自行睡眠及喚醒後可安全重新連線，不等於允許遠端喚醒。

## 2. 基準與已知缺口

| 項目 | 文件發布時的已知狀態 | 證據與限制 |
| --- | --- | --- |
| 公開程式碼基準 | `6958594d61217aa47a61fe11649e3570283180fb` | [該提交](https://github.com/baobaoagi-cpu/monkey-mouse/commit/6958594d61217aa47a61fe11649e3570283180fb)；本計畫會另有文件提交 |
| Windows 交接 | `codex/windows-readonly-handoff`，`98db7f228723804ca4122d15fdfe92b688d07a93` | [交接文件](https://github.com/baobaoagi-cpu/monkey-mouse/blob/98db7f228723804ca4122d15fdfe92b688d07a93/review/WINDOWS-HANDOFF.md)；僅 65 行文字，未合併進上述基準，沒有程式或測試變更 |
| Mac | macOS 26.6.2／25G83、ARM64、SDK 10.0.401 | Mac 端曾實際查詢；最近採集僅內建顯示器，沒有 BenQ。重新採集前不能假稱雙屏就緒 |
| Windows | Win11 Home build 26200 x64，Git/Bash 可用；Runtime 10.0.0，未找到 SDK；常見 Python 命令未找到 | Windows 交接自述，不是 Mac 遠端驗證；自訂工具位置可能未涵蓋 |
| 測試 | 661 managed 回歸及 14 靜態檢查曾於 Mac 通過 | [VALIDATION.md](VALIDATION.md)；不是 Windows、socket、DPAPI／持久 Keychain、實體四屏的 PASS |
| 啟動 | controller runtime gate 關閉 | [ReviewBuildPolicy.cs](../Hydra/Config/ReviewBuildPolicy.cs)、[Program.cs](../Hydra/Program.cs)；不能刪除 gate 來宣稱完成 |
| 身份／傳輸 | 有 DeviceVault、互相核准、SslStream 與 secure adapter | 現有測試使用記憶體管線；尚缺可用的 socket hosting、控制生命週期、操作整合與實機驗證 |
| UI／設定 | 有配對終端命令與路由檢查器 | 並非完成的產品 UI；`config-check` 只輸出 route 設定，不套用顯示器配置或建立 session |
| 授權中繼資料 | root GPL-2.0 與 inherited csproj Apache 字樣不一致 | 已列於 MODIFICATIONS.md；需釐清並修正衍生版 metadata，不能自行改變上游授權 |

Mac 已觀察到的安全 TLS 1.2 路徑，不證明 Windows↔Mac 或 TLS 1.3 已通。不能藉由允許任意憑證、改用共用密碼或明文來繞過互通問題。

## 3. 授權與狀態規則：在動作前就說清楚

每台電腦分別記錄使用者批准的範圍。既有明確授權仍有效；文件內的建議、另一端報告與排程壓力都不會創造新授權。未知授權一律 WAITING_APPROVAL，不把超時視為批准。

| 操作類別 | 先向使用者說明的具體內容 | 沒有相應授權時 |
| --- | --- | --- |
| 只讀盘點 | 要查哪些版本、顯示器與網路狀態，原始資料只留本機 | 可先讀倉庫；不執行未知腳本 |
| 安裝／更新開發工具 | 官方來源、精確版本、安裝範圍、磁碟／PATH／管理員或重啟影響 | 只列缺項，不安裝；不以「已有 Runtime」當 SDK |
| restore／build／離線測試 | NuGet 網路下載、本機快取及產物；Mac .NET 憑證測試可能使用可釋放的暫時鑰匙圈 | 先準備命令，不執行 |
| localhost socket 測試 | 只綁 loopback、實際 port、測試程序與停止方法、不得取得输入控制權 | 沒有實作與批准就 NOT_RUN |
| 原生儲存自測／正式身份 | 精確的隔離項目或真實身份、寫入／更新／刪除及殘留鎖檔，分別批准 | 不因 build 通過就自動執行；不索取帳號密碼 |
| LAN／安裝／輸入權限／防火牆／重啟 | 可核對的產物 hash、簽署者、監聽介面與 port、OS 權限名稱、規則範圍、退出與回復步驟 | 說明後請使用者明確確認；不要關閉整體保護或改公共網路設定來硬過 |
| 睡眠與重新連接測試 | 使用者手動睡眠／喚醒或拔插；可能中斷工作；先保存工作 | 不自動鎖定、睡眠、喚醒或重啟另一台 |

結果只用 `PASS`（實際執行且符合）、`FAIL`（執行但不符）、`BLOCKED`（已知阻礙）、`NOT_RUN`（未執行）、`WAITING_APPROVAL`。規劃中的門檻標 `TARGET`，不得先填 PASS。每個 PASS 都要有主機、精確 SHA、命令／步驟、時間、結果與證據；缺一則不算驗收。

## 4. Git 交接與公開資料規則

### 4.1 分支、責任與合併

- 每輪整合分支：`integration/round-1`、`integration/round-2`、`integration/round-3`。Mac 從雙方同意的乾淨 main SHA 建立；若已存在先檢查，不能覆寫。
- 各端工作分支：`codex/mac-round-1`、`codex/windows-round-1`，後續輪次類推。報告路徑：`review/handoffs/round-1/mac.md`、`windows.md`、`decision.md`，後續輪次類推。這些是本計畫約定，尚未建立。
- Windows 擁有 Windows 平台實作與報告；Mac 擁有 Mac 平台與整合報告。共用傳輸、協定、CLI、設定格式先由 Mac 發布介面約定，再分工，避免雙端各自改同一協定。
- 兩端 PR 指向該輪整合分支，互讀 diff／測試證據；Mac 整合並在相同候選 SHA 驗證。需要 Windows 重驗時，提供新 SHA，不混用舊二進位。最後用 PR 回 main，合併依當前使用者授權進行。
- 本計畫的文件提交不合併 Windows 交接分支，也不改寫歷史。衝突時先停下、列出衝突檔及原因，由責任端處理並重跑受影響檢查；禁止盲目 `ours/theirs`、force push、`reset --hard` 或清除別人的工作。

新 clone 的只讀起點（PowerShell 與 macOS shell 均可逐行執行）：

```sh
git clone https://github.com/baobaoagi-cpu/monkey-mouse.git
cd monkey-mouse
git status --short
git fetch origin
git rev-parse origin/main
git log -5 --oneline origin/main
git show origin/main:review/THREE-ROUND-EXECUTION.md
```

已有 clone 就從 `git status --short` 開始。若有未提交工作，保留並回報；不要直接切換或覆蓋。上面的 HEAD 查詢只是讀取，不替代明確的輪次基準。開始一輪前把完整 40 位 SHA 放進兩端報告。

### 4.2 作者隱私與提交前審查

Git Author／Committer email 也會公開。既有歷史已出現個人 email；改目前設定不會清除歷史。是否清理歷史另由使用者決定，不能擅自 force push 或刪除分支。

未來提交只在該 repo 設定經帳號 GitHub Settings → Emails 顯示、由使用者確認的 noreply 地址，不能猜測地址或更改全域設定。可準備以下命令，替換占位值後才執行：

```sh
git config --local user.name "Monkey Mouse Development"
git config --local user.email "REPLACE_WITH_CONFIRMED_GITHUB_NOREPLY"
git var GIT_AUTHOR_IDENT
git var GIT_COMMITTER_IDENT
```

最後兩行可能顯示個人資訊，只在本機核對、不貼公開日誌。使用 connector／網頁提交也必須核對其實際 author/committer，不能假設它會採用本機 git config。若無法控制作者隱私，先完成本地文件並回報阻礙，勿為方便再公開個人地址。

本機原始 inventory、hostname、序號、MAC／內網 IP、SSID、真實路徑、指紋、邀請、信任庫、金鑰、憑證、剪貼簿內容與 raw log 放於已忽略的 `local-only/`，永不公開。`.review-cache/`、`.review-results/` 也只留本機。提交前執行 `git check-ignore local-only/檔名` 確认；已追蹤檔案不受 ignore 保護。

只提交明確指定的去識別化報告與程式：`git add review/handoffs/round-1/windows.md` 為例；不要用 `git add .`。再看 `git diff --cached --name-status`、`git diff --cached --check` 與完整 staged diff。公開報告用 `MAC`／`ASUS`、顯示器角色、狀態、版本與測試計數，不放實際 inventory。需要共享真實座標時用使用者同意的私下傳遞方式；公開只說「本地已核對四角色」，不要為了重現貼出整套本機資料。掃描命中須人工判斷：SDK 版本不是 IP；既有合成測試輸入不是實際密碼。

## 5. 第一輪：建立雙端真實前置資料與可行基線

**输入：** 本文件 SHA、程式碼基準 SHA、Windows 交接 SHA、使用者對讀取項目的同意。**負責：** 各端本機盤點；Mac 匯總。**本輪不操作真實鍵鼠或 LAN listener。**

### Mac 任務

1. 讀完 Windows 交接並確認「尚無 Windows build/test」；實際查詢 `sw_vers`、`uname -m`、已安裝 `dotnet --list-sdks`（若不在 PATH，僅使用已核對的本機 SDK 路徑，不公開路徑）。
2. 讓使用者接妥 BenQ；不要代改顯示設定。只讀確認是否真的同時偵測 MacBook、BenQ，左右相接與有垂直重疊。未連接則 BLOCKED，不能複製範例尺寸補上。
3. 對已建置且 hash 可核對的 review CLI，可在只讀執行範圍獲准後使用下方 `displays`。若尚無可核對產物，先用 OS 顯示設定讀取；原始 `system_profiler SPDisplaysDataType -json` 可能含序號，僅可本地留存。

### ASUS 任務

1. 只讀版本盤點：`git --version`、`bash --version`、`dotnet --list-sdks`、`dotnet --list-runtimes`；PowerShell 用 `Get-Command dotnet,python,py,python3 -ErrorAction SilentlyContinue` 查是否找到命令，不公開其個人路徑。
2. 最小工具需求：精確 .NET SDK **10.0.401**（`global.json` 不允許 roll-forward）、可由 Git Bash 呼叫的 `python3`、既有 Git/Bash。Node/npm/Rust/C++ 工具不是這條 C# review 建置流程的新增必要工具。安裝若需進行，先列官方來源與影響讓使用者確認；本文件不自動批准安裝。
3. 確認 ASUS 外接大螢幕及筆電顯示器均啟用。可以讀 Windows 顯示設定；既有 collector 可在閱讀內容及取得腳本執行同意後於普通 PowerShell 執行：

```powershell
& ./review/four-screen/collect-windows-readonly.ps1
```

此 collector 使用 Add-Type 及顯示列舉 API，輸出為 preliminary／DPI-awareness 未確認，不是可直接套用的 engine inventory。若 execution policy 阻擋，回報，不加 bypass 參數或降低政策。Primary 不是筆電判斷依據；HMONITOR 不是永久 ID。未建置 CLI 不准假稱有 engine screen ID。

### 雙端資料對齊與網路盤點

在各端既有 review CLI 可用、並獲准只讀執行時，從 repo root 使用：

```sh
# Mac
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll displays --host Mac
# ASUS
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll displays --host ASUS
```

不可改跑 `Hydra.dll`／Styx／WebConfig 來採集。分清 raw X/Y 與 AdvertisedX/Y、logical bounds 與 physical pixels、OS DPI 與 `EngineDefaultMouseScale`（目前固定引擎預設 1.0，不是量測的 OS 縮放）。輸入 `measurements.template.json` 的 X/Y 必須同一主機同一座標空間；四角色 mapping、縮放、引擎 ID、主機間路由要逐項核對。連接／移除顯示器或改排列後重採，單屏 `Mac` 可變成 `Mac:0`，不能沿用舊 ID。

網路只查現有介面狀態、Windows network category／防火牆是否啟用，以及使用者能否確認兩端位於同一可信 LAN。Windows 可本地閱讀 `Get-NetConnectionProfile`；不要公開其 Name／SSID，不能改 network category 以取得通行。Mac 從網路設定只讀查連線。公開只記「可信LAN條件待確認／已確認」，實際地址留本機。這不是連通性 PASS：本輪不掃描子網、不改路由、不設定 port forwarding、不開 listener；稍後針對已知端點測試才證明可達。

**交付：** `round-1/mac.md`、`windows.md`；環境表、工具缺項與安裝需求、兩端顯示數及資料是否齊、私下資料對齊是否完成、網路條件、批准範圍。Mac 產出 `decision.md`，明列每個 blocker 的 owner 與解除条件。

**退出門檻：** 四個實體螢幕均在線、角色及座標來源可解釋；需要的工具已在相應授權下準備好或其缺失明確阻擋第二輪執行；兩端同意同一基準和介面約定。只缺「engine CLI 尚未建置」可將 OS Preliminary 狀態明確列為第二輪前置驗證，不能標成座標已驗證。BenQ 未接、未知授權、無法取得必要工具或無可信網路路徑時，本輪 BLOCKED；可以先寫程式方案，但不得宣稱進入實機驗收。

## 6. 第二輪：交付受控可運行候選版，而非只讓測試變綠

**输入：** 第一輪結論、雙端資料與核准範圍、精確候選基準。**本輪可開發／建置的授權須另行成立。** Mac 擁有共用介面設計；Windows 驗證原生 Windows 行為。目標是提出可供第三輪審核的候選產物，不預先解鎖日常控制器。

### 必須實作或補齊的內容

| Owner | 工作 | 必須交出的證據 |
| --- | --- | --- |
| Mac 主導、Windows 互審 | 可停止的 socket hosting，連接 timeout、重試退避、連線／斷線狀態；第一版優先手動指定端點，不以未完成 discovery 阻塞核心 | 新增命令的真實 `--help`／程式入口、bind 範圍、關閉程序、loopback 測試。Discovery 如有，只能提供候選位置，不能成為信任來源 |
| 兩端 | 現有雙端指紋配對操作接到明確產品流程；權限、取消、過期、撤銷、身份重建及 key rotation 策略 | 本機核對完整指紋，未批准 peer 永不接收控制；提示不能要求帳號密碼或複製私鑰 |
| 兩端 | Keychain／CurrentUser DPAPI 正確寫入、重開、撤銷持久化；vault lifetime lock 與 active session 撤銷整合 | 隔離儲存自測結果；重開／失敗／競爭程序測試；不能從 memory mock PASS 推導 native PASS |
| Mac | CG 顯示器座標、native input／clipboard、權限失效退出、簽署與公證路径 | 平台檢查與簽署產物計畫；未取得 entitlements／公證所需条件則報 blocker，不關閉保護 |
| Windows | Windows input、顯示器 DPI-awareness、DPAPI、UI 啟動／停止與錯誤恢復 | 同一候選 SHA 下的 Windows build／離線／loopback 結果，不能只照抄 Mac 結果 |
| 共用 | 把 route fragment 接成可審核的完整每屏設定及 session lifecycle；只允許指定跨機邊界 | synthetic mixed-DPI／負座標／邊界點／重新列舉測試；以本機實際資料只做本地校驗 |
| 共用 | 純文字 clipboard、按鍵 key-up 清理、滑鼠按鈕釋放、失聯恢復本機控制、範圍受限的停止方式 | 禁止 HTML/RTF/圖片/檔案/未知系統訊息，雙向 echo loop 與舊內容測試；stop/failure 不留下按键按住 |

控制器 gate 的預設拒絕必須保留。若實機候選版需要新增前景啟動路徑，必須走獨立審查：明確 opt-in、已核准 peer、通過的原生儲存／權限／設定 preflight、綁定範圍、可见狀態及立即停止操作，任一失敗均拒絕。禁止僅把 `RuntimeEnabled => false` 改為 `true`、移除測試或放寬失敗條件。緊急停止可以是明確 UI 操作；它不是日常跨屏切換快捷鍵。安全 preflight 不能取代使用者允許啟動的決定。

### 現有可核對命令與副作用

以下命令均已存在於基準；只在相應範圍獲准後使用。從 repo root，Git Bash／macOS shell 可用：

```sh
bash review/test.sh
```

此腳本會設置本地 `.review-cache`，NuGet locked restore、建置 review tools、執行選定回歸與 python3 static checks；SkipMacShield=true，不啟動 KVM／native-store self-test。測試會建立本機 scratch lock 與測試子程序；Mac 憑證測試可能使用 .NET 管理的可釋放暫時鑰匙圈。若還原失敗，先記錄套件／網路錯誤，不任意更新 lockfile。舊版 `run-tests.sh`、manual-tests 與上游发布流程不屬於這條受控驗證入口。

單独建置的精確參數（先按腳本設定快取／關閉 ASP.NET 開發憑證生成，不能漏掉）：

```sh
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_HOME="$PWD/.review-cache/dotnet-cli"
export NUGET_PACKAGES="$PWD/.review-cache/nuget"
dotnet restore MonkeyMouse.Tools/MonkeyMouse.Tools.csproj --locked-mode -p:SkipMacShield=true
dotnet build MonkeyMouse.Tools/MonkeyMouse.Tools.csproj --no-restore -p:SkipMacShield=true
```

原生儲存測試是另一個批准項目，先完整閱讀 [NATIVE-STORAGE.md](NATIVE-STORAGE.md)：

```sh
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll storage-self-test --allow-store-write
```

它在專用隨機 ID 項目執行 create/read/update/delete，留下空鎖檔；不建立正式裝置身份。程序異常中止可能留下該隔離項目，僅記錄其 ID 並依精確範圍清理，不能枚舉或清空其他鑰匙圈／憑證。重開真實身份與撤銷驗證要在另外的批准／測試隔離策略下完成。Mac 遭簽署／entitlement 拒絕時，修正受支援的簽署流程，不加明文後備。

route-only 檢查（先在本機填好資料，不把 null 全換成示範數字）：

```sh
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll config-check --measurements local-only/measurements.json
```

基準尚未提供 socket／loopback／run／install／service 命令；本文件不捏造它們。實作者須在本輪 PR 補上實際命令、參數、預期 bind、終止方法與測試檔名，經另一端核對後才執行。第一個 socket 測試只用明確的 127.0.0.1／::1，不能悄悄退到所有介面；必要時標單一位址族的 coverage，不能假稱IPv4/IPv6均通。loopback 通過也不是跨機互通。

**安全測試門檻：** 雙端批准正例；單方批准／錯誤指紋／冒名／撤銷／儲存不可用／過期／重放／改動封包負例；TLS 協商及 allowlist；frame 大小／序號／佇列界限；競爭程序與釋放；未知命令／檔案／電源訊息拒絕；純文字大小與 echo；socket 中斷回本機；兩平台離線及各自 loopback。記錄每項真正執行的平台與 SHA；未測的TLS版本不得列PASS。

**交付：** 第二輪各端報告＋程式 PR＋實際運行手冊＋產物 hash／來源 SHA。Mac 整合報告包含候選版本、權限／監聽／儲存變更清單、已驗證的 stop／rollback 方法，以及第三輪給使用者看的批准卡。正式配對參考 [PAIRING.md](PAIRING.md)，其命令只代表儲存核准，不代表已連線。

**退出門檻：** 同一整合 SHA 在 Mac 與 Windows build／離線／本機 socket 測試完成；兩端 native store 及受控候選入口通過；安全負例全部有證據；無敏感資料；第三輪啟動、權限、網路、產物和停止方式可被具體審查。缺簽署／公證／原生儲存／任一平台資料即 BLOCKED，不把「程式可編譯」當成可交付給使用者的共享工具。若三輪内无法排除，列出追加輪次而不是提供繞過命令。

## 7. 第三輪：使用者批准後的四實體螢幕驗收與修正

**输入：** 第二輪通過的確切候選 SHA 與兩端產物 SHA-256、四屏在線、新鮮的本機資料，以及各操作的使用者確認。**禁止以本計畫的發布當成批准。**

啟動前，Mac 整合端列出「本次產物、來源、簽署／公證結果、所需 OS 權限、哪台監聽哪個介面與 port、Windows 防火牆是否需要一條具體受限規則、如何立即停止、如何回復本次變更」。由使用者確認後才執行。不要在公開批准卡放 LAN 地址；讓使用者本地查看。若 Windows 需要防火牆規則，只授權特定程式／埠、指定可信範圍和必要對端，不開 all profiles/all ports，不關整體防火牆。

未公證 Mac 下載包不可透過 `xattr` 清除 quarantine、臨時重簽、停用 Gatekeeper/SIP、加入任意信任或降低驗證來執行。使用經驗證的正規簽署與公證產物；開發編譯不能冒稱等同可散布的公證版。缺少所需憑證／entitlement／發布能力時，保持 BLOCKED。系統密碼與安全對話框由使用者本機處理，不貼到聊天。

先確認 ShareMouse 與 Monkey Mouse 的現況，避免兩套同時攔截同一組輸入。若必須暫停既有 ShareMouse，先取得使用者確認，只做該次明確暫停並記錄原狀；不卸載、不改其設定、不自動開啟兩套。備妥各機本地觸控板／鍵盤及第二輪已驗證的停止操作。程序失去信任、斷線或權限失效時，應立即釋放所有按鍵／按鈕並恢復本機；不能等使用者靠另一台電腦才能救回控制。

### 必須逐項記錄的實機驗收表

以下次數是預先定義的 TARGET，不是已完成結果。使用普通測試文字與空白測試文件，避開密碼欄位／真實機密。每個 case 記錄兩端同一版本、操作者、實際次數、異常及修正後重測。

| ID | 使用者／兩端共同操作 | PASS 門檻 |
| --- | --- | --- |
| H01 | 四屏在线並確認角色、座標與縮放 | 實際資料一致；非鏡像誤判；左右相接；缺任一屏就停止 |
| H02 | ASUS 筆電↔Windows大螢幕，同機往返 | 20 次無阻擋、跳錯屏或按鍵殘留 |
| H03 | Windows大螢幕底邊↔BenQ頂邊 | 20 次完整往返，包含邊界10%、50%、90%位置及快慢移動；無切換熱鍵、無錯邊、無卡住 |
| H04 | BenQ↔MacBook，同機左右移動 | 20 次往返；再串接四屏完整巡迴10次，焦點／游標／鍵盤目標一致 |
| H05 | 跨邊界拖曳與各類输入 | 測一般視窗／非檔案測試區域、左右鍵、滾輪、Shift/Ctrl/Alt/Command對應；不得啟用檔案拖傳，停止／斷線後无按键或按钮卡住 |
| H06 | 中英文鍵盤與 IME | 兩端測拼音／注音等實際使用的輸入法、組字／候選／Enter/Esc、修飾鍵、切換後輸入；記錄使用的輸入法，不默認所有IME皆通 |
| H07 | 純文字剪貼簿雙向 | 各10次，含中文、emoji、多行、空內容／連續更新；按需貼上內容正確、不回灌舊文字、不造成echo；圖片/HTML/RTF/檔案不傳輸 |
| H08 | 未核准、錯誤指紋、單方批准及撤銷 | 不取得輸入／剪貼簿；已連線撤銷後立即退出；重連仍拒絕，必須重新雙方核准才可恢復 |
| H09 | 一端程序停止／網路失聯／權限撤回 | 本機控制恢復、所有按鍵釋放、無無限忙迴圈；未改信任的合法重連有清楚狀態，身份變更拒絕自動接受 |
| H10 | 使用者自行睡眠／喚醒及顯示器拔插 | 每端至少3次手動睡眠/喚醒，至少一次拔插；重新列舉ID、可信peer安全重連；未發送遠端睡眠/喚醒/鎖定訊息 |
| H11 | 禁止功能與短時穩定性 | 禁檔案及電源同步持續成立；30分鐘一般操作無失聯、焦點錯位或可見卡頓，記錄CPU/記憶體／延遲觀察方法，不捏造精度 |

若發現輸入卡住、錯誤 peer 能控制、明文／未知訊息通過、權限被意外擴大，立即停止該候選版，標 FAIL，保留去識別化原因。修正必須提交新 SHA，重跑受影響的第二輪安全測試與第三輪路徑，兩端更新到相同產物。正常跨屏不能靠永久開啟遠端鎖定／喚醒或快捷鍵補丁通過。

**交付：** 第三輪 Mac／Windows 各端報告、H01–H11表、候選與修正SHA、使用者實測確認、剩餘限制。raw log留本機，公開只列case結果與去識別化證據引用。Mac 在 `round-3/decision.md` 將「程式測試」「雙機通訊」「四屏使用者驗收」分開，全部門檻有證據才寫最終 PASS。

**退出門檻：** H01–H11均實測通過，兩端可獨立停止與安全恢復，禁止功能未放寬，沒有未解決的高風險錯誤。達不到就如實結案為 BLOCKED/FAIL，列出追加第4輪需求、owner與理由；三輪時間／交換目標不得凌駕保護與證據。

## 8. 停止、失敗與回復

1. 先用已驗證的本地停止操作關閉本次候選程序、停止監聽、釋放輸入；回復本機操作後再分析。若候選版尚無可靠停止路徑，不准啟動第三輪。
2. 僅回復本輪明確記錄並獲准的變更：精確程式權限／精確防火牆規則／本次隔離項目；使用者確認後依平台支援方式執行。禁止整體重設防火牆、清空鑰匙圈或刪除其他工具資料。需要重啟先告知並确认。
3. 原生 test item 異常殘留時，按當次隨機 ID處理。正式裝置身份無通用一鍵刪除命令；不能假造 `identity-reset`。需要換鍵時先列受影響信任及雙方重新核准流程。
4. 原來暫停的 ShareMouse 只按使用者確認恢復先前狀態；不得與候選程序同時自動重啟。不要為測試解除其安全設定。
5. Git 回復用新分支／經審查 revert 提交處理，不重寫公開歷史、不刪使用者未提交內容。回到舊程式版本不代表其二進位與新設定相容，先核對格式與原生儲存版本。

## 9. 每輪交接提交模板

複製至該輪自己的 `mac.md` 或 `windows.md`。僅填去識別化內容；狀態預設 NOT_RUN。收件端可依此獨立接手，不需要聊天記憶。

```markdown
# Round N / MAC 或 ASUS
- 狀態：NOT_RUN | WAITING_APPROVAL | BLOCKED | FAIL | PASS
- 本報告提交SHA：由整合decision引用，避免自引用SHA循環
- 本輪輸入SHA／受測程式SHA：完整40位
- 合併分支／PR：確切URL，沒有就寫無
- OS／架構／SDK：實際查詢或標明引用其他端報告
- 使用者批准範圍：動作、主機、時點；不貼私密聊天
- 未批准或未執行的項目：明列

| Case | 命令或手動步驟 | 預期 | 實際 | 狀態 | 證據 |
| --- | --- | --- | --- | --- | --- |
| 例：H03 | 尚未執行 | 20次跨機往返 | 無 | NOT_RUN | 無 |

- 實際改動：檔案、用途、副作用
- 實際工具／網路／儲存／權限變更：沒有也寫明
- 公開資料審查：檔案與author/committer均核對；不貼email/指紋
- 本地證據：只列相對檔名或case代碼；不公開raw log/inventory
- Blocker：原因、owner、最小解除條件、是否需要使用者決定
- 回復狀態：已恢復／尚有哪個明確變更待處理
- 給另一端的唯一下一步：操作、精確SHA、預期交付、停止條件
```

整合端 `decision.md` 必須引用兩端報告的完整 commit SHA，逐項確認可比性，列出 `GO_NEXT_ROUND` 或 `STOP`，附理由。缺另一端回報、只有一端測試、基準不一致，都不能結案為PASS。發布計畫的這次任務在文件提交完成後停止；任何代理開始第一輪之前，先確認当前用户要求与授权范围。
