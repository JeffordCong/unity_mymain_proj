using System.Collections.Generic;
using UnityEngine;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 自动化Shader变体（Keyword）收集与统计工具
    /// </summary>
    public static class ShaderVariantCollector
    {
        // HashSet自动去重，记录所有唯一Keyword
        private static HashSet<string> _collectedKeywords = new HashSet<string>();

        // Dictionary用于统计Keyword被收集的次数（如需统计重复“热度”）
        private static Dictionary<string, int> _keywordCount = new Dictionary<string, int>();

        /// <summary>
        /// 注册一个变体Keyword，自动去重并累计次数
        /// </summary>
        public static void AddKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _collectedKeywords.Add(keyword); // HashSet自动去重

            if (_keywordCount.ContainsKey(keyword))
                _keywordCount[keyword]++;
            else
                _keywordCount[keyword] = 1;
        }

        /// <summary>
        /// 获得所有唯一收集到的Keyword（无重复）
        /// </summary>
        public static IEnumerable<string> GetAllKeywords() => _collectedKeywords;

        /// <summary>
        /// 获取某个Keyword被收集（启用）的总次数
        /// </summary>
        public static int GetCount(string keyword) => _keywordCount.TryGetValue(keyword, out int count) ? count : 0;

        /// <summary>
        /// 清空所有收集数据（如需重新批量处理）
        /// </summary>
        public static void Clear()
        {
            _collectedKeywords.Clear();
            _keywordCount.Clear();
        }

        /// <summary>
        /// 输出所有唯一变体与出现次数（可用于调试或批量分析）
        /// </summary>
        public static void PrintSummary()
        {
            Debug.Log("【Shader变体收集器统计结果】");
            foreach (var kw in _collectedKeywords)
            {
                Debug.Log($"Keyword: {kw}，被收集次数: {GetCount(kw)}");
            }
        }
    }
}