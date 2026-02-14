using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RecycleScroll
{
    /// <summary>
    /// 문자열 기반 조건식에 대응하는 데이터
    /// 예: "(10 > and 20 <) or (30 ==)"
    /// </summary>
    [Serializable]
    public class ConditionExpression : ConditionBase
    {
        [SerializeField, TextArea]
        private string m_expression;

        private string m_cachedExpression;
        private INode m_parsedNode;

        public override bool IsSatisfied(int groupCount)
        {
            if (string.IsNullOrWhiteSpace(m_expression))
                return false;

            if (m_expression != m_cachedExpression)
            {
                m_cachedExpression = m_expression;
                m_parsedNode = null; // 캐시 무효화

                try
                {
                    ExpressionParser parser = new ExpressionParser(m_expression);
                    m_parsedNode = parser.ParseRoot(); // 한 번만 파싱
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConditionExpression] 파싱 실패: {e.Message}\n식: {m_expression}");
                    m_parsedNode = null;
                    return false;
                }
            }

            // 파싱 실패 등으로 노드가 없으면 false
            if (m_parsedNode == null)
                return false;

            // 캐싱된 노드 트리를 사용해 Evaluate
            return m_parsedNode.Evaluate(groupCount);
        }
    }

    public class RS_LDE_ChangeAlignment_UsingByGroupCount2 : RS_LDE_ChangeAlignment_UsingByGroupCount_Base<ConditionExpression>
    {
    }

    #region 간단한 문자열 파서 (괄호, and/or, 비교연산 지원)

    /// <summary>
    /// 문자열 식을 토큰(Token)으로 쪼갠 뒤,
    /// ( ) / and / or / < / <= / == / >= / > / 숫자 등을 해석하여
    /// 최종 bool 을 평가하는 간단한 파서.
    ///
    /// 예) "(10 > and 20 <) or (30 ==)"
    /// </summary>
    public class ExpressionParser
    {
        private readonly List<Token> m_tokens;
        private int m_pos = 0;

        public ExpressionParser(string input)
        {
            m_tokens = Tokenize(input);
        }

        /// <summary>
        /// 최상위 Evaluate (groupCount가 식을 만족하는지 여부)
        /// </summary>
        public bool Evaluate(int groupCount)
        {
            m_pos = 0;
            INode root = ParseOrExpr(); // 루트 노드 (ORExpr)
            if (m_pos < m_tokens.Count)
            {
                Debug.LogWarning($"[ExpressionParser] 파싱 후 읽지 않은 토큰이 남아있습니다. Expression: {DumpTokens()}");
            }

            return root.Evaluate(groupCount);
        }

        public INode ParseRoot()
        {
            m_pos = 0;
            INode root = ParseOrExpr(); // 루트 파싱
            if (m_pos < m_tokens.Count)
            {
                Debug.LogWarning("[ExpressionParser] 파싱 후 ...");
            }

            return root; // 여기서는 트리(노드)만 반환
        }

        #region Grammar (재귀 하향 파서)

        // Expression -> OrExpr
        // OrExpr -> AndExpr ("or" AndExpr)*   (왼쪽부터 순서대로 파싱)
        private INode ParseOrExpr()
        {
            INode left = ParseAndExpr();

            while (Match(TokenType.OR))
            {
                m_pos++; // consume 'or'
                INode right = ParseAndExpr();
                left = new OrNode(left, right);
            }

            return left;
        }

        // AndExpr -> Factor ("and" Factor)*
        private INode ParseAndExpr()
        {
            INode left = ParseFactor();

            while (Match(TokenType.AND))
            {
                m_pos++; // consume 'and'
                INode right = ParseFactor();
                left = new AndNode(left, right);
            }

            return left;
        }

        // Factor -> "(" OrExpr ")" | Condition
        private INode ParseFactor()
        {
            if (Match(TokenType.LPAREN))
            {
                m_pos++;                    // consume '('
                INode expr = ParseOrExpr(); // 괄호 안에 다시 Expression 파싱

                if (!Match(TokenType.RPAREN))
                    throw new Exception("괄호가 올바르게 닫히지 않았습니다.");

                m_pos++; // consume ')'
                return expr;
            }
            else
            {
                return ParseCondition();
            }
        }

        // Condition -> NUMBER COMPARE_OP
        // 예: "10 >", "20 ==", "30 <=" ...
        private INode ParseCondition()
        {
            if (!Match(TokenType.NUMBER))
                throw new Exception("숫자(정수)가 필요합니다.");

            var numberToken = m_tokens[m_pos];
            m_pos++;

            if (!Match(TokenType.COMPARE_OP))
                throw new Exception("비교 연산자가 필요합니다. (>, <, >=, <=, ==)");

            var opToken = m_tokens[m_pos];
            m_pos++;

            int standardValue = int.Parse(numberToken.m_value);
            EqualityType eqType = ConvertStringToEqualityType(opToken.m_value);

            return new ConditionNode(eqType, standardValue);
        }

        #endregion

        #region 내부 구현 (Token, 토큰화, 보조함수)

        private bool Match(TokenType type)
        {
            if (m_pos < m_tokens.Count && m_tokens[m_pos].m_type == type)
                return true;
            return false;
        }

        private static EqualityType ConvertStringToEqualityType(string op)
        {
            return op switch
            {
                ">" => EqualityType.LessThan,
                ">=" => EqualityType.LessThanOrEqual,
                "<" => EqualityType.GreaterThan,
                "<=" => EqualityType.GreaterThanOrEqual,
                "==" => EqualityType.Equal,
                _ => throw new Exception($"지원하지 않는 비교연산자: {op}")
            };
        }

        // 문자열 -> 토큰 리스트
        private static List<Token> Tokenize(string input)
        {
            // 정규식 패턴:
            //  (  )  or  and  <=  >=  ==  <  >  숫자  공백 ...
            // 필요에 따라 더 정교하게 조정 가능
            var regex = new Regex(@"\(|\)|\d+|<=|>=|==|<|>|and|or|[\s]+");
            var matches = regex.Matches(input);

            var tokens = new List<Token>();
            foreach (Match m in matches)
            {
                string val = m.Value.ToLower().Trim();
                if (string.IsNullOrEmpty(val))
                    continue;

                if (val == "(") tokens.Add(new Token(TokenType.LPAREN, val));
                else if (val == ")") tokens.Add(new Token(TokenType.RPAREN, val));
                else if (val == "and") tokens.Add(new Token(TokenType.AND, val));
                else if (val == "or") tokens.Add(new Token(TokenType.OR, val));
                else if (Regex.IsMatch(val, @"^\d+$"))
                    tokens.Add(new Token(TokenType.NUMBER, val));
                else if (val == "<" || val == "<=" || val == ">" || val == ">=" || val == "==")
                    tokens.Add(new Token(TokenType.COMPARE_OP, val));
                else
                    Debug.LogWarning($"[ExpressionParser] 알 수 없는 토큰: '{val}'");
            }

            return tokens;
        }

        private string DumpTokens()
        {
            var list = new List<string>();
            for (int i = m_pos; i < m_tokens.Count; i++)
            {
                list.Add(m_tokens[i].m_value);
            }

            return string.Join(" ", list);
        }

        #endregion
    }

    // 비교 연산 Enum
    public enum EqualityType
    {
        Equal,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    // 파서에서 사용되는 토큰 종류
    public enum TokenType
    {
        LPAREN,    // "("
        RPAREN,    // ")"
        AND,       // "and"
        OR,        // "or"
        NUMBER,    // "10", "25" ...
        COMPARE_OP // ">", "<", ">=", "<=", "=="
    }

    public struct Token
    {
        public readonly TokenType m_type;
        public readonly string m_value;
        public Token(TokenType t, string v)
        {
            m_type = t;
            m_value = v;
        }
    }

    #region 노드 계층 구조 (AST 트리, Evaluate)

    /// <summary>
    /// 모든 노드가 구현해야 할 인터페이스
    /// </summary>
    public interface INode
    {
        bool Evaluate(int groupCount);
    }

    /// <summary>
    /// 'and' (논리곱)
    /// </summary>
    public class AndNode : INode
    {
        private INode m_left, m_right;
        public AndNode(INode left, INode right)
        {
            m_left = left;
            m_right = right;
        }
        public bool Evaluate(int groupCount)
        {
            return m_left.Evaluate(groupCount) && m_right.Evaluate(groupCount);
        }
    }

    /// <summary>
    /// 'or' (논리합)
    /// </summary>
    public class OrNode : INode
    {
        private INode m_left, m_right;
        public OrNode(INode left, INode right)
        {
            m_left = left;
            m_right = right;
        }
        public bool Evaluate(int groupCount)
        {
            return m_left.Evaluate(groupCount) || m_right.Evaluate(groupCount);
        }
    }

    /// <summary>
    /// 실제 비교식 노드. 예: "10 >", "20 <=", "30 =="
    /// </summary>
    public class ConditionNode : INode
    {
        private readonly EqualityType m_eqType;
        private readonly int m_standardValue;

        public ConditionNode(EqualityType eqType, int stdValue)
        {
            m_eqType = eqType;
            m_standardValue = stdValue;
        }

        public bool Evaluate(int groupCount)
        {
            switch (m_eqType)
            {
                case EqualityType.Equal: return groupCount == m_standardValue;
                case EqualityType.LessThan: return groupCount < m_standardValue;
                case EqualityType.GreaterThan: return groupCount > m_standardValue;
                case EqualityType.LessThanOrEqual: return groupCount <= m_standardValue;
                case EqualityType.GreaterThanOrEqual: return groupCount >= m_standardValue;
            }

            return false;
        }
    }

    #endregion

    #endregion
}