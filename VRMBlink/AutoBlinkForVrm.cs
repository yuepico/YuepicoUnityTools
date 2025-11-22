using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

namespace UniVRM10.UserComponents
{
    public class AutoBlinkForVrm : MonoBehaviour
    {
        [Tooltip("瞬きさせるモデル")]
        public Vrm10Instance VRM10;

        [Tooltip("瞬きさせるかどうか")]
        public bool IsActive = true;

        [Tooltip("瞬きの強さ（表情の目の開き具合に合わせる）")]
        [Range(0, 2.0f)]
        public float ModulateRatio = 1.0f;

        public BlinkParameterSet blinkParameters = new BlinkParameterSet();

        [Header("首の動きで瞬きを誘発する設定(必要なら使用)")]
        public Transform HeadObject; // ← 自動アサイン等は割愛

        // 瞬き途中の状態を管理
        private TransitionPlayer player;
        private ExpressionKey? currentBlinkKey = null;

        /// <summary>
        /// 現在瞬き中かどうか
        /// </summary>
        public bool IsBlinking
        {
            get { return player != null && !player.IsFinished; }
        }

        void Start()
        {
            // ランダム瞬きを開始
            StartCoroutine(BlinkSignaler());
        }

        void LateUpdate()
        {
            // 瞬き中ならウェイトを更新
            if (IsBlinking && currentBlinkKey.HasValue)
            {
                float weight = player.Next(Time.deltaTime);
                VRM10.Runtime.Expression.SetWeight(currentBlinkKey.Value, weight);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// ランダムに瞬きを実行するコルーチン
        /// </summary>
        private IEnumerator BlinkSignaler()
        {
            while (true)
            {
                if (IsActive && !IsBlinking)
                {
                    float seed = UnityEngine.Random.Range(0.0f, 1.0f);
                    if (seed > blinkParameters.randomThreshold)
                    {
                        float patternSeed = UnityEngine.Random.Range(0.0f, 1.0f);
                        if (patternSeed < blinkParameters.partialBlinkProbability)
                        {
                            BlinkBothPartial();
                        }
                        else
                        {
                            BlinkBoth();
                        }
                    }
                }
                // 毎回新たに WaitForSeconds を生成するが、ランダム値のため仕方ない
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.0f, 3.0f));
            }
        }

        /// <summary>通常の両目瞬き</summary>
        private void BlinkBoth()
        {
            currentBlinkKey = ExpressionKey.CreateFromPreset(ExpressionPreset.blink);
            float currentWeight = VRM10.Runtime.Expression.GetWeight(currentBlinkKey.Value);

            // 「都度 new」せずに、キャッシュ済みの Transition データを使えるようにする例
            // ただし ratioHalf, ratioClose, ModulateRatio などが動的変更される場合は要注意
            var transition = CreateBlinkTransitionCached(
                blinkParameters.ratioHalf,
                blinkParameters.ratioClose,
                blinkParameters.closeDuration,
                blinkParameters.openDuration
            );

            // TransitionPlayer を生成
            player = new TransitionPlayer(transition, currentWeight);
        }

        /// <summary>両目の部分的瞬き</summary>
        private void BlinkBothPartial()
        {
            currentBlinkKey = ExpressionKey.CreateFromPreset(ExpressionPreset.blink);
            float currentWeight = VRM10.Runtime.Expression.GetWeight(currentBlinkKey.Value);

            // キャッシュ済みのデータを使う場合はパラメータを適宜変える
            var transition = CreateBlinkTransitionCached(
                blinkParameters.ratioHalf * 0.5f,
                blinkParameters.ratioClose * 0.5f,
                blinkParameters.closeDuration,
                blinkParameters.openDuration
            );

            player = new TransitionPlayer(transition, currentWeight);
        }

        // =================================================================
        // ★ 改良ポイント: Transition を使い回す仕組みの例
        //    1) 4キー固定なら事前に配列を作り、Update毎にウェイトだけ変更する方法もある
        //    2) ここでは "必要に応じてパラメータを変える" 場合、都度 new する代わりに
        //       クラス内の同じ配列/リストを使い回す例を示す
        // =================================================================
        private Transition CreateBlinkTransitionCached(
            float half,
            float close,
            float closeDur,
            float openDur
        )
        {
            // 再利用用に1つだけインスタンスを持ち回りすると、複数の瞬きが同時進行できないので
            // ここでは「インスタンス自体は new するが中身のリストを作らずに再利用する」例を示す。

            // 1) TransitionKeyの配列を事前に持つ
            //    (本クラスのフィールドに static or readonly で宣言してもOK)
            // 2) その配列の要素を書き換えて使い回す（都度 List<> を new しない）

            var transition = new Transition();
            float closePartDuration = closeDur / 2f;
            float openPartDuration = openDur / 2f;

            // 4ステップのキーを都度「AddKey」するのではなく、listを再利用してセットする
            // ここでは簡易的に "生成する" けれど LinqやAsEnumerableを排除し、Listを直接操作

            transition.ClearAndAdd(
                new Transition.TransitionKey(half * ModulateRatio,    closePartDuration),
                new Transition.TransitionKey(close * ModulateRatio,   closePartDuration),
                new Transition.TransitionKey(half * ModulateRatio,    openPartDuration),
                new Transition.TransitionKey(0f,                      openPartDuration)
            );

            return transition;
        }

        #region BlinkParameterSet
        [Serializable]
        public class BlinkParameterSet
        {
            [Range(0, 1.0f)]
            public float ratioHalf = 0.3f;
            [Range(0, 1.0f)]
            public float ratioClose = 0.9f;

            public float closeDuration = 0.1f;
            public float openDuration = 0.2f;

            [Range(0, 1.0f)]
            public float randomThreshold = 0.7f;

            [Range(0, 1.0f)]
            public float partialBlinkProbability = 0.2f;
        }
        #endregion

        #region Transition
        public class Transition
        {
            // ★ 改良: 直接リストを公開して走査し、LINQやAsEnumerable()は使わない
            private List<TransitionKey> keys = new List<TransitionKey>(4); // 初期容量4

            /// <summary>現在のキー一覧を直接取得</summary>
            public List<TransitionKey> Keys => keys;

            /// <summary>リストを一度クリアし、指定したキーをまとめて追加するユーティリティ</summary>
            public void ClearAndAdd(params TransitionKey[] newKeys)
            {
                keys.Clear();
                keys.AddRange(newKeys);
            }

            // これまでのようにAddKeyするメソッドも一応残しておく
            public Transition AddKey(float weight, float duration)
            {
                keys.Add(new TransitionKey(weight, duration));
                return this;
            }

            [Serializable]
            public class TransitionKey
            {
                public float targetWeight;
                public float duration;

                public TransitionKey(float targetWeight, float duration)
                {
                    this.targetWeight = targetWeight;
                    this.duration = duration;
                }
            }
        }

        private class TransitionPlayer
        {
            private readonly List<Transition.TransitionKey> keys;
            private Transition.TransitionKey previousKey;
            private Transition.TransitionKey currentKey;
            private float currentTime; // 経過時間

            public bool IsFinished { get; private set; }

            public TransitionPlayer(Transition t, float startingWeight)
            {
                // newしても参照切れになるとGC対象になるのでリークしない
                keys = t.Keys; 
                // 先頭キーがあるか確認
                if (keys.Count == 0)
                {
                    IsFinished = true;
                    return;
                }

                // 前の状態として "startingWeight" をキー化
                previousKey = new Transition.TransitionKey(startingWeight, 0f);
                currentKey = keys[0];
                keys.RemoveAt(0);

                currentTime = 0f;
                IsFinished = false;
            }

            public float Next(float deltaTime)
            {
                if (IsFinished)
                {
                    return currentKey.targetWeight;
                }

                currentTime += deltaTime;
                if (currentTime > currentKey.duration)
                {
                    if (keys.Count == 0)
                    {
                        // もう遷移が無ければ終了
                        IsFinished = true;
                        return currentKey.targetWeight;
                    }
                    // 次キーへ切り替え
                    previousKey = currentKey;
                    currentKey = keys[0];
                    keys.RemoveAt(0);
                    currentTime = 0f;
                }

                // 線形補間
                float ratio = currentKey.duration > 0f
                    ? (currentTime / currentKey.duration)
                    : 1f;

                return Mathf.Lerp(
                    previousKey.targetWeight,
                    currentKey.targetWeight,
                    ratio
                );
            }

            public void Abort()
            {
                IsFinished = true;
            }
        }
        #endregion
    }
}
