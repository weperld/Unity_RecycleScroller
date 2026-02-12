using System;
using UnityEngine;

namespace RecycleScroll
{
    [Serializable]
    public class ConditionData : ConditionBase
    {
        [Serializable]
        private struct ConditionValue
        {
            [SerializeField] private EqualityType m_equalityType;
            [SerializeField, Min(0)] private int m_standardValue;

            public bool IsSatisfied(int value)
            {
                switch (m_equalityType)
                {
                    case EqualityType.Equal:
                        return value == m_standardValue;
                    case EqualityType.LessThan:
                        return value < m_standardValue;
                    case EqualityType.GreaterThan:
                        return value > m_standardValue;
                    case EqualityType.LessThanOrEqual:
                        return value <= m_standardValue;
                    case EqualityType.GreaterThanOrEqual:
                        return value >= m_standardValue;
                    default:
                        return false;
                }
            }
        }

        [SerializeField] private ConditionValue[] m_conditions;

        public override bool IsSatisfied(int value)
        {
            // 조건이 하나도 없으면 false반환
            if (m_conditions.Length == 0) return false;

            // 조건을 전부 만족하면 true반환
            foreach (var condition in m_conditions)
            {
                if (condition.IsSatisfied(value) is false)
                    return false;
            }

            return true;
        }
    }

    public class RS_LDE_ChangeAlignment_UsingByGroupCount : RS_LDE_ChangeAlignment_UsingByGroupCount_Base<ConditionData>
    {
    }
}