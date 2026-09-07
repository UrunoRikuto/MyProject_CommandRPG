# CLAUDE.md

このファイルはリポジトリのルートに置くことを想定した、Claude Code向けのプロジェクト引き継ぎ資料です。これまでチャット(Cowork)上で「コマンドRPGゲーム制作アドバイザー」として進めてきた内容を移管するために作成しました。

## プロジェクト概要

Unity製のコマンドRPG(Unity 6000.3.20f1 / URP)。目標の戦闘スタイルは「モンハンストーリーズ式」:

- プレイヤーは1体の操作キャラクターのみを操作する(パーティ全員を操作する王道コマンドRPG式ではない)
- 味方の残りメンバーと敵は全員AIが自動で行動する
- 操作キャラクターの行動は、コマンド(たたかう/スキル/にげる)選択後にターゲットをクリックして決める
- 味方AI/敵AIはキャラクターごとに設定した重みに基づいて「たたかう」「スキル」を選ぶ(個性づけ)

命名規則・コーディングルールの参照元: https://github.com/DA-Yoshi-KYO/SummerVacationGameJam/wiki/ルール:コーディングルール

## これまでの進め方(参考)

Coworkでは以下のサイクルで1タスクずつ進めてきました。Claude Codeでの作業スタイルは開発者の判断で変えて構いませんが、参考として残します。

1. タスクを1つ決める(要件・受け入れ条件つき)
2. 実装してpush
3. 実際のコードをレビューし、問題なければ次のタスクへ、問題があれば修正指示

GitHub Issueは作らず、このドキュメント(および元のチャット)でタスクを管理してきました。規模の大きいタスクは依存順にサブタスクへ分割する運用にしています。

## 命名規則・コーディングルール(確定)

SummerVacationGameJamプロジェクトのコーディングルールを採用。⭐の数が多いほど重要度が高い。

### 変数命名(⭐⭐⭐)

- ローワーキャメルケース(lowerCamelCase)
- private/protected変数は先頭に`_`
- 外部公開は式形式メンバー(`=>`)でバッキングフィールドを公開(通常のプロパティでも可)
- 複雑な処理は専用メソッドに分離

```csharp
private CS_PlayerMove _playerMove;
private float _playerHP;
protected float _hpValue;
public float playerHP => _playerHP;

public void TakeDamage(float damage)
{
    _playerHP -= damage;
    if (_playerHP < 0f) Destroy(gameObject);
}
```

### ファイル(クラス)命名の接頭辞(⭐⭐⭐)

| ファイルの種類 | 接頭辞 | 例 |
|---|---|---|
| 継承なしのcs / MonoBehaviour継承のcs | `CS_xxx` | `CS_BattleStateMachine`, `CS_CharacterState`, `CS_AttackCommand`, `CS_CommandButtonInput`, `CS_BattleAI` |
| ScriptableObject継承のcs(型定義) | `CSO_xxx` | `CSO_CharacterData`, `CSO_SkillData` |
| VolumeComponent継承のcs | `CSV_xxx` | (現状未使用) |
| Editor用のcs | `CSED_xxx` | `CSED_ValueObserverWindow` |
| Enum専用のcs | `CSE_xxx` | `CSE_BattleResult` |
| ScriptableObject(実データ側のアセット) | `DB_xxx` | `DB_TestCharacterA`〜`D`, `DB_TestA`/`DB_TestB`(スキル) |
| その他(Prefab, Sceneなど) | アッパーキャメルケース | `MainScene`, `BattleStateMachine`, `SkillButtonPrefab` |

スクリプト(型定義)の接頭辞と、そこから作るデータアセットの接頭辞は別物です。インターフェース(`IBattleState`, `IBattleCommand`)はC#標準の`I`接頭辞を使用。

### その他のルール

- ⭐⭐⭐ `GameObject.Find()`は重い処理なので`Start()`内か生成時イベントでのみ呼ぶ。毎フレーム呼び出す設計は避ける
- ⭐⭐ コメントは「何を」「何のために」を簡潔に
- ⭐⭐ 毎フレーム呼ばれる`Debug.Log`はpush前に削除
- ⭐ クラスは800行以内、メソッドは80行以内、ネストは3重まで目安。責任ごとに関数化し早期return
- ⭐ 意味の通らない英訳・活用・スペルミスは避ける

備考: スクリプトファイルはUTF-8ではなくShift-JIS(CP932)で保存される慣習(`CS_ValueObserver.cs`由来)。動作に影響はないが、UTF-8前提のツールで見ると日本語コメントが文字化けする。

## アーキテクチャ概要

戦闘は状態パターン(`IBattleState`)で進行する。

```
Start → CommandInput → ActionOrder → ActionExecute → JudgeResult →(継続ならCommandInputへ戻る / 決着ならEnd)
```

- `CS_BattleStateMachine`(MonoBehaviour)が唯一の`CS_CommandButtonInput`参照(UIの窓口)を持ち、`_playerPartyData`/`_enemyPartyData`(`List<CSO_CharacterData>`)からAwakeでパーティ全員分の`CS_CharacterState`を生成する
- `ChangeState`は再入防止フラグ(`_isChangingState`)+`while`ループで実装。各Stateの`Enter`が自分自身の中で`ChangeState`を呼んでも、実行中なら次の遷移先を予約するだけで即座には処理しない。これにより決着まで何ラウンドかかっても呼び出しスタックが一定の深さしか使わない(元は再帰呼び出しで、パーティ戦導入後にStackOverflowが発生したための対策)
- `CS_BattleContext`が戦闘全体の共有状態(`allyParty`/`enemyParty`、`actionQueue`、`result`)を保持。`playerState`は`allyParty[0]`(操作キャラクター)を指す。`allyPartyWithoutPlayer`で非操作の味方だけを取得できる。`GetOpposingParty(actor)`/`PickRandomLivingTarget(party)`をターゲティング用ヘルパーとして提供
- コマンドは`IBattleCommand`(`Execute(context, user, target)`)。`CS_AttackCommand`/`CS_SkillCommand`/`CS_EscapeCommand`
- AIの行動決定は静的クラス`CS_BattleAI.DecideCommand(actor)`に集約。`CSO_CharacterData`の`attackWeight`/`skillWeights`(MP不足スキルは除外)による重み付き抽選で「たたかう」か「スキル」かを一発計算で決める(リトライループなし)。操作キャラクター(`allyParty[0]`)はこのメソッドを呼ばない
- UI⇔戦闘ロジックはUnityEventではなくC#の`event Action<T>`で疎結合にしている(`CS_CommandButtonInput.onCommandDecided`, `CS_SkillSelectWindow.onSkillSelected`, `CS_TargetSelectWindow.onTargetSelected`)。ステートクラスはUIのButton/Text/ScrollRect等の内部を一切知らない
- `CS_CommandButtonInput`は操作キャラクターの入力待ち中だけ`Show()`され、それ以外は`Hide()`される。スキル/ターゲット選択のサブウィンドウが開いている間は`CanvasGroup`でボタンを無効化する

### 実装済みスクリプト一覧

```
Assets/Scripts/Character/CSO_CharacterData.cs   # キャラ定義(名前/HP/MP/攻撃力/防御力/素早さ/初期スキル/attackWeight/skillWeights)。OnValidateでskillWeightsの数をinitialSkillsに自動調整
Assets/Scripts/Character/CS_CharacterState.cs   # 実行時状態(現在HP/MP、ダメージ計算・適用、MP消費、現在スキルリスト、attackWeight/skillWeightsの読み取り専用プロパティ)
Assets/Scripts/Command/IBattleCommand.cs        # Execute(context, user, target)
Assets/Scripts/Command/CS_AttackCommand.cs      # たたかう。Debug.Logで誰が誰を攻撃したか出力
Assets/Scripts/Command/CS_SkillCommand.cs       # スキル(インデックス指定、範囲チェックあり)。Debug.Logで使用スキル名とダメージを出力
Assets/Scripts/Command/CS_EscapeCommand.cs      # にげる(素早さ比で成功率算出、成功時はcontext.resultにEscapeを設定)
Assets/Scripts/Skill/CSO_SkillData.cs           # スキル定義(名前/MPコスト/攻撃力倍率)
Assets/Scripts/Battle/CSE_BattleResult.cs       # None/Win/Lose/Escape
Assets/Scripts/Battle/CS_BattleActionEntry.cs   # actor/target/commandを保持する行動順キューの1エントリ
Assets/Scripts/Battle/CS_BattleContext.cs       # allyParty/enemyParty、actionQueue、result、GetOpposingParty、PickRandomLivingTarget
Assets/Scripts/Battle/CS_BattleStateMachine.cs  # ステートマシン本体。ChangeStateは再入防止+ループ。public StartBattle(playerPartyData, enemyPartyData)で外部から任意パーティで起動可能(_hasStartedで二重起動防止)。onBattleEnd(Action<CSE_BattleResult>)で結果を通知
Assets/Scripts/Battle/CS_BattleAI.cs            # DecideCommand(actor)。重み付き抽選、リトライなし
Assets/Scripts/Battle/State/IBattleState.cs
Assets/Scripts/Battle/State/CS_BattleStateStart.cs
Assets/Scripts/Battle/State/CS_BattleStateCommandInput.cs   # allyPartyWithoutPlayer/enemyPartyはAIで決定。操作キャラクター(生存時)はUIで入力を待つ
Assets/Scripts/Battle/State/CS_BattleStateActionOrder.cs    # actionQueueを素早さ降順(同速はally優先)に並べ替え
Assets/Scripts/Battle/State/CS_BattleStateActionExecute.cs  # 実行直前に対象死亡なら再ターゲット。context.result確定でラウンド打ち切り
Assets/Scripts/Battle/State/CS_BattleStateJudgeResult.cs    # 敵/味方両パーティの全滅判定
Assets/Scripts/Battle/State/CS_BattleStateEnd.cs   # Win/Lose/Escapeでログ出し分け
Assets/Scripts/UI/CS_CommandButtonInput.cs      # onCommandDecided(command, target)。たたかう/スキルはターゲット選択を挟む。CanvasGroupで選択中は無効化。Show/Hide
Assets/Scripts/UI/CS_SkillSelectWindow.cs       # スキル一覧を動的ボタン生成、縦スクロール(RectMask2D)
Assets/Scripts/UI/CS_TargetSelectWindow.cs      # 生存している敵の一覧を動的ボタン生成、縦スクロール(RectMask2D)。CS_SkillSelectWindowと同構造
Assets/Editor/CharacterData/CSED_CharacterDataWindow.cs  # Tools > Character Data Table。CSO_CharacterDataを表形式で一覧・直接編集。新規キャラクター/新規スキル作成、スキル自体(名前/コスト/倍率)の編集、列幅ドラッグ調整+EditorPrefs保存に対応
Assets/Scripts/Field/CS_PlayerMove.cs           # フィールド移動。Rigidbody2D.MovePositionで上下左右に自由移動(Input.GetAxisRaw)
Assets/Scripts/Field/CSO_EncounterData.cs       # エンカウント定義(出現する敵パーティのリスト)。DB_接頭辞でAssets/Data/EncounterData/に配置
Assets/Scripts/Field/CS_EncounterSymbol.cs      # エンカウントシンボル。接触判定+確率+クールタイムで発生を制御し、複数のCSO_EncounterDataからランダム抽選。成立するとCS_GameManager.RequestBattleを呼ぶ
Assets/Scripts/CS_GameManager.cs                # シーンをまたぐ橋渡し役(DontDestroyOnLoadシングルトン)。RequestBattle/ConsumePendingEncounter/ReturnToField/ReturnTownでフィールド⇔戦闘を仲介
Assets/Scripts/CS_SceneManager.cs               # SceneManagerのAdditiveロード/アンロードをコールバック付きでラップする薄いシングルトン
Assets/Scripts/CS_BattleResultHandler.cs        # BattleSceneに配置。CS_BattleStateMachine.onBattleEndを購読し、Win/Escape→ReturnToField、Lose→ReturnTownに分岐
Assets/Prefabs/Battle/SkillButtonPrefab.prefab
Assets/Prefabs/Battle/EnemyTargetButtonPrefab.prefab
Assets/Prefabs/Field/Player.prefab              # SpriteRenderer+Rigidbody2D(Interpolate)+Collider2D+CS_PlayerMove。子にMain Camera(追従用、FieldScene用)
Assets/Data/Character/Slime/DB_Char_Slime.asset      # 最弱、たたかうのみ
Assets/Data/Character/Goblin/DB_Char_Goblin.asset    # Slime比で全ステータス2倍、たたかうのみ
Assets/Data/Character/Skeleton/DB_Char_Skeleton.asset + DB_Skill_Skeleton_BoneSlash.asset  # バランス型、attackWeight/skillWeight半々
Assets/Data/Character/Golem/DB_Char_Golem.asset      # 高HP高防御の低速タンク、スキルなし
Assets/Data/Character/Wyvern/DB_Char_Wyvern.asset + DB_Skill_Wyvern_BlazeBreath.asset / DB_Skill_Wyvern_ClawRush.asset  # 最強格、スキル比重高め
Assets/Data/Test/                                    # 旧DB_TestCharacterA〜D・DB_TestA〜Dの退避先(削除はしていない)
Assets/Scenes/FieldScene.unity                  # フィールド検証用シーン。Grid+Tilemap(Tilemap Collider 2D)+Player+CS_GameManager。Player/Main Camera以外(Grid、エンカウントシンボル)は`FieldEnvironment`配下にまとめてある
Assets/Scenes/BattleScene.unity                 # 戦闘シーン(旧MainSceneをリネーム)。BattleStateMachine+CS_BattleResultHandlerを配置。FieldSceneにAdditiveで重ねてロードされる
Assets/Tiles/Square.asset                       # 壁タイル(Assets/Sprites/TestSprite.pngベース)
```

`BattleScene`の`BattleStateMachine`は`Player Party Data`にWyvern(操作キャラクター)・Slime、`Enemy Party Data`にGolem・Goblin・Skeletonを登録した2vs3のテスト編成(タスク7での動作確認用のInspectorデフォルト値。フィールドのエンカウント経由で開始した場合は`CS_GameManager`から受け取った敵パーティで上書きされる)。

## 既知の割り切り・未解決事項

- HP/MPの画面表示は未実装(タスク4で方針変更)。将来的にキャラクター素材の上に重ねるHPバーとして別途実装予定
- `Tools > Value Observer`へのHP監視登録(`CS_ValueObserver.Instance.Register`)はパーティ対応のリファクタ時に削除済み。デバッグ用ツールなので復活は必須ではない
- 両者が同ターンで倒れた場合、敵の撃破判定を優先して「勝利」扱いになる(仕様として割り切り)
- `Assets/_Recovery/`のクラッシュ復旧用シーンファイルは削除済み。`Assets/TextMesh Pro/Examples & Extras/`は引き続きリポジトリに残っており、`.gitignore`対象にすることを推奨(未対応、ブロッカーではない)

## 進捗状況(タスク一覧)

1. **[完了]** 基本戦闘プロトタイプ(1vs1・たたかうのみ)
2. **[完了]** ダメージ計算式の改善(防御力・乱数)
3. **[完了]** スキルコマンドの追加(MP消費)
4. **[完了]** コマンド選択の仮UI実装(ボタン+イベント駆動、スキル動的選択)
5. **[完了]** にげるコマンドの追加
6. **[完了]** パーティ戦(モンハンストーリーズ式)への拡張
   - 6-1 データ構造のパーティ対応
   - 6-2 行動順序・AI(重み付き個性)のパーティ対応
   - 6-3 操作キャラクターのコマンド入力+クリックターゲティング(UI)
   - 6-4 勝敗判定のパーティ対応(全滅判定)
7. **[完了]** 敵データの追加とバランスの土台作り
   - Slime/Goblin/Skeleton/Golem/Wyvernの5体を新規作成。HP/攻撃力/防御力/素早さに難易度差をつけ、Skeleton・Wyvernにはスキルを持たせて`attackWeight`/`skillWeights`で個性(スキル寄り)を表現した
   - 副産物として`Tools > Character Data Table`(`CSED_CharacterDataWindow`)を新規作成し、キャラクター/スキルデータの一覧・直接編集ができるようにした
   - シーンの`Enemy Party Data`に組み合わせて配置し、戦闘が最後まで問題なく動作することを確認済み
