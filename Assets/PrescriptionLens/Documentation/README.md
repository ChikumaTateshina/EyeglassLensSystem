# Prescription Lens — Unity / PC VRChat

Built-in Render Pipeline用の眼鏡レンズシステムです。処方値から厚みのあるMeshをEditorで生成し、透明・Fresnel反射・ARコーティング近似・色収差・方向性のある像変形を描画します。

## 導入

1. この `Assets/PrescriptionLens` フォルダをUnityプロジェクトの `Assets` にコピーします。
2. Projectで右クリック → **Create → Prescription Lens → Settings**。
3. Source Meshにレンズ前面のMeshを設定します。左右が分離した2枚のレンズを1つのMeshとして指定することもできます。Model Import SettingsのRead/Writeを有効にしてください。
4. 球面度数SPH・円柱度数CYL・軸度AXISの3項目だけを入力します。CYLが0ならAXISの入力は不要です。
5. **プレビュー**でScene Viewの原点に生成形状を表示できます。**プレビューを閉じる**で削除します。元Meshは変更しません。
6. **レンズを生成・保存**で設定Assetと同じフォルダ内の `Generated` にMesh、Material、Metadataを保存します。保存先を明示指定した場合はそちらを使用します。
7. アバターのMeshFilter / MeshRendererに生成Mesh / Materialを割り当てます。SkinnedMeshRendererに使用する場合は別途ボーン・ウェイトの設定が必要です。生成Meshはウェイトを含みません。

生成後のアバターに独自MonoBehaviourは必要ありません。左右で度数が異なる場合は片眼ごとに別のSettingsを使います。左右同じ度数なら、分離した左右の面を1つのMeshとして生成できます。このリポジトリ自体をUnityの検証用プロジェクトとして開くこともできます。

通常のUIでは度数3項目と元Meshだけを表示します。屈折率1.60、Base Curve 4D、中心厚1.5mm、最小厚1mm、見かけの背景距離12mmを表示用の既定値として使用します。処方3項目から材質や実際の製品厚みを一意に決定するものではありません。必要なときだけ「詳細設定」を開いて調整できます。

MaterialのInspectorも度数3項目を中心に表示します。生成済みMaterialの度数を手動変更すると、Bake済み係数から手入力計算へ自動的に切り替わります。既存Meshの厚みは変化しないため、形状も変更する場合はSettingsから再生成してください。

## 2種類のShader

| Shader | 用途 |
| --- | --- |
| `PrescriptionLens/Standard` | GrabPassなし。透明ブレンド、反射、AR、Tint、端面。 |
| `PrescriptionLens/Lens` | PC向け。共有された名前付きGrabPass、屈折、SPH倍率、CYL/AXISの方向性、RGB色収差。 |

SettingsのRefractiveを切り替えて生成します。MaterialのRender Queueは通常のInspectorで変更できます。初期値3020です。

Refractiveは取得した背景を透過色で減衰させ、反射を加えて置き換えます（alpha=1、ZWrite Off）。透過率を下げると背景が暗くなります。Standardは背景取得なしの透明ブレンド近似のため、透過色の正確な乗算はできません。Standardの透明なTintは近似です。

閉じたMeshにCull Backを使い、カメラ側の面のみ描画します。表面と裏面の反射を二重に加算しません。端面は頂点色で識別します。反射はReflection Probeと主ライトを利用します。追加ライト・影・Lightmapは初期実装に含みません。

## 単位・座標

既定で、Mesh内部の三角形の面積加重法線から主軸を自動判定します。FBXの取り込み変換やGameObjectの回転によってScene上のZ正面とMesh内部の正面方向が異なっていても、軸を追加指定する必要はありません。手動指定したい場合は詳細設定で `Auto Orient Source` をOFFにします。任意の斜め向きのMeshや、側面を含む閉じたMeshの自動修復は行いません。

光学中心は、つながっているレンズ面ごとに外形の水平・垂直Bounds中央から自動設定します。左右レンズが1つのMesh内で分離していても、それぞれに独立した光学中心を持ちます。`Optical Center Mm` は自動判定時にはその中央からの追加オフセットです。絶対座標で指定する場合は `Auto Optical Center` をOFFにします。生成MeshのUV1に各レンズの光学中心を保存し、Shaderでも同じ中心を使用します。

- 1 Unity Unit = 1 m。厚み・光学中心・見かけの背景距離はmm、度数はD。
- Forward Axisがレンズの表側。Defaultは+Z、水平+X、垂直+Y。
- ±YのForward Axisではローカル+Zが垂直方向です。±Xではローカル+Yが垂直方向です。水平は `up × forward` です。
- 自動光学中心をOFFにした場合のOptical Centerは、この水平・垂直基底の原点からの絶対座標です。
- モデルの実寸とTransform Scale=1を基準に生成してください。非一様スケールは曲率・厚みの意味を変えます。
- Preserve Source Front Shape=falseでは元頂点の平均奥行きを基準面とし、前面をBase Curveから再生成します。trueでは入力前面のみ保存し、後面は同じ基準面の処方曲面です。

## 光学計算

主経線の度数は `SPH` と `SPH+CYL`。CYLは負円柱表記を基準とし、正CYLも計算可能です。AXISは180°周期で正規化します。

前面曲率 `c1=BaseCurve/(n−1)`。厚レンズ式を逆算した後面曲率は

`c2=(BaseCurve−P)/((n−1)(1−d·BaseCurve/n))`。

各方向の `P=SPH+CYL·sin²(θ−AXIS)` から後面Sagを計算します。これは主経線に一致する方向別Sagによるトーリック近似で、厳密なトーラス面ではありません。前面Zは `plane−frontSag`、後面Zは `plane−d−backSag`。負レンズは周辺が厚く、正レンズは中心が厚くなります。

最小端面厚に加え、サンプルされた内側頂点でも同じ厚み下限を守ります。必要時に中心厚を増やし、その厚みで後面曲率を再計算します。要求中心厚と実際の中心厚はMetadataに記録されます。厚み下限の確認は頂点に対して行うため、曲面の連続領域を保証するものではありません。

像変形は実際の光線追跡ではなく、ユーザー指定のApparent Background Depthを使った近似です。逆倍率 `1−P·depth` を主経線ごとにBakeします。負度数は中心から離れた背景を採取して縮小、正度数は中心寄りを採取して拡大します。背景のDepth Textureは使用しません。

Generatorで作ったMaterialはBake済み光学係数を使用します。処方とMeshの両方の更新はSettingsから再生成してください。Materialの度数を直接調整すると、自動的にSPH/CYL/AXIS/Depthから係数を計算するモードへ切り替わります。既定の基底は+Zです。

## Meshの条件・制約

入力は一貫した法線方向を持つ開いた三角形Surfaceです。完全一致する頂点位置はUV seamを含め溶接します。UV seamの片側UVを採用しますが、本ShaderはUV0に依存しません。近接するだけの頂点は溶接しません。

前面・後面・端面で頂点を分離し、端面を鋭い法線境界にします。Color.rは前面0、後面0.5、端面1。Color.gは厚みmm（正規化せずfloatで保存）。外部ツールで頂点色を8bitに変換すると厚み情報が失われます。

非多様体、境界の分岐、不整合な法線、レンズ平面に対し退化したTriangle、閉じた入力、NaN/Infinity、Sagの定義域外はエラーにします。複数Boundaryはすべて接続し、警告します。全体の自己交差の検出は未対応なので、交差のない入力を使用してください。Subdivisionは0〜4、Edge Segmentsは1〜8。片眼5000頂点以上で警告します。

## サンプルと検証

**Tools → Prescription Lens → Create Sample Assets**から直径50mmの円形表面と、Plano / Myopia / HighMyopia / Hyperopia / Astigmatism / RotatedAstigmatismのSettings、生成Mesh、Materialを作成できます。同名の既存Assetを上書きしません。

純粋な光学計算・Boundary検証:

```powershell
dotnet run --project Tests/OpticsCheck
```

Unityバッチ検証は `PrescriptionLens.Editor.LensValidation.ValidateBatch` をexecuteMethodに指定します。ShaderのImport、6種類の処方×Subdivision 0/1/2、厚み下限、位置溶接後の閉じたTopologyと一貫したwinding、法線、元Meshの保持、MaterialのBake、Asset保存を確認します。

描画確認ではGrid・Checkerboard・文字・顔を背景に置き、SPHの倍率の向き、CYLの方向性、AXISの回転を確認してください。

## VRChatで確認する点

### フレームの二重映りを防ぐ

フレームを不透明描画の既定Queue 2000で描くと、GrabPassの背景に含まれ、レンズ内で変形したフレームがもう一度表示されることがあります。フレームのMaterialのRender Queueを **3030**、レンズを **3020** にすると、背景取得とレンズ描画の後にフレーム本体を描くため、フレームを屈折用の背景から除外できます。フレームのShader・色・度数設定を変更する必要はありません。

Render QueueはMaterialごとの設定です。フレームに複数Materialがある場合はすべて3030に設定します。フレームのShaderを変更した場合もこの値を維持してください。

Stereo用のUnityマクロ、眼別のスクリーン投影、スクリーンテクスチャ宣言、眼別UV領域のClampを使用します。VRChat実機でのMulti Pass / Single Pass Instanced / ミラー描画の確認は別途必要です。

共有GrabPassは同名で左右眼や複数レンズの背景取得を共有します。最初のGrabPassより後に描画された透明物は取得背景に含まれません。髪・目などのRender Queueと交差する透明物の描画順を調整してください。URP/HDRPとQuest/Androidは対象外です。
