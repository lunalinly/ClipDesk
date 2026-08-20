# Contributing to ClipDesk

感謝你願意協助改進 ClipDesk。

## 開始修改

1. Fork 專案並建立功能分支。
2. 修改 `native/ClipDesk.cs`。
3. 執行 `.\build-native.ps1`。
4. 確認免安裝版可以啟動，並測試受影響功能。
5. 提交 Pull Request，清楚說明變更、測試方式與畫面差異。

## 設計原則

- 維持小視窗可用，最小尺寸為 300 × 420。
- 文字不可因縮小視窗而溢出欄位。
- 新功能需同時適用免安裝版與安裝版。
- 不可把個人資料、`data.json`、備份檔或剪貼簿內容提交到專案。
- 保持資料格式向後相容；變更匯入格式時需提供遷移方式。

## 主題

- 公開版：`PUBLIC_RELEASE,CUSTOM_CHROME`
- 紫色主題：額外加入 `PURPLE_THEME`

## 回報問題

請附上 Windows 版本、ClipDesk 版本、重現步驟及必要畫面。請先遮蔽剪貼簿中的個人或公司資料。