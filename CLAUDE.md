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
Assets/Scripts/Battle/CS_BattleStateMachine.cs  # ステートマシン本体。ChangeStateは再入防止+ループ
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
Assets/Prefabs/SkillButtonPrefab.prefab
Assets/Prefabs/EnemyTargetButtonPrefab.prefab
Assets/Data/Character/DB_TestCharacterA〜D.asset
Assets/Data/Skill/DB_TestA.asset / DB_TestB.asset
```

シーン(`MainScene`)の`BattleStateMachine`は`Player Party Data`にA(操作キャラクター)・D、`Enemy Party Data`にB・Cを登録した2vs2のテスト編成。

## 既知の割り切り・未解決事項

- HP/MPの画面表示は未実装(タスク4で方針変更)。将来的にキャラクター素材の上に重ねるHPバーとして別途実装予定
- `Tools > Value Observer`へのHP監視登録(`CS_ValueObserver.Instance.Register`)はパーティ対応のリファクタ時に削除済み。デバッグ用ツールなので復活は必須ではない
- 両者が同ターンで倒れた場合、敵の撃破判定を優先して「勝利」扱いになる(仕様として割り切り)
- `Assets/_Recovery/`にUnityのクラッシュ復旧用シーンファイルが誤ってコミットされている。`Assets/TextMesh Pro/Examples & Extras/`とあわせて`.gitignore`対象にすることを推奨(未対応、ブロッカーではない)

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
7. **[進行中]** 敵データの追加とバランスの土台作り
   - 難易度違いの敵キャラクターデータ(`CSO_CharacterData`、`DB_`接頭辞)を3〜5体新規作成する
   - HP/攻撃力/防御力/素早さに難易度差をつける
   - 少なくとも1〜2体はスキルを持たせ、`attackWeight`/`skillWeights`で行動の個性(たたかう寄り/スキル寄り)を表現する
   - 既存の`DB_TestA`/`DB_TestB`スキルを再利用してもよいし、新しいスキルデータを追加してもよい
   - シーンの`Enemy Party Data`に新しい敵データを組み合わせて配置し、実際に戦闘が最後まで問題なく動作することを確認する
   - 敵キャラクター名の候補(英語名5案): Slime(最弱、たたかうのみ)/ Goblin(素早さ重視、attackWeight高め)/ Skeleton(バランス型、スキル半々)/ Golem(高HP高防御の低速タンク)/ Wyvern(最強、スキル(高倍率技)を積極使用)
8. **[完了]** Editorスクリプトの命名規則統一(`CS_ValueObserverWindow`→`CSED_ValueObserverWindow`)

## 今後の候補(タスク7以降、未着手)

- マップ/探索(フィールド移動、エンカウント、戦闘への遷移)
- セーブ/ロード(パーティ状態・進行状況の永続化)
- HP/MPの画面表示(キャラクター素材上のHPバー)
- アイテムのマスタデータ設計
- 装備・成長(レベル/経験値)システム
