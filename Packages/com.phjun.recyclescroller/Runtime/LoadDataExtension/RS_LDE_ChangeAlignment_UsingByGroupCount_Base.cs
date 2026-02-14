using CustomSerialization;
using UnityEngine;

namespace RecycleScroll
{
    public partial class RecycleScroller
    {
        public void OverwriteChildAlignment(TextAnchor textAnchor)
        {
            m_overwriteChildAlignment.Overwrite(textAnchor);
        }

        public void RemoveOverwriteChildAlignment()
        {
            m_overwriteChildAlignment.RemoveOverwrite();
        }
    }

    public abstract class ConditionBase
    {
        public abstract bool IsSatisfied(int groupCount);
    }

    [DisallowMultipleComponent]
    public abstract class RS_LDE_ChangeAlignment_UsingByGroupCount_Base<TConditionType> : LoadDataExtensionComponent
        where TConditionType : ConditionBase
    {
        [SerializeField] private SerializableDictionary<TextAnchor, TConditionType> m_overwriteChildAlignment;

        public override void LoadDataExtendFunction(RecycleScroller scroller, eLoadDataResultState state)
        {
            if (state is not eLoadDataResultState.Complete) return;

            var groupCount = scroller.GroupCount;

            // Dictionary 순회
            // 가장 먼저 조건이 true가 되는 TextAnchor를 찾으면 적용
            foreach (var (textAnchor, exprData) in m_overwriteChildAlignment)
            {
                if (exprData.IsSatisfied(groupCount))
                {
                    Overwrite(scroller, textAnchor);
                    return;
                }
            }

            // 어떤 조건에도 해당되지 않으면 Overwrite 제거
            scroller.RemoveOverwriteChildAlignment();
        }

        protected static void Overwrite(RecycleScroller scroller, TextAnchor textAnchor)
        {
            scroller.OverwriteChildAlignment(textAnchor);
            scroller.ReloadCellView();
        }
    }
}