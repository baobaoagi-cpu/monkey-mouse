# ASUS Windows Codex：立即完成新版 Windows 驗證與必要修復

ASUS Codex，請直接執行以下工作，不要停留在重述舊錯誤或等待使用者代轉技術細節。這是一次 Windows 離線建置／測試委託；完成後推送可供 Mac 接手的程式修復與證據。

## 固定基準

- 倉庫：https://github.com/baobaoagi-cpu/monkey-mouse
- 實作分支：`codex/secure-socket-slice`
- 必須先測的完整 SHA：`5fcdc307fab5bca0f38802aa55c33bdc0ea338b1`
- Windows 舊報告 `codex/windows-round-1` / `f9705d9b538978809da1aa5df988287881edb90c` 的 NU1004／cp950 錯誤發生在修正前，不能當成本次結果。

新版已修復雙 RID locked restore 與 UTF-8 checker，並加入 loopback TCP／mTLS controller 和 10 項 socket 測試。Mac 已通過完整 672 項、預設 662 項測試、雙 RID locked restore 及 win-x64 交叉建置；這些均不等於 Windows 實機通過。你的任務是補上真正的 Windows 證據。

## 執行步驟

1. 檢查現有工作樹，保留所有未提交變更。建立獨立 checkout／worktree，fetch 實作分支並鎖定上述完整 SHA；確認 `git rev-parse HEAD` 相符。不要 reset、clean、覆蓋或切走目前工作樹。從該 SHA 建立未被占用的 `codex/windows-secure-slice-validation` 或其他 `codex/windows-...` 分支。
2. 使用本機已批准的隔離 .NET SDK **10.0.401**、既有 Python 與 process-scoped wrapper。先讀 `review/test.sh` 與 wrapper，確認只走已批准的 restore/build/test、既有套件來源與隔離快取；符合既有 Windows 離線建置／測試授權即可繼續，不必重問。若腳本多出未授權操作，停在該操作前，指出具體差異；本文件不擴大系統或網路操作授權。
3. 在固定 SHA 的 checkout，以既有 wrapper 執行預設 **`bash review/test.sh`**，保留真實 Windows 本機日誌與原始退出碼。預設應排除 `Loopback`，基準預期 662 項；14 項靜態檢查及 cp950 回歸也應完成。不要用 Mac 結果或交叉建置代替。若數量不同，說明原因，不能硬填預期值。
4. 若有 Windows 專屬程式缺陷，在本分支自主做最小修復並重跑相關檢查及完整預設腳本，提交**程式碼與結果**。不要只提交失敗文字就停止。不得刪測試、放寬驗證、跳過 locked mode，或為通過測試移除平台支援。若是授權範圍外操作、缺少工具、外部服務或需要架構決定而無法前進，提交可重現的阻塞點與最小下一步；不要自行安裝或擴權。
5. 如有修復，先提交程式碼，再對該完整 commit SHA 重跑預設測試；另以文件 commit 提交精簡報告並推送分支。如原版即通過，只提交證據報告。使用現有核准 GitHub 身分及 noreply 作者：`279388084+baobaoagi-cpu@users.noreply.github.com`，勿修改全域 Git 設定。不要自行合併 main。

## 回傳格式

新增 `review/handoffs/windows-secure-slice-result.md`，包含：

- **PASS／BLOCKED** 與一句結論；實際被測完整 SHA、分支、修復 commit（若有）。文件 commit 與被測程式 SHA 請分清。
- Windows／SDK／Python 版本；實際執行命令、每步退出碼、測試通過／失敗／略過數、靜態與編碼檢查結果。命令中的個人路徑以明確標示的代稱呈現。
- `packages.lock.json` 執行前後是否改變；如有差異，說明是否為必要修復，附 commit。不得靜默更新鎖檔。
- 本機日誌的最小必要摘要及錯誤代碼；若未執行某步，寫明 NOT_RUN 與原因，不推測結果。不必公開完整日誌。
- 下一輪 Mac 能直接接手的判斷：可審查合併的內容、仍需修復的項目、尚未驗證的能力，以及報告與程式 commit 的 GitHub URL。

推送成功後核對遠端 SHA／文件可讀。Mac 端會依分支與提交追蹤後續進度；請讓每次結果都能追溯到確切被測 SHA。

## 本次邊界

- 不執行 `--with-loopback`、`socket-smoke`、任何 socket／跨機試跑，亦不啟動 Monkey Mouse 輸入控制器。
- 不操作真實鍵鼠／系統剪貼簿，不更動 ShareMouse、防火牆、權限、系統設定；不安裝工具、不改全域 PATH、不建立持久裝置信任。
- 不公開私鑰、密碼、token、IP、個資或原始裝置清單；使用最小摘要。
- **本輪 PASS 只代表 Windows 預設建置與測試通過，不代表四螢幕、跨機控制或正式產品 PASS。不要擅自進入第二輪。**
