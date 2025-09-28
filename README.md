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
- TCP/UDP通信対応（デュアルプロトコル）
- 非同期通信による高性能処理
- 包括的なテストカバレッジ（43テストケース）

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
- **通信方式**: TCP/UDP（デュアルプロトコル対応）
- **プログラミング**: 非同期プログラミング（async/await）
- **テスト**: xUnit + FluentAssertions（43テストケース）

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

// TCP接続のみ開始（ポート5007）
await simulator.StartAsync(5007);

// UDP接続のみ開始（ポート5008）
await simulator.StartUdpAsync(5008);

// TCP/UDP両方同時開始（TCPポート5007、UDPポート5008）
await simulator.StartBothAsync(5007, 5008);
```

#### 2. オムロンFINSプロトコルシミュレータ

```csharp
var simulator = new OmronFINSSimulator();

// 初期データ設定
simulator.SetDeviceValue(new PLCAddress("D", 0, 1), BitConverter.GetBytes((short)9999));
simulator.SetDeviceValue(new PLCAddress("C", 0, 1), new byte[] { 0x01, 0x00 });

// TCP接続のみ開始（ポート9600）
await simulator.StartAsync(9600);

// UDP接続のみ開始（ポート9601）
await simulator.StartUdpAsync(9601);

// TCP/UDP両方同時開始（TCPポート9600、UDPポート9601）
await simulator.StartBothAsync(9600, 9601);
```

#### 3. クライアント接続例

```csharp
// 三菱MCプロトコルクライアント（TCP接続）
var tcpClient = new MitsubishiMCProtocol();
await tcpClient.ConnectAsync("127.0.0.1", 5007);

// データ読み取り
var data = await tcpClient.ReadAsync(new PLCAddress("D", 0, 1));
var value = data.GetValue<short>();

// データ書き込み
var writeData = BitConverter.GetBytes((short)5678);
await tcpClient.WriteAsync(new PLCAddress("D", 1, 1), writeData);

await tcpClient.DisconnectAsync();

// 三菱MCプロトコルクライアント（UDP接続）
var udpClient = new MitsubishiMCProtocol();
await udpClient.ConnectUdpAsync("127.0.0.1", 5008);

// UDP通信でのデータ読み取り・書き込み
var udpData = await udpClient.ReadAsync(new PLCAddress("D", 0, 1));
await udpClient.WriteAsync(new PLCAddress("D", 1, 1), writeData);

await udpClient.DisconnectAsync();
```

## 通信プロトコル対応

### TCP/UDP デュアルプロトコル
- **TCP通信**: 安定した接続型通信（従来からサポート）
- **UDP通信**: 高速な非接続型通信（新機能）
- **同時動作**: TCP/UDPを同じシミュレータで並行実行可能
- **プロトコル透過性**: 同一APIでTCP/UDP切り替え可能

### 通信方式の選択指針
- **TCP推奨**: 確実なデータ配送が必要な制御システム
- **UDP推奨**: リアルタイム性を重視する監視システム
- **両方併用**: 制御用TCP + 監視用UDPの混在構成

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

## テストカバレッジ

### 包括的テストスイート（43テストケース）
- **基本機能テスト**: プロトコル接続・切断、データ読み書き
- **TCP通信テスト**: 安定性・接続維持・エラーハンドリング
- **UDP通信テスト**: パケット送受信・並行処理・ライフサイクル管理
- **統合テスト**: プロトコル間相互運用・実際の通信検証
- **並行処理テスト**: マルチスレッド環境での動作確認

### テスト実行
```bash
# 全テスト実行
dotnet test

# 詳細出力付きテスト実行
dotnet test --verbosity normal

# カバレッジレポート生成
dotnet test --collect:"XPlat Code Coverage"
```

## 開発環境要件

- .NET 8.0 SDK
- Visual Studio 2022 または Visual Studio Code
- C# 拡張機能
- Docker (オプション)
- xUnit テストランナー（テスト実行用）

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

## 最新の更新情報

### v2.0 新機能
- ✅ **UDP通信サポート**: 高速・低遅延通信の実現
- ✅ **デュアルプロトコル**: TCP/UDP同時動作による柔軟な構成
- ✅ **非同期API拡張**: ConnectUdpAsync、StartUdpAsync、StartBothAsyncメソッド
- ✅ **テストスイート拡充**: 27→43テストケースに大幅増加
- ✅ **パフォーマンス向上**: 並行処理とメモリ効率の最適化

### ロードマップ
- 🔄 WebSocket通信サポート（開発中）
- 🔄 REST APIインターフェース（計画中）
- 🔄 設定ファイルによる動的構成（計画中）
- 🔄 ログ記録・モニタリング機能強化（計画中）

## 注意事項

- このシミュレータは開発・テスト目的で作成されており、実際の産業用途での使用には十分な検証が必要です
- 実際のPLCとの互換性については、各メーカーの仕様書を参照してください
- セキュリティ機能は基本的なもののみ実装されています
- UDP通信使用時は、ネットワーク環境でのパケット損失を考慮した実装を推奨します