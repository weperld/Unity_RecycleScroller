using RecycleScroll;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestCell : RecycleScrollerCell
{
    private TextMeshProUGUI m_text;
    private Image m_bg;

    public override void OnCellBecameVisible(RecycleScroller scroller, int dataIndex)
    {
        if (m_text == null) m_text = GetComponentInChildren<TextMeshProUGUI>();
        if (m_bg == null) m_bg = GetComponent<Image>();

        if (m_text != null) m_text.text = $"Cell {dataIndex}";

        if (m_bg != null)
        {
            // dataIndex에 따라 색상 변경
            float hue = (dataIndex * 0.07f) % 1f;
            m_bg.color = Color.HSVToRGB(hue, 0.3f, 0.95f);
        }
    }

    public override void OnCellBecameInvisible(RecycleScroller scroller)
    {
        // 풀로 반환될 때 정리할 내용이 있으면 여기에
    }
}
