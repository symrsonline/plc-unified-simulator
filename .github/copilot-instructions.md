# PLC Unified Simulator

## プロジェクト概要
C#で開発されたPLCシミュレータ。以下のPLCプロトコルに対応：
- 三菱Q・iQシリーズ（MC Protocol）
- オムロンFINS（FINS Protocol）

## 機能
- PLCデバイスの読み取り（Read）
- PLCデバイスの書き込み（Write）
- 複数プロトコル対応
- シミュレーション機能

## 開発進捗
- [x] プロジェクト要件の確認完了
- [x] プロジェクト構造のセットアップ完了
- [x] 基本クラス設計完了
- [x] プロトコル実装完了（三菱MC、オムロンFINS）
- [x] テスト実装完了
- [x] ドキュメント作成完了

## 技術スタック
- .NET 8.0
- C# 12
- TCP/UDP通信
- 非同期プログラミング

## プロジェクト構造
```
PLCUnifiedSimulator/
├── Core/                   # コアライブラリ
├── Protocols/             # プロトコル実装
│   ├── Mitsubishi/       # 三菱MC Protocol
│   └── Omron/            # オムロンFINS Protocol
├── Simulators/           # シミュレータ実装
├── Tests/                # テストプロジェクト
└── Examples/             # サンプルコード
```