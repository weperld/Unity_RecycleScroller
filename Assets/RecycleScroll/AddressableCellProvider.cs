#if ENABLE_ADDRESSABLES
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RecycleScroll
{
    /// <summary>
    /// Addressable 기반 셀 프리팹 비동기 로드 어댑터.
    /// LoadData 호출 전에 PreloadCellPrefabsAsync()를 호출하여 프리팹을 캐시한 뒤,
    /// 캐시된 프리팹을 GetCell()에서 사용합니다.
    ///
    /// <para><b>사용 예시:</b></para>
    /// <code>
    /// // 1. 컴포넌트 참조
    /// [SerializeField] private RecycleScroller scroller;
    /// [SerializeField] private AddressableCellProvider cellProvider;
    ///
    /// // 2. 초기화 (Start 또는 별도 Init에서)
    /// private async UniTaskVoid Start()
    /// {
    ///     // Addressable 키로 프리팹을 미리 로드
    ///     await cellProvider.PreloadCellPrefabsAsync("Cell_Chat", "Cell_Image", "Cell_Notice");
    ///
    ///     // 델리게이트 설정 후 LoadData 호출
    ///     scroller.del = new RecycleScrollDelegate
    ///     {
    ///         action_GetCellCount = (s) => dataList.Count,
    ///         action_GetCellRect  = (s, i) => new RSCellRect(100f, 400f),
    ///         action_GetCell      = (s, dataIndex, viewIndex) =>
    ///         {
    ///             // dataIndex에 따라 적절한 Addressable 키 선택
    ///             var data = dataList[dataIndex];
    ///             string key = data.type switch
    ///             {
    ///                 CellType.Chat   => "Cell_Chat",
    ///                 CellType.Image  => "Cell_Image",
    ///                 CellType.Notice => "Cell_Notice",
    ///                 _ => "Cell_Chat"
    ///             };
    ///
    ///             // 캐시된 프리팹으로 셀 인스턴스 획득
    ///             var prefab = cellProvider.GetCachedPrefab(key);
    ///             var cell = s.GetCellInstance(prefab, dataIndex, key);
    ///             cell.UpdateData(data);
    ///             return cell;
    ///         }
    ///     };
    ///     scroller.LoadData();
    /// }
    ///
    /// // 3. 정리 (OnDestroy에서 자동 호출되지만, 씬 전환 시 명시적 호출 가능)
    /// private void OnDisable()
    /// {
    ///     cellProvider.ReleaseAll();
    /// }
    /// </code>
    ///
    /// <para><b>설정 방법:</b></para>
    /// <para>1. Unity Package Manager에서 Addressables 패키지 설치</para>
    /// <para>2. Project Settings > Player > Scripting Define Symbols에 ENABLE_ADDRESSABLES 추가</para>
    /// <para>3. 셀 프리팹을 Addressable로 마킹하고 키(Address) 설정</para>
    /// <para>4. AddressableCellProvider 컴포넌트를 스크롤러와 같은 GameObject에 추가</para>
    /// </summary>
    public class AddressableCellProvider : MonoBehaviour
    {
        private readonly Dictionary<string, RecycleScrollerCell> m_cachedPrefabs = new();
        private readonly List<AsyncOperationHandle<GameObject>> m_handles = new();

        /// <summary>
        /// 셀 프리팹들을 비동기로 미리 로드합니다.
        /// LoadData 호출 전에 await하세요.
        /// </summary>
        public async UniTask PreloadCellPrefabsAsync(params string[] addressKeys)
        {
            foreach (var key in addressKeys)
            {
                if (m_cachedPrefabs.ContainsKey(key)) continue;

                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                m_handles.Add(handle);
                var go = await handle.ToUniTask();
                var cell = go.GetComponent<RecycleScrollerCell>();
                if (cell != null)
                    m_cachedPrefabs[key] = cell;
            }
        }

        /// <summary>
        /// 캐시된 프리팹을 반환합니다. GetCell()에서 프리팹으로 사용하세요.
        /// </summary>
        public RecycleScrollerCell GetCachedPrefab(string addressKey)
        {
            return m_cachedPrefabs.TryGetValue(addressKey, out var prefab) ? prefab : null;
        }

        /// <summary>
        /// 특정 키의 프리팹이 캐시되어 있는지 확인합니다.
        /// </summary>
        public bool HasCachedPrefab(string addressKey) => m_cachedPrefabs.ContainsKey(addressKey);

        /// <summary>
        /// Addressable 핸들을 해제하고 캐시를 정리합니다.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var handle in m_handles)
                if (handle.IsValid()) Addressables.Release(handle);
            m_handles.Clear();
            m_cachedPrefabs.Clear();
        }

        private void OnDestroy() => ReleaseAll();
    }
}
#endif
