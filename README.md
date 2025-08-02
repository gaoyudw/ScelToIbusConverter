# ScelToIbusConverter
chinese input methord sougou pinyin scel to ibus text file
# 搜狗词库批量转换工具

功能亮点
1. 批量处理与智能去重
```
private static List<VocabularyItem> RemoveDuplicates(List<VocabularyItem> vocabulary)
{
    var uniqueDict = new Dictionary<string, VocabularyItem>();
    
    foreach (var item in vocabulary)
    {
        // 创建唯一键：词语 + 分隔符 + 拼音
        string key = $"{item.Word}|{item.Pinyin}";
        
        if (uniqueDict.TryGetValue(key, out var existingItem))
        {
            // 保留频率更高的词条
            if (item.Frequency > existingItem.Frequency)
            {
                uniqueDict[key] = item;
            }
        }
        else
        {
            uniqueDict.Add(key, item);
        }
    }
    
    return uniqueDict.Values.ToList();
}
```
多音字处理说明：

使用"词语|拼音"作为唯一键

相同词语不同拼音视为不同词条（如：银行|yin hang 和 银行|yin xing）

相同词语相同拼音保留词频最高的条目

高效字典操作，时间复杂度O(n)

2完整处理流程
graph TD
    A[开始] --> B{参数分析}
    B -->|单文件| C[处理单个文件]
    B -->|批量模式 -R| D[处理整个目录]
    C --> E[转换词库文件]
    D --> F[遍历所有.scel文件]
    F --> E
    E --> G[解析拼音表]
    E --> H[解析词条]
    G --> I[多编码支持]
    H --> J[动态定位]
    E --> K[输出预览]
    D --> L[合并所有词条]
    L --> M[智能去重]
    M --> N[多音字处理]
    M --> O[保留最高频]
    L --> P[按拼音排序]
    P --> Q[写入最终文件]
    Q --> R[输出统计信息]
    
