# PLC Unified Simulator

[![CI](https://github.com/symrsonline/plc-unified-simulator/workflows/CI/badge.svg)](https://github.com/symrsonline/plc-unified-simulator/actions/workflows/ci.yml)
[![Release](https://github.com/symrsonline/plc-unified-simulator/workflows/Release/badge.svg)](https://github.com/symrsonline/plc-unified-simulator/actions/workflows/release.yml)
[![codecov](https://codecov.io/gh/symrsonline/plc-unified-simulator/branch/master/graph/badge.svg)](https://codecov.io/gh/symrsonline/plc-unified-simulator)
[![Docker](https://img.shields.io/docker/v/symrsonline/plc-unified-simulator?label=Docker&logo=docker)](https://github.com/symrsonline/plc-unified-simulator/pkgs/container/plc-unified-simulator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

C#で開発されたPLCシミュレータ。三菱Q・iQシリーズ（MCプロトコル）とオムロンFINS（FINSプロトコル）に対応した統合シミュレーション環境を提供します。

## 機能

### サポートプロトコル
- **三菱MCプロトコル**: Q・iQシリーズPLC対応
- **オムロンFINSプロトコル**: CPシリーズ、CJシリーズ等対応

### 主要機能
- PLCデバイスの読み取り（Read）
- PLCデバイスの書き込み（Write）
- 複数デバイスの一括読み書き
- リアルタイムシミュレーション
- TCP/IP通信対応

## プロジェクト構造

```
PLCUnifiedSimulator/
├── src/
│   ├── PLCUnifiedSimulator.Core/           # コアライブラリ
│   ├── PLCUnifiedSimulator.Protocols.Mitsubishi/  # 三菱MCプロトコル
│   ├── PLCUnifiedSimulator.Protocols.Omron/       # オムロンFINSプロトコル
│   ├── PLCUnifiedSimulator.Simulators/     # シミュレータ実装
│   └── PLCUnifiedSimulator.Console/        # コンソールアプリケーション
├── tests/
│   └── PLCUnifiedSimulator.Tests/          # 単体テスト
└── PLCUnifiedSimulator.sln                 # ソリューションファイル
```

## 技術仕様

- **.NET**: 8.0
- **C#**: 12
- **通信方式**: TCP/IP
- **プログラミング**: 非同期プログラミング（async/await）

## 使用方法

### ビルド

```bash
dotnet build
```

### テスト実行

```bash
dotnet test
```

### シミュレータ起動

```bash
dotnet run --project src/PLCUnifiedSimulator.Console
```

### 基本的な使用例

#### 1. 三菱MCプロトコルシミュレータ

```csharp
var simulator = new MitsubishiMCSimulator();

// 初期データ設定
simulator.SetDeviceValue(new PLCAddress("D", 0, 1), BitConverter.GetBytes((short)1234));
simulator.SetDeviceValue(new PLCAddress("M", 0, 1), new byte[] { 0x01, 0x00 });

// シミュレータ開始（ポート5007）
await simulator.StartAsync(5007);
```

#### 2. オムロンFINSプロトコルシミュレータ

```csharp
var simulator = new OmronFINSSimulator();

// 初期データ設定
simulator.SetDeviceValue(new PLCAddress("D", 0, 1), BitConverter.GetBytes((short)9999));
simulator.SetDeviceValue(new PLCAddress("C", 0, 1), new byte[] { 0x01, 0x00 });

// シミュレータ開始（ポート9600）
await simulator.StartAsync(9600);
```

#### 3. クライアント接続例

```csharp
// 三菱MCプロトコルクライアント
var client = new MitsubishiMCProtocol();
await client.ConnectAsync("127.0.0.1", 5007);

// データ読み取り
var data = await client.ReadAsync(new PLCAddress("D", 0, 1));
var value = data.GetValue<short>();

// データ書き込み
var writeData = BitConverter.GetBytes((short)5678);
await client.WriteAsync(new PLCAddress("D", 1, 1), writeData);

await client.DisconnectAsync();
```

## サポートデバイス

### 三菱MCプロトコル
- **D**: データレジスタ
- **X**: 入力リレー
- **Y**: 出力リレー
- **M**: 内部リレー
- **B**: リンクリレー
- **F**: ラッチリレー
- **V**: エッジリレー
- **S**: ステップリレー
- **W**: リンクレジスタ
- **R**: ファイルレジスタ
- **Z**: インデックスレジスタ

### オムロンFINSプロトコル
- **D**: DM領域
- **C**: CIO領域
- **W**: WR領域
- **H**: HR領域
- **A**: AR領域
- **T**: タイマ
- **CT**: カウンタ

## インストール

### Docker を使用

```bash
# 最新版を取得して実行
docker run -p 5000-5050:5000-5050 ghcr.io/symrsonline/plc-unified-simulator:latest

# または Docker Compose を使用
docker-compose up -d
```

### リリースバイナリ

[Releases](https://github.com/symrsonline/plc-unified-simulator/releases) から各プラットフォーム用のビルド済みバイナリをダウンロードできます。

- Windows (x64): `plc-unified-simulator-win-x64.zip`
- Linux (x64): `plc-unified-simulator-linux-x64.tar.gz`
- macOS (x64): `plc-unified-simulator-osx-x64.tar.gz`

### ソースからビルド

```bash
git clone https://github.com/symrsonline/plc-unified-simulator.git
cd plc-unified-simulator
dotnet build
dotnet run --project src/PLCUnifiedSimulator.Console
```

## 開発環境要件

- .NET 8.0 SDK
- Visual Studio 2022 または Visual Studio Code
- C# 拡張機能
- Docker (オプション)

## CI/CD

このプロジェクトは GitHub Actions を使用した CI/CD パイプラインを実装しています：

- **継続的インテグレーション**: プッシュ・プルリクエスト時の自動テスト実行
- **マルチプラットフォームテスト**: Ubuntu、Windows、macOS での動作確認
- **コードカバレッジ**: Codecov による自動カバレッジレポート
- **自動リリース**: タグ作成時の自動バイナリビルド・配布
- **Docker イメージ**: GitHub Container Registry への自動公開
- **依存関係管理**: Dependabot による自動アップデート

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## 貢献

プルリクエストやイシューの報告を歓迎します。開発に参加される場合は、以下の手順に従ってください：

1. このリポジトリをフォーク
2. フィーチャーブランチを作成 (`git checkout -b feature/AmazingFeature`)
3. 変更をコミット (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュ (`git push origin feature/AmazingFeature`)
5. プルリクエストを開く

## 注意事項

- このシミュレータは開発・テスト目的で作成されており、実際の産業用途での使用には十分な検証が必要です
- 実際のPLCとの互換性については、各メーカーの仕様書を参照してください
- セキュリティ機能は基本的なもののみ実装されています