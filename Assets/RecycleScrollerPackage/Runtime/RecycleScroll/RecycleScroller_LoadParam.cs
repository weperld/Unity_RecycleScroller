using System;
using System.Linq;
using RecycleScroll.Attributes;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        // ↓↓↓참고 사항↓↓↓
        /*
         * 코드 작성 실수 방지차 여러 개의 같은 타입 LoadParam을 입력받을 경우
         * 같은 타입의 LoadParam 중 마지막에 입력된 LoadParam만 실행하도록 하기 위해 OnlyLast 속성 부착
         * 해당 속성 파생 타입 별 상세 작동은 OnlyLastAttributes.cs 참고
         */

        #region LoadData Params

        public abstract class LoadParam
        {
            public abstract void Execute(RecycleScroller scroller);
        }

        #region Scroll Pos Setter

        [OnlyLastIncludingDerived]
        public abstract class LoadParam_ScrollPosSetter : LoadParam
        {
            public abstract override void Execute(RecycleScroller scroller);
        }

        public abstract class LoadParam_UsingScrollPos : LoadParam_ScrollPosSetter
        {
            public float position;

            public sealed override void Execute(RecycleScroller scroller)
            {
                SetPosition(scroller, position);
            }

            protected abstract void SetPosition(RecycleScroller scroller, float position);
        }

        public class LoadParam_NormalScrollPos : LoadParam_UsingScrollPos
        {
            protected override void SetPosition(RecycleScroller scroller, float position)
            {
                scroller.RealNormalizedScrollPosition = position;
            }
        }

        public class LoadParam_NormalScrollPos_Showing : LoadParam_UsingScrollPos
        {
            protected override void SetPosition(RecycleScroller scroller, float position)
            {
                scroller.ShowingNormalizedScrollPosition = position;
            }
        }

        public class LoadParam_DenormalScrollPos : LoadParam_UsingScrollPos
        {
            protected override void SetPosition(RecycleScroller scroller, float position)
            {
                scroller.RealScrollPosition = position;
            }
        }

        public class LoadParam_DenormalScrollPos_Showing : LoadParam_UsingScrollPos
        {
            protected override void SetPosition(RecycleScroller scroller, float position)
            {
                scroller.ShowingScrollPosition = position;
            }
        }

        #region Using Cell Index

        public class LoadParam_FocusToCellIndex : LoadParam_ScrollPosSetter
        {
            public int cellIndex = 0;
            public float offset = 0f;

            public override void Execute(RecycleScroller scroller)
            {
                scroller.JumpToIndex(cellIndex, offset);
            }
        }

        public class LoadParam_FocusToCellIndex_ViewportCenter : LoadParam_ScrollPosSetter
        {
            public int cellIndex = 0;
            public float offset = 0f;

            public override void Execute(RecycleScroller scroller)
            {
                scroller.JumpToIndex_ViewportCenter(cellIndex, offset);
            }
        }

        public class LoadParam_FocusToCellIndex_ViewportCenter_BasedCellCenter : LoadParam_ScrollPosSetter
        {
            public int cellIndex = 0;

            public override void Execute(RecycleScroller scroller)
            {
                scroller.JumpToIndex_ViewportCenter_BasedCellCenter(cellIndex);
            }
        }

        #endregion

        #endregion

        #region Initializer

        [OnlyLastPerDerived]
        public abstract class LoadParam_Initializer : LoadParam
        {
            public abstract override void Execute(RecycleScroller scroller);
        }

        public class LoadParam_UseOneCellRect : LoadParam_Initializer
        {
            public bool useOneCellRect = true;

            public override void Execute(RecycleScroller scroller)
            {
                scroller.m_useOneCellRect = useOneCellRect;
            }
        }

        /// <summary>
        /// 관련 기능은 아직 작업 중...
        /// 루프 기능이 켜진 경우 무시하도록 설정
        /// 루프 기능이 켜져 있을 때 아직 어떤 식으로 처리해야 할 지 마땅한 방법이 떠오르지 않아 이와 같이 결정
        /// </summary>
        public class LoadParam_ScrollOptimization : LoadParam_Initializer
        {
            public bool use;
            public bool fromEnd;
            public int groupLimit;

            public override void Execute(RecycleScroller scroller)
            {
                if (scroller.LoopScrollIsOn) return;

                var values = scroller.m_scrollOptimizationValues;
                values.use = use;
                if (use == false) return;

                values.fromEnd = fromEnd;
                values.limit = groupLimit < 1 ? 100 : groupLimit;
            }
        }

        #endregion

        #endregion

        #region Util

        private bool TryFindLoadParams<T>(LoadParam[] _params, out T[] findItems)
            where T : LoadParam
        {
            if (_params is null or { Length: 0 })
            {
                findItems = null;
                return false;
            }

            findItems = _params.OfType<T>().ToArray();
            return findItems.Length > 0;
        }

        #endregion

        #region Execute LoadParam

        private void ExecuteLoadParam<T>(LoadParam[] _params, Action actionOnNotFound = null)
            where T : LoadParam
        {
            if (_params is null or { Length: 0 }) return;

            if (TryFindLoadParams(_params, out T[] findItems) == false)
            {
                actionOnNotFound?.Invoke();
                return;
            }

            findItems.ActionForOnlyLastAttribute(param => param.Execute(this));
        }

        private void ExecuteInitializer(LoadParam[] _params) => ExecuteLoadParam<LoadParam_Initializer>(_params);

        #endregion
    }
}