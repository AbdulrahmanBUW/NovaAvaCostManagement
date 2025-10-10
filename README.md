# AVA XML Editor

A Windows Forms application for editing AVA NOVA XML cost management files

## Features

### Core Functionality
- **SPEC Parameter Management**: Dedicated fields for DX.SPEC parameters with automatic property generation
- **Multiple Catalog Support**: Handle up to 3 catalog assignments per element
- **WBS Hierarchy View**: Tree-structured view of cost elements with catalog grouping
- **Excel-like Editing**: Direct cell editing with copy/paste support
- **Undo/Redo**: Full undo/redo support for all operations
- **Auto-calculation**: Automatic total calculation (Quantity × Unit Price)

### XML Import/Export
- **Import**: Reads AVA NOVA XML format preserving all original data
- **Export**: Generates exact XML structure matching input format
- **Data Preservation**: Maintains all XML fields including read-only data
- **GAEB Format Support**: Alternative export format available

### User Interface
- **Dual View Modes**: Toggle between Flat List and WBS Hierarchy views
- **Context Menu**: Right-click context menu for common operations
- **Form-based Editing**: Detailed edit form for complex element editing
- **Real-time Validation**: Input validation with error highlighting
- **Status Tracking**: Comprehensive status and progress information

## Project Structure

### Main Components

#### Data Models
- **CostElement.cs**: Core data model storing all XML fields with SPEC parameter support
- **CatalogAssignment.cs**: Represents catalog assignments with name, number, and type
- **WbsNode.cs**: WBS hierarchy structure for tree view display

#### Forms
- **MainForm.cs**: Primary application window with grid and controls
- **ElementEditForm.cs**: Detailed element editing dialog

#### Services
- **XmlImporter.cs**: XML parsing and data import
- **XmlExporter.cs**: XML generation and export
- **PropertiesSerializer.cs**: PHP-style property serialization for SPEC parameters
- **UndoRedoManager.cs**: Change tracking and undo/redo functionality
- **WbsBuilder.cs**: WBS tree construction from flat element list

#### Utilities
- **ProjectManager.cs**: Project validation and management
- **Program.cs**: Application entry point

## Key Technical Features
### Undo/Redo System
- Tracks cell-level changes with proper type conversion
- Supports batch operations (copy/paste, clear cells)
- Maintains 50-level undo history

### WBS Hierarchy
- Automatic tree building from catalog assignments
- Multi-level indentation in grid display
- Group rows with visual differentiation

### Excel-like Operations
- **Cell Selection**: Click and drag to select multiple cells
- **Copy/Paste**: Ctrl+C/Ctrl+V for cell data
- **Fill Handle**: Drag to fill adjacent cells
- **Keyboard Navigation**: Arrow keys, Tab, Enter
- **Multi-cell Editing**: Edit multiple selected cells simultaneously

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open XML file |
| `Ctrl+S` | Save XML file |
| `Ctrl+N` | Add new element |
| `F2` | Edit selected element |
| `Delete` | Delete selected element(s) or clear cells |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+C` | Copy selected cells/elements |
| `Ctrl+V` | Paste to selected cells |
| `Ctrl+W` | Toggle WBS view |
| `Ctrl+L` | Switch to flat list view |
| `Ctrl+Double-Click` | Open element edit form |

## Installation & Building

### Requirements
- .NET Framework 4.8
- Visual Studio 2019 or later
- Windows 7 or newer

### Building from Source
1. Clone or download the project files
2. Open `NovaAvaCostManagement.sln` in Visual Studio
3. Restore NuGet packages if prompted
4. Build the solution (Ctrl+Shift+B)
5. Run the application (F5)

### File Structure
```
NovaAvaCostManagement/
├── MainForm.cs              # Main application window
├── ElementEditForm.cs       # Element editing dialog
├── CostElement.cs           # Data model
├── XmlImporter.cs           # XML import logic
├── XmlExporter.cs           # XML export logic
├── PropertiesSerializer.cs  # SPEC property handling
├── UndoRedoManager.cs       # Undo/redo functionality
├── WbsNode.cs               # WBS hierarchy classes
└── Properties/
    └── AssemblyInfo.cs      # Assembly metadata
```

## Usage Guide

### Basic Workflow
1. **Open XML File**: Use File → Open or Ctrl+O to load an AVA XML file
2. **View Data**: Switch between Flat List and WBS Hierarchy views
3. **Edit Elements**: 
   - Double-click cells for quick editing
   - Use F2 or Ctrl+Double-Click for detailed form editing
   - Use context menu for bulk operations
4. **Save Changes**: Use File → Save or Ctrl+S to export modified data

### SPEC Parameter Management
1. Open element edit form (F2)
2. Fill in SPEC fields (DX.SPEC_Name, DX.SPEC_Size, etc.)
3. Click "Generate Properties" to create PHP-serialized properties string
4. Properties are automatically parsed when loading XML files

### Catalog Assignments
- Each element supports up to 4 catalog assignments
- Catalog data is displayed in dedicated columns
- WBS view groups elements by catalog type and number

### Cell Operations
- **Select**: Click individual cells or drag to select range
- **Copy**: Ctrl+C copies selected cells to clipboard
- **Paste**: Ctrl+V pastes clipboard data starting from current cell
- **Clear**: Delete key clears selected cell values
- **Edit**: Double-click or F2 to edit cell content

## Data Model

The application preserves all original XML fields while providing enhanced editing for key fields:

### User-Editable Fields
- `Name`, `Description`, `Children`, `Properties`
- SPEC parameters: `SpecFilter`, `SpecName`, `SpecSize`, `SpecType`, `SpecManufacturer`, `SpecMaterial`
- Calculation fields: `Text`, `LongText`, `Qty`, `QtyResult`, `Up`, `Qu`
- Identification: `Ident` (GUID)

### Read-Only Fields (Preserved)
- System IDs, timestamps, calculated values
- VOB data, tax information, component pricing
- IFC parameters, material data, dimension info

## XML Format Support

### Input Format
```xml
<cefexport version="2">
  <costelements>
    <costelement id="1">
      <name>Element Name</name>
      <cecalculations>
        <cecalculation>
          <id>1</id>
          <text>Calculation Text</text>
          <!-- ... -->
        </cecalculation>
      </cecalculations>
    </costelement>
  </costelements>
</cefexport>
```

### SPEC Properties Format
```php
a:6:{
  s:12:"DX.SPEC_Name";s:18:"3-Piece Ball Valve";
  s:12:"DX.SPEC_Size";s:4:"1/4\"";
  s:12:"DX.SPEC_Type";s:5:"H6800";
  // ...
}
```

## Troubleshooting

### Common Issues

1. **XML Import Fails**
   - Verify XML file is valid AVA NOVA format
   - Check for proper encoding (UTF-8 recommended)
   - Ensure required elements are present

2. **Properties Not Generating**
   - Fill at least one SPEC field before generating
   - Check for special characters that might break serialization

3. **Undo/Redo Not Working**
   - Some programmatic changes are not tracked
   - Cell edits must complete (press Enter or move focus) to be recorded

4. **WBS View Empty**
   - Ensure elements have catalog assignments
   - Check that CatalogType and CatalogNumber fields are populated

### Performance Tips
- Use Flat List view for large datasets (>1000 elements)
- Avoid excessive undo levels for memory management
- Save frequently when working with large files

## Development

### Extending the Application

#### Adding New Fields
1. Add property to `CostElement` class
2. Update XML import/export in `XmlImporter`/`XmlExporter`
3. Add to grid columns in `MainForm.SetupColumns()`
4. Add to edit form in `ElementEditForm` if editable

#### Custom Export Formats
Implement new export logic in `XmlExporter` following existing patterns for GAEB or other formats.

#### Validation Rules
Add custom validation logic in `CostElement.Validate()` method.

## Support

For issues and feature requests, please refer to the application's About dialog or contact the development team.
