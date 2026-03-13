using System;
using UnityEngine;

namespace RecycleScroll
{
    public struct CellSizeVector
    {
        public float Size { get; }
        public float CrossAxisSize { get; }

        public CellSizeVector(float size, float crossAxisSize)
        {
            this.Size = size;
            this.CrossAxisSize = crossAxisSize;
        }
    }
    
    public struct RSCellRect
    {
        private struct Value
        {
            private float m_val;
            private float m_scale;

            public float Val => m_val;
            public float Scale => m_scale;
            public float ScaledVal => m_val * m_scale;

            public Value(float val, float scale)
            {
                this.m_val = val;
                this.m_scale = scale;
            }
        }
        
        private readonly Value m_sizeValue;
        private readonly Value m_crossAxisSizeValue;

        public float SizeScale => m_sizeValue.Scale;
        public float CrossAxisSizeScale => m_crossAxisSizeValue.Scale;
        public float UnScaledSize => m_sizeValue.Val;
        public float UnScaledCrossAxisSize => m_crossAxisSizeValue.Val;
        public float ScaledSize => m_sizeValue.ScaledVal;
        public float ScaledCrossAxisSize => m_crossAxisSizeValue.ScaledVal;

        public RSCellRect(float size, float crossAxisSize, float scaleSize, float scaleCrossAxisSize)
        {
            m_sizeValue = new Value(size, scaleSize);
            m_crossAxisSizeValue = new Value(crossAxisSize, scaleCrossAxisSize);
        }
        public RSCellRect(float size, float crossAxisSize, float scale = 1f) : this(size, crossAxisSize, scale, scale) { }
        public RSCellRect(Rect rect, Vector2 scale, eScrollAxis scrollAxis) : this(rect.size.Size(scrollAxis), rect.size.CrossAxisSize(scrollAxis),
            scale.Size(scrollAxis), scale.CrossAxisSize(scrollAxis)) { }
        public RSCellRect(RectTransform rtf, eScrollAxis scrollAxis, Vector2 scale) : this(rtf.rect, scale, scrollAxis) { }
        public RSCellRect(RectTransform rtf, eScrollAxis scrollAxis, float scale = 1f) : this(rtf.rect, new Vector2(scale, scale), scrollAxis) { }
        public RSCellRect(RectTransform rtf, RecycleScroller scroller)
            : this(
                rtf,
                scroller.ScrollAxis,
                new Vector2
                {
                    x = scroller.UseChildScale.Width ? rtf.localScale.x : 1f,
                    y = scroller.UseChildScale.Height ? rtf.localScale.y : 1f
                })
        { }
        
        public CellSizeVector ToUnScaledValues => new(UnScaledSize, UnScaledCrossAxisSize);
        public CellSizeVector ToScaledValues => new(ScaledSize, ScaledCrossAxisSize);
        
        public static RSCellRect Default => new(0f, 0f);
    }
    
    public interface IRecycleScrollerDelegate
    {
        /// <summary>
        /// Warning: cellViewIdx의 값은 GetCell이 완료된 후에 RecycleScrollerCell의 CellViewIndex에 업데이트됨<para/>
        /// 이미 Viewport영역 내에 생성되어 있는 cell을 재사용한다면 이 함수는 호출되지 않음. RecycleScrollerCell의 CellViewIndex 업데이트만 진행
        /// </summary>
        /// <param name="scroller"></param>
        /// <param name="dataIndex"></param>
        /// <param name="cellViewIndex">이 파라미터 값과 RecycleScrollerCell의 CellViewIndex값은 GetCell 내부에서 비교할 경우 서로 다를 가능성이 있음</param>
        /// <returns></returns>
        public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex);
        public int GetCellCount(RecycleScroller scroller);
        public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex);
    }
    
    public class RecycleScrollDelegate : IRecycleScrollerDelegate
    {
        public Func<RecycleScroller, int, int, RecycleScrollerCell> action_GetCell;
        public Func<RecycleScroller, int> action_GetCellCount;
        public Func<RecycleScroller, int, RSCellRect> action_GetCellRect;
        
        public RecycleScrollerCell GetCell(RecycleScroller scroller, int dataIndex, int cellViewIndex)
        {
            if (action_GetCell == null) return null;
            
            return action_GetCell.Invoke(scroller, dataIndex, cellViewIndex);
        }
        
        public int GetCellCount(RecycleScroller scroller)
        {
            if (action_GetCellCount == null) return 0;
            
            return action_GetCellCount.Invoke(scroller);
        }
        
        public RSCellRect GetCellRect(RecycleScroller scroller, int dataIndex)
        {
            if (action_GetCellRect == null) return RSCellRect.Default;
            
            return action_GetCellRect.Invoke(scroller, dataIndex);
        }
    }
    
    public static class Extensions_ForRecycleScroller
    {
        public static float Size(this Vector2 v2, eScrollAxis axis)
            => axis switch
            {
                eScrollAxis.VERTICAL => v2.y,
                eScrollAxis.HORIZONTAL => v2.x,
                _ => 0f
            };
        public static float CrossAxisSize(this Vector2 v2, eScrollAxis axis)
            => axis switch
            {
                eScrollAxis.VERTICAL => v2.x,
                eScrollAxis.HORIZONTAL => v2.y,
                _ => 0f
            };

        public static float Size(this Vector3 v3, eScrollAxis axis) => ((Vector2)v3).Size(axis);
        public static float CrossAxisSize(this Vector3 v3, eScrollAxis axis) => ((Vector2)v3).CrossAxisSize(axis);
    }
}