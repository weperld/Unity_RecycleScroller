using UnityEngine;

namespace RecycleScroll.Samples
{
    /// <summary>
    /// IRecycleScrollerDelegate 기본 구현 예제.
    /// 셀마다 크기가 다른 가변 크기 목록을 다룬다.
    /// </summary>
    public class BasicUsageSample : MonoBehaviour, IRecycleScrollerDelegate
    {
        [SerializeField] private RecycleScroller m_scroller;
        [SerializeField] private RecycleScrollerCell m_cellPrefab;
        [SerializeField] private int m_cellCount = 100;

        [Header("셀 크기 (주축 = 스크롤 방향)")]
        [SerializeField] private float m_cellCrossAxisSize = 400f;
        [SerializeField] private Vector2 m_cellSizeRange = new(80f, 160f);

        private void Start()
        {
            if (m_scroller == false || m_cellPrefab == false)
            {
                Debug.LogError("[BasicUsageSample] scroller / cellPrefab 참조를 Inspector에서 연결해주세요");
                return;
            }

            m_scroller.del = this;
            m_scroller.LoadData();
        }

        public int GetCellCount(RecycleScroller scroller) => m_cellCount;

        /// <summary>
        /// 셀이 차지할 크기를 "선언"한다. 스크롤러는 이 값을 배치 공간 계산에만 사용하고
        /// 셀의 RectTransform은 건드리지 않는다
        /// </summary>
        public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
        {
            // 프리팹의 크기·스케일을 그대로 읽는 생성자도 있다 (Use Child Scale 설정을 자동 반영)
            //   return new RSCellRect(m_cellPrefab.rectTransform, scroller);
            //
            // 여기서는 인덱스별로 크기가 달라지므로 직접 선언한다.
            // 셀 프리팹에 localScale을 적용했다면 스케일도 함께 선언할 것
            // (Inspector의 Use Child Scale이 켜진 축만 반영)
            var scale = m_cellPrefab.rectTransform.localScale;
            var useScale = new Vector2(
                scroller.UseChildScale.Width ? scale.x : 1f,
                scroller.UseChildScale.Height ? scale.y : 1f);

            return new RSCellRect(GetCellSize(dataIndex), m_cellCrossAxisSize,
                useScale.Size(scroller.ScrollAxis),
                useScale.CrossAxisSize(scroller.ScrollAxis));
        }

        public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
        {
            var cell = scroller.GetCellInstance(m_cellPrefab, dataIndex);

            // 셀 크기 세팅은 델리게이트 책임이다.
            // 재활용된 셀에는 이전 인덱스의 크기가 남아 있으므로, 가변 크기 목록이라면
            // 매번 세팅해야 한다. 모든 셀이 같은 크기라면 이 블록 자체가 필요 없다.
            //
            // 반드시 ToUnScaledValues(스케일 적용 전)를 넣을 것 —
            // ToScaledValues를 넣으면 localScale과 이중으로 곱해진다
            var size = GetCellRect(scroller, dataIndex).ToUnScaledValues;
            cell.UpdateCellSize(scroller.ScrollAxis == eScrollAxis.VERTICAL
                ? new Vector2(size.CrossAxisSize, size.Size)
                : new Vector2(size.Size, size.CrossAxisSize));

            // 여기서 셀에 데이터를 채운다
            return cell;
        }

        /// <summary>인덱스별 고정 의사난수 — 같은 인덱스는 항상 같은 크기</summary>
        private float GetCellSize(int dataIndex)
        {
            if (m_cellSizeRange.y <= m_cellSizeRange.x) return m_cellSizeRange.x;

            var t = Mathf.Abs(Mathf.Sin((dataIndex + 1) * 12.9898f) * 43758.5453f) % 1f;
            return Mathf.Lerp(m_cellSizeRange.x, m_cellSizeRange.y, t);
        }
    }
}
