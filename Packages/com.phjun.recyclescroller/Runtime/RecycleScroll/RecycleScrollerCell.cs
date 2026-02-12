using UnityEngine;

namespace RecycleScroll
{
    public class RecycleScrollerCell : MonoBehaviour
    {
        private RectTransform m_RectTransform;
        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    if (!gameObject.TryGetComponent<RectTransform>(out m_RectTransform))
                        m_RectTransform = gameObject.AddComponent<RectTransform>();
                    m_RectTransform.pivot = Vector2.up;
                    m_RectTransform.anchorMin = Vector2.up;
                    m_RectTransform.anchorMax = Vector2.up;
                }

                return m_RectTransform;
            }
        }
        
        public string PoolSubKey { get; internal set; } = RecycleScroller.DEFAULT_POOL_SUBKEY;

        /// <summary>
        /// 활성화된 셀 중 몇 번째의 셀인지에 대한 번호<para/>
        /// 비활성화: -1, 활성화: 0 ~
        /// </summary>
        public int CellViewIndex { get; private set; } = -1;
        public void SetCellViewIndex(int i) => CellViewIndex = i;

        public void UpdateCellSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// 셀이 풀에서 꺼내져 뷰포트에 배치될 때 호출
        /// </summary>
        public virtual void OnBecameVisible(RecycleScroller scroller, int dataIndex) { }

        /// <summary>
        /// 셀이 뷰포트에서 벗어나 풀로 반환될 때 호출
        /// </summary>
        public virtual void OnBecameInvisible(RecycleScroller scroller) { }
    }
    
    public static class Extension_RecycleScrollerCell
    {
        public static TCell AddOrGetRSCell<TCell>(this GameObject gameObject) where TCell : RecycleScrollerCell
        {
            if (gameObject.TryGetComponent<TCell>(out var cell) == false)
                cell = gameObject.AddComponent<TCell>();
            return cell;
        }
        
        public static RecycleScrollerCell AddOrGetRSCell(this GameObject gameObject)
            => gameObject.AddOrGetRSCell<RecycleScrollerCell>();
        
        public static TCell AddOrGetRSCell<TCell>(this Component component) where TCell : RecycleScrollerCell
            => AddOrGetRSCell<TCell>(component.gameObject);
        
        public static RecycleScrollerCell AddOrGetRSCell(this Component component)
            => component.AddOrGetRSCell<RecycleScrollerCell>();
    }
}