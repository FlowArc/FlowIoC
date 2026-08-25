# FlowIoC Code Generator System - User Manual

## Table of Contents
1. [Getting Started](#getting-started)
   - [Installation](#installation)
   - [Initial Setup](#initial-setup)
   - [Basic Concepts](#basic-concepts)
2. [Creating Modules](#creating-modules)
   - [Main Module Creation](#main-module-creation)
   - [Screen Module Creation](#screen-module-creation)
   - [Test Module Creation](#test-module-creation)
   - [Module Configuration Options](#module-configuration-options)
3. [Working with Module Components](#working-with-module-components)
   - [Creating Views and Mediators](#creating-views-and-mediators)
   - [Creating Models](#creating-models)
   - [Creating Commands](#creating-commands)
   - [Modifying Context Files](#modifying-context-files)
4. [Directory Structure Management](#directory-structure-management)
   - [Understanding the Module Structure](#understanding-the-module-structure)
   - [Required vs Optional Folders](#required-vs-optional-folders)
   - [Managing the Module Index](#managing-the-module-index)
5. [Namespace Management](#namespace-management)
   - [Namespace Conventions](#namespace-conventions)
   - [Assembly Definition Files](#assembly-definition-files)
   - [Cross-Module References](#cross-module-references)
6. [Advanced Features](#advanced-features)
   - [Screen Module Features](#screen-module-features)
   - [Signal System Integration](#signal-system-integration)
   - [Command Sequences](#command-sequences)
7. [Best Practices](#best-practices)
   - [Module Organization](#module-organization)
   - [Component Naming](#component-naming)
   - [Dependency Management](#dependency-management)
8. [Troubleshooting](#troubleshooting)
   - [Common Issues](#common-issues)
   - [Diagnostics](#diagnostics)
   - [Module Repair](#module-repair)
9. [Extending the System](#extending-the-system)
   - [Custom Templates](#custom-templates)
   - [Configuration Modification](#configuration-modification)
   - [New Component Types](#new-component-types)

## Getting Started

### Installation

The FlowIoC Code Generator system is included as part of the FlowIoC package:

1. Import the FlowIoC package into your Unity project
   - Via Package Manager: Add package from git URL `https://github.com/your-repository/FlowIoC.git`
   - Or import the FlowIoC unitypackage directly

2. After installation, verify that the following folder exists in your project:
   - `Packages/FlowIoC/Editor/CodeGenerator`

### Initial Setup

Before using the Code Generator, ensure your project is properly configured:

1. Create the base Modules folder (if it doesn't exist):
   - Right-click in the Project window > Create > Folder > Name it "Modules"
   - This will be the parent directory for all your generated modules

2. Configure the CodeGeneratorSettings (optional):
   - Navigate to Tools > FlowIoC > Module Configuration > Update Namespace Settings
   - This will create the default settings if they don't exist
   - You can customize these settings to match your project structure

### Basic Concepts

Before using the Code Generator, understand these key concepts:

- **Module**: A self-contained unit of functionality with its own directory structure, classes, and resources
- **Module Type**: The category of module (Main, Screen, or Test) that determines its structure and functionality
- **Component**: A code element within a module (View, Mediator, Model, Command, etc.)
- **Template**: A code template used to generate new components with standardized patterns

## Creating Modules

### Main Module Creation

To create a Main module (for game systems, services, etc.):

1. Open the Create Module window:
   - Navigate to Tools > FlowIoC > Create Module
   - Or use the keyboard shortcut (if configured)

2. Configure the module:
   - Enter the Module Name (e.g., "Gameplay", "Map", "Inventory")
   - Select "Main" as the Module Type
   - Enable "Create Root" and "Create Context" (recommended)
   - Select any optional folders you need (Editor, Resources, etc.)

3. Click "Create Module"

The system will generate:
- A complete directory structure in `Assets/Modules/YourModuleName/`
- Root and Context classes in the `Scripts/Runtime/RootsContexts/` folder
- Registration in the project's module index (`FlowIoCModuleIndex.asset`), which is how the tools recognise this folder as a module
- Assembly definition file (`Modules.YourModuleName.asmdef`)

### Screen Module Creation

To create a Screen module (for UI elements):

1. Open the Create Module window (Tools > FlowIoC > Create Module)

2. Configure the module:
   - Enter the Module Name (e.g., "GameBoard", "MainMenu")
   - Select "Screen" as the Module Type
   - Enable "Create Root", "Create Context", and "Create Scene" (recommended)
   - Configure screen actions (e.g., "OnBackButtonClicked", "OnSettingsClicked")
   - Configure screen settings (animations, tags, etc.)
   - Select any optional folders needed

3. Click "Create Module"

The system will generate:
- A complete directory structure in `Assets/Modules/YourNameScreen/`
- Screen-specific Root and Context classes
- Screen View and Mediator with the configured actions
- Screen configuration files

### Test Module Creation

To create a Test module:

1. Open the Create Module window (Tools > FlowIoC > Create Module)

2. Configure the module:
   - Enter the Module Name (e.g., "MapEditor", "GameBoardTest")
   - Select "Test" as the Module Type
   - Enable required options
   - Select any optional folders needed

3. Click "Create Module"

The system will generate:
- A complete directory structure in `Assets/Modules/YourNameTest/`
- Test-specific Root and Context classes with appropriate preprocessor directives
- An assembly definition file, and registration in the project's module index

### Module Configuration Options

When creating modules, you can configure various options:

- **Create Root**: Generates a Root class for the module (recommended)
- **Create Context**: Generates a Context class for the module (recommended)
- **Create Scene**: Creates a Unity scene file for the module
- **Optional Folders**: Select additional directories to include in the module:
  - Resources: For assets loaded at runtime
  - Editor: For editor-only scripts
  - Scenes: For additional scene files
  - Prefabs: For prefab assets
  - And others based on your configuration

## Working with Module Components

### Creating Views and Mediators

To create a View-Mediator pair within a module:

1. Open the Create View window:
   - Navigate to Tools > FlowIoC > Create View

2. Configure the View:
   - Select the target module from the dropdown
   - Enter the View name (e.g., "GameplayView", "InventoryView")
   - Enter actions that the View will dispatch (e.g., "OnButtonClick", "OnItemSelected")
   - Choose whether this is a Screen view (for UI) or a regular view
   - Select the context file where the mediation should be bound

3. Click "Create"

The system will generate:
- A View class with the specified actions
- A Mediator class with handlers for those actions
- Automatic binding in the selected context file

Example of a generated View:

```csharp
namespace Modules.Gameplay.ViewsMediators
{
    public class GameplayView : View
    {
        public Action OnStartButtonClick;
        public Action OnQuitButtonClick;
        
        // Other View code...
    }
}
```

Example of a generated Mediator:

```csharp
namespace Modules.Gameplay.ViewsMediators
{
    public class GameplayMediator : Mediator
    {
        [Inject] private GameplayView _view { get; set; }
        
        public override void OnRegister()
        {
            _view.OnStartButtonClick += OnOnStartButtonClick;
            _view.OnQuitButtonClick += OnOnQuitButtonClick;
        }
        
        public override void OnRemove()
        {
            _view.OnStartButtonClick -= OnOnStartButtonClick;
            _view.OnQuitButtonClick -= OnOnQuitButtonClick;
        }
        
        private void OnOnStartButtonClick()
        {
            // Your implementation here
        }
        
        private void OnOnQuitButtonClick()
        {
            // Your implementation here
        }
    }
}
```

### Creating Models

To create a Model and its interface:

1. Open the Create Model window:
   - Navigate to Tools > FlowIoC > Create Model

2. Configure the Model:
   - Select the target module from the dropdown
   - Enter the Model name (e.g., "GameplayModel", "PlayerModel")
   - Add injectables that the model will use (e.g., "IPlayerService", "IGameSettings")
   - Select whether to use dummy binding (for testing)
   - Select the context file where the model should be bound

3. Click "Create"

The system will generate:
- An interface (e.g., `IGameplayModel`) with property definitions
- A class implementation (e.g., `GameplayModel`) with injectable dependencies
- Automatic binding in the selected context file

Example of a generated Model:

```csharp
namespace Modules.Gameplay.Models
{
    public interface IGameplayModel
    {
        // Interface properties and methods
    }
    
    public class GameplayModel : IGameplayModel
    {
        [Inject] private IPlayerService _playerService { get; set; }
        [Inject] private IGameSettings _gameSettings { get; set; }
        
        // Implementation code...
    }
}
```

### Creating Commands

To create a Command:

1. Open the Create Command window:
   - Navigate to Tools > FlowIoC > Create Command

2. Configure the Command:
   - Select the target module from the dropdown
   - Enter the Command name (e.g., "StartGameCommand", "LoadLevelCommand")
   - Select the signal class that will trigger this command
   - Enter the signal name within that class
   - Add injectables that the command will use
   - Choose sequence execution if applicable
   - Select the context file where the command should be bound

3. Click "Create"

The system will generate:
- A Command class with the Execute method
- Injectable dependencies
- Automatic binding to the specified signal in the context

Example of a generated Command:

```csharp
namespace Modules.Gameplay.Commands
{
    public class StartGameCommand : Command
    {
        [Inject] private IGameplayModel _gameplayModel { get; set; }
        [Inject] private IPlayerService _playerService { get; set; }
        
        public override void Execute()
        {
            // Your implementation here
        }
    }
}
```

### Modifying Context Files

The Code Generator automatically updates context files when adding components:

1. **View-Mediator Binding**:
   ```csharp
   // Added to MediationBindings() method
   MediationBinder.Bind<GameplayView>().To<GameplayMediator>();
   ```

2. **Model Binding**:
   ```csharp
   // Added to InjectionBindings() method
   InjectionBinder.Bind<IGameplayModel, GameplayModel>();
   ```

3. **Command Binding**:
   ```csharp
   // Added to CommandBindings() method
   CommandBinder.Bind(_gameplaySignals.StartGame).To<StartGameCommand>();
   
   // Or for sequences
   CommandBinder.Bind(_gameplaySignals.StartGame)
       .To<PrepareGameCommand>()
       .To<StartGameCommand>()
       .InSequence();
   ```

4. **Signal Binding**:
   ```csharp
   // Added to SignalBindings() method
   _gameplaySignals = InjectionBinder.Bind<GameplaySignals>();
   ```

## Directory Structure Management

### Understanding the Module Structure

Each module follows a standardized directory structure:

```
ModuleName/
├── Modules.ModuleName.asmdef    # Assembly definition
├── Art/                         # Art assets
├── Prefabs/                     # Prefab assets
├── Resources/                   # Resources for runtime loading
├── Scenes/                      # Unity scenes
├── Scriptables/                 # ScriptableObject assets
└── Scripts/                     # Code files
    ├── Runtime/                 # Runtime code
    │   ├── Controllers/         # Controllers and handlers
    │   ├── Models/              # Data models
    │   ├── RootsContexts/       # Root and Context classes
    │   ├── Signals/             # Signal definitions
    │   └── ViewsMediators/      # Views and their mediators
    └── Editor/                  # Editor-only code
```

Type-specific folders:
- **Main Modules**: Standard structure as above
- **Screen Modules**: Additional screen-specific folders
- **Test Modules**: Test-specific folders with preprocessor directives

### Required vs Optional Folders

Each module type has certain required folders that are always created:

- **Main Module Required**: Scripts, RootsContexts, ViewsMediators, Models, Controllers, Signals
- **Screen Module Required**: All Main folders plus ScreenViews, ScreenConfigs
- **Test Module Required**: A subset focused on testing components

Optional folders can be selected during module creation:
- Resources
- Editor
- Scenes
- Prefabs
- Art
- Scriptables
- And others based on your configuration

### Managing the Module Index

Every module is recorded as an entry in one project asset,
`Assets/Plugins/FlowIoC/Editor/CodeGenerator/FlowIoCModuleIndex.asset`. An entry's name
and type are not stored opinions — they are read back off the folder tree each time the
index rebuilds, keyed on the module folder's own Unity GUID so a rename or a move in the
Project window does not desynchronise the tools from what is actually on disk.

The index is used by the system to:
- Identify the module type
- Determine namespace conventions
- Control visibility in tools and hierarchy views

Because it is a cache rather than something you maintain, there is nothing to
hand-edit. A stale or missing entry is fixed by rebuilding it:
- Tools > FlowIoC > Module Configuration > Detect & Fix Module Index
- Or simply reopen the project — the same rebuild runs automatically on load

## Namespace Management

### Namespace Conventions

The Code Generator enforces consistent namespace conventions:

```csharp
// Main module
namespace Modules.GameplayModule.ViewsMediators
namespace Modules.GameplayModule.Models
namespace Modules.GameplayModule.Controllers
namespace Modules.GameplayModule.Signals
namespace Modules.GameplayModule.RootsContexts

// Screen module
namespace Modules.GameBoard.Screen.ViewsMediators
namespace Modules.GameBoard.Screen.Models
// etc.

// Test module
namespace Modules.MapEditor.Test.ViewsMediators
namespace Modules.MapEditor.Test.Models
// etc.
```

These conventions ensure:
- Consistent organization across the project
- Clear module boundaries
- Proper assembly dependencies

### Assembly Definition Files

Each module has an assembly definition file (asmdef) that:
- Defines the compilation unit
- Specifies dependencies on other assemblies
- Controls platform targeting
- Ensures proper code isolation

Example for a Main module:
```json
{
    "name": "Modules.Gameplay",
    "rootNamespace": "",
    "references": [
        "FlowIoC",
        "Modules.Common"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": []
}
```

### Cross-Module References

To reference code between modules:

1. Add the target module's assembly definition as a reference in your module's asmdef file
2. Import the namespace in your code:
   ```csharp
   using Modules.Common.Services;
   using Modules.Gameplay.Models;
   ```

3. When using injectable dependencies across modules, use cross-context binding:
   ```csharp
   // In your module context
   InjectionBinderCrossContext.Bind<IMapSystem, MapSystem>();
   ```

## Advanced Features

### Screen Module Features

Screen modules include special features for UI management:

1. **Screen Actions**: Define interaction events (clicks, selections, etc.) that the screen will respond to
2. **Screen Configuration**: Configure screen behavior like:
   - Animation settings (open/close animations)
   - Default layer
   - Loading type (Addressable, Resource, DirectPrefab)
   - Screen tags

3. **Integration with ScreenService**: Generated screen views automatically work with the FlowIoC Screen System

Example screen integration in a Context:
```csharp
public override void Launch()
{
    _screenService.Open<GameBoardScreenView>()
                 .SetLayer(1)
                 .AddToHistory()
                 .Show();
}
```

### Signal System Integration

The Code Generator integrates with FlowIoC's signal system:

1. **Signal Generation**: Create signal classes with methods for specific events:
   ```csharp
   public class GameplaySignals : ISignalHolder
   {
       public Signal<int, string> StartGame = new();
       public Signal LoadLevel = new();
       public Signal<PlayerData> UpdatePlayer = new();
   }
   ```

2. **Signal Binding**: Automatically bind signals in the context:
   ```csharp
   public override void SignalBindings()
   {
       _gameplaySignals = InjectionBinder.Bind<GameplaySignals>();
       _gameplayConnectorSignals = InjectionBinderCrossContext.Bind<GameplayConnectorSignals>();
   }
   ```

3. **Command Triggering**: Trigger commands through signals:
   ```csharp
   // In your code
   _gameplaySignals.StartGame.Dispatch(levelId, difficultyMode);
   ```

### Command Sequences

Create sequential command execution chains:

1. **Define Sequence**: In the Command creation window, enable "Is Sequence"

2. **Update Existing Binding**: The generator will modify the binding to create a sequence:
   ```csharp
   CommandBinder.Bind(_gameplaySignals.StartGame)
       .To<PrepareGameCommand>()
       .To<LoadLevelCommand>()
       .To<StartGameplayCommand>()
       .InSequence();
   ```

3. **Add to Existing Sequence**: You can add new commands to existing sequences

## Best Practices

### Module Organization

For effective module organization:

1. **Group by Functionality**: Create modules around coherent features or systems
   - Example: `GameplayModule`, `InventoryModule`, `CombatModule`

2. **Common Module**: Create a `CommonModule` for shared utilities and services

3. **Hierarchy Levels**:
   - Main modules for major systems
   - Sub-modules for specialized components within a system
   - Screen modules for UI components
   - Test modules for testing isolated functionality

4. **Module Responsibilities**: Keep modules focused on specific responsibilities

### Component Naming

Follow consistent naming conventions:

1. **Views**: Suffix with "View" (e.g., `GameplayView`, `InventoryItemView`)

2. **Mediators**: Suffix with "Mediator" (e.g., `GameplayMediator`, `InventoryItemMediator`)

3. **Models**: Suffix with "Model" (e.g., `GameplayModel`, `PlayerModel`)
   - Interfaces: Prefix with "I" (e.g., `IGameplayModel`, `IPlayerModel`)

4. **Commands**: Suffix with "Command" and name by action (e.g., `StartGameCommand`, `LoadLevelCommand`)

5. **Signals**: Suffix with "Signals" and group by domain (e.g., `GameplaySignals`, `PlayerSignals`)

### Dependency Management

Manage dependencies effectively:

1. **Inject Interfaces**: Always inject interfaces rather than concrete implementations
   ```csharp
   [Inject] private IPlayerModel _playerModel { get; set; }
   ```

2. **Cross-Module Communication**: Use signals for cross-module communication

3. **Service Localization**: Keep service registration in appropriate modules

4. **Assembly References**: Only reference necessary assemblies in your asmdef file

## Troubleshooting

### Common Issues

1. **Namespace Errors**: If you see namespace errors:
   - Ensure the assembly reference is set in your asmdef file
   - Check the namespace follows the convention (`Modules.ModuleName.SubNamespace`)
   - Verify the module is registered correctly — run Detect & Fix Module Index to rebuild the index

2. **Missing Bindings**: If bindings are not being generated:
   - Check the context file structure follows the template pattern
   - Ensure the binding methods (SignalBindings, CommandBindings, etc.) exist
   - Try regenerating the binding using the tools

3. **File Generation Failures**:
   - Ensure you have write access to the project folders
   - Check for special characters in names
   - Verify Unity is not in play mode

### Diagnostics

To diagnose issues with the Code Generator:

1. **Verify Module Structure**:
   - Run Tools > FlowIoC > Module Configuration > Detect & Fix Module Index
   - This will identify and repair common structure issues

2. **Check the Module Index**:
   - Open `FlowIoCModuleIndex.asset` in the Inspector to see every module FlowIoC has found
   - A missing or wrong entry means the index is stale — rebuild it rather than editing it

3. **Assembly Definition Validation**:
   - Check that assembly definitions have the correct references
   - Verify the assembly name matches the module name

### Module Repair

If a module becomes corrupted or inconsistent:

1. **Rebuild the Module Index**:
   - Run Tools > FlowIoC > Module Configuration > Detect & Fix Module Index
   - A module that no longer exists on disk is simply absent from the rebuilt index —
     there is no separate cleanup step

2. **Update Namespace Settings**:
   - Run Tools > FlowIoC > Module Configuration > Update Namespace Settings
   - This synchronizes namespace configuration with the current module structure

3. **Manual Repair** (for severe issues):
   - Create a new module with the same name and type
   - Copy your custom code to the new module
   - Delete the corrupted module

## Extending the System

### Custom Templates

To create custom templates:

1. **Locate Template Files**:
   - Navigate to `Packages/FlowIoC/Editor/CodeGenerator/TempViews/`
   - Navigate to `Packages/FlowIoC/Editor/CodeGenerator/TempModels/`
   - Navigate to `Packages/FlowIoC/Editor/CodeGenerator/TempCommands/`
   - Navigate to `Packages/FlowIoC/Editor/CodeGenerator/TempRoots/`

2. **Create Backup**:
   - Copy existing templates before modification

3. **Modify Templates**:
   - Edit template files to match your project needs
   - Keep placeholder tags (e.g., `//@Actions`, `//@Register`) for dynamic content

### Configuration Modification

To customize the generator configuration:

1. **Edit CodeGeneratorSettings**:
   - Locate the settings asset at `Assets/Plugins/FlowIoC/Editor/CodeGenerator/CodeGeneratorSettings.asset`
   - Modify folder naming conventions
   - Update directory structure configurations

2. **Directory Structure Configs**:
   - Modify the required and optional folders for each module type
   - Customize folder names and descriptions

### New Component Types

To add support for new component types:

1. **Create Template Files**:
   - Create a template file in the appropriate template directory
   - Include necessary placeholder tags

2. **Extend CodeGeneratorUtils**:
   - Add methods for processing your new component type
   - Implement any special placeholder tag handling

3. **Create Menu Item**:
   - Add a menu item in CodeGeneratorTools.cs
   - Create an editor window for configuring your new component

4. **Add Binding Logic**:
   - Implement binding generation in CodeGeneratorUtils
   - Test the integration with existing modules 