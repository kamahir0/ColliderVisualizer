# ColliderVisualizer
A Unity collider visualization tool built with ScriptableRendererFeature, designed for URP RenderGraph.

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)
![unity-version](https://img.shields.io/badge/unity-6.0+-000.svg)
[![releases](https://img.shields.io/github/release/kamahir0/ColliderVisualizer.svg)](https://github.com/kamahir0/ColliderVisualizer/releases)

ColliderVisualizer は、Unity6 の Universal Render Pipeline 向けに作られた、コライダー可視化ツールです。
特に、RenderGraph 環境で使用するために実装されています。

## セットアップ
### 要件

* Unity 6.0 以上

### インストール

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下のURLを入力する

```
https://github.com/kamahir0/ColliderVisualizer.git?path=src/ColliderVisualizer/Assets/ColliderVisualizer
```

またはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記

```json
{
    "dependencies": {
        "com.kamahir0.collider-visualizer": "https://github.com/kamahir0/ColliderVisualizer.git?path=src/ColliderVisualizer/Assets/ColliderVisualizer"
    }
}
```

## 使い方
### 1. 機能の有効化
ColliderVisualizer を使用するには、まず URP アセットに ColliderVisualizerFeature を追加してください。

Project Settings > Graphics > URP Asset を開く

Renderer セクションの Forward Renderer アセットを選択

Inspector 上で「+ Add Renderer Feature」から ColliderVisualizerFeature を追加

[ここで画像：ColliderVisualizerFeature が追加された Forward Renderer のスクリーンショット]

### 2. コライダーの可視化を有効にする
ランタイム中に以下の静的プロパティを変更することで、コライダーの可視化を制御できます。

// コライダー可視化の有効化
ColliderVisualize.VisualizeCollisions = true; // 衝突コライダー（isTrigger == false）
ColliderVisualize.VisualizeTriggers = true;   // トリガーコライダー（isTrigger == true）
3. その他の設定
// 色や透明度、品質の設定
ColliderVisualize.CollisionColor = Color.green;
ColliderVisualize.TriggerColor = Color.magenta;
ColliderVisualize.Alpha = 0.4f;
ColliderVisualize.MeshQuality = 2;
これらはすべてランタイム中に動的に変更できます。

### 4. コライダー情報の更新
新たに生成された GameObject やコライダーの変化に対応するには、以下を呼び出します：

ColliderVisualize.UpdateColliders();

### 5. イベントの監視
ColliderVisualize.OnEnabled += () => Debug.Log("ColliderVisualizer 有効化");
ColliderVisualize.OnDisabled += () => Debug.Log("ColliderVisualizer 無効化");

### 6. 実行中の確認
if (ColliderVisualize.IsRuntimeInitialized)
{
    Debug.Log("初期化済み。設定可能です。");
}
[ここでgif：ランタイム中にコライダー表示がON/OFFされる様子]

## ライセンス
MIT License。詳細は LICENSE を参照してください。