8. **[完了]** Editorスクリプトの命名規則統一(`CS_ValueObserverWindow`→`CSED_ValueObserverWindow`)
9. **[完了]** マップ/探索機能の実装(フィールド移動・エンカウント・戦闘への遷移)。フィールドは2Dトップダウン(Tilemap)で作成
   - 9-1 **[完了]** フィールド用シーンとプレイヤー移動
     - `FieldScene`を作成し、Grid+Tilemap(`Tilemap Collider 2D`)+Tile Paletteで壁タイルを配置
     - `Player.prefab`(SpriteRenderer+Rigidbody2D+Collider2D+`CS_PlayerMove`)で上下左右に移動、壁で衝突して止まることを確認済み
   - 9-2 **[完了]** エンカウント判定の実装
     - `CSO_EncounterData`(`_enemyDataList`)と`CS_EncounterSymbol`(`OnTriggerEnter2D`でプレイヤー判定)を新設
     - 要件を上回り、エンカウント確率(`_encounterRate`)・クールタイム(`_encounterCoolTime`)・複数エンカウントからのランダム抽選、および未設定データへのnullガードも実装済み
     - `FieldScene`にシンボルを配置し、接触時に敵パーティ名がログ出力されることを確認済み
   - 9-3 **[完了]** 戦闘システムの外部起動対応(既存コードのリファクタ)
     - `CS_BattleStateMachine`にパーティ構築処理を`BuildContext`として切り出し、`public StartBattle(playerPartyData, enemyPartyData)`を追加。`_hasStarted`フラグで`Start()`側の自動起動と外部からの`StartBattle`呼び出しが二重に走らないようガード
     - `public event Action<CSE_BattleResult> onBattleEnd`と`NotifyBattleEnd(result)`を追加し、`CS_BattleStateEnd.Enter()`から結果を通知するようにした
     - `BattleScene`(旧`MainScene`)の2vs3自動戦闘の動作は維持したまま、外部からの任意パーティ起動にも対応
   - 9-4 **[完了]** マップ→戦闘→マップの遷移実装
     - `BattleScene`は`SceneManager.LoadSceneAsync(Additive)`で`FieldScene`に重ねてロードする方式に決定(Single方式だとFieldSceneが毎回作り直され、プレイヤー位置やエンカウントシンボルのクールタイムが戦闘のたびにリセットされる問題があったため)
     - `CS_GameManager`が`_player`(Playerのroot GameObject)と`_fieldEnvironment`(Grid・エンカウントシンボルの親)をキャッシュし、戦闘中は`SetActive(false)`、終了後に`SetActive(true)`で一括休止/再開。Playerを個別のスクリプト単位で止めるのではなくGameObjectごと止めているが、Trigger再発火はエンカウントのクールタイムが凍結される(Updateごと止まる)ため実害なしと確認済み
     - フィールド用のMain Cameraは`Player.prefab`の子オブジェクトにして追従させる方式に変更(`Rigidbody2D.Interpolate`も有効化し追従を滑らかに)。これにより`CS_GameManager`側でカメラを個別に制御する必要がなくなった
     - 勝利/逃走→`ReturnToField()`、敗北→`ReturnTown()`(現状は中身が同じ`FieldScene`復帰。将来、町・宿屋シーンができたら`ReturnTown`側だけ差し替える想定)
     - パーティのHP/MPは毎回`CSO_CharacterData`の初期値から組み直す簡易方式(Additive方式によりフィールド側の状態=位置・クールタイムは保持されるが、パーティのHP/MP自体は戦闘のたびにフルリセットされる。恒久的な引き継ぎは将来のセーブ/ロードタスクで対応)
     - 受け入れ条件: フィールドでシンボルに接触→`BattleScene`が重なって表示され戦闘開始→勝利/逃走/敗北いずれでも`FieldScene`(位置・クールタイム維持)に戻る、を確認済み

## 今後の候補(タスク9以降、未着手)

- セーブ/ロード(パーティ状態・進行状況の永続化)
- HP/MPの画面表示(キャラクター素材上のHPバー)
- アイテムのマスタデータ設計
- 装備・成長(レベル/経験値)システム
