using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MathUtils
{
    public static partial class MathUtil
    {
        public const float DEFAULT_TIME_STEP = 0.01f;
        
        public static float GetGradient(float x, float y)
        {
            if (!Mathf.Approximately(x, 0f)) return y / x;
            
            if (y > 0f) return float.PositiveInfinity;
            if (y < 0f) return float.NegativeInfinity;
            return 0f;
        }
        public static float GetGradient(float x1, float y1, float x2, float y2) => GetGradient(x2 - x1, y2 - y1);
        
        private static int AddNewKeyFrame(this AnimationCurve curve, float time, float value, float inTangent, float outTangent, float inWeight = 0f,
            float outWeight = 0f)
        {
            var newKey = new Keyframe(time, value, inTangent, outTangent);
            newKey.weightedMode = WeightedMode.None;
            newKey.inWeight = inWeight;
            newKey.outWeight = outWeight;
            var idx = curve.AddKey(newKey);
            
            return idx;
        }
        public static AnimationCurve Derivative(this AnimationCurve curve, float timeStep = DEFAULT_TIME_STEP)
        {
            var result = new AnimationCurve();
            var keys = curve.keys;
            if (keys.Length <= 1) return result;
            
            if (timeStep <= 0f) timeStep = DEFAULT_TIME_STEP;
            int lastNewAddKeyIdx = 0;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                var curKey = keys[i];
                var nextKey = keys[i + 1];
                float gradient = GetGradient(curKey.time, curKey.value, nextKey.time, nextKey.value);
                
                // Constant 구간...Mathf.Approximately를 활용해야 하나??
                // 특별한 상수 값을 사용하므로 == 연산자 사용
                if (curKey.outTangent == float.PositiveInfinity || curKey.outTangent == float.NegativeInfinity
                    || nextKey.inTangent == float.PositiveInfinity || nextKey.inTangent == float.NegativeInfinity)
                {
                    AddNewOrMoveKey(lastNewAddKeyIdx, curKey.time, 0f, 0f, 0f);
                    lastNewAddKeyIdx = result.AddNewKeyFrame(nextKey.time, 0f, 0f, 0f);
                }
                // 선형 구간
                else if (Mathf.Approximately(gradient, curKey.outTangent) && Mathf.Approximately(curKey.outTangent, nextKey.inTangent))
                {
                    lastNewAddKeyIdx = AddNewOrMoveKey(lastNewAddKeyIdx, curKey.time, gradient, 0f, 0f);
                    var tempKey = result[lastNewAddKeyIdx];
                    tempKey.outTangent = float.PositiveInfinity;
                    result.MoveKey(lastNewAddKeyIdx, tempKey);
                    
                    lastNewAddKeyIdx = result.AddNewKeyFrame(nextKey.time, gradient, 0f, 0f, inWeight: 1f);
                }
                // 비선형 구간
                else
                {
                    for (float t = curKey.time; t < nextKey.time; t += timeStep)
                    {
                        float nextT = Mathf.Min(t + timeStep, nextKey.time);
                        float tempTimeStep = nextT - t;
                        
                        float eval = curve.Evaluate(t);
                        float nextEval = curve.Evaluate(nextT);
                        
                        float grad = (nextEval - eval) / tempTimeStep;
                        
                        AddNewOrMoveKey(lastNewAddKeyIdx, curKey.time, grad, 0f, grad);
                        lastNewAddKeyIdx = result.AddNewKeyFrame(nextT, grad, grad, 0f);
                    }
                }
            }
            
            return result;
            
            /// <summary>키 프레임 추가 및 키 내부 값 변경.
            /// index는 이미 해당 시간의 키가 추가되었는지 확인하기 위함.
            /// 이미 추가되어 있다면 해당 키의 값을 수정, 없다면 새로 추가</summary>
            int AddNewOrMoveKey(int index, float time, float val, float inT, float outT, float inW = 0f, float outW = 0f)
            {
                if (result.length <= index)
                    index = result.AddNewKeyFrame(time, val, inT, outT, inW, outW);
                else
                {
                    var tmpKey = result.keys[index];
                    tmpKey.value = val;
                    tmpKey.inTangent = inT;
                    tmpKey.outTangent = outT;
                    tmpKey.inWeight = inW;
                    tmpKey.outWeight = outW;
                    index = result.MoveKey(index, tmpKey);
                }
                
                return index;
            }
        }
        
        /// <summary></summary>
        /// <param name="curve"></param>
        /// <param name="timeStep"></param>
        /// <returns>If curve key length is 0 or 1, return value is 0f</returns>
        public static float Integrate(this AnimationCurve curve, float timeStep = DEFAULT_TIME_STEP)
        {
            var keys = curve.keys;
            if (keys.Length <= 1) return 0f;
            
            if (timeStep <= 0f) timeStep = DEFAULT_TIME_STEP;
            float result = 0f;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                var curKey = keys[i];
                var nextKey = keys[i + 1];
                float gradient = GetGradient(curKey.time, curKey.value, nextKey.time, nextKey.value);
                float width = nextKey.time - curKey.time;
                
                // Constant 구간
                if (curKey.outTangent == float.PositiveInfinity || curKey.outTangent == float.NegativeInfinity
                    || nextKey.inTangent == float.PositiveInfinity || nextKey.inTangent == float.NegativeInfinity)
                {
                    // 두 키 프레임 중 하나라도 outTangent가 무한대인 경우
                    // 두 키 프레임의 값의 대소와는 상관없이 앞에 있는 키의 값이 Constant 구간의 값이 됨
                    var constantValue = curKey.outTangent == float.PositiveInfinity || nextKey.outTangent == float.PositiveInfinity ? curKey.value : nextKey.value;
                    result += constantValue * width;
                }
                // 선형 구간
                else if (Mathf.Approximately(gradient, curKey.outTangent) && Mathf.Approximately(curKey.outTangent, nextKey.inTangent))
                {
                    result += (curKey.value + nextKey.value) * 0.5f * width;
                }
                // 비선형 구간
                else
                {
                    for (float t = curKey.time; t < nextKey.time; t += timeStep)
                    {
                        float nextT = Mathf.Min(t + timeStep, nextKey.time);
                        float tempTimeStep = nextT - t; // width
                        
                        float eval = curve.Evaluate(t);
                        float nextEval = curve.Evaluate(nextT);
                        
                        result += (nextEval + eval) * 0.5f * tempTimeStep;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 각도값을 0~360도 사이의 값으로 변환합니다.
        /// ex) -40도 => 320도, -440도 => 280도
        /// </summary>
        /// <param name="angle"></param>
        /// <returns>0도 이상, 360도 미만</returns>
        public static float DegreeAngleTo360(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            
            return angle;
        }
        
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        
        public static bool Approximately(double a, double b, double epsilon = double.Epsilon) => Math.Abs(a - b) < epsilon;

        public static int[] GetRandomIndices(int listCount, int randomCount, bool duplicatable)
        {
            if (listCount <= 0 || randomCount <= 0) return Array.Empty<int>();
            randomCount = Mathf.Min(listCount, randomCount);

            var result = new List<int>();
            if (duplicatable)
            {
                for (int i = 0; i < randomCount; i++)
                    result.Add(UnityEngine.Random.Range(0, listCount));
            }
            else
            {
                var randomIndices = new List<int>(Enumerable.Range(0, listCount));
                for (int i = 0; i < randomCount; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, randomIndices.Count);
                    result.Add(randomIndices[randomIndex]);
                    randomIndices.RemoveAt(randomIndex);
                }
            }

            return result.ToArray();
        }
    }

    [Serializable]
    public struct MinMaxInt
    {
        public int min;
        public int max;
        
        public int RandomValue => UnityEngine.Random.Range(min, max);
        
        public MinMaxInt(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public static MinMaxInt Deserialize(string json)
        {
            if (JsonHelper.TryDeserialize(json, out MinMaxInt minMax)) return minMax;
            return new MinMaxInt(0, 0);
        }
    }
    
    [Serializable]
    public struct MinMaxLong
    {
        public long min;
        public long max;
        
        public long RandomValue => min + (long)(UnityEngine.Random.value * (max - min));
        
        public MinMaxLong(long min, long max)
        {
            this.min = min;
            this.max = max;
        }

        public static MinMaxLong Deserialize(string json)
        {
            if (JsonHelper.TryDeserialize(json, out MinMaxLong minMax)) return minMax;
            return new MinMaxLong(0L, 0L);
        }
    }
    
    [Serializable]
    public struct MinMaxFloat
    {
        public float min;
        public float max;
        
        public float RandomValue => UnityEngine.Random.Range(min, max);
        
        public MinMaxFloat(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public static MinMaxFloat Deserialize(string json)
        {
            if (JsonHelper.TryDeserialize(json, out MinMaxFloat minMax)) return minMax;
            return new MinMaxFloat(0f, 0f);
        }
    }
    
    [Serializable]
    public struct MinMaxDouble
    {
        public double min;
        public double max;
        
        public double RandomValue => min + (UnityEngine.Random.value * (max - min));
        
        public MinMaxDouble(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public static MinMaxDouble Deserialize(string json)
        {
            if (JsonHelper.TryDeserialize(json, out MinMaxDouble minMax)) return minMax;
            return new MinMaxDouble(0d, 0d);
        }
    }
}