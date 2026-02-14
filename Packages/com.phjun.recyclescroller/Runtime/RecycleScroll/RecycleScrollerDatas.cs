using System;
using UnityEngine;
using EaseUtil;

namespace RecycleScroll
{
    public struct CellGroupData
    {
        public int cellCount;
        public int startDataIndex;
        public int endDataIndex => startDataIndex + cellCount - 1;
        
        public float size;
        
        public CellGroupData(int startDataIndex)
        {
            cellCount = 1;
            this.startDataIndex = startDataIndex;
            size = 0f;
        }
    }
    
    [Serializable]
    public struct BoolVector2
    {
        [SerializeField] public bool width, height;

        public ReadOnlyBoolVector2 AsReadOnly => new(width, height);
    }

    public struct ReadOnlyBoolVector2
    {
        public bool Width { get; }
        public bool Height { get; }

        public ReadOnlyBoolVector2(bool width, bool height)
        {
            Width = width;
            Height = height;
        }
    }
    
    [Serializable]
    public struct ScrollPagingConfig
    {
        public bool usePaging;
        
        public bool addEmptySpaceToLastPage;
        
        public int countPerPage;
        public float duration;
        
        public eMagnetPivotType scrollViewPivotType;
        [SerializeField, Range(0f, 1f)] private float scrollViewCustomPivot;
        public readonly float ScrollViewPivot => GetPivot(scrollViewPivotType, scrollViewCustomPivot);
        
        public eMagnetPivotType pagePivotType;
        [SerializeField, Range(0f, 1f)] private float pageCustomPivot;
        public readonly float PagePivot => GetPivot(pagePivotType, pageCustomPivot);
        
        public bool useCustomEase;
        public Ease easeFunction;
        public AnimationCurve customEase;
        
        private readonly float GetPivot(eMagnetPivotType pivotType, float customValue)
        {
            return pivotType switch
            {
                eMagnetPivotType.Constant__0 => 0f,
                eMagnetPivotType.Constant__0_5 => 0.5f,
                eMagnetPivotType.Constant__1 => 1f,
                eMagnetPivotType.Custom => customValue,
                _ => 0f,
            };
        }
        
        public readonly float EvaluateEase(float s, float e, float t)
        {
            return useCustomEase ? Mathf.Lerp(s, e, customEase?.Evaluate(t) ?? 1f) : easeFunction.Evaluate(s, e, t);
        }
    }
    
    public class ScrollOptimizationValues
    {
        public bool use;
        public bool fromEnd;
        public int limit;
        public int currentUsingCount;
    }

    /// <summary>
    /// LoadData 작업의 결과 상태를 나타내는 열거형.
    /// </summary>
    public enum LoadDataResultState
    {
        #region 동기/비동기 공통
        /// <summary>
        /// 작업이 완료되었을 때 이 값이 넘어감.
        /// </summary>
        Complete,

        /// <summary>
        /// 게임 오브젝트가 하이어라키 상에서 비활성화 되어 있을 때 이 값이 넘어감.
        /// </summary>
        Fail_NotActive,

        /// <summary>
        /// (IRecycleScrollerDelegate)del에 인터페이스 구현 객체가 등록되어 있지 않은 경우 이 값이 넘어감.
        /// </summary>
        Fail_EmptyDel,
        #endregion

        #region 비동기 전용
        /// <summary>
        /// 비동기 콜에서 현재 작업이 캔슬되었을 때 이 값이 넘어감.
        /// </summary>
        Fail_Cancelled,

        /// <summary>
        /// 비동기 콜에서 현재 작업이 부여받은 콜 id와 인스턴스가 들고 있는 최신 콜 id를 비교하여 값이 서로 다를 때 이 값이 넘어감.<para/>
        /// 일반적인 상황에서 이 값이 넘어갈 일은 없음.
        /// 캔슬되면서 이미 Fail_cancelled 값이 넘어갔을 것이기 때문.
        /// </summary>
        Fail_NotLatest,
        #endregion
    }

    public enum LoadDataProceedState
    {
        NotLoaded,
        Loading,
        Loaded,
    }

    public class LoadDataCallbacks
    {
        private bool m_isInvoked = false;
        private LoadDataResultState m_result = LoadDataResultState.Complete;

        private Action<LoadDataResultState> m_complete;
        public Action<LoadDataResultState> Complete
        {
            get => m_complete;
            set
            {
                m_complete = value;
                if (m_isInvoked == false || m_complete == null) return;

                Internal_Invoke(m_result);
            }
        }

        public void Invoke(LoadDataResultState result)
        {
            m_isInvoked = true;
            m_result = result;
            Internal_Invoke(result);
        }

        private void Internal_Invoke(LoadDataResultState result)
        {
            m_complete?.Invoke(result);
            m_complete = null;
        }
    }

    public class OverwriteValue<T>
    {
        private bool m_isOverwritten = false;
        private T m_value;

        public T GetValue(T originalValue)
        {
            return m_isOverwritten ? m_value : originalValue;
        }

        public void Overwrite(T value)
        {
            m_value = value;
            m_isOverwritten = true;
        }

        public void RemoveOverwrite()
        {
            m_isOverwritten = false;
        }
    }
}