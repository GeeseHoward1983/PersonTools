using PersonalTools.Utils;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PersonalTools.MarkdownToWord
{
    /// <summary>
    /// 通过后台 STA 线程调用本机 Word，刷新导出文档的域结果缓存（不解除域链接）。
    /// 未安装 Word、更新失败或超时时返回 false；只在更新完成后替换原文件。
    /// </summary>
    internal static class WordFieldUpdater
    {
        private const int WdDoNotSaveChanges = 0;
        private const int WdAlertsNone = 0;
        private const int MsoAutomationSecurityForceDisable = 3;
        private static readonly TimeSpan WordTimeout = TimeSpan.FromSeconds(60);

        /// <summary>刷新正文、页眉页脚等域及目录，保存成功返回 true；失败保留原文件。</summary>
        public static bool TryUpdateFields(string docxPath) => TryOperation(() => UpdateFile(docxPath));

        private static bool UpdateFile(string docxPath) => UpdateExistingFile(GetDocumentPath(docxPath));

        private static bool UpdateExistingFile(string fullPath) =>
            UpdateIfWordInstalled(Type.GetTypeFromProgID("Word.Application"), fullPath);

        private static bool UpdateIfWordInstalled(Type? wordType, string fullPath) =>
            wordType != null && UpdateWritableFile(wordType, fullPath);

        private static bool UpdateWritableFile(Type wordType, string fullPath) =>
            !new FileInfo(fullPath).IsReadOnly && UpdateWorkingCopy(wordType, fullPath);

        private static string GetDocumentPath(string docxPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(docxPath);
            return GetExistingFullPath(docxPath);
        }

        private static string GetExistingFullPath(string docxPath)
        {
            string fullPath = Path.GetFullPath(docxPath);
            return EnsureDocumentExists(fullPath);
        }

        private static string EnsureDocumentExists(string fullPath) =>
            File.Exists(fullPath) ? fullPath : throw MissingDocument(fullPath);

        private static FileNotFoundException MissingDocument(string fullPath) =>
            new("待更新的 Word 文档不存在。", fullPath);

        private static bool UpdateWorkingCopy(Type wordType, string fullPath) => new UpdateSession(wordType, fullPath).Run();

        private sealed class UpdateSession(Type wordType, string path)
        {
            public bool Run()
            {
                using CancellationTokenSource timeout = new();
                return Update(timeout);
            }

            private bool Update(CancellationTokenSource timeout) => UpdateWithTimeout(wordType, path, timeout, WordTimeout);
        }

        private static bool UpdateWithTimeout(Type wordType, string fullPath, CancellationTokenSource timeout, TimeSpan wait) =>
            RunRequest(new UpdateRequest(wordType, fullPath, timeout, wait));

        private static bool RunRequest(UpdateRequest request)
        {
            using (request) { return request.Run(); }
        }

        private sealed class UpdateRequest(Type wordType, string source, CancellationTokenSource timeout, TimeSpan wait) : IDisposable
        {
            private readonly CancellationToken token = timeout.Token;
            private readonly byte[] version = WordFieldFile.Fingerprint(source);
            private readonly string output = WordFieldFile.ReserveOutput(source);
            private Task<bool> worker = Task.FromResult(false);

            public bool Run()
            {
                worker = Launch();
                return WaitAndCommit();
            }

            private Task<bool> Launch() => StartUpdateTask(new Task<bool>(Refresh));

            private bool WaitAndCommit() => WaitForUpdate(worker, timeout, wait) && WordFieldFile.Commit(source, output, version);
            private bool Refresh() => TryOperation(StartWord);

            private bool StartWord()
            {
                token.ThrowIfCancellationRequested();
                return UpdateApplication(Activator.CreateInstance(wordType));
            }

            private bool UpdateApplication(object? app) => app != null && WithCleanup(app, ConfigureAndUpdate, CloseApplication);

            private bool ConfigureAndUpdate(dynamic app)
            {
                ConfigureApplication(app);
                return UpdateDocuments((object)app.Documents);
            }

            private bool UpdateDocuments(object documents)
            {
                UseComObject(documents, OpenAndUpdate);
                return true;
            }

            private void OpenAndUpdate(dynamic documents) => WithCleanup((object)OpenSource(documents), RefreshAndSave, CloseDocument);

            // 同一只读锁覆盖取指纹与 Word.Open，确保版本对应 Word 实际读入的内容。
            private object OpenSource(dynamic documents)
            {
                using var input = OpenInput();
                return OpenVersion(documents, input);
            }

            private FileStream OpenInput() => File.OpenRead(source);

            private object OpenVersion(dynamic documents, FileStream input) =>
                IsExpectedVersion(input) ? OpenReadOnly(documents) : throw new IOException("Word 更新前源文件已变更。");

            private bool IsExpectedVersion(FileStream input) => version.AsSpan().SequenceEqual(SHA256.HashData(input));

            private object OpenReadOnly(dynamic documents)
            {
                token.ThrowIfCancellationRequested();
                return OpenWordDocument(documents);
            }

            private object OpenWordDocument(dynamic documents) =>
                documents.Open(FileName: source, ReadOnly: true, AddToRecentFiles: false, Visible: false);

            private void RefreshAndSave(dynamic doc)
            {
                UpdateDocument(doc, token);
                SaveResult(doc);
            }

            // 域按原文件名和目录计算；SaveAs2 只写同目录的结果，不保存原文档。
            private void SaveResult(dynamic doc)
            {
                token.ThrowIfCancellationRequested();
                SaveAs(doc);
            }

            private void SaveAs(dynamic doc) => doc.SaveAs2(FileName: output, FileFormat: doc.SaveFormat, AddToRecentFiles: false);

            // 超时的 SaveAs2 可能仍在使用输出路径；在工作线程结束后才可释放该路径。
            public void Dispose()
            {
                if (worker.IsCompleted) DeleteOutput();
                else DeleteAfterWorker();
            }

            private void DeleteAfterWorker() => _ = worker.ContinueWith(_ => DeleteOutput(), TaskScheduler.Default);

            private void DeleteOutput() => TryCleanup(() => File.Delete(output));
        }

        private static Task<bool> StartUpdateTask(Task<bool> task)
        {
            StartWorker(task.RunSynchronously);
            return task;
        }

        private static void StartWorker(ThreadStart action) => CreateWorker(action).Start();

        private static Thread CreateWorker(ThreadStart action) => InitializeApartment(CreateBackgroundWorker(action));

        private static Thread CreateBackgroundWorker(ThreadStart action) =>
            new(action) { IsBackground = true, Name = "WordFieldUpdater" };

        private static Thread InitializeApartment(Thread worker)
        {
            worker.SetApartmentState(ApartmentState.STA);
            return worker;
        }

        private static bool WaitForUpdate(Task<bool> task, CancellationTokenSource timeout, TimeSpan wait) =>
            (task.Wait(wait) || ReportTimeout(timeout)) && task.GetAwaiter().GetResult();

        private static bool ReportTimeout(CancellationTokenSource timeout)
        {
            timeout.Cancel();
            return LogTimeout();
        }

        private static bool LogTimeout()
        {
            AppLogger.Log("Word 域更新超时；原文件未替换。后台 COM 调用无法强制取消，可能残留隐藏的 WINWORD.EXE 和工作副本，请确认后手动清理。");
            return false;
        }

        private static void ConfigureApplication(dynamic app)
        {
            ConfigureApplicationUi(app);
            app.AutomationSecurity = MsoAutomationSecurityForceDisable;
        }

        private static void ConfigureApplicationUi(dynamic app)
        {
            app.Visible = false;
            app.DisplayAlerts = WdAlertsNone;
        }

        private static void UpdateDocument(dynamic doc, CancellationToken token)
        {
            UpdateAllStories(doc, token);
            UpdateContentsAndPagination(doc, token);
        }

        private static void UpdateContentsAndPagination(dynamic doc, CancellationToken token)
        {
            UpdateTablesOfContents(doc, false, token);
            RefreshPagination(doc, token);
        }

        // 目录生成后页数可能改变：再次更新域、重新分页，最后仅更新目录页码。
        private static void RefreshPagination(dynamic doc, CancellationToken token)
        {
            RepaginateAndRefreshFields(doc, token);
            RepaginateAndRefreshContents(doc, token);
        }

        private static void RepaginateAndRefreshFields(dynamic doc, CancellationToken token)
        {
            doc.Repaginate();
            UpdateAllStories(doc, token);
        }

        private static void RepaginateAndRefreshContents(dynamic doc, CancellationToken token)
        {
            doc.Repaginate();
            UpdateTablesOfContents(doc, true, token);
        }

        private static void UpdateAllStories(dynamic doc, CancellationToken token) =>
            UseComObject((object)doc.StoryRanges, stories => EnumerateStories((IEnumerable)stories, token));

        private static void EnumerateStories(IEnumerable stories, CancellationToken token) =>
            UseComObject(stories.GetEnumerator(), enumerator => UpdateStories((IEnumerator)enumerator, token));

        private static void UpdateStories(IEnumerator enumerator, CancellationToken token)
        {
            while (enumerator.MoveNext())
            {
                UpdateStoryChain(enumerator.Current, token);
            }
        }

        // NextStoryRange 覆盖后续分节的页眉页脚及链接文本框；当前范围在读取 next 后释放。
        private static void UpdateStoryChain(object? range, CancellationToken token)
        {
            for (; range != null; range = UpdateStoryRange(range, token))
            {
                // 每一步由 UpdateStoryRange 更新并释放当前范围。
            }
        }

        private static object? UpdateStoryRange(object range, CancellationToken token) =>
            UseComResult(range, story => UpdateAndGetNextStory(story, token));

        private static object? UpdateAndGetNextStory(dynamic range, CancellationToken token)
        {
            UpdateRangeFields(range, token);
            return range.NextStoryRange;
        }

        private static void UpdateRangeFields(dynamic range, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            UpdateFieldsCollection(range);
        }

        private static void UpdateFieldsCollection(dynamic range) => UseComObject((object)range.Fields, UpdateFields);

        private static void UpdateFields(dynamic fields) => CheckFieldUpdate((int)fields.Update());

        private static void CheckFieldUpdate(int firstError)
        {
            if (firstError != 0)
            {
                throw CreateFieldUpdateException(firstError);
            }
        }

        private static InvalidOperationException CreateFieldUpdateException(int firstError) =>
            new(string.Create(CultureInfo.InvariantCulture, $"Word 域更新失败，首个错误域的位置为 {firstError}。"));

        private static void UpdateTablesOfContents(dynamic doc, bool pagesOnly, CancellationToken token) =>
            UseComObject((object)doc.TablesOfContents, tables => UpdateContentsMode(tables, pagesOnly, token));

        private static void UpdateContentsMode(dynamic tables, bool pagesOnly, CancellationToken token) =>
            UpdateContentsCollection(tables, GetContentsUpdate(pagesOnly), token);

        private static Action<dynamic> GetContentsUpdate(bool pagesOnly) =>
            pagesOnly ? UpdateContentsPageNumbers : UpdateContents;

        private static void UpdateContentsPageNumbers(dynamic toc) => toc.UpdatePageNumbers();

        private static void UpdateContents(dynamic toc) => toc.Update();

        private static void UpdateContentsCollection(dynamic tables, Action<dynamic> update, CancellationToken token) =>
            UpdateContentsItems(tables, (int)tables.Count, update, token);

        private sealed record ContentsRequest(object Tables, Action<dynamic> Update, CancellationToken Token)
        {
            public void Run(int count)
            {
                foreach (int index in Enumerable.Range(1, count))
                {
                    UpdateIndex(index);
                }
            }

            private void UpdateIndex(int index) => UpdateContentsItem(Tables, index, Update, Token);
        }

        private static void UpdateContentsItems(object tables, int count, Action<dynamic> update, CancellationToken token) =>
            new ContentsRequest(tables, update, token).Run(count);

        private static void UpdateContentsItem(dynamic tables, int index, Action<dynamic> update, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            UseComObject((object)tables.Item(index), update);
        }

        private static void UseComObject(object resource, Action<dynamic> action)
        {
            try
            {
                action(resource);
            }
            finally
            {
                ReleaseComObject(resource);
            }
        }

        private sealed class ResourceCleanup(object state, Action<dynamic> cleanup) : IDisposable
        {
            public void Dispose() => cleanup(state);

            public bool Run(Func<dynamic, bool> operation)
            {
                using (this) { return operation(state); }
            }

            public object? Run(Func<dynamic, object?> operation)
            {
                using (this) { return operation(state); }
            }

            public void Run(Action<dynamic> operation)
            {
                using (this) { operation(state); }
            }
        }

        private static void WithCleanup(object state, Action<dynamic> operation, Action<dynamic> cleanup) =>
            new ResourceCleanup(state, cleanup).Run(operation);

        private static bool WithCleanup(object state, Func<dynamic, bool> operation, Action<dynamic> cleanup) =>
            new ResourceCleanup(state, cleanup).Run(operation);

        private static object? WithCleanup(object state, Func<dynamic, object?> operation, Action<dynamic> cleanup) =>
            new ResourceCleanup(state, cleanup).Run(operation);

        private static object? UseComResult(object resource, Func<dynamic, object?> operation) =>
            WithCleanup(resource, operation, ReleaseComObject);

        private static void CloseDocument(dynamic doc) => UseComObject((object)doc, CloseDocumentWithoutRelease);

        private static void CloseDocumentWithoutRelease(dynamic doc) => TryCleanup(() => doc.Close(WdDoNotSaveChanges));

        private static void CloseApplication(dynamic app) => UseComObject((object)app, CloseApplicationWithoutRelease);

        private static void CloseApplicationWithoutRelease(dynamic app) => TryCleanup(() => app.Quit(WdDoNotSaveChanges));

        // 每个显式获取的 COM 引用只 Release 一次，避免 FinalRelease 使共享 RCW 别名失效。
        private static void ReleaseComObject(object resource) => TryCleanup(() => ReleaseResource(resource));

        private static void ReleaseResource(object resource) => GetResourceRelease(resource)(resource);

        private static Action<object> GetResourceRelease(object resource) =>
            Marshal.IsComObject(resource) ? ReleaseComReference : DisposeManagedResource;

        private static void DisposeManagedResource(object resource) => (resource as IDisposable)?.Dispose();

        private static void ReleaseComReference(object resource)
        {
            if (Marshal.IsComObject(resource))
            {
                Marshal.ReleaseComObject(resource);
            }
        }

        private static void TryCleanup(Action action) => TryOperation(() => CompleteCleanup(action));

        private static bool CompleteCleanup(Action action)
        {
            action();
            return true;
        }

        // 同步执行并捕获异常，保留原始错误用于日志；不会在线程池上异步执行 COM。
        // 与 STA 工作线程共用 Task 的异常传递机制，避免线程异常逃逸和重复 catch 边界。
        private static bool TryOperation(Func<bool> operation) => GetOperationResult(RunOperation(operation));

        private static Task<bool> RunOperation(Func<bool> operation) => RunSynchronousTask(new Task<bool>(operation));

        private static Task<bool> RunSynchronousTask(Task<bool> task)
        {
            task.RunSynchronously(TaskScheduler.Default);
            return task;
        }

        private static bool GetOperationResult(Task<bool> task) =>
            task.IsCompletedSuccessfully ? task.Result : ReportTaskFailure(task);

        private static bool ReportTaskFailure(Task<bool> task) => ReportFailure(task.Exception!.GetBaseException());

        private static bool ReportFailure(Exception ex)
        {
            LogFailure(ex);
            return false;
        }

        private static void LogFailure(Exception ex) =>
            AppLogger.Log(string.Create(CultureInfo.InvariantCulture, $"调用 Word 更新域或清理失败: {ex.Message}"));
    }
}
