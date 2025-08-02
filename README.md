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
