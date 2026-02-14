using UnityEngine;

namespace RecycleScroll
{
    [RequireComponent(typeof(RecycleScroller))]
    public abstract class LoadDataExtensionComponent : MonoBehaviour
    {
        public abstract void LoadDataExtendFunction(RecycleScroller scroller, eLoadDataResultState state);
    }
}
