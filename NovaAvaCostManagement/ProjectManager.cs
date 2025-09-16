using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Enhanced project manager with better memory management and async operations
    /// </summary>
    public class ProjectManager : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<CostElement> _elements;
        private string _projectFilePath;
        private readonly CircularBuffer<string> _logMessages;
        private bool _isDirty;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        // Constants
        private const int MAX_LOG_MESSAGES = 1000;
        private const int MAX_UNDO_LEVELS = 50;

        // Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ProjectEventArgs> ProjectChanged;
        public event EventHandler<ValidationEventArgs> ValidationCompleted;

        /// <summary>
        /// Observable collection of cost elements
        /// </summary>
        public ObservableCollection<CostElement> Elements
        {
            get => _elements;
            set
            {
                if (_elements != value)
                {
                    _elements = value;
                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Current project file path
        /// </summary>
        public string ProjectFilePath
        {
            get => _projectFilePath;
            set
            {
                if (_projectFilePath != value)
                {
                    _projectFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Project has unsaved changes
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Read-only view of log messages
        /// </summary>
        public IReadOnlyList<string> LogMessages => _logMessages.ToList();

        /// <summary>
        /// Undo/Redo manager
        /// </summary>
        public UndoRedoManager UndoManager { get; private set; }

        /// <summary>
        /// Project metadata
        /// </summary>
        public ProjectMetadata Metadata { get; set; }

        public ProjectManager()
        {
            _elements = new ObservableCollection<CostElement>();
            _logMessages = new CircularBuffer<string>(MAX_LOG_MESSAGES);
            UndoManager = new UndoRedoManager(MAX_UNDO_LEVELS);
            Metadata = new ProjectMetadata();

            // Subscribe to element collection changes
            _elements.CollectionChanged += (s, e) => IsDirty = true;
        }

        /// <summary>
        /// Create new project
        /// </summary>
        public void CreateNewProject()
        {
            lock (_lockObject)
            {
                // Clear existing data
                Elements.Clear();
                _logMessages.Clear();
                UndoManager.Clear();

                ProjectFilePath = null;
                IsDirty = false;

                Metadata = new ProjectMetadata
                {
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Version = "2.0",
                    Author = Environment.UserName
                };

                AddLogMessage("New project created");
                OnProjectChanged(ProjectEventType.Created);
            }
        }

        /// <summary>
        /// Save project asynchronously
        /// </summary>
        public async Task SaveProjectAsync(string filePath)
        {
            await Task.Run(() => SaveProject(filePath));
        }

        /// <summary>
        /// Save project to XML file
        /// </summary>
        public void SaveProject(string filePath)
        {
            try
            {
                lock (_lockObject)
                {
                    // Create backup of existing file
                    if (File.Exists(filePath))
                    {
                        var backupPath = $"{filePath}.backup";
                        File.Copy(filePath, backupPath, true);
                    }

                    var doc = new XDocument(
                        new XElement("NovaAvaProject",
                            new XAttribute("version", Metadata.Version),
                            new XAttribute("created", Metadata.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss")),
                            new XAttribute("modified", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                            new XAttribute("author", Metadata.Author),

                            // Metadata section
                            new XElement("Metadata",
                                new XElement("ProjectName", Metadata.ProjectName),
                                new XElement("Description", Metadata.Description),
                                new XElement("Client", Metadata.Client),
                                new XElement("ProjectNumber", Metadata.ProjectNumber),
                                new XElement("Currency", Metadata.Currency),
                                new XElement("Tags", string.Join(",", Metadata.Tags))
                            ),

                            // Elements section
                            new XElement("Elements",
                                new XAttribute("count", Elements.Count),
                                Elements.Select(e => SerializeElementToXml(e))
                            ),

                            // Statistics section
                            new XElement("Statistics",
                                new XElement("TotalElements", Elements.Count),
                                new XElement("TotalValue", Elements.Sum(e => e.Sum)),
                                new XElement("AveragePrice", Elements.Any() ? Elements.Average(e => e.Up) : 0),
                                new XElement("UniqueTypes", Elements.Select(e => e.Type).Distinct().Count())
                            )
                        )
                    );

                    doc.Save(filePath);

                    ProjectFilePath = filePath;
                    IsDirty = false;
                    Metadata.ModifiedDate = DateTime.Now;

                    AddLogMessage($"Project saved to {filePath}");
                    OnProjectChanged(ProjectEventType.Saved);
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error saving project: {ex.Message}");
                throw new ProjectException($"Failed to save project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load project asynchronously
        /// </summary>
        public async Task LoadProjectAsync(string filePath)
        {
            await Task.Run(() => LoadProject(filePath));
        }

        /// <summary>
        /// Load project from XML file
        /// </summary>
        public void LoadProject(string filePath)
        {
            try
            {
                lock (_lockObject)
                {
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"Project file not found: {filePath}");
                    }

                    var doc = XDocument.Load(filePath);
                    var root = doc.Root;

                    if (root == null)
                    {
                        throw new ProjectException("Invalid project file: No root element");
                    }

                    // Load metadata
                    LoadMetadata(root);

                    // Load elements
                    Elements.Clear();
                    var elementsNode = root.Element("Elements");
                    if (elementsNode != null)
                    {
                        foreach (var elementNode in elementsNode.Elements("Element"))
                        {
                            var element = DeserializeElementFromXml(elementNode);
                            if (element != null)
                            {
                                Elements.Add(element);
                            }
                        }
                    }

                    ProjectFilePath = filePath;
                    IsDirty = false;

                    AddLogMessage($"Project loaded from {filePath}");
                    OnProjectChanged(ProjectEventType.Loaded);
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error loading project: {ex.Message}");
                throw new ProjectException($"Failed to load project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Import from AVA XML with enhanced error handling
        /// </summary>
        public async Task<XmlImporter.ImportResult> ImportAvaXmlAsync(string filePath)
        {
            return await Task.Run(() => ImportAvaXml(filePath));
        }

        /// <summary>
        /// Import from AVA XML
        /// </summary>
        public XmlImporter.ImportResult ImportAvaXml(string filePath)
        {
            try
            {
                var result = XmlImporter.ImportFromXmlWithValidation(filePath);

                if (result.Success)
                {
                    lock (_lockObject)
                    {
                        // Store current state for undo
                        var previousElements = Elements.ToList();
                        UndoManager.Execute(new UndoableAction(
                            () => { Elements.Clear(); foreach (var e in result.Elements) Elements.Add(e); },
                            () => { Elements.Clear(); foreach (var e in previousElements) Elements.Add(e); },
                            "Import XML"
                        ));

                        foreach (var element in result.Elements)
                        {
                            Elements.Add(element);
                        }

                        IsDirty = true;
                        AddLogMessage($"Imported {result.Elements.Count} elements from {Path.GetFileName(filePath)}");

                        foreach (var warning in result.Warnings)
                        {
                            AddLogMessage($"Import warning: {warning}");
                        }

                        OnProjectChanged(ProjectEventType.Modified);
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        AddLogMessage($"Import error: {error}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = new XmlImporter.ImportResult();
                result.Errors.Add($"Import failed: {ex.Message}");
                AddLogMessage($"Error importing AVA XML: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Export to AVA XML asynchronously
        /// </summary>
        public async Task ExportAvaXmlAsync(string filePath, bool useGaebFormat = false)
        {
            await Task.Run(() => ExportAvaXml(filePath, useGaebFormat));
        }

        /// <summary>
        /// Export to AVA XML
        /// </summary>
        public void ExportAvaXml(string filePath, bool useGaebFormat = false)
        {
            try
            {
                lock (_lockObject)
                {
                    XmlExporter.ExportToXml(Elements.ToList(), filePath, useGaebFormat);
                    AddLogMessage($"Exported {Elements.Count} elements to {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error exporting AVA XML: {ex.Message}");
                throw new ProjectException($"Failed to export XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate all elements for export with detailed results
        /// </summary>
        public async Task<ValidationResult> ValidateForExportAsync()
        {
            return await Task.Run(() => ValidateForExport());
        }

        /// <summary>
        /// Validate all elements for export
        /// </summary>
        public ValidationResult ValidateForExport()
        {
            var result = new ValidationResult();

            lock (_lockObject)
            {
                var elementIndex = 0;
                foreach (var element in Elements)
                {
                    elementIndex++;
                    var errors = element.Validate();

                    foreach (var error in errors)
                    {
                        var message = $"Element {elementIndex} ({element.Name ?? element.Id}): {error}";

                        switch (error.Severity)
                        {
                            case ValidationSeverity.Critical:
                            case ValidationSeverity.Error:
                                result.Errors.Add(message);
                                break;
                            case ValidationSeverity.Warning:
                                result.Warnings.Add(message);
                                break;
                            case ValidationSeverity.Info:
                                result.Information.Add(message);
                                break;
                        }
                    }
                }

                // Check for duplicate IDs
                var duplicateIds = Elements
                    .GroupBy(e => e.Id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var id in duplicateIds)
                {
                    result.Warnings.Add($"Duplicate ID found: {id}");
                }

                // Check for missing required fields in batch
                if (Elements.Any(e => string.IsNullOrWhiteSpace(e.Name)))
                {
                    result.Errors.Add($"{Elements.Count(e => string.IsNullOrWhiteSpace(e.Name))} elements have no name");
                }

                AddLogMessage($"Validation completed: {result.Errors.Count} errors, {result.Warnings.Count} warnings");
            }

            OnValidationCompleted(result);
            return result;
        }

        /// <summary>
        /// Generate comprehensive diagnostics
        /// </summary>
        public string GenerateQuickDiagnostics()
        {
            lock (_lockObject)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== NOVA AVA Project Diagnostics ===");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Project info
                sb.AppendLine("PROJECT INFORMATION:");
                sb.AppendLine($"  File: {ProjectFilePath ?? "Not saved"}");
                sb.AppendLine($"  Name: {Metadata.ProjectName}");
                sb.AppendLine($"  Version: {Metadata.Version}");
                sb.AppendLine($"  Created: {Metadata.CreatedDate:yyyy-MM-dd}");
                sb.AppendLine($"  Modified: {Metadata.ModifiedDate:yyyy-MM-dd}");
                sb.AppendLine($"  Unsaved Changes: {IsDirty}");
                sb.AppendLine();

                // Statistics
                sb.AppendLine("STATISTICS:");
                sb.AppendLine($"  Total Elements: {Elements.Count}");
                sb.AppendLine($"  Valid Elements: {Elements.Count(e => e.IsValid)}");
                sb.AppendLine($"  Elements with Properties: {Elements.Count(e => !string.IsNullOrEmpty(e.Properties))}");
                sb.AppendLine($"  Elements with IFC Type: {Elements.Count(e => !string.IsNullOrEmpty(e.IfcType))}");
                sb.AppendLine();

                // Financial summary
                sb.AppendLine("FINANCIAL SUMMARY:");
                sb.AppendLine($"  Total Quantity: {Elements.Sum(e => e.Qty):N2}");
                sb.AppendLine($"  Total Value: {Elements.Sum(e => e.Sum):C2}");
                sb.AppendLine($"  Average Unit Price: {(Elements.Any() ? Elements.Average(e => e.Up) : 0):C2}");
                sb.AppendLine($"  Min Unit Price: {(Elements.Any() ? Elements.Min(e => e.Up) : 0):C2}");
                sb.AppendLine($"  Max Unit Price: {(Elements.Any() ? Elements.Max(e => e.Up) : 0):C2}");
                sb.AppendLine();

                // Type distribution
                sb.AppendLine("TYPE DISTRIBUTION:");
                var typeGroups = Elements.GroupBy(e => e.Type ?? "Unspecified")
                    .OrderByDescending(g => g.Count());
                foreach (var group in typeGroups.Take(10))
                {
                    sb.AppendLine($"  {group.Key}: {group.Count()} elements ({group.Sum(e => e.Sum):C2})");
                }
                if (typeGroups.Count() > 10)
                {
                    sb.AppendLine($"  ... and {typeGroups.Count() - 10} more types");
                }
                sb.AppendLine();

                // IFC type distribution
                sb.AppendLine("IFC TYPE DISTRIBUTION:");
                var ifcGroups = Elements
                    .Where(e => !string.IsNullOrEmpty(e.IfcType))
                    .GroupBy(e => e.IfcType)
                    .OrderByDescending(g => g.Count());
                foreach (var group in ifcGroups)
                {
                    sb.AppendLine($"  {group.Key}: {group.Count()} elements");
                }
                sb.AppendLine();

                // Memory usage
                sb.AppendLine("MEMORY USAGE:");
                var process = System.Diagnostics.Process.GetCurrentProcess();
                sb.AppendLine($"  Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
                sb.AppendLine($"  Private Memory: {process.PrivateMemorySize64 / (1024 * 1024)} MB");
                sb.AppendLine($"  GC Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
                sb.AppendLine();

                // Recent log messages
                sb.AppendLine("RECENT LOG MESSAGES:");
                var recentMessages = LogMessages.Skip(Math.Max(0, LogMessages.Count - 10));
                foreach (var message in recentMessages)
                {
                    sb.AppendLine($"  {message}");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Add log message with automatic cleanup
        /// </summary>
        public void AddLogMessage(string message)
        {
            lock (_lockObject)
            {
                _logMessages.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            }
        }

        /// <summary>
        /// Clear log messages
        /// </summary>
        public void ClearLogs()
        {
            lock (_lockObject)
            {
                _logMessages.Clear();
            }
        }

        /// <summary>
        /// Batch update elements
        /// </summary>
        public void BatchUpdate(Action<CostElement> updateAction, Func<CostElement, bool> predicate = null)
        {
            lock (_lockObject)
            {
                var elementsToUpdate = predicate != null
                    ? Elements.Where(predicate)
                    : Elements;

                foreach (var element in elementsToUpdate)
                {
                    updateAction(element);
                    element.CalculateFields();
                }

                IsDirty = true;
                OnProjectChanged(ProjectEventType.Modified);
            }
        }

        /// <summary>
        /// Find elements matching criteria
        /// </summary>
        public List<CostElement> FindElements(Func<CostElement, bool> predicate)
        {
            lock (_lockObject)
            {
                return Elements.Where(predicate).ToList();
            }
        }

        // Private methods
        private void LoadMetadata(XElement root)
        {
            Metadata = new ProjectMetadata
            {
                Version = root.Attribute("version")?.Value ?? "2.0",
                CreatedDate = DateTime.TryParse(root.Attribute("created")?.Value, out var created) ? created : DateTime.Now,
                ModifiedDate = DateTime.TryParse(root.Attribute("modified")?.Value, out var modified) ? modified : DateTime.Now,
                Author = root.Attribute("author")?.Value ?? Environment.UserName
            };

            var metadataNode = root.Element("Metadata");
            if (metadataNode != null)
            {
                Metadata.ProjectName = metadataNode.Element("ProjectName")?.Value ?? "";
                Metadata.Description = metadataNode.Element("Description")?.Value ?? "";
                Metadata.Client = metadataNode.Element("Client")?.Value ?? "";
                Metadata.ProjectNumber = metadataNode.Element("ProjectNumber")?.Value ?? "";
                Metadata.Currency = metadataNode.Element("Currency")?.Value ?? "EUR";

                var tags = metadataNode.Element("Tags")?.Value;
                if (!string.IsNullOrEmpty(tags))
                {
                    Metadata.Tags = tags.Split(',').Select(t => t.Trim()).ToList();
                }
            }
        }

        private XElement SerializeElementToXml(CostElement element)
        {
            return new XElement("Element",
                new XElement("Id", element.Id),
                new XElement("Id2", element.Id2),
                new XElement("Name", element.Name),
                new XElement("Type", element.Type),
                new XElement("Text", element.Text),
                new XElement("LongText", element.LongText),
                new XElement("Qty", element.Qty),
                new XElement("Qu", element.Qu),
                new XElement("Up", element.Up),
                new XElement("Sum", element.Sum),
                new XElement("Properties", element.Properties),
                new XElement("BimKey", element.BimKey),
                new XElement("Note", element.Note),
                new XElement("Color", element.Color),
                new XElement("IfcType", element.IfcType),
                new XElement("Material", element.Material),
                new XElement("Dimension", element.Dimension),
                new XElement("SegmentType", element.SegmentType),
                new XElement("Created", element.Created.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("AdditionalData",
                    element.AdditionalData.Select(kvp => new XElement(kvp.Key, kvp.Value))
                )
            );
        }

        private CostElement DeserializeElementFromXml(XElement node)
        {
            var element = new CostElement();

            element.Id = node.Element("Id")?.Value ?? element.Id;
            element.Id2 = node.Element("Id2")?.Value ?? element.Id2;
            element.Name = node.Element("Name")?.Value ?? "";
            element.Type = node.Element("Type")?.Value ?? "";
            element.Text = node.Element("Text")?.Value ?? "";
            element.LongText = node.Element("LongText")?.Value ?? "";
            element.Qu = node.Element("Qu")?.Value ?? "";
            element.Properties = node.Element("Properties")?.Value ?? "";
            element.BimKey = node.Element("BimKey")?.Value ?? "";
            element.Note = node.Element("Note")?.Value ?? "";
            element.Color = node.Element("Color")?.Value ?? "";
            element.IfcType = node.Element("IfcType")?.Value ?? "";
            element.Material = node.Element("Material")?.Value ?? "";
            element.Dimension = node.Element("Dimension")?.Value ?? "";
            element.SegmentType = node.Element("SegmentType")?.Value ?? "";

            if (decimal.TryParse(node.Element("Qty")?.Value, out decimal qty))
                element.Qty = qty;
            if (decimal.TryParse(node.Element("Up")?.Value, out decimal up))
                element.Up = up;
            if (decimal.TryParse(node.Element("Sum")?.Value, out decimal sum))
                element.Sum = sum;
            if (DateTime.TryParse(node.Element("Created")?.Value, out DateTime created))
                element.Created = created;

            var additionalDataNode = node.Element("AdditionalData");
            if (additionalDataNode != null)
            {
                foreach (var child in additionalDataNode.Elements())
                {
                    element.AdditionalData[child.Name.LocalName] = child.Value;
                }
            }

            return element;
        }

        // Event handlers
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnProjectChanged(ProjectEventType eventType)
        {
            ProjectChanged?.Invoke(this, new ProjectEventArgs(eventType));
        }

        protected virtual void OnValidationCompleted(ValidationResult result)
        {
            ValidationCompleted?.Invoke(this, new ValidationEventArgs(result));
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _elements?.Clear();
                    _logMessages?.Clear();
                    UndoManager?.Clear();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Enhanced validation result with categorized messages
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Information { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;

        public int TotalIssues => Errors.Count + Warnings.Count;

        public string Summary => $"{Errors.Count} errors, {Warnings.Count} warnings, {Information.Count} info";
    }

    /// <summary>
    /// Project metadata
    /// </summary>
    public class ProjectMetadata
    {
        public string ProjectName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Client { get; set; } = "";
        public string ProjectNumber { get; set; } = "";
        public string Version { get; set; } = "2.0";
        public string Author { get; set; } = Environment.UserName;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Currency { get; set; } = "EUR";
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Event args for project events
    /// </summary>
    public class ProjectEventArgs : EventArgs
    {
        public ProjectEventType EventType { get; }
        public string Message { get; }

        public ProjectEventArgs(ProjectEventType eventType, string message = null)
        {
            EventType = eventType;
            Message = message;
        }
    }

    public enum ProjectEventType
    {
        Created,
        Loaded,
        Saved,
        Modified,
        Validated
    }

    /// <summary>
    /// Event args for validation events
    /// </summary>
    public class ValidationEventArgs : EventArgs
    {
        public ValidationResult Result { get; }

        public ValidationEventArgs(ValidationResult result)
        {
            Result = result;
        }
    }

    /// <summary>
    /// Custom exception for project operations
    /// </summary>
    public class ProjectException : Exception
    {
        public ProjectException(string message) : base(message) { }
        public ProjectException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Circular buffer for log messages
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;
        private readonly object _lock = new object();

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % _buffer.Length;

                if (_count < _buffer.Length)
                {
                    _count++;
                }
                else
                {
                    _head = (_head + 1) % _buffer.Length;
                }
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                var result = new List<T>(_count);
                var index = _head;
                for (int i = 0; i < _count; i++)
                {
                    result.Add(_buffer[index]);
                    index = (index + 1) % _buffer.Length;
                }
                return result;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _head = 0;
                _tail = 0;
                _count = 0;
                Array.Clear(_buffer, 0, _buffer.Length);
            }
        }
    }

    /// <summary>
    /// Simple undo/redo manager
    /// </summary>
    public class UndoRedoManager
    {
        private readonly Stack<UndoableAction> _undoStack;
        private readonly Stack<UndoableAction> _redoStack;
        private readonly int _maxLevels;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public UndoRedoManager(int maxLevels = 50)
        {
            _maxLevels = maxLevels;
            _undoStack = new Stack<UndoableAction>();
            _redoStack = new Stack<UndoableAction>();
        }

        public void Execute(UndoableAction action)
        {
            action.Execute();
            _undoStack.Push(action);
            _redoStack.Clear();

            // Limit stack size
            while (_undoStack.Count > _maxLevels)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < _maxLevels - 1; i++)
                {
                    _undoStack.Push(items[i]);
                }
            }
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var action = _undoStack.Pop();
                action.Undo();
                _redoStack.Push(action);
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                var action = _redoStack.Pop();
                action.Execute();
                _undoStack.Push(action);
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }

    /// <summary>
    /// Undoable action
    /// </summary>
    public class UndoableAction
    {
        private readonly Action _execute;
        private readonly Action _undo;
        public string Description { get; }

        public UndoableAction(Action execute, Action undo, string description = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
            Description = description;
        }

        public void Execute() => _execute();
        public void Undo() => _undo();
    }
}