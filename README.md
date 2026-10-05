# EyeglassLensSystem

球面度数（SPH）・円柱度数（CYL）・軸度（AXIS）から、厚みのある眼鏡レンズMeshとUnity用Materialを生成するシステムです。Unity Built-in Render Pipeline / PC VRChat向け。

**[紹介・導入ページ](https://chikumatateshina.github.io/EyeglassLensSystem/)** · **[ダウンロード](https://github.com/ChikumaTateshina/EyeglassLensSystem/releases/latest)** · **[詳細説明](Assets/PrescriptionLens/Documentation/README.md)**

## 導入

1. Releasesから `EyeglassLensSystem-1.0.0.unitypackage` をダウンロードし、Unityで **Assets → Import Package → Custom Package** からImportします。
2. Projectの右クリック → **Create → Prescription Lens → Settings**。
3. レンズの前面Meshを指定します。FBX本体の **Model → Read/Write Enabled** をONにして **Apply**。
4. **球面度数・円柱度数・軸度**を入力します。CYLが0なら軸度は不要です。
5. **プレビュー**で確認し、**レンズを生成・保存**。Settingsの隣の `Generated` にMesh / Material / Metadataが保存されます。
6. レンズのMeshFilter・MeshRendererへ生成物を割り当てます。

フレームの二重映りを防ぐ場合は **レンズのRender Queueを3020、フレームを3030** にします。フレームが複数Materialを使う場合はすべて3030にしてください。

ZIPの場合は解凍した `Assets/PrescriptionLens` をUnityプロジェクトの `Assets` にコピーします。既存インストールの更新時はファイルを上書きし、`.meta` のGUIDを維持してください。Scene・モデル・生成済みMeshは更新で自動変更されません。

## 機能

- 前面・後面・端面からなる閉じたMesh。負レンズは周辺厚、正レンズは中心厚。
- CYL / AXISを反映した方向別曲率と像変形。
- FBX内部の主軸を自動判定。分離した左右レンズの光学中心をそれぞれ自動設定。
- StandardとRefractiveの2種類のShader。Fresnel、AR近似、Tint、端面、画面空間屈折、弱いRGB色収差。
- 追加パラメータは折り畳み。度数3項目と元Meshだけで使用可能。
- 元Meshを保持し、PreviewはEditor専用。アバター上に独自MonoBehaviourは不要。

## 対応環境と制約

検証環境は **Unity 2022.3.22f1 / Built-in / Windows D3D11**。URP・HDRP・Quest/Androidは対象外です。VRChat SDKへの依存はありません。

入力は、レンズの前面のみの開いたSurface Meshです。三角形に分割された形状や、分離した左右2枚の面にも対応します。フレーム・裏面・側面を含むMeshは使用できません。SkinnedMeshRendererで使う場合のボーンとウェイトは別途設定が必要です。

屈折はGrabPassによる視覚近似で、背景に含まれたものを度数に応じて変形します。完全な光線追跡や実際の視覚の再現ではありません。屈折率・厚み等の既定値は表示用です。透明物の描画順、VRChat実機・HMD・ミラー描画の確認は使用環境で行ってください。

## 構成

```text
Assets/PrescriptionLens/  配布用コード・Shader・人工サンプル・詳細説明
Assets/Tests/Editor/     Unityでの検証とunitypackage出力
Tests/OpticsCheck/       Unity不要の光学計算・境界テスト
docs/                   GitHub Pages紹介サイト
Tools/                  配布ZIP作成とサイト検証
.github/workflows/      検証CI・Pages自動デプロイ
```

このリポジトリをUnityプロジェクトとして開けます。配布用unitypackageは `Assets/PrescriptionLens` のみを含みます。

## 開発・検証

```powershell
dotnet run --project Tests/OpticsCheck
python Tools/check_site.py
```

Unityでのバッチ検証:

```text
Unity.exe -batchmode -nographics -projectPath <repo> -executeMethod PrescriptionLens.Editor.LensValidation.ValidateBatch -logFile <log>
Unity.exe -batchmode -force-d3d11 -projectPath <repo> -executeMethod PrescriptionLens.Editor.LensValidation.CompileShadersBatch -logFile <log>
Unity.exe -batchmode -force-d3d11 -projectPath <repo> -executeMethod PrescriptionLens.Editor.LensFrameExclusionValidation.ValidateBatch -logFile <log>
Unity.exe -batchmode -nographics -projectPath <repo> -executeMethod PrescriptionLens.Editor.LensRelease.ExportBatch -logFile <log>
```

CIは.NETの数学・Boundary検証とサイトの内部リンク検証を実行します。Unityの検証はローカルで行います。

## ライセンス

[MIT](LICENSE)。改変・再配布・商用利用が可能です。
