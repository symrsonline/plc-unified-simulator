# PLC Unified Simulator

[![CI](https://github.com/symrsonline/plc-unified-simulator/workflows/CI/badge.svg)](https://github.com/symrsonline/plc-unified-simulator/actions/workflows/ci.yml)
[![Release](https://github.com/symrsonline/plc-unified-simulator/workflows/Release/badge.svg)](https://github.com/symrsonline/plc-unified-simulator/actions/workflows/release.yml)
[![codecov](https://codecov.io/gh/symrsonline/plc-unified-simulator/branch/master/graph/badge.svg)](https://codecov.io/gh/symrsonline/plc-unified-simulator)
[![Docker](https://img.shields.io/docker/v/symrsonline/plc-unified-simulator?label=Docker&logo=docker)](https://github.com/symrsonline/plc-unified-simulator/pkgs/container/plc-unified-simulator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![API Docs](https://img.shields.io/badge/API%20Docs-GitHub%20Pages-blue)](https://symrsonline.github.io/plc-unified-simulator/)

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
- 包括的なテストカバレッジ

## プロジェクト構造

```
PLCUnifiedSimulator/
├── src/
│   ├── PLCUnifiedSimulator.Core/           # コアライブラリ
│   ├── PLCUnifiedSimulator.Protocols.Mitsubishi/  # 三菱MCプロトコル
│   ├── PLCUnifiedSimulator.Protocols.Omron/       # オムロンFINSプロトコル
│   ├── PLCUnifiedSimulator.Simulators/     # シミュレータ実装
│   ├── PLCUnifiedSimulator.Console/        # コンソールアプリケーション
│   └── PLCUnifiedSimulator.GUI/            # GUIアプリケーション（WPF）
├── tests/
│   └── PLCUnifiedSimulator.Tests/          # 単体テスト
└── PLCUnifiedSimulator.sln                 # ソリューションファイル
```

## 技術仕様

- **.NET**: 8.0
- **C#**: 12
- **通信方式**: TCP/UDP（デュアルプロトコル対応）
- **プログラミング**: 非同期プログラミング（async/await）
- **テスト**: xUnit + FluentAssertions

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

### 三菱MCプロトコル（全28デバイス対応）

#### 📟 リレーデバイス（ビット型）
| デバイス | 名称 | 説明 | 機種対応 |
|----------|------|------|----------|
| **X** | 入力リレー | 外部入力信号 | 🟢 全シリーズ |
| **Y** | 出力リレー | 外部出力信号 | 🟢 全シリーズ |
| **M** | 内部リレー | 内部制御用リレー | 🟢 全シリーズ |
| **SM** | 特殊内部リレー | システム用特殊リレー | 🟡 上位機種のみ |
| **L** | ラッチリレー | 停電保持リレー | 🟢 全シリーズ |
| **F** | アナンシエータ | 警報・表示用 | 🟡 機種により制限 |
| **C** | カウンタ | カウンタ接点 | 🟢 全シリーズ |
| **B** | リンクリレー | ネットワーク間リンク | 🟡 ネットワーク機能付き |
| **SB** | リンク特殊リレー | リンク用特殊リレー | 🟡 ネットワーク機能付き |
| **S** | ステップリレー | シーケンス制御用 | 🟢 全シリーズ |

#### ⏱️ タイマ・カウンタ関連
| デバイス | 名称 | 説明 | 機種対応 |
|----------|------|------|----------|
| **TS** | タイマ接点 | タイマ動作接点 | 🟢 全シリーズ |
| **TC** | タイマコイル | タイマ制御コイル | 🟢 全シリーズ |
| **TN** | タイマ現在値 | タイマカウント値 | 🟢 全シリーズ |
| **SS** | アナンシエータ接点 | 警報接点 | 🟡 機種により制限 |
| **SC** | アナンシエータコイル | 警報制御コイル | 🟡 機種により制限 |
| **SN** | アナンシエータ現在値 | 警報カウント値 | 🟡 機種により制限 |
| **CS** | カウンタ接点 | カウンタ動作接点 | 🟢 全シリーズ |
| **CC** | カウンタコイル | カウンタ制御コイル | 🟢 全シリーズ |
| **CN** | カウンタ現在値 | カウンタ値 | 🟢 全シリーズ |

#### 🗂️ レジスタデバイス（ワード型）
| デバイス | 名称 | 説明 | 機種対応 |
|----------|------|------|----------|
| **D** | データレジスタ | 汎用データ格納 | 🟢 全シリーズ |
| **SD** | 特殊データレジスタ | システム用データ | 🟡 上位機種のみ |
| **W** | リンクレジスタ | ネットワーク間データ | 🟡 ネットワーク機能付き |
| **SW** | リンク特殊レジスタ | リンク用特殊データ | 🟡 ネットワーク機能付き |
| **Z** | インデックスレジスタ | アドレス修飾用 | 🟢 全シリーズ |
| **R** | ファイルレジスタ | 大容量データ格納 | 🟡 機種により制限 |
| **ZR** | ファイルレジスタ拡張 | 拡張ファイル領域 | 🟡 上位機種のみ |
| **ZZR** | ファイルレジスタ拡張2 | 超大容量ファイル領域 | 🔴 最上位機種のみ |

#### 🏭 機種別対応状況

| シリーズ | 対応デバイス数 | サポート範囲 |
|----------|-------------|-------------|
| **Q/L/iQ-Rシリーズ** | 28/28 (100%) | 🟢 全デバイス完全対応 |
| **iQ-F FX5Uシリーズ** | 25/28 (89%) | 🟡 主要デバイス対応 |
| **QnAシリーズ** | 25/28 (89%) | 🟡 レガシー上位機種 |
| **Aシリーズ** | 26/28 (93%) | 🟡 レガシー中位機種 |
| **FXシリーズ** | 12/28 (43%) | 🔴 基本デバイスのみ |

> 🟢 = 完全サポート　🟡 = 部分サポート　🔴 = 制限あり

#### ⚠️ エラーハンドリング
- **未サポートデバイス**: `NotSupportedException`で適切なエラーメッセージ
- **機種固有制限**: サポートデバイス一覧を含むエラー情報
- **事前チェック**: `IsDeviceSupported()`メソッドによる安全なアクセス

#### 💡 デバイス使用例

```csharp
// 機種に応じたプロトコル初期化
var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.QJ71E71_Binary_Station1);

// 事前チェック（推奨）
if (protocol.IsDeviceSupported("ZZR")) 
{
    var data = await protocol.ReadAsync(new PLCAddress("ZZR", 100, 1));
    Console.WriteLine($"ZZRデバイス読み取り成功: {data.GetValue<short>()}");
}
else 
{
    Console.WriteLine("ZZRデバイスは現在の機種でサポートされていません");
}

// サポートデバイス一覧取得
var supportedDevices = protocol.GetSupportedDevices();
Console.WriteLine($"サポートデバイス数: {supportedDevices.Count}");
foreach (var device in supportedDevices)
{
    var deviceType = device.Value.IsWordDevice ? "ワード" : "ビット";
    Console.WriteLine($"  {device.Key}: {deviceType}デバイス (コード: 0x{device.Value.Code:X2})");
}

// エラーハンドリング例
try 
{
    await protocol.ReadAsync(new PLCAddress("UNKNOWN", 0, 1));
}
catch (NotSupportedException ex) 
{
    Console.WriteLine($"エラー: {ex.Message}");
    // → "デバイス 'UNKNOWN' は MELSEC-Q/L/iQ-Rシリーズ でサポートされていません。
    //    サポートされているデバイス: X, Y, M, SM, L, F, C, B, SB, S, ..."
}
```

### オムロンFINSプロトコル（19デバイス対応）

#### 📟 リレーデバイス（ビット型）
| デバイス | 名称 | メモリ領域コード | 説明 |
|----------|------|----------------|------|
| **IO** | 入出力リレー | 0xB0 | チャネルI/O入出力リレー |
| **WR** | 内部補助リレー | 0xB1 | 内部制御用補助リレー |
| **HR** | 保持リレー | 0xB2 | 電源断でも保持されるリレー |
| **AR** | 補助記憶リレー | 0xB3 | 補助記憶用リレー |

#### ⏱️ タイマ・カウンタ関連
| デバイス | 名称 | メモリ領域コード | 説明 |
|----------|------|----------------|------|
| **TS** | タイマアップフラグ | 0x09 | タイマ動作完了フラグ |
| **CS** | カウンタアップフラグ | 0x09 | カウンタ動作完了フラグ |
| **TN** | タイマ現在値 | 0x89 | タイマのカウント値 |
| **CN** | カウンタ現在値 | 0x89 | カウンタのカウント値 |

#### 🗂️ レジスタデバイス（ワード型）
| デバイス | 名称 | メモリ領域コード | 説明 |
|----------|------|----------------|------|
| **DM** | データメモリ | 0x82 | 汎用データ格納領域 |
| **EM** | 拡張メモリ | 0x98 | 拡張データメモリ（カレントバンク） |
| **EB** | 拡張メモリ（バンク指定） | 0xA0 | 拡張メモリ（バンク指定） |
| **IR** | インデックスレジスタ | 0xDC | アドレス修飾用レジスタ |
| **DR** | データレジスタ | 0xBC | 汎用データレジスタ |

#### 🎯 タスク・フラグ関連
| デバイス | 名称 | メモリ領域コード | 説明 |
|----------|------|----------------|------|
| **TKB** | タスクフラグ（ビット） | 0x06 | タスク制御用ビットフラグ |
| **TKS** | タスクフラグ（ステータス） | 0x46 | タスク制御用ステータス |

#### 🔄 後方互換デバイス
| デバイス | 名称 | メモリ領域コード | 対応デバイス |
|----------|------|----------------|-------------|
| **W** | WR領域（旧表記） | 0x31 | WRと同等 |
| **H** | HR領域（旧表記） | 0x32 | HRと同等 |
| **A** | AR領域（旧表記） | 0x33 | ARと同等 |
| **C** | カウンタ（旧表記） | 0x09 | TS/CSと同等 |

#### 💡 デバイス使用例

```csharp
var simulator = new OmronFINSSimulator();

// 拡張デバイスの使用例
simulator.SetDeviceValue(new PLCAddress("IO", 100, 1), BitConverter.GetBytes((short)1));     // 入出力リレー
simulator.SetDeviceValue(new PLCAddress("DM", 200, 1), BitConverter.GetBytes((short)1234));  // データメモリ
simulator.SetDeviceValue(new PLCAddress("TN", 10, 1), BitConverter.GetBytes((short)500));    // タイマ現在値
simulator.SetDeviceValue(new PLCAddress("EM", 0, 1), BitConverter.GetBytes((short)9999));    // 拡張メモリ

// 後方互換デバイスの使用例（同じ動作）
simulator.SetDeviceValue(new PLCAddress("W", 100, 1), BitConverter.GetBytes((short)1));      // WR領域（旧表記）
simulator.SetDeviceValue(new PLCAddress("C", 10, 1), BitConverter.GetBytes((short)500));     // カウンタ（旧表記）

// サポートデバイス一覧取得
var supportedDevices = simulator.GetSupportedDevices();
Console.WriteLine($"サポートデバイス数: {supportedDevices.Count}");
foreach (var device in supportedDevices)
{
    Console.WriteLine($"  {device.Key}: メモリ領域コード 0x{device.Value:X2}");
}
```

#### ⚠️ メモリ領域コードの注意点
- **同じコードの共有**: TS/CS、TN/CNは同じメモリ領域コードを使用
- **後方互換性**: W/H/A/Cは旧表記のデバイス名としてサポート
- **拡張デバイス**: IO/WR/HR/AR等の新しいデバイスは0xB0-0xBCの範囲を使用

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

### ソースからビルド

```bash
git clone https://github.com/symrsonline/plc-unified-simulator.git
cd plc-unified-simulator
dotnet build
dotnet run --project src/PLCUnifiedSimulator.Console
```

## テストカバレッジ

### 包括的テストスイート
- **基本機能テスト**: プロトコル接続・切断、データ読み書き
- **TCP/UDP通信テスト**: 安定性・接続維持・エラーハンドリング
- **拡張デバイステスト**: 機種別デバイス対応検証
  - Q/L/iQ-Rシリーズ全デバイス検証
  - FX5U/QnA/A/FXシリーズ個別検証
  - 未サポートデバイス例外処理
  - 引数検証・エラーハンドリング
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
- **マルチプラットフォームテスト**: Ubuntu、Windows での動作確認
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

### v2.2 品質向上・保守性強化（最新版）
- ✅ **リポジトリ整理**: 不要ファイル削除、.gitignore最適化
- ✅ **ビルド品質向上**: 警告除去、クリーンコンパイル実現
- ✅ **ドキュメント改善**: DocFX APIドキュメント生成
- ✅ **CI/CD最適化**: マルチプラットフォームテスト強化

### v2.1 新機能（UDP通信・拡張デバイス対応）
- ✅ **拡張デバイス対応**: 三菱MCプロトコル28デバイス完全対応
- ✅ **機種別サポート**: Q/L/iQ-R、FX5U、QnA、A、FXシリーズ個別最適化
- ✅ **UDP通信サポート**: 高速・低遅延通信の実現
- ✅ **デュアルプロトコル**: TCP/UDP同時動作による柔軟な構成
- ✅ **エラーハンドリング強化**: 未サポートデバイス適切例外処理
- ✅ **テストスイート大幅拡充**: 181テストケース完全カバレッジ
- ✅ **デバイス検証機能**: 事前チェック・サポート一覧取得API

### v2.0 基盤機能
- ✅ **非同期API拡張**: ConnectUdpAsync、StartUdpAsync、StartBothAsyncメソッド
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