namespace PersonalTools.UserControls
{
    /// <summary>
    /// 文件分析控件的统一接口，供 <see cref="FileTabHostControl"/> 按完整路径建/覆盖 tab。
    /// 实现者须为 UserControl（以便作为 tab 内容）。
    /// </summary>
    internal interface IFileAnalyzerView
    {
        /// <summary>加载并展示指定文件的分析结果。</summary>
        void LoadFile(string filePath);

        /// <summary>拖入文件时回调宿主，由宿主按完整路径决定新建/覆盖 tab。</summary>
        Action<IReadOnlyList<string>>? FilesDropped { get; set; }
    }
}
