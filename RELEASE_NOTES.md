## EyeglassLensSystem v1.0.0

Unity Built-in Render Pipeline / PC VRChat向け眼鏡レンズ生成システムの初回公開です。

### 導入

- **EyeglassLensSystem-1.0.0.unitypackage**：UnityへImportして使用する推奨形式。
- **EyeglassLensSystem-1.0.0.zip**：配布用Assets、README、MITライセンスを含むZIP。
- **SHA256SUMS.txt**：配布ファイルのSHA-256チェックサム。

[紹介・導入手順](https://chikumatateshina.github.io/EyeglassLensSystem/)

### 主な機能

- SPH / CYL / AXISの3項目から閉じたレンズMeshとMaterialを生成。
- 前面・後面・端面、最小厚み補正、入力Meshを保持するプレビュー。
- FBX内部の軸の自動判定と、左右レンズ別の光学中心。
- Standard / Refractive、Fresnel反射、AR近似、度数の像倍率、CYL方向性、RGB色収差。
- フレームの二重映りを防ぐ描画順の説明と6種類の人工サンプル。

### 対応環境

Unity 2022.3.22f1 / Built-in / Windows D3D11。URP・HDRP・Quest/Androidは対象外です。VRChat SDKへの依存はありません。

光学計算・Boundaryの18項目、Mesh生成18ケース、Shader8バリアント、フレーム除外の描画テストで検証。VRChat実機・HMD・ミラーは未検証です。

屈折は画面空間の視覚近似です。実際の眼鏡製造・診断・視力評価には使用しません。

MIT License / Copyright 2026 ChikumaTateshina.
