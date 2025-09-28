# PLC Unified Simulator API Reference

PLC Unified Simulatorは、C#で開発されたPLCシミュレータで、三菱Q・iQシリーズ（MCプロトコル）とオムロンFINS（FINSプロトコル）に対応した統合シミュレーション環境を提供します。

## 主要コンポーネント

### Core Library (`PLCUnifiedSimulator.Core`)
PLC通信の基礎となるコアライブラリです。

- [`PLCAddress`](api/PLCUnifiedSimulator.Core.PLCAddress.html) - PLCデバイスのアドレス表現
- [`PLCData`](api/PLCUnifiedSimulator.Core.PLCData.html) - PLCデータの格納と変換
- [`IPLCProtocol`](api/PLCUnifiedSimulator.Core.IPLCProtocol.html) - PLCプロトコルのインターフェース

### Mitsubishi Protocol (`PLCUnifiedSimulator.Protocols.Mitsubishi`)
三菱PLCとの通信を実装したプロトコルライブラリです。

- [`MitsubishiMCProtocol`](api/PLCUnifiedSimulator.Protocols.Mitsubishi.MitsubishiMCProtocol.html) - MCプロトコル実装
- [`MitsubishiPLCSeries`](api/PLCUnifiedSimulator.Protocols.Mitsubishi.MitsubishiPLCSeries.html) - サポート機種定義

### Omron Protocol (`PLCUnifiedSimulator.Protocols.Omron`)
オムロンPLCとの通信を実装したプロトコルライブラリです。

- [`OmronFINSProtocol`](api/PLCUnifiedSimulator.Protocols.Omron.OmronFINSProtocol.html) - FINSプロトコル実装

### Simulators (`PLCUnifiedSimulator.Simulators`)
PLCシミュレータの実装です。

- [`PLCSimulatorBase`](api/PLCUnifiedSimulator.Simulators.PLCSimulatorBase.html) - シミュレータ基底クラス
- [`MitsubishiMCSimulator`](api/PLCUnifiedSimulator.Simulators.MitsubishiMCSimulator.html) - 三菱MCプロトコルシミュレータ
- [`OmronFINSSimulator`](api/PLCUnifiedSimulator.Simulators.OmronFINSSimulator.html) - オムロンFINSプロトコルシミュレータ

## 使用方法

### 基本的な使用例

```csharp
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Simulators;

// シミュレータの作成
var simulator = new MitsubishiMCSimulator();
await simulator.StartAsync(5000);

// プロトコルの作成と接続
var protocol = new MitsubishiMCProtocol();
await protocol.ConnectAsync("127.0.0.1", 5000);

// データの読み書き
var address = new PLCAddress("D", 100, 1);
await protocol.WriteAsync(address, BitConverter.GetBytes((short)1234));
var data = await protocol.ReadAsync(address);

// 切断
await protocol.DisconnectAsync();
await simulator.StopAsync();
```

### サポートされているデバイス

#### 三菱MCプロトコル
- **ビットデバイス**: X, Y, M, SM, L, F, C, B, SB, S
- **ワードデバイス**: D, SD, W, SW, Z, R, ZR, ZZ

#### オムロンFINSプロトコル
- **ビットデバイス**: IO, WR, HR, AR, TS, CS, TKB, TKS
- **ワードデバイス**: DM, EM, EB, IR, DR, TN, CN

## APIドキュメント

各クラスの詳細なAPIドキュメントは、左側のナビゲーションメニューから参照してください。

## 関連リンク

- [プロジェクト概要](../README.md)
- [GitHubリポジトリ](https://github.com/symrsonline/plc-unified-simulator)