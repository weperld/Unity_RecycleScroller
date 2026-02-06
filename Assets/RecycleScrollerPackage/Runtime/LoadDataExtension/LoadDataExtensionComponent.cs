using UnityEngine;

namespace RecycleScroll
{
    [HelpBox("적용 시키고자 하는 RecycleScroller 컴포넌트가 부착된 게임오브젝트에 추가해 주어야 올바르게 작동합니다.")]
    [RequireComponent(typeof(RecycleScroller))]
    public abstract class LoadDataExtensionComponent : MonoBehaviour
    {
        public abstract void LoadDataExtendFunction(RecycleScroller scroller, LoadDataResultState state);
    }
}