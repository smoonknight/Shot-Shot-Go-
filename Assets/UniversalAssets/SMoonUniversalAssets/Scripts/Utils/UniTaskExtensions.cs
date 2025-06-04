using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SMoonUniversalAsset
{
    public static partial class UniTaskExtensions
    {
        public static async UniTask DelayWithCancel(float duration, CancellationToken token)
        {
            float time = 0f;
            while (time < duration)
            {
                if (token.IsCancellationRequested)
                    return;

                time += Time.deltaTime;
                await UniTask.Yield();
            }
        }
    }
}