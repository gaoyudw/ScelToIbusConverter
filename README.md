# ScelToIbusConverter
chinese input methord sougou pinyin scel to ibus text file
# 搜狗词库批量转换工具

## 将搜狗拼音输入法的词库转换为可以导入ibus的text

## 使用说明
### 单文件转换
```bash
./ScelToIbusConverter 计算机词汇.scel
```
### 批量转换目录
```bash
./ScelToIbusConverter -R ./词库目录 合并词库.txt
```
### 输出示例
```
开始处理目录: /home/user/词库目录
找到 15 个词库文件

[1/15] 处理文件: 计算机词汇.scel
词库名称: 计算机专业词汇
解析拼音表...
找到拼音表起始位置: 0x1548
拼音表解析完成，共 412 个拼音
解析词条内容...
词条解析完成，共 1250 个词条
预览:
  CPU        c'p'u                          5
  内存       nei'cun                        8
  硬盘       ying'pan                       6
当前总词条数: 1250

[2/15] 处理文件: 医学词汇.scel
...

所有文件处理完成! 耗时: 8.32秒
共转换 18500 个词条, 来自 15 个文件

开始智能去重处理...
去重完成! 耗时: 0.45秒
原始词条: 18500, 去重后: 17200, 重复项: 1300

开始排序处理...
排序完成! 耗时: 0.87秒

写入合并文件: 合并词库.txt
文件大小: 420 KB
词条数量: 17200

导入到ibus-libpinyin的命令:
ibus-libpinyin-import-text-db "/home/user/词库目录/合并词库.txt"
```
## 技术优势
- 精准的多音字处理：

- 基于"词语+拼音"组合去重

- 保留不同拼音的多音词

- 相同拼音保留最高频率

## 高效内存管理：

- 字典去重算法(O(n)时间复杂度)

- 流式写入减少内存占用

- 异步I/O提高吞吐量

## 详细处理反馈：

- 文件处理进度

- 词条数量统计

- 各阶段耗时分析

- 去重效果可视化

## 健壮的错误处理：

- 文件头验证

- 异常长度检测

- 无效位置恢复

- 错误隔离处理

## 功能亮点
#### 批量处理与智能去重
``` c#
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
#### 多音字处理说明：

- 使用"词语|拼音"作为唯一键

- 相同词语不同拼音视为不同词条（如：银行|yin hang 和 银行|yin xing）

- 相同词语相同拼音保留词频最高的条目

- 高效字典操作，时间复杂度O(n)

### 完整处理流程


```graph TD
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
```
### 性能优化
#### 异步文件操作：
```c#
byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
```
- 使用异步I/O减少阻塞

- 提高大文件处理效率

#### 进度跟踪：
```c#
Console.Write($"\r写入进度: {counter}/{vocabulary.Count} 条");
```
- 实时显示处理进度

- 每1000条更新一次显示

#### 耗时统计：
```c#
var stopwatch = Stopwatch.StartNew();
// ...处理过程...
stopwatch.Stop();
Console.WriteLine($"去重完成! 耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒");
```
- 精确测量各阶段耗时
- 帮助了解处理效率

### 多音字处理示例
#### 假设有以下词条：
```
银行  yin'hang  100
银行  yin'xing  50
行   xing      80
行   hang      70
```
#### 去重后结果：
```
银行  yin'hang  100  // 保留高频
银行  yin'xing  50   // 不同拼音视为不同词条
行   xing      80    // 多音字不同读音
行   hang      70    // 多音字不同读音
```
