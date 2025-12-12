using System;
using System.Globalization;

namespace MyTool
{
    /// <summary>
    /// PE资源解析器版本信息语言解析辅助模块
    /// 包含版本信息中语言和代码页相关解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionLanguage
    {
        /// <summary>
        /// 将语言ID和代码页转换为可读的文本信息
        /// </summary>
        /// <param name="languageId">语言ID</param>
        /// <param name="codePage">代码页</param>
        /// <returns>可读的翻译信息</returns>
        internal static string GetReadableTranslationInfo(uint languageId, uint codePage)
        {
            // 获取语言名称
            string languageName = GetLanguageName(languageId);
            
            // 获取代码页名称
            string codePageName = GetCodePageName(codePage);
            
            // 返回格式化的字符串
            return $"{languageName} (0x{languageId:X4}), {codePageName} ({codePage})";
        }

        /// <summary>
        /// 根据语言ID获取语言名称
        /// </summary>
        /// <param name="languageId">语言ID</param>
        /// <returns>语言名称</returns>
        private static string GetLanguageName(uint languageId)
        {
            // 检查系统语言环境
            bool isSimplifiedChinese = false;
            bool isTraditionalChinese = false;
            GetChinese(ref isSimplifiedChinese, ref isTraditionalChinese);

            // 常见的语言ID映射
            return languageId switch
            {
                0x0400 => isTraditionalChinese ? "進程默認語言" : (isSimplifiedChinese ? "进程默认语言" : "Process Default Language"),
                0x0401 => isTraditionalChinese ? "阿拉伯語（沙特阿拉伯）" : (isSimplifiedChinese ? "阿拉伯语（沙特阿拉伯）" : "Arabic (Saudi Arabia)"),
                0x0402 => isTraditionalChinese ? "保加利亞語" : (isSimplifiedChinese ? "保加利亚语" : "Bulgarian"),
                0x0403 => isTraditionalChinese ? "加泰羅尼亞語" : (isSimplifiedChinese ? "加泰罗尼亚语" : "Catalan"),
                0x0404 => isTraditionalChinese ? "中文（繁體）" : (isSimplifiedChinese ? "中文（繁体）" : "Chinese (Traditional)"),
                0x0405 => isTraditionalChinese ? "捷克語" : (isSimplifiedChinese ? "捷克语" : "Czech"),
                0x0406 => isTraditionalChinese ? "丹麥語" : (isSimplifiedChinese ? "丹麦语" : "Danish"),
                0x0407 => isTraditionalChinese ? "德語" : (isSimplifiedChinese ? "德语" : "German"),
                0x0408 => isTraditionalChinese ? "希臘語" : (isSimplifiedChinese ? "希腊语" : "Greek"),
                0x0409 => isTraditionalChinese ? "英語（美國）" : (isSimplifiedChinese ? "英语（美国）" : "English (United States)"),
                0x040A => isTraditionalChinese ? "西班牙語（傳統排序）" : (isSimplifiedChinese ? "西班牙语（传统排序）" : "Spanish (Traditional Sort)"),
                0x040B => isTraditionalChinese ? "芬蘭語" : (isSimplifiedChinese ? "芬兰语" : "Finnish"),
                0x040C => isTraditionalChinese ? "法語（標準）" : (isSimplifiedChinese ? "法语（标准）" : "French (Standard)"),
                0x040D => isTraditionalChinese ? "希伯來語" : (isSimplifiedChinese ? "希伯来语" : "Hebrew"),
                0x040E => isTraditionalChinese ? "匈牙利語" : (isSimplifiedChinese ? "匈牙利语" : "Hungarian"),
                0x040F => isTraditionalChinese ? "冰島語" : (isSimplifiedChinese ? "冰岛语" : "Icelandic"),
                0x0410 => isTraditionalChinese ? "意大利語" : (isSimplifiedChinese ? "意大利语" : "Italian"),
                0x0411 => isTraditionalChinese ? "日語" : (isSimplifiedChinese ? "日语" : "Japanese"),
                0x0412 => isTraditionalChinese ? "韓語" : (isSimplifiedChinese ? "韩语" : "Korean"),
                0x0413 => isTraditionalChinese ? "荷蘭語" : (isSimplifiedChinese ? "荷兰语" : "Dutch"),
                0x0414 => isTraditionalChinese ? "挪威語（博克馬爾）" : (isSimplifiedChinese ? "挪威语（博克马尔）" : "Norwegian (Bokmal)"),
                0x0415 => isTraditionalChinese ? "波蘭語" : (isSimplifiedChinese ? "波兰语" : "Polish"),
                0x0416 => isTraditionalChinese ? "葡萄牙語（巴西）" : (isSimplifiedChinese ? "葡萄牙语（巴西）" : "Portuguese (Brazilian)"),
                0x0417 => isTraditionalChinese ? "羅曼什語" : (isSimplifiedChinese ? "罗曼什语" : "Romansh"),
                0x0418 => isTraditionalChinese ? "羅馬尼亞語" : (isSimplifiedChinese ? "罗马尼亚语" : "Romanian"),
                0x0419 => isTraditionalChinese ? "俄語" : (isSimplifiedChinese ? "俄语" : "Russian"),
                0x041A => isTraditionalChinese ? "克羅地亞語" : (isSimplifiedChinese ? "克罗地亚语" : "Croatian"),
                0x041B => isTraditionalChinese ? "斯洛伐克語" : (isSimplifiedChinese ? "斯洛伐克语" : "Slovak"),
                0x041C => isTraditionalChinese ? "阿爾巴尼亞語" : (isSimplifiedChinese ? "阿尔巴尼亚语" : "Albanian"),
                0x041D => isTraditionalChinese ? "瑞典語" : (isSimplifiedChinese ? "瑞典语" : "Swedish"),
                0x041E => isTraditionalChinese ? "泰語" : (isSimplifiedChinese ? "泰语" : "Thai"),
                0x041F => isTraditionalChinese ? "土耳其語" : (isSimplifiedChinese ? "土耳其语" : "Turkish"),
                0x0420 => isTraditionalChinese ? "烏爾都語" : (isSimplifiedChinese ? "乌尔都语" : "Urdu"),
                0x0421 => isTraditionalChinese ? "印度尼西亞語" : (isSimplifiedChinese ? "印度尼西亚语" : "Indonesian"),
                0x0422 => isTraditionalChinese ? "烏克蘭語" : (isSimplifiedChinese ? "乌克兰语" : "Ukrainian"),
                0x0423 => isTraditionalChinese ? "白俄羅斯語" : (isSimplifiedChinese ? "白俄罗斯语" : "Belarusian"),
                0x0424 => isTraditionalChinese ? "斯洛文尼亞語" : (isSimplifiedChinese ? "斯洛文尼亚语" : "Slovenian"),
                0x0425 => isTraditionalChinese ? "愛沙尼亞語" : (isSimplifiedChinese ? "爱沙尼亚语" : "Estonian"),
                0x0426 => isTraditionalChinese ? "拉脫維亞語" : (isSimplifiedChinese ? "拉脱维亚语" : "Latvian"),
                0x0427 => isTraditionalChinese ? "立陶宛語" : (isSimplifiedChinese ? "立陶宛语" : "Lithuanian"),
                0x0428 => isTraditionalChinese ? "塔吉克語" : (isSimplifiedChinese ? "塔吉克语" : "Tajik"),
                0x0429 => isTraditionalChinese ? "波斯語（法爾斯語）" : (isSimplifiedChinese ? "波斯语（法尔斯语）" : "Persian (Farsi)"),
                0x042A => isTraditionalChinese ? "越南語" : (isSimplifiedChinese ? "越南语" : "Vietnamese"),
                0x042B => isTraditionalChinese ? "亞美尼亞語" : (isSimplifiedChinese ? "亚美尼亚语" : "Armenian"),
                0x042C => isTraditionalChinese ? "阿塞拜疆語（拉丁文）" : (isSimplifiedChinese ? "阿塞拜疆语（拉丁文）" : "Azerbaijani (Latin)"),
                0x042D => isTraditionalChinese ? "巴斯克語" : (isSimplifiedChinese ? "巴斯克语" : "Basque"),
                0x042E => isTraditionalChinese ? "上索布語" : (isSimplifiedChinese ? "上索布语" : "Upper Sorbian"),
                0x042F => isTraditionalChinese ? "馬其頓語" : (isSimplifiedChinese ? "马其顿语" : "Macedonian"),
                0x0432 => isTraditionalChinese ? "茨瓦納語" : (isSimplifiedChinese ? "茨瓦纳语" : "Setswana"),
                0x0436 => isTraditionalChinese ? "南非荷蘭語" : (isSimplifiedChinese ? "南非荷兰语" : "Afrikaans"),
                0x0437 => isTraditionalChinese ? "格魯吉亞語" : (isSimplifiedChinese ? "格鲁吉亚语" : "Georgian"),
                0x0438 => isTraditionalChinese ? "法羅語" : (isSimplifiedChinese ? "法罗语" : "Faroese"),
                0x0439 => isTraditionalChinese ? "印地語" : (isSimplifiedChinese ? "印地语" : "Hindi"),
                0x043A => isTraditionalChinese ? "馬耳他語" : (isSimplifiedChinese ? "马耳他语" : "Maltese"),
                0x043B => isTraditionalChinese ? "薩米語（北部）" : (isSimplifiedChinese ? "萨米语（北部）" : "Sami (Northern)"),
                0x043E => isTraditionalChinese ? "馬來語" : (isSimplifiedChinese ? "马来语" : "Malay"),
                0x043F => isTraditionalChinese ? "哈薩克語" : (isSimplifiedChinese ? "哈萨克语" : "Kazakh"),
                0x0440 => isTraditionalChinese ? "吉爾吉斯語" : (isSimplifiedChinese ? "吉尔吉斯语" : "Kyrgyz"),
                0x0441 => isTraditionalChinese ? "斯瓦希里語" : (isSimplifiedChinese ? "斯瓦希里语" : "Swahili"),
                0x0442 => isTraditionalChinese ? "土庫曼語" : (isSimplifiedChinese ? "土库曼语" : "Turkmen"),
                0x0443 => isTraditionalChinese ? "烏茲別克語（拉丁文）" : (isSimplifiedChinese ? "乌兹别克语（拉丁文）" : "Uzbek (Latin)"),
                0x0444 => isTraditionalChinese ? "韃靼語" : (isSimplifiedChinese ? "鞑靼语" : "Tatar"),
                0x0445 => isTraditionalChinese ? "孟加拉語" : (isSimplifiedChinese ? "孟加拉语" : "Bengali"),
                0x0446 => isTraditionalChinese ? "旁遮普語" : (isSimplifiedChinese ? "旁遮普语" : "Punjabi"),
                0x0447 => isTraditionalChinese ? "古吉拉特語" : (isSimplifiedChinese ? "古吉拉特语" : "Gujarati"),
                0x0448 => isTraditionalChinese ? "奧里雅語" : (isSimplifiedChinese ? "奥里雅语" : "Odia"),
                0x0449 => isTraditionalChinese ? "泰米爾語" : (isSimplifiedChinese ? "泰米尔语" : "Tamil"),
                0x044A => isTraditionalChinese ? "泰盧固語" : (isSimplifiedChinese ? "泰卢固语" : "Telugu"),
                0x044B => isTraditionalChinese ? "卡納達語" : (isSimplifiedChinese ? "卡纳达语" : "Kannada"),
                0x044C => isTraditionalChinese ? "馬拉雅拉姆語" : (isSimplifiedChinese ? "马拉雅拉姆语" : "Malayalam"),
                0x044D => isTraditionalChinese ? "阿薩姆語" : (isSimplifiedChinese ? "阿萨姆语" : "Assamese"),
                0x044E => isTraditionalChinese ? "馬拉地語" : (isSimplifiedChinese ? "马拉地语" : "Marathi"),
                0x044F => isTraditionalChinese ? "梵語" : (isSimplifiedChinese ? "梵语" : "Sanskrit"),
                0x0450 => isTraditionalChinese ? "蒙古語（西里爾文）" : (isSimplifiedChinese ? "蒙古语（西里尔文）" : "Mongolian (Cyrillic)"),
                0x0451 => isTraditionalChinese ? "藏語" : (isSimplifiedChinese ? "藏语" : "Tibetan"),
                0x0452 => isTraditionalChinese ? "威爾士語" : (isSimplifiedChinese ? "威尔士语" : "Welsh"),
                0x045A => isTraditionalChinese ? "敘利亞語" : (isSimplifiedChinese ? "叙利亚语" : "Syriac"),
                0x045B => isTraditionalChinese ? "僧伽羅語" : (isSimplifiedChinese ? "僧伽罗语" : "Sinhala"),
                0x0461 => isTraditionalChinese ? "尼泊爾語" : (isSimplifiedChinese ? "尼泊尔语" : "Nepali"),
                0x0462 => isTraditionalChinese ? "弗裡西亞語" : (isSimplifiedChinese ? "弗里西亚语" : "Frisian"),
                0x0463 => isTraditionalChinese ? "普什圖語" : (isSimplifiedChinese ? "普什图语" : "Pashto"),
                0x0464 => isTraditionalChinese ? "菲律賓語" : (isSimplifiedChinese ? "菲律宾语" : "Filipino"),
                0x0465 => isTraditionalChinese ? "迪維希語" : (isSimplifiedChinese ? "迪维希语" : "Divehi"),
                0x0468 => isTraditionalChinese ? "豪薩語" : (isSimplifiedChinese ? "豪萨语" : "Hausa"),
                0x046A => isTraditionalChinese ? "約魯巴語" : (isSimplifiedChinese ? "约鲁巴语" : "Yoruba"),
                0x046B => isTraditionalChinese ? "克丘亞語（玻利維亞）" : (isSimplifiedChinese ? "克丘亚语（玻利维亚）" : "Quechua (Bolivia)"),
                0x046C => isTraditionalChinese ? "北索托語" : (isSimplifiedChinese ? "北索托语" : "Sepedi"),
                0x046D => isTraditionalChinese ? "巴什基爾語" : (isSimplifiedChinese ? "巴什基尔语" : "Bashkir"),
                0x046E => isTraditionalChinese ? "盧森堡語" : (isSimplifiedChinese ? "卢森堡语" : "Luxembourgish"),
                0x046F => isTraditionalChinese ? "格陵蘭語" : (isSimplifiedChinese ? "格陵兰语" : "Greenlandic"),
                0x0470 => isTraditionalChinese ? "伊博語" : (isSimplifiedChinese ? "伊博语" : "Igbo"),
                0x0473 => isTraditionalChinese ? "提格利尼亞語" : (isSimplifiedChinese ? "提格利尼亚语" : "Tigrinya"),
                0x0475 => isTraditionalChinese ? "夏威夷語" : (isSimplifiedChinese ? "夏威夷语" : "Hawaiian"),
                0x0478 => isTraditionalChinese ? "彝語" : (isSimplifiedChinese ? "彝语" : "Yi"),
                0x047A => isTraditionalChinese ? "馬普切語" : (isSimplifiedChinese ? "马普切语" : "Mapudungun"),
                0x047C => isTraditionalChinese ? "莫霍克語" : (isSimplifiedChinese ? "莫霍克语" : "Mohawk"),
                0x047E => isTraditionalChinese ? "布列塔尼語" : (isSimplifiedChinese ? "布列塔尼语" : "Breton"),
                0x0480 => isTraditionalChinese ? "維吾爾語" : (isSimplifiedChinese ? "维吾尔语" : "Uyghur"),
                0x0481 => isTraditionalChinese ? "毛利語" : (isSimplifiedChinese ? "毛利语" : "Maori"),
                0x0482 => isTraditionalChinese ? "奧克西坦語" : (isSimplifiedChinese ? "奥克西坦语" : "Occitan"),
                0x0483 => isTraditionalChinese ? "科西嘉語" : (isSimplifiedChinese ? "科西嘉语" : "Corsican"),
                0x0484 => isTraditionalChinese ? "阿爾薩斯語" : (isSimplifiedChinese ? "阿尔萨斯语" : "Alsatian"),
                0x0485 => isTraditionalChinese ? "雅庫特語" : (isSimplifiedChinese ? "雅库特语" : "Sakha"),
                0x0486 => isTraditionalChinese ? "盧旺達語" : (isSimplifiedChinese ? "卢旺达语" : "Kinyarwanda"),
                0x0487 => isTraditionalChinese ? "沃洛夫語" : (isSimplifiedChinese ? "沃洛夫语" : "Wolof"),
                0x0488 => isTraditionalChinese ? "達里語" : (isSimplifiedChinese ? "达里语" : "Dari"),
                0x048C => isTraditionalChinese ? "斯瓦蒂語" : (isSimplifiedChinese ? "斯瓦蒂语" : "SiSwati"),
                0x048F => isTraditionalChinese ? "低地德語" : (isSimplifiedChinese ? "低地德语" : "Low German"),
                0x0491 => isTraditionalChinese ? "雅庫特語" : (isSimplifiedChinese ? "雅库特语" : "Yakut"),
                0x0493 => isTraditionalChinese ? "土庫曼語（西里爾文）" : (isSimplifiedChinese ? "土库曼语（西里尔文）" : "Turkmen (Cyrillic)"),
                0x0800 => isTraditionalChinese ? "中文（簡體）" : (isSimplifiedChinese ? "中文（简体）" : "Chinese (Simplified)"),
                0x0801 => isTraditionalChinese ? "阿拉伯語（伊拉克）" : (isSimplifiedChinese ? "阿拉伯语（伊拉克）" : "Arabic (Iraq)"),
                0x0804 => isTraditionalChinese ? "中文（簡體）" : (isSimplifiedChinese ? "中文（简体）" : "Chinese (Simplified)"),
                0x0807 => isTraditionalChinese ? "德語（瑞士）" : (isSimplifiedChinese ? "德语（瑞士）" : "German (Switzerland)"),
                0x0809 => isTraditionalChinese ? "英語（英國）" : (isSimplifiedChinese ? "英语（英国）" : "English (United Kingdom)"),
                0x080A => isTraditionalChinese ? "西班牙語（墨西哥）" : (isSimplifiedChinese ? "西班牙语（墨西哥）" : "Spanish (Mexico)"),
                0x080C => isTraditionalChinese ? "法語（比利時）" : (isSimplifiedChinese ? "法语（比利时）" : "French (Belgian)"),
                0x0810 => isTraditionalChinese ? "意大利語（瑞士）" : (isSimplifiedChinese ? "意大利语（瑞士）" : "Italian (Switzerland)"),
                0x0813 => isTraditionalChinese ? "荷蘭語（比利時）" : (isSimplifiedChinese ? "荷兰语（比利时）" : "Dutch (Belgium)"),
                0x0814 => isTraditionalChinese ? "挪威語（尼諾斯克）" : (isSimplifiedChinese ? "挪威语（尼诺斯克）" : "Norwegian (Nynorsk)"),
                0x0816 => isTraditionalChinese ? "葡萄牙語（標準）" : (isSimplifiedChinese ? "葡萄牙语（标准）" : "Portuguese (Standard)"),
                0x0818 => isTraditionalChinese ? "羅馬尼亞語（摩爾多瓦）" : (isSimplifiedChinese ? "罗马尼亚语（摩尔多瓦）" : "Romanian (Moldova)"),
                0x0819 => isTraditionalChinese ? "俄語（摩爾多瓦）" : (isSimplifiedChinese ? "俄语（摩尔多瓦）" : "Russian (Moldova)"),
                0x081A => isTraditionalChinese ? "塞爾維亞語（拉丁文）" : (isSimplifiedChinese ? "塞尔维亚语（拉丁文）" : "Serbian (Latin)"),
                0x081D => isTraditionalChinese ? "瑞典語（芬蘭）" : (isSimplifiedChinese ? "瑞典语（芬兰）" : "Swedish (Finland)"),
                0x0820 => isTraditionalChinese ? "烏爾都語（印度）" : (isSimplifiedChinese ? "乌尔都语（印度）" : "Urdu (India)"),
                0x0827 => isTraditionalChinese ? "立陶宛語（經典）" : (isSimplifiedChinese ? "立陶宛语（经典）" : "Lithuanian (Classic)"),
                0x082C => isTraditionalChinese ? "阿塞拜疆語（西里爾文）" : (isSimplifiedChinese ? "阿塞拜疆语（西里尔文）" : "Azerbaijani (Cyrillic)"),
                0x082E => isTraditionalChinese ? "下索布語" : (isSimplifiedChinese ? "下索布语" : "Lower Sorbian"),
                0x083B => isTraditionalChinese ? "薩米語（北部，芬蘭）" : (isSimplifiedChinese ? "萨米语（北部，芬兰）" : "Sami (Northern, Finland)"),
                0x083E => isTraditionalChinese ? "馬來語（汶萊達魯薩蘭國）" : (isSimplifiedChinese ? "马来语（文莱达鲁萨兰国）" : "Malay (Brunei Darussalam)"),
                0x0843 => isTraditionalChinese ? "烏茲別克語（西里爾文）" : (isSimplifiedChinese ? "乌兹别克语（西里尔文）" : "Uzbek (Cyrillic)"),
                0x0845 => isTraditionalChinese ? "孟加拉語（印度）" : (isSimplifiedChinese ? "孟加拉语（印度）" : "Bengali (India)"),
                0x0846 => isTraditionalChinese ? "旁遮普語（印度）" : (isSimplifiedChinese ? "旁遮普语（印度）" : "Punjabi (India)"),
                0x0849 => isTraditionalChinese ? "泰米爾語（印度）" : (isSimplifiedChinese ? "泰米尔语（印度）" : "Tamil (India)"),
                0x0850 => isTraditionalChinese ? "蒙古語（傳統）" : (isSimplifiedChinese ? "蒙古语（传统）" : "Mongolian (Traditional)"),
                0x0851 => isTraditionalChinese ? "藏語（中國）" : (isSimplifiedChinese ? "藏语（中国）" : "Tibetan (PRC)"),
                0x085D => isTraditionalChinese ? "因紐特語（拉丁文）" : (isSimplifiedChinese ? "因纽特语（拉丁文）" : "Inuktitut (Latin)"),
                0x0860 => isTraditionalChinese ? "克什米爾語（天城文）" : (isSimplifiedChinese ? "克什米尔语（天城文）" : "Kashmiri (Devanagari)"),
                0x0861 => isTraditionalChinese ? "尼泊爾語（印度）" : (isSimplifiedChinese ? "尼泊尔语（印度）" : "Nepali (India)"),
                0x086B => isTraditionalChinese ? "克丘亞語（厄瓜多爾）" : (isSimplifiedChinese ? "克丘亚语（厄瓜多尔）" : "Quechua (Ecuador)"),
                0x0873 => isTraditionalChinese ? "提格利尼亞語（厄立特裡亞）" : (isSimplifiedChinese ? "提格利尼亚语（厄立特里亚）" : "Tigrinya (Eritrea)"),
                0x0C04 => isTraditionalChinese ? "中文（香港特別行政區）" : (isSimplifiedChinese ? "中文（香港特别行政区）" : "Chinese (Hong Kong S.A.R.)"),
                0x0C09 => isTraditionalChinese ? "英語（澳大利亞）" : (isSimplifiedChinese ? "英语（澳大利亚）" : "English (Australia)"),
                0x0C0A => isTraditionalChinese ? "西班牙語（現代排序）" : (isSimplifiedChinese ? "西班牙语（现代排序）" : "Spanish (Modern Sort)"),
                0x0C0C => isTraditionalChinese ? "法語（加拿大）" : (isSimplifiedChinese ? "法语（加拿大）" : "French (Canada)"),
                0x0C1A => isTraditionalChinese ? "塞爾維亞語（西里爾文）" : (isSimplifiedChinese ? "塞尔维亚语（西里尔文）" : "Serbian (Cyrillic)"),
                0x0C6B => isTraditionalChinese ? "克丘亞語（秘魯）" : (isSimplifiedChinese ? "克丘亚语（秘鲁）" : "Quechua (Peru)"),
                0x1004 => isTraditionalChinese ? "中文（新加坡）" : (isSimplifiedChinese ? "中文（新加坡）" : "Chinese (Singapore)"),
                0x1009 => isTraditionalChinese ? "英語（加拿大）" : (isSimplifiedChinese ? "英语（加拿大）" : "English (Canada)"),
                0x100A => isTraditionalChinese ? "西班牙語（危地馬拉）" : (isSimplifiedChinese ? "西班牙语（危地马拉）" : "Spanish (Guatemala)"),
                0x100C => isTraditionalChinese ? "法語（瑞士）" : (isSimplifiedChinese ? "法语（瑞士）" : "French (Switzerland)"),
                0x101A => isTraditionalChinese ? "克羅地亞語（拉丁文）" : (isSimplifiedChinese ? "克罗地亚语（拉丁文）" : "Croatian (Latin)"),
                0x106B => isTraditionalChinese ? "克丘亞語（玻利維亞）" : (isSimplifiedChinese ? "克丘亚语（玻利维亚）" : "Quechua (Bolivia)"),
                0x1401 => isTraditionalChinese ? "阿拉伯語（利比亞）" : (isSimplifiedChinese ? "阿拉伯语（利比亚）" : "Arabic (Libya)"),
                0x1404 => isTraditionalChinese ? "中文（澳門特別行政區）" : (isSimplifiedChinese ? "中文（澳门特别行政区）" : "Chinese (Macao S.A.R.)"),
                0x1409 => isTraditionalChinese ? "英語（新西蘭）" : (isSimplifiedChinese ? "英语（新西兰）" : "English (New Zealand)"),
                0x140A => isTraditionalChinese ? "西班牙語（哥斯達黎加）" : (isSimplifiedChinese ? "西班牙语（哥斯达黎加）" : "Spanish (Costa Rica)"),
                0x140C => isTraditionalChinese ? "法語（盧森堡）" : (isSimplifiedChinese ? "法语（卢森堡）" : "French (Luxembourg)"),
                0x141A => isTraditionalChinese ? "波斯尼亞語（拉丁文）" : (isSimplifiedChinese ? "波斯尼亚语（拉丁文）" : "Bosnian (Latin)"),
                0x1801 => isTraditionalChinese ? "阿拉伯語（阿爾及利亞）" : (isSimplifiedChinese ? "阿拉伯语（阿尔及利亚）" : "Arabic (Algeria)"),
                0x1809 => isTraditionalChinese ? "英語（愛爾蘭）" : (isSimplifiedChinese ? "英语（爱尔兰）" : "English (Ireland)"),
                0x180A => isTraditionalChinese ? "西班牙語（巴拿馬）" : (isSimplifiedChinese ? "西班牙语（巴拿马）" : "Spanish (Panama)"),
                0x180C => isTraditionalChinese ? "法語（摩納哥）" : (isSimplifiedChinese ? "法语（摩纳哥）" : "French (Monaco)"),
                0x1C01 => isTraditionalChinese ? "阿拉伯語（摩洛哥）" : (isSimplifiedChinese ? "阿拉伯语（摩洛哥）" : "Arabic (Morocco)"),
                0x1C09 => isTraditionalChinese ? "英語（南非）" : (isSimplifiedChinese ? "英语（南非）" : "English (South Africa)"),
                0x1C0A => isTraditionalChinese ? "西班牙語（多米尼加共和國）" : (isSimplifiedChinese ? "西班牙语（多米尼加共和国）" : "Spanish (Dominican Republic)"),
                0x1C0C => isTraditionalChinese ? "法語（加蓬）" : (isSimplifiedChinese ? "法语（加蓬）" : "French (West Indies)"),
                0x2001 => isTraditionalChinese ? "阿拉伯語（突尼斯）" : (isSimplifiedChinese ? "阿拉伯语（突尼斯）" : "Arabic (Tunisia)"),
                0x2009 => isTraditionalChinese ? "英語（牙買加）" : (isSimplifiedChinese ? "英语（牙买加）" : "English (Jamaica)"),
                0x200A => isTraditionalChinese ? "西班牙語（委內瑞拉）" : (isSimplifiedChinese ? "西班牙语（委内瑞拉）" : "Spanish (Venezuela)"),
                0x200C => isTraditionalChinese ? "法語（剛果）" : (isSimplifiedChinese ? "法语（刚果）" : "French (Congo)"),
                0x2401 => isTraditionalChinese ? "阿拉伯語（阿曼）" : (isSimplifiedChinese ? "阿拉伯语（阿曼）" : "Arabic (Oman)"),
                0x2409 => isTraditionalChinese ? "英語（加勒比海地區）" : (isSimplifiedChinese ? "英语（加勒比海地区）" : "English (Caribbean)"),
                0x240A => isTraditionalChinese ? "西班牙語（哥倫比亞）" : (isSimplifiedChinese ? "西班牙语（哥伦比亚）" : "Spanish (Colombia)"),
                0x240C => isTraditionalChinese ? "法語（塞內加爾）" : (isSimplifiedChinese ? "法语（塞内加尔）" : "French (Senegal)"),
                0x2801 => isTraditionalChinese ? "阿拉伯語（也門）" : (isSimplifiedChinese ? "阿拉伯语（也门）" : "Arabic (Yemen)"),
                0x2809 => isTraditionalChinese ? "英語（伯利茲）" : (isSimplifiedChinese ? "英语（伯利兹）" : "English (Belize)"),
                0x280A => isTraditionalChinese ? "西班牙語（秘魯）" : (isSimplifiedChinese ? "西班牙语（秘鲁）" : "Spanish (Peru)"),
                0x280C => isTraditionalChinese ? "法語（馬里）" : (isSimplifiedChinese ? "法语（马里）" : "French (Mali)"),
                0x2C01 => isTraditionalChinese ? "阿拉伯語（約旦）" : (isSimplifiedChinese ? "阿拉伯语（约旦）" : "Arabic (Jordan)"),
                0x2C09 => isTraditionalChinese ? "英語（特立尼達和多巴哥）" : (isSimplifiedChinese ? "英语（特立尼达和多巴哥）" : "English (Trinidad)"),
                0x2C0A => isTraditionalChinese ? "西班牙語（阿根廷）" : (isSimplifiedChinese ? "西班牙语（阿根廷）" : "Spanish (Argentina)"),
                0x2C0C => isTraditionalChinese ? "法語（科特迪瓦）" : (isSimplifiedChinese ? "法语（科特迪瓦）" : "French (Côte d'Ivoire)"),
                0x3001 => isTraditionalChinese ? "阿拉伯語（黎巴嫩）" : (isSimplifiedChinese ? "阿拉伯语（黎巴嫩）" : "Arabic (Lebanon)"),
                0x3009 => isTraditionalChinese ? "英語（津巴布韋）" : (isSimplifiedChinese ? "英语（津巴布韦）" : "English (Zimbabwe)"),
                0x300A => isTraditionalChinese ? "西班牙語（厄瓜多爾）" : (isSimplifiedChinese ? "西班牙语（厄瓜多尔）" : "Spanish (Ecuador)"),
                0x300C => isTraditionalChinese ? "法語（布基納法索）" : (isSimplifiedChinese ? "法语（布基纳法索）" : "French (Burkina Faso)"),
                0x3401 => isTraditionalChinese ? "阿拉伯語（科威特）" : (isSimplifiedChinese ? "阿拉伯语（科威特）" : "Arabic (Kuwait)"),
                0x3409 => isTraditionalChinese ? "英語（菲律賓）" : (isSimplifiedChinese ? "英语（菲律宾）" : "English (Philippines)"),
                0x340A => isTraditionalChinese ? "西班牙語（智利）" : (isSimplifiedChinese ? "西班牙语（智利）" : "Spanish (Chile)"),
                0x340C => isTraditionalChinese ? "法語（貝寧）" : (isSimplifiedChinese ? "法语（贝宁）" : "French (Benin)"),
                0x3801 => isTraditionalChinese ? "阿拉伯語（阿聯酋）" : (isSimplifiedChinese ? "阿拉伯语（阿联酋）" : "Arabic (U.A.E.)"),
                0x380A => isTraditionalChinese ? "西班牙語（烏拉圭）" : (isSimplifiedChinese ? "西班牙语（乌拉圭）" : "Spanish (Uruguay)"),
                0x380C => isTraditionalChinese ? "法語（尼日爾）" : (isSimplifiedChinese ? "法语（尼日尔）" : "French (Niger)"),
                0x3C01 => isTraditionalChinese ? "阿拉伯語（巴林）" : (isSimplifiedChinese ? "阿拉伯语（巴林）" : "Arabic (Bahrain)"),
                0x3C09 => isTraditionalChinese ? "英語（印度尼西亞）" : (isSimplifiedChinese ? "英语（印度尼西亚）" : "English (Indonesia)"),
                0x3C0A => isTraditionalChinese ? "西班牙語（巴拉圭）" : (isSimplifiedChinese ? "西班牙语（巴拉圭）" : "Spanish (Paraguay)"),
                0x3C0C => isTraditionalChinese ? "法語（多哥）" : (isSimplifiedChinese ? "法语（多哥）" : "French (Togo)"),
                0x4001 => isTraditionalChinese ? "阿拉伯語（卡塔爾）" : (isSimplifiedChinese ? "阿拉伯语（卡塔尔）" : "Arabic (Qatar)"),
                0x4009 => isTraditionalChinese ? "英語（馬來西亞）" : (isSimplifiedChinese ? "英语（马来西亚）" : "English (Malaysia)"),
                0x400A => isTraditionalChinese ? "西班牙語（薩爾瓦多）" : (isSimplifiedChinese ? "西班牙语（萨尔瓦多）" : "Spanish (El Salvador)"),
                0x400C => isTraditionalChinese ? "法語（乍得）" : (isSimplifiedChinese ? "法语（乍得）" : "French (Chad)"),
                0x4401 => isTraditionalChinese ? "阿拉伯語（敘利亞）" : (isSimplifiedChinese ? "阿拉伯语（叙利亚）" : "Arabic (Syria)"),
                0x4409 => isTraditionalChinese ? "英語（新加坡）" : (isSimplifiedChinese ? "英语（新加坡）" : "English (Singapore)"),
                0x440A => isTraditionalChinese ? "西班牙語（洪都拉斯）" : (isSimplifiedChinese ? "西班牙语（洪都拉斯）" : "Spanish (Honduras)"),
                0x440C => isTraditionalChinese ? "法語（中非共和國）" : (isSimplifiedChinese ? "法语（中非共和国）" : "French (Central African Republic)"),
                0x4809 => isTraditionalChinese ? "英語（阿拉伯聯合酋長國）" : (isSimplifiedChinese ? "英语（阿拉伯联合酋长国）" : "English (U.A.E.)"),
                0x480A => isTraditionalChinese ? "西班牙語（尼加拉瓜）" : (isSimplifiedChinese ? "西班牙语（尼加拉瓜）" : "Spanish (Nicaragua)"),
                0x480C => isTraditionalChinese ? "法語（剛果共和國）" : (isSimplifiedChinese ? "法语（刚果共和国）" : "French (Congo [DRC])"),
                0x4C0A => isTraditionalChinese ? "西班牙語（波多黎各）" : (isSimplifiedChinese ? "西班牙语（波多黎各）" : "Spanish (Puerto Rico)"),
                0x4C0C => isTraditionalChinese ? "法語（喀麥隆）" : (isSimplifiedChinese ? "法语（喀麦隆）" : "French (Cameroon)"),
                0x500A => isTraditionalChinese ? "西班牙語（美國）" : (isSimplifiedChinese ? "西班牙语（美国）" : "Spanish (United States)"),
                0x500C => isTraditionalChinese ? "法語（剛果民主共和國）" : (isSimplifiedChinese ? "法语（刚果民主共和国）" : "French (Congo [Republic])"),
                0x540A => isTraditionalChinese ? "西班牙語（拉丁美洲）" : (isSimplifiedChinese ? "西班牙语（拉丁美洲）" : "Spanish (Latin America)"),
                0x540C => isTraditionalChinese ? "法語（留尼汪）" : (isSimplifiedChinese ? "法语（留尼汪）" : "French (Réunion)"),
                0x580A => isTraditionalChinese ? "西班牙語（古巴）" : (isSimplifiedChinese ? "西班牙语（古巴）" : "Spanish (Cuba)"),
                0x580C => isTraditionalChinese ? "法語（馬約特）" : (isSimplifiedChinese ? "法语（马约特）" : "French (Mayotte)"),
                0x6009 => isTraditionalChinese ? "英語（印度）" : (isSimplifiedChinese ? "英语（印度）" : "English (India)"),
                0x640A => isTraditionalChinese ? "西班牙語（多米尼加共和國）" : (isSimplifiedChinese ? "西班牙语（多米尼加共和国）" : "Spanish (Dominican Republic)"),
                0x640C => isTraditionalChinese ? "法語（馬格里布）" : (isSimplifiedChinese ? "法语（马格里布）" : "French (Maghreb)"),
                0x6809 => isTraditionalChinese ? "英語（馬來西亞）" : (isSimplifiedChinese ? "英语（马来西亚）" : "English (Malaysia)"),
                0x6C0A => isTraditionalChinese ? "西班牙語（玻利維亞）" : (isSimplifiedChinese ? "西班牙语（玻利维亚）" : "Spanish (Bolivia)"),
                0x7009 => isTraditionalChinese ? "英語（新加坡）" : (isSimplifiedChinese ? "英语（新加坡）" : "English (Singapore)"),
                0x700A => isTraditionalChinese ? "西班牙語（巴拉圭）" : (isSimplifiedChinese ? "西班牙语（巴拉圭）" : "Spanish (Paraguay)"),
                0x700C => isTraditionalChinese ? "法語（赤道幾內亞）" : (isSimplifiedChinese ? "法语（赤道几内亚）" : "French (Equatorial Guinea)"),
                0x740A => isTraditionalChinese ? "西班牙語（秘魯）" : (isSimplifiedChinese ? "西班牙语（秘鲁）" : "Spanish (Peru)"),
                0x740C => isTraditionalChinese ? "法語（瑞士）" : (isSimplifiedChinese ? "法语（瑞士）" : "French (Switzerland)"),
                0x780A => isTraditionalChinese ? "西班牙語（烏拉圭）" : (isSimplifiedChinese ? "西班牙语（乌拉圭）" : "Spanish (Uruguay)"),
                0x780C => isTraditionalChinese ? "法語（盧旺達）" : (isSimplifiedChinese ? "法语（卢旺达）" : "French (Rwanda)"),
                0x7C0A => isTraditionalChinese ? "西班牙語（委內瑞拉）" : (isSimplifiedChinese ? "西班牙语（委内瑞拉）" : "Spanish (Venezuela)"),
                0x7C0C => isTraditionalChinese ? "法語（北非）" : (isSimplifiedChinese ? "法语（北非）" : "French (North Africa)"),
                _ => isTraditionalChinese ? $"未知語言 (0x{languageId:X4})" : (isSimplifiedChinese ? $"未知语言 (0x{languageId:X4})" : $"Unknown Language (0x{languageId:X4})"),
            };
        }

        /// <summary>
        /// 获取语言名称
        /// </summary>
        /// <param name="isSimplifiedChinese">是否是简体中文</param>
        /// <param name="isTraditionalChinese">是否是繁体中文</param>
        internal static void GetChinese(ref bool isSimplifiedChinese, ref bool isTraditionalChinese)
        {
            string cultureName = CultureInfo.CurrentCulture.Name;
            isSimplifiedChinese = cultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase);
            isTraditionalChinese = cultureName.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                                       cultureName.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                                       cultureName.Equals("zh-MO", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 根据代码页获取代码页名称
        /// </summary>
        /// <param name="codePage">代码页</param>
        /// <returns>代码页名称</returns>
        private static string GetCodePageName(uint codePage)
        {
            // 检查系统语言环境
            bool isSimplifiedChinese = false;
            bool isTraditionalChinese = false;
            GetChinese(ref isSimplifiedChinese, ref isTraditionalChinese);

            // 常见的代码页映射
            return codePage switch
            {
                0 => isTraditionalChinese ? "默認代碼頁" : (isSimplifiedChinese ? "默认代码页" : "Default Code Page"),
                37 => isTraditionalChinese ? "IBM EBCDIC 美國/加拿大" : (isSimplifiedChinese ? "IBM EBCDIC 美国/加拿大" : "IBM EBCDIC US-Canada"),
                437 => isTraditionalChinese ? "OEM 美國" : (isSimplifiedChinese ? "OEM 美国" : "OEM United States"),
                500 => isTraditionalChinese ? "IBM EBCDIC 國際" : (isSimplifiedChinese ? "IBM EBCDIC 国际" : "IBM EBCDIC International"),
                708 => isTraditionalChinese ? "阿拉伯語 (ASMO 708)" : (isSimplifiedChinese ? "阿拉伯语 (ASMO 708)" : "Arabic (ASMO 708)"),
                720 => isTraditionalChinese ? "阿拉伯語 (DOS)" : (isSimplifiedChinese ? "阿拉伯语 (DOS)" : "Arabic (DOS)"),
                737 => isTraditionalChinese ? "希臘語 (DOS)" : (isSimplifiedChinese ? "希腊语 (DOS)" : "Greek (DOS)"),
                775 => isTraditionalChinese ? "波羅的海語 (DOS)" : (isSimplifiedChinese ? "波罗的海语 (DOS)" : "Baltic (DOS)"),
                850 => isTraditionalChinese ? "西歐 (DOS)" : (isSimplifiedChinese ? "西欧 (DOS)" : "Western European (DOS)"),
                852 => isTraditionalChinese ? "中歐 (DOS)" : (isSimplifiedChinese ? "中欧 (DOS)" : "Central European (DOS)"),
                855 => isTraditionalChinese ? "OEM 西里爾文" : (isSimplifiedChinese ? "OEM 西里尔文" : "OEM Cyrillic"),
                857 => isTraditionalChinese ? "土耳其語 (DOS)" : (isSimplifiedChinese ? "土耳其语 (DOS)" : "Turkish (DOS)"),
                858 => isTraditionalChinese ? "OEM 多語言拉丁語 I" : (isSimplifiedChinese ? "OEM 多语言拉丁语 I" : "OEM Multilingual Latin I"),
                860 => isTraditionalChinese ? "葡萄牙語 (DOS)" : (isSimplifiedChinese ? "葡萄牙语 (DOS)" : "Portuguese (DOS)"),
                861 => isTraditionalChinese ? "冰島語 (DOS)" : (isSimplifiedChinese ? "冰岛语 (DOS)" : "Icelandic (DOS)"),
                862 => isTraditionalChinese ? "希伯來語 (DOS)" : (isSimplifiedChinese ? "希伯来语 (DOS)" : "Hebrew (DOS)"),
                863 => isTraditionalChinese ? "法語加拿大 (DOS)" : (isSimplifiedChinese ? "法语加拿大 (DOS)" : "French Canadian (DOS)"),
                864 => isTraditionalChinese ? "阿拉伯語 (864)" : (isSimplifiedChinese ? "阿拉伯语 (864)" : "Arabic (864)"),
                865 => isTraditionalChinese ? "北歐 (DOS)" : (isSimplifiedChinese ? "北欧 (DOS)" : "Nordic (DOS)"),
                866 => isTraditionalChinese ? "西里爾文 (DOS)" : (isSimplifiedChinese ? "西里尔文 (DOS)" : "Cyrillic (DOS)"),
                869 => isTraditionalChinese ? "希臘語，現代 (DOS)" : (isSimplifiedChinese ? "希腊语，现代 (DOS)" : "Greek, Modern (DOS)"),
                874 => isTraditionalChinese ? "泰語 (Windows)" : (isSimplifiedChinese ? "泰语 (Windows)" : "Thai (Windows)"),
                932 => isTraditionalChinese ? "日語 (Shift-JIS)" : (isSimplifiedChinese ? "日语 (Shift-JIS)" : "Japanese (Shift-JIS)"),
                936 => isTraditionalChinese ? "中文簡體 (GB2312)" : (isSimplifiedChinese ? "中文简体 (GB2312)" : "Chinese Simplified (GB2312)"),
                949 => isTraditionalChinese ? "韓語" : (isSimplifiedChinese ? "韩语" : "Korean"),
                950 => isTraditionalChinese ? "中文繁體 (Big5)" : (isSimplifiedChinese ? "中文繁体 (Big5)" : "Chinese Traditional (Big5)"),
                1200 => isTraditionalChinese ? "UTF-16 (小端序)" : (isSimplifiedChinese ? "UTF-16 (小端序)" : "UTF-16 (Little endian)"),
                1201 => isTraditionalChinese ? "UTF-16 (大端序)" : (isSimplifiedChinese ? "UTF-16 (大端序)" : "UTF-16 (Big endian)"),
                1250 => isTraditionalChinese ? "中歐 (Windows)" : (isSimplifiedChinese ? "中欧 (Windows)" : "Central European (Windows)"),
                1251 => isTraditionalChinese ? "西里爾文 (Windows)" : (isSimplifiedChinese ? "西里尔文 (Windows)" : "Cyrillic (Windows)"),
                1252 => isTraditionalChinese ? "西歐 (Windows)" : (isSimplifiedChinese ? "西欧 (Windows)" : "Western European (Windows)"),
                1253 => isTraditionalChinese ? "希臘語 (Windows)" : (isSimplifiedChinese ? "希腊语 (Windows)" : "Greek (Windows)"),
                1254 => isTraditionalChinese ? "土耳其語 (Windows)" : (isSimplifiedChinese ? "土耳其语 (Windows)" : "Turkish (Windows)"),
                1255 => isTraditionalChinese ? "希伯來語 (Windows)" : (isSimplifiedChinese ? "希伯来语 (Windows)" : "Hebrew (Windows)"),
                1256 => isTraditionalChinese ? "阿拉伯語 (Windows)" : (isSimplifiedChinese ? "阿拉伯语 (Windows)" : "Arabic (Windows)"),
                1257 => isTraditionalChinese ? "波羅的海語 (Windows)" : (isSimplifiedChinese ? "波罗的海语 (Windows)" : "Baltic (Windows)"),
                1258 => isTraditionalChinese ? "越南語 (Windows)" : (isSimplifiedChinese ? "越南语 (Windows)" : "Vietnamese (Windows)"),
                10000 => isTraditionalChinese ? "西歐 (Mac)" : (isSimplifiedChinese ? "西欧 (Mac)" : "Western European (Mac)"),
                10001 => isTraditionalChinese ? "日語 (Mac)" : (isSimplifiedChinese ? "日语 (Mac)" : "Japanese (Mac)"),
                10002 => isTraditionalChinese ? "中文繁體 (Mac)" : (isSimplifiedChinese ? "中文繁体 (Mac)" : "Chinese Traditional (Mac)"),
                10003 => isTraditionalChinese ? "韓語 (Mac)" : (isSimplifiedChinese ? "韩语 (Mac)" : "Korean (Mac)"),
                10004 => isTraditionalChinese ? "阿拉伯語 (Mac)" : (isSimplifiedChinese ? "阿拉伯语 (Mac)" : "Arabic (Mac)"),
                10005 => isTraditionalChinese ? "希伯來語 (Mac)" : (isSimplifiedChinese ? "希伯来语 (Mac)" : "Hebrew (Mac)"),
                10006 => isTraditionalChinese ? "希臘語 (Mac)" : (isSimplifiedChinese ? "希腊语 (Mac)" : "Greek (Mac)"),
                10007 => isTraditionalChinese ? "西里爾文 (Mac)" : (isSimplifiedChinese ? "西里尔文 (Mac)" : "Cyrillic (Mac)"),
                10008 => isTraditionalChinese ? "中文簡體 (Mac)" : (isSimplifiedChinese ? "中文简体 (Mac)" : "Chinese Simplified (Mac)"),
                10021 => isTraditionalChinese ? "泰語 (Mac)" : (isSimplifiedChinese ? "泰语 (Mac)" : "Thai (Mac)"),
                10029 => isTraditionalChinese ? "中歐 (Mac)" : (isSimplifiedChinese ? "中欧 (Mac)" : "Central European (Mac)"),
                10079 => isTraditionalChinese ? "冰島語 (Mac)" : (isSimplifiedChinese ? "冰岛语 (Mac)" : "Icelandic (Mac)"),
                10081 => isTraditionalChinese ? "土耳其語 (Mac)" : (isSimplifiedChinese ? "土耳其语 (Mac)" : "Turkish (Mac)"),
                10082 => isTraditionalChinese ? "克羅地亞語 (Mac)" : (isSimplifiedChinese ? "克罗地亚语 (Mac)" : "Croatian (Mac)"),
                12000 => isTraditionalChinese ? "UTF-32 (小端序)" : (isSimplifiedChinese ? "UTF-32 (小端序)" : "UTF-32 (Little endian)"),
                12001 => isTraditionalChinese ? "UTF-32 (大端序)" : (isSimplifiedChinese ? "UTF-32 (大端序)" : "UTF-32 (Big endian)"),
                20936 => isTraditionalChinese ? "中文簡體 (GB2312-80)" : (isSimplifiedChinese ? "中文简体 (GB2312-80)" : "Chinese Simplified (GB2312-80)"),
                28591 => isTraditionalChinese ? "ISO 8859-1 拉丁語 1" : (isSimplifiedChinese ? "ISO 8859-1 拉丁语 1" : "ISO 8859-1 Latin 1"),
                28592 => isTraditionalChinese ? "ISO 8859-2 中歐" : (isSimplifiedChinese ? "ISO 8859-2 中欧" : "ISO 8859-2 Central European"),
                28593 => isTraditionalChinese ? "ISO 8859-3 拉丁語 3" : (isSimplifiedChinese ? "ISO 8859-3 拉丁语 3" : "ISO 8859-3 Latin 3"),
                28594 => isTraditionalChinese ? "ISO 8859-4 波羅的海語" : (isSimplifiedChinese ? "ISO 8859-4 波罗的海语" : "ISO 8859-4 Baltic"),
                28595 => isTraditionalChinese ? "ISO 8859-5 西里爾文" : (isSimplifiedChinese ? "ISO 8859-5 西里尔文" : "ISO 8859-5 Cyrillic"),
                28596 => isTraditionalChinese ? "ISO 8859-6 阿拉伯語" : (isSimplifiedChinese ? "ISO 8859-6 阿拉伯语" : "ISO 8859-6 Arabic"),
                28597 => isTraditionalChinese ? "ISO 8859-7 希臘語" : (isSimplifiedChinese ? "ISO 8859-7 希腊语" : "ISO 8859-7 Greek"),
                28598 => isTraditionalChinese ? "ISO 8859-8 希伯來語" : (isSimplifiedChinese ? "ISO 8859-8 希伯来语" : "ISO 8859-8 Hebrew"),
                28599 => isTraditionalChinese ? "ISO 8859-9 土耳其語" : (isSimplifiedChinese ? "ISO 8859-9 土耳其语" : "ISO 8859-9 Turkish"),
                28603 => isTraditionalChinese ? "ISO 8859-13 愛沙尼亞語" : (isSimplifiedChinese ? "ISO 8859-13 爱沙尼亚语" : "ISO 8859-13 Estonian"),
                28605 => isTraditionalChinese ? "ISO 8859-15 拉丁語 9" : (isSimplifiedChinese ? "ISO 8859-15 拉丁语 9" : "ISO 8859-15 Latin 9"),
                51936 => isTraditionalChinese ? "EUC 中文簡體" : (isSimplifiedChinese ? "EUC 中文简体" : "EUC Simplified Chinese"),
                51949 => isTraditionalChinese ? "EUC 韓語" : (isSimplifiedChinese ? "EUC 韩语" : "EUC Korean"),
                52936 => isTraditionalChinese ? "HZ-GB2312 中文簡體" : (isSimplifiedChinese ? "HZ-GB2312 中文简体" : "HZ-GB2312 Simplified Chinese"),
                54936 => isTraditionalChinese ? "GB18030 中文簡體 (4 字節)" : (isSimplifiedChinese ? "GB18030 中文简体 (4 字节)" : "GB18030 Simplified Chinese (4 byte)"),
                65000 => isTraditionalChinese ? "UTF-7" : (isSimplifiedChinese ? "UTF-7" : "UTF-7"),
                65001 => isTraditionalChinese ? "UTF-8" : (isSimplifiedChinese ? "UTF-8" : "UTF-8"),
                _ => isTraditionalChinese ? $"未知代碼頁 ({codePage})" : (isSimplifiedChinese ? $"未知代码页 ({codePage})" : $"Unknown Code Page ({codePage})"),
            };
        }
    }
}