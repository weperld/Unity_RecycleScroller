using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CustomSerialization;

public static partial class RecycleScrollerHelper
{
    private static IEnumerator RunYieldInstruction(YieldInstruction yieldInstruction, UniTaskCompletionSource<bool> tcs, CancellationToken? cancellationToken)
    {
        // 실행 전 취소 여부 확인
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested)
        {
            tcs.TrySetCanceled();
            yield break;
        }

        yield return yieldInstruction;

        // yield 완료 후 취소 여부 확인
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested)
        {
            tcs.TrySetCanceled();
            yield break;
        }

        tcs.TrySetResult(true);
    }

    public static UniTask AsTask(this YieldInstruction yieldInstruction, MonoBehaviour monoBehaviour, CancellationToken? cancellationToken = null)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        monoBehaviour.StartCoroutine(RunYieldInstruction(yieldInstruction, tcs, cancellationToken));
        return tcs.Task;
    }

    public static UniTask WaitForEndOfFrameTask(this MonoBehaviour monoBehaviour, CancellationToken? cancellationToken = null)
    {
        return new WaitForEndOfFrame().AsTask(monoBehaviour, cancellationToken);
    }
}