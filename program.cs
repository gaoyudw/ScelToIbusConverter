using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScelToIbusConverter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("搜狗词库批量转换工具 (.scel 转 ibus-libpinyin txt 格式)");
            Console.WriteLine("版本: 4.0 (批量处理与智能去重版)");
            Console.WriteLine("==================================================");
            Console.WriteLine("功能说明:");
            Console.WriteLine("1. 单文件模式: 转换单个.scel词库文件");
            Console.WriteLine("2. 批量模式(-R): 转换整个目录下的.scel文件");
            Console.WriteLine("3. 智能去重: 基于词语和拼音组合去重，保留最高频率词条");
            Console.WriteLine("4. 多音字处理: 相同词语不同拼音视为不同词条");
            Console.WriteLine("5. 排序输出: 按拼音排序，相同拼音按词语排序");
            Console.WriteLine("==================================================");
            
            // 检查是否批量处理模式
            if (args.Length > 0 && args[0] == "-R")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("错误: 请指定目录路径");
                    Console.WriteLine("用法: ScelToIbusConverter -R <目录路径> [输出文件]");
                    return;
                }
                
                string directory = args[1];
                string outputFile = args.Length > 2 ? args[2] : "all.txt";
                
                // 批量处理目录
                await ProcessDirectory(directory, outputFile);
            }
            else
            {
                // 单文件处理模式
                if (args.Length < 1)
                {
                    PrintUsage();
                    return;
                }

                string inputFile = args[0];
                string outputFile = args.Length > 1 ? args[1] : Path.ChangeExtension(inputFile, "txt");
                await ProcessSingleFile(inputFile, outputFile);
            }
        }

        /// <summary>
        /// 打印使用说明
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("\n使用方法:");
            Console.WriteLine("1. 转换单个文件:");
            Console.WriteLine("   ScelToIbusConverter <输入文件.scel> [输出文件.txt]");
            Console.WriteLine("   示例: ScelToIbusConverter 计算机词汇.scel");
            Console.WriteLine();
            Console.WriteLine("2. 批量转换目录:");
            Console.WriteLine("   ScelToIbusConverter -R <目录路径> [输出文件.txt]");
            Console.WriteLine("   示例: ScelToIbusConverter -R ./词库目录 合并词库.txt");
            Console.WriteLine();
            Console.WriteLine("3. 环境检测:");
            Console.WriteLine("   ScelToIbusConverter -checkenv");
        }

        /// <summary>
        /// 批量处理目录中的所有词库文件
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <param name="outputFile">输出文件路径</param>
        private static async Task ProcessDirectory(string directory, string outputFile)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"错误: 目录不存在 - {directory}");
                return;
            }

            Console.WriteLine($"开始处理目录: {Path.GetFullPath(directory)}");
            
            // 获取所有.scel文件
            var scelFiles = Directory.GetFiles(directory, "*.scel", SearchOption.AllDirectories);
            Console.WriteLine($"找到 {scelFiles.Length} 个词库文件");
            
            if (scelFiles.Length == 0)
            {
                Console.WriteLine("没有找到可处理的.scel文件");
                return;
            }

            // 进度跟踪
            int processed = 0;
            int totalWords = 0;
            var allVocabulary = new List<VocabularyItem>();
            var stopwatch = Stopwatch.StartNew();
            
            // 处理每个文件
            foreach (var file in scelFiles)
            {
                Console.WriteLine($"\n[{++processed}/{scelFiles.Length}] 处理文件: {Path.GetFileName(file)}");
                
                try
                {
                    // 转换单个文件
                    var fileVocabulary = await ConvertScelFile(file);
                    totalWords += fileVocabulary.Count;
                    
                    // 添加到总词库
                    allVocabulary.AddRange(fileVocabulary);
                    Console.WriteLine($"当前总词条数: {allVocabulary.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"文件处理失败: {ex.Message}");
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"\n所有文件处理完成! 耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒");
            Console.WriteLine($"共转换 {totalWords} 个词条, 来自 {scelFiles.Length} 个文件");
            
            if (allVocabulary.Count == 0)
            {
                Console.WriteLine("错误: 没有转换出任何词条");
                return;
            }

            // 去重处理
            Console.WriteLine("\n开始智能去重处理...");
            stopwatch.Restart();
            var uniqueVocabulary = RemoveDuplicates(allVocabulary);
            stopwatch.Stop();
            
            int duplicateCount = allVocabulary.Count - uniqueVocabulary.Count;
            Console.WriteLine($"去重完成! 耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒");
            Console.WriteLine($"原始词条: {allVocabulary.Count}, 去重后: {uniqueVocabulary.Count}, 重复项: {duplicateCount}");
            
            // 排序处理
            Console.WriteLine("\n开始排序处理...");
            stopwatch.Restart();
            uniqueVocabulary.Sort((a, b) => 
            {
                // 先按拼音排序，再按词语排序
                int pinyinCompare = string.Compare(a.Pinyin, b.Pinyin, StringComparison.Ordinal);
                if (pinyinCompare != 0) return pinyinCompare;
                return string.Compare(a.Word, b.Word, StringComparison.Ordinal);
            });
            stopwatch.Stop();
            Console.WriteLine($"排序完成! 耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒");
            
            // 写入最终文件
            Console.WriteLine($"\n写入合并文件: {outputFile}");
            WriteOutputFile(uniqueVocabulary, outputFile);
            
            // 文件信息
            var fileInfo = new FileInfo(outputFile);
            Console.WriteLine($"文件大小: {fileInfo.Length / 1024} KB");
            Console.WriteLine($"词条数量: {uniqueVocabulary.Count}");
            
            // 提供导入命令建议
            Console.WriteLine("\n导入到ibus-libpinyin的命令:");
            Console.WriteLine($"ibus-libpinyin-import-text-db \"{Path.GetFullPath(outputFile)}\"");
        }

        /// <summary>
        /// 处理单个词库文件
        /// </summary>
        private static async Task ProcessSingleFile(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"错误: 文件不存在 - {inputFile}");
                return;
            }

            Console.WriteLine($"\n处理文件: {inputFile}");
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var vocabulary = await ConvertScelFile(inputFile);
                stopwatch.Stop();
                
                Console.WriteLine($"转换完成! 耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒");
                Console.WriteLine($"词条数量: {vocabulary.Count}");
                
                // 写入文件
                WriteOutputFile(vocabulary, outputFile);
                Console.WriteLine($"\n输出文件: {outputFile}");
                
                // 提供导入命令建议
                Console.WriteLine("\n导入到ibus-libpinyin的命令:");
                Console.WriteLine($"ibus-libpinyin-import-text-db \"{Path.GetFullPath(outputFile)}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换单个词库文件
        /// </summary>
        private static async Task<List<VocabularyItem>> ConvertScelFile(string filePath)
        {
            Console.WriteLine("读取文件内容...");
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            
            if (!ValidateFileHeader(fileBytes))
            {
                throw new InvalidOperationException($"无效的词库文件: {Path.GetFileName(filePath)}");
            }

            // 解析元数据
            ParseAndPrintMetadata(fileBytes);
            
            // 解析拼音表
            Console.WriteLine("解析拼音表...");
            Dictionary<ushort, string> pyTable = ParsePinyinTable(fileBytes);
            Console.WriteLine($"拼音表解析完成，共 {pyTable.Count} 个拼音");
            
            // 解析词条
            Console.WriteLine("解析词条内容...");
            List<VocabularyItem> vocabulary = ParseVocabulary(fileBytes, pyTable);
            
            if (vocabulary.Count == 0)
            {
                Console.WriteLine("警告: 未解析到任何词条");
            }
            else
            {
                Console.WriteLine($"词条解析完成，共 {vocabulary.Count} 个词条");
                
                // 显示前3个转换结果作为预览
                Console.WriteLine("预览:");
                for (int i = 0; i < Math.Min(3, vocabulary.Count); i++)
                {
                    var item = vocabulary[i];
                    Console.WriteLine($"  {item.Word.PadRight(10)} {item.Pinyin.PadRight(30)} {item.Frequency}");
                }
            }
            
            return vocabulary;
        }

        /// <summary>
        /// 移除重复词条（基于词语和拼音组合）
        /// 专门处理多音字情况：相同词语不同拼音视为不同词条
        /// </summary>
        private static List<VocabularyItem> RemoveDuplicates(List<VocabularyItem> vocabulary)
        {
            // 使用字典进行高效去重，键为"词语|拼音"组合
            var uniqueDict = new Dictionary<string, VocabularyItem>();
            
            foreach (var item in vocabulary)
            {
                // 创建唯一键：词语 + 分隔符 + 拼音
                // 这样"银行"的"yin hang"和"yin xing"会被视为不同词条
                string key = $"{item.Word}|{item.Pinyin}";
                
                if (uniqueDict.TryGetValue(key, out var existingItem))
                {
                    // 如果已存在相同词语和拼音的词条，保留频率更高的
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

        /// <summary>
        /// 写入输出文件 (UTF-8编码)
        /// </summary>
        private static void WriteOutputFile(List<VocabularyItem> vocabulary, string outputPath)
        {
            try
            {
                int counter = 0;
                using (StreamWriter writer = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
                {
                    foreach (var item in vocabulary)
                    {
                        // 格式: 词语\t拼音\t频率
                        writer.WriteLine($"{item.Word}\t{item.Pinyin}\t{item.Frequency}");
                        counter++;
                        
                        // 每1000条输出一次进度
                        if (counter % 1000 == 0)
                        {
                            Console.Write($"\r写入进度: {counter}/{vocabulary.Count} 条");
                        }
                    }
                }
                
                Console.WriteLine($"\n成功写入 {counter} 个词条到 {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入文件错误: {ex.Message}");
                throw;
            }
        }

        #region 词库解析核心功能
        
        /// <summary>
        /// 验证搜狗词库文件头
        /// 搜狗词库文件有特定的头部签名
        /// </summary>
        private static bool ValidateFileHeader(byte[] data)
        {
            // 搜狗词库文件头特征
            if (data.Length < 16) 
            {
                Console.WriteLine($"错误: 文件过小 ({data.Length}字节)");
                return false;
            }
            
            // 更宽松的头部验证（支持多种版本）
            // 检查前4字节的常见特征
            bool headerValid = (data[0] == 0x40 && data[1] == 0x15 && data[2] == 0x00 && data[3] == 0x00) ||
                              (data[4] == 0x44 && data[5] == 0x43 && data[6] == 0x53); // "DCS"
            
            if (!headerValid)
            {
                Console.WriteLine("文件头验证失败:");
                Console.WriteLine(BitConverter.ToString(data, 0, 16).Replace("-", " "));
            }
            
            return headerValid;
        }

        /// <summary>
        /// 解析并打印词库元数据
        /// 搜狗词库在固定位置存储名称、类型等信息
        /// </summary>
        private static void ParseAndPrintMetadata(byte[] data)
        {
            // 词库名偏移: 0x130-0x338 (UTF-16编码)
            string name = ReadUtf16String(data, 0x130, 0x338 - 0x130);
            // 词库类型偏移: 0x338-0x540
            string type = ReadUtf16String(data, 0x338, 0x540 - 0x338);
            // 描述信息偏移: 0x540-0xD40
            string description = ReadUtf16String(data, 0x540, 0xD40 - 0x540);
            // 词库示例偏移: 0xD40-0x1540
            string example = ReadUtf16String(data, 0xD40, 0x1540 - 0xD40);
            
            Console.WriteLine($"词库名称: {name}");
            Console.WriteLine($"词库类型: {type}");
            Console.WriteLine($"描述信息: {description}");
            Console.WriteLine($"词库示例: {example}");
        }

        /// <summary>
        /// 从字节数组读取UTF-16字符串
        /// 自动处理空终止符和边界检查
        /// </summary>
        private static string ReadUtf16String(byte[] data, int start, int length)
        {
            if (start >= data.Length) 
            {
                Console.WriteLine($"警告: 字符串读取位置越界 (0x{start:X4})");
                return "";
            }
            
            if (start + length > data.Length)
            {
                length = data.Length - start;
                Console.WriteLine($"警告: 调整字符串长度到 {length} 字节");
            }
            
            // 找到实际的结束位置 (第一个双字节0的位置)
            int end = start;
            for (int i = start; i < start + length - 1; i += 2)
            {
                if (data[i] == 0 && data[i + 1] == 0)
                {
                    length = i - start;
                    break;
                }
            }
            
            // 确保长度是偶数（UTF-16要求）
            if (length % 2 != 0)
            {
                length--;
                Console.WriteLine($"警告: 调整字符串长度为偶数 ({length})");
            }
            
            if (length <= 0) 
            {
                Console.WriteLine($"警告: 无效字符串长度 ({length})");
                return "";
            }
            
            try
            {
                return Encoding.Unicode.GetString(data, start, length).TrimEnd('\0');
            }
            catch (Exception ex)
            {
                Console.WriteLine($"字符串解码错误: {ex.Message}");
                return "解码错误";
            }
        }

        /// <summary>
        /// 解析拼音表 (动态定位起始位置)
        /// 拼音表存储了拼音索引到拼音字符串的映射
        /// </summary>
        private static Dictionary<ushort, string> ParsePinyinTable(byte[] data)
        {
            var pyTable = new Dictionary<ushort, string>();
            
            // 动态搜索拼音表起始标记 (0x9D01)
            int position = 0x1540; // 默认起始位置
            bool found = false;
            
            // 搜索范围：0x1300 - 0x2600（覆盖大多数词库）
            int searchStart = Math.Max(0x1300, 0);
            int searchEnd = Math.Min(0x2600, data.Length - 2);
            
            for (int i = searchStart; i < searchEnd; i++)
            {
                if (data[i] == 0x9D && data[i + 1] == 0x01)
                {
                    position = i;
                    found = true;
                    Console.WriteLine($"找到拼音表起始位置: 0x{position:X4}");
                    break;
                }
            }
            
            if (!found)
            {
                Console.WriteLine("警告: 未找到拼音表起始标记，使用默认位置 0x1540");
                position = 0x1540;
                
                // 验证默认位置是否合理
                if (position + 4 > data.Length)
                {
                    Console.WriteLine("错误: 默认位置超出文件范围");
                    return pyTable;
                }
            }

            position += 4; // 跳过表头

            int entryCount = 0;
            while (position + 4 <= data.Length && entryCount < 500) // 防止无限循环
            {
                // 读取索引 (2字节小端)
                ushort index = BitConverter.ToUInt16(data, position);
                position += 2;
                
                // 读取拼音长度 (2字节小端)
                ushort length = BitConverter.ToUInt16(data, position);
                position += 2;
                
                // 增强长度验证
                if (length == 0)
                {
                    Console.WriteLine($"警告: 遇到零长度拼音 @ 0x{position:X4}");
                    break;
                }
                
                if (length > 100)
                {
                    Console.WriteLine($"警告: 异常拼音长度 {length} @ 0x{position:X4}");
                    break;
                }
                
                if (position + length > data.Length)
                {
                    Console.WriteLine($"警告: 拼音数据超出文件范围 @ 0x{position:X4}");
                    break;
                }
                
                // 解码拼音字符串
                string pinyin = DecodePinyinString(data, position, length);
                position += length;
                
                // 添加到拼音表
                if (!pyTable.ContainsKey(index))
                {
                    pyTable.Add(index, pinyin);
                    entryCount++;
                }
                else
                {
                    Console.WriteLine($"警告: 重复拼音索引 0x{index:X4} @ 0x{position:X4}");
                }
            }
            
            return pyTable;
        }

        /// <summary>
        /// 解码拼音字符串（尝试多种编码）
        /// 解决不同版本词库的编码差异问题
        /// </summary>
        private static string DecodePinyinString(byte[] data, int start, int length)
        {
            // 优先尝试UTF-16LE解码（大多数词库使用）
            try
            {
                string utf16 = Encoding.Unicode.GetString(data, start, length);
                if (IsValidPinyin(utf16)) return utf16;
            }
            catch { }
            
            // 尝试GB18030解码（较旧词库可能使用）
            try
            {
                Encoding gb18030 = Encoding.GetEncoding("GB18030");
                string gb = gb18030.GetString(data, start, length);
                if (IsValidPinyin(gb)) return gb;
            }
            catch { }
            
            // 尝试UTF-8解码（少数特殊词库）
            try
            {
                string utf8 = Encoding.UTF8.GetString(data, start, length);
                if (IsValidPinyin(utf8)) return utf8;
            }
            catch { }
            
            // 最后手段：十六进制表示
            return "0x" + BitConverter.ToString(data, start, Math.Min(length, 8)).Replace("-", "");
        }

        /// <summary>
        /// 验证是否为有效拼音
        /// 过滤非拼音字符
        /// </summary>
        private static bool IsValidPinyin(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            // 有效拼音应只包含字母、数字和标点
            return str.All(c => 
                char.IsLetterOrDigit(c) || 
                c == '\'' || 
                c == '-' || 
                c == ' ' || 
                c == ':' || 
                c == '.');
        }

        /// <summary>
        /// 解析词库词条（增强容错）
        /// 处理不同版本词库的结构差异
        /// </summary>
        private static List<VocabularyItem> ParseVocabulary(byte[] data, Dictionary<ushort, string> pyTable)
        {
            var vocabulary = new List<VocabularyItem>();
            int position = 0x2628; // 默认词条起始位置
            int itemCount = 0;
            int skippedCount = 0;
            
            // 动态定位词条起始位置（如果默认位置无效）
            if (position + 4 > data.Length)
            {
                Console.WriteLine("警告: 默认词条位置无效，尝试动态定位...");
                position = FindVocabularyStartPosition(data);
                Console.WriteLine($"新词条起始位置: 0x{position:X4}");
            }

            while (position + 4 <= data.Length && itemCount < 100000) // 防止无限循环
            {
                // 同音词数量
                if (position + 2 > data.Length) break;
                ushort sameCount = BitConverter.ToUInt16(data, position);
                position += 2;
                
                // 跳过无效的sameCount（过大值通常表示位置错误）
                if (sameCount > 1000)
                {
                    Console.WriteLine($"警告: 异常同音词数量 {sameCount} @ 0x{position-2:X4}");
                    skippedCount++;
                    if (skippedCount > 10) break; // 避免无限跳过
                    continue;
                }
                
                // 拼音索引表长度
                if (position + 2 > data.Length) break;
                ushort pyTableLength = BitConverter.ToUInt16(data, position);
                position += 2;
                
                // 验证拼音表长度
                if (pyTableLength > 200 || pyTableLength % 2 != 0)
                {
                    Console.WriteLine($"警告: 异常拼音表长度 {pyTableLength} @ 0x{position-2:X4}");
                    skippedCount++;
                    if (skippedCount > 10) break;
                    continue;
                }
                
                // 读取拼音索引
                var pyIndexes = new List<ushort>();
                for (int i = 0; i < pyTableLength / 2; i++)
                {
                    if (position + 2 > data.Length) break;
                    ushort index = BitConverter.ToUInt16(data, position);
                    position += 2;
                    pyIndexes.Add(index);
                }
                
                // 构建拼音字符串（增强容错）
                var pinyinBuilder = new StringBuilder();
                foreach (var index in pyIndexes)
                {
                    // 使用string?处理可能的null值
                    if (pyTable.TryGetValue(index, out string? py) && !string.IsNullOrEmpty(py))
                    {
                        if (pinyinBuilder.Length > 0)
                            pinyinBuilder.Append('\'');
                        pinyinBuilder.Append(py);
                    }
                    else
                    {
                        if (pinyinBuilder.Length > 0)
                            pinyinBuilder.Append('\'');
                        pinyinBuilder.Append($"[0x{index:X4}]");
                    }
                }
                string pinyin = pinyinBuilder.ToString();
                
                // 处理每个同音词
                for (int i = 0; i < sameCount; i++)
                {
                    if (position + 4 > data.Length) break;
                    
                    // 中文词组长度
                    ushort wordLength = BitConverter.ToUInt16(data, position);
                    position += 2;
                    
                    // 验证词长度
                    if (wordLength > 500)
                    {
                        Console.WriteLine($"警告: 异常词长度 {wordLength} @ 0x{position-2:X4}");
                        break;
                    }
                    
                    if (position + wordLength > data.Length)
                    {
                        Console.WriteLine($"警告: 词数据超出文件范围 @ 0x{position:X4}");
                        break;
                    }
                    
                    // 解码中文字符串
                    string word = DecodeWordString(data, position, wordLength);
                    position += wordLength;
                    
                    // 扩展信息
                    if (position + 2 > data.Length) break;
                    ushort extLength = BitConverter.ToUInt16(data, position);
                    position += 2;
                    
                    ushort frequency = 1;
                    if (extLength >= 2 && position + 2 <= data.Length)
                    {
                        frequency = BitConverter.ToUInt16(data, position);
                    }
                    
                    // 跳过扩展信息
                    if (position + extLength > data.Length)
                    {
                        Console.WriteLine($"警告: 扩展信息超出文件范围 @ 0x{position:X4}");
                        break;
                    }
                    position += extLength;
                    
                    // 添加到词库
                    vocabulary.Add(new VocabularyItem(word, pinyin, frequency));
                    itemCount++;
                }
            }
            
            if (skippedCount > 0)
            {
                Console.WriteLine($"跳过 {skippedCount} 个异常词条组");
            }
            
            return vocabulary;
        }

        /// <summary>
        /// 动态定位词条起始位置
        /// 解决不同版本词库偏移量不同的问题
        /// </summary>
        private static int FindVocabularyStartPosition(byte[] data)
        {
            // 搜索范围：0x2000 - 0x3000
            int start = 0x2000;
            int end = Math.Min(0x3000, data.Length - 4);
            
            for (int i = start; i < end; i++)
            {
                // 寻找可能的词条起始模式：smallCount (1-10) + pyTableLength (2-50)
                if (data[i] <= 10 && data[i] > 0 && 
                    data[i + 2] > 0 && data[i + 2] < 50 &&
                    data[i + 3] == 0)
                {
                    Console.WriteLine($"找到可能的词条起始位置: 0x{i:X4}");
                    return i;
                }
            }
            
            Console.WriteLine("警告: 未找到词条起始位置，使用默认值");
            return 0x2628;
        }

        /// <summary>
        /// 解码中文字符串（增强容错）
        /// 处理不同编码的词库
        /// </summary>
        private static string DecodeWordString(byte[] data, int start, int length)
        {
            // 优先尝试UTF-16LE解码（大多数词库使用）
            try
            {
                return Encoding.Unicode.GetString(data, start, length);
            }
            catch
            {
                // 尝试GB18030解码（较旧词库可能使用）
                try
                {
                    Encoding gb18030 = Encoding.GetEncoding("GB18030");
                    return gb18030.GetString(data, start, length);
                }
                catch
                {
                    // 回退到十六进制表示
                    return "0x" + BitConverter.ToString(data, start, Math.Min(length, 8)).Replace("-", "");
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// 词条项类
    /// 表示一个转换后的词条
    /// </summary>
    public class VocabularyItem
    {
        /// <summary>
        /// 中文词语
        /// </summary>
        public string Word { get; }
        
        /// <summary>
        /// 拼音字符串（单引号分隔）
        /// </summary>
        public string Pinyin { get; }
        
        /// <summary>
        /// 词频
        /// </summary>
        public int Frequency { get; }

        /// <summary>
        /// 创建词条项
        /// </summary>
        /// <param name="word">中文词语</param>
        /// <param name="pinyin">拼音字符串</param>
        /// <param name="frequency">词频</param>
        public VocabularyItem(string word, string pinyin, int frequency)
        {
            Word = word;
            Pinyin = pinyin;
            Frequency = frequency;
        }
    }
}
