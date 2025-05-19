# Godot Easy Inject

## Table of Contents

- [Introduction](#introduction)
- [Why Choose Godot Easy Inject?](#why-choose-godot-easy-inject)
- [Installation and Activation](#installation-and-activation)
- [Usage](#usage)
  - [CreateNode-Automatic Node Creation](#createnode--automatic-node-creation)
  - [GameObjectBean-Game Object Registration](#gameobjectbean--game-object-registration)
  - [Component-Regular Class Objects](#component--regular-class-objects)
  - [Dependency Injection](#dependency-injection)
  - [Bean Naming](#bean-naming)
  - [Cross-Scene Persistence](#cross-scene-persistence)
  - [Using the Container API](#using-the-container-api)
- [Inheritance and Interfaces Based on the Liskov Substitution Principle](#inheritance-and-interfaces-based-on-the-liskov-substitution-principle)
- [Contact Information](#contact-information)

## Introduction

Godot Easy Inject is a dependency injection plugin developed for the Godot game engine, helping developers better manage dependencies between game components, making code more modular, testable, and maintainable.

## Why Choose Godot Easy Inject?

In traditional Godot development, obtaining node references usually requires using ``GetNode<T>(path)`` or exporting variables and manually dragging them in the editor. For example:

    // Traditional way to get node references
    public class Player : Node3D
    {
        // Needs to be manually dragged in the editor or found using a path
        [Export]
        private InventorySystem inventory;
        
        private GameStateManager gameState;
        
        public override void _Ready()
        {
            // Hard-coded path to get the node
            gameState = GetNode<GameStateManager>("/root/GameStateManager");
        }
    }

This approach can lead to high code coupling, easy errors due to path changes, and difficulty in testing in large projects.
With Godot Easy Inject, you only need to add a few attribute markers to achieve automatic dependency injection:

    [GameObjectBean]
    public class Player : Node3D
    {
        [Autowired]
        private InventorySystem inventory;
        
        [Autowired]
        private GameStateManager gameState;
        
        public override void _Ready()
        {
            // Dependency injected, use directly
        }
    }

Can't wait to try it out? Let's get started now!

## Installation and Activation

### Install the Plugin

Download the plugin from GitHub

Click the green Code button on the GitHub repository interface and select Download ZIP to download the source code.

After extracting, copy the entire EasyInject folder to the addons directory of your Godot project. If there is no addons directory, create one in the project root directory.

### Enable the Plugin

Enable the plugin in the Godot editor

Open the Godot editor and go to Project Settings (Project → Project Settings).

Select the "Plugins" tab, find the "core_system" plugin, and change its status to "Enable".

#### Verify that the plugin is working properly

After the plugin is enabled, the IoC container will only be automatically initialized when the scene is running.

To verify that the plugin is working properly, you can create a simple test script and run the scene.

## Usage

### CreateNode-Automatic Node Creation

The `CreateNode` attribute allows the container to automatically create node instances and register them as Beans.

    // Automatically create a node and register it as a Bean
    [CreateNode]
    public class DebugOverlay : Control
    {
        public override void _Ready()
        {
            // Node creation logic
        }
    }

### GameObjectBean-Game Object Registration

The `GameObjectBean` attribute is used to register existing nodes in the scene as Beans.

    // Register the node as a Bean
    [GameObjectBean]
    public class Player : CharacterBody3D
    {
        [Autowired]
        private GameManager gameManager;
    
        public override void _Ready()
        {
            // gameManager is injected and can be used directly
        }
    }

### Component-Regular Class Objects

The `Component` attribute is used to register regular C# classes (non-`Node`) as Beans.

    // Register a regular class as a Bean
    [Component]
    public class GameManager
    {
        public void StartGame()
        {
            GD.Print("Game started!");
        }
    }
    
    // Use a custom name
    [Component("MainScoreService")]
    public class ScoreService
    {
        public int Score { get; private set; }
        
        public void AddScore(int points)
        {
            Score += points;
            GD.Print($"Score: {Score}");
        }
    }
    
### Dependency Injection

The `Autowired` attribute is used to mark dependencies that need to be injected.

    // Field injection
    [GameObjectBean]
    public class UIController : Control
    {
        // Basic injection
        [Autowired]
        private GameManager gameManager;
        
        // Property injection
        [Autowired]
        public ScoreService ScoreService { get; set; }
        
        // Injection with a name
        [Autowired("MainScoreService")]
        private ScoreService mainScoreService;
        
        public override void _Ready()
        {
            gameManager.StartGame();
            mainScoreService.AddScore(100);
        }
    }
    
    // Constructor injection (only applicable to regular classes, not Node)
    [Component]
    public class GameLogic
    {
        private readonly GameManager gameManager;
        private readonly ScoreService scoreService;
        
        // Constructor injection
        public GameLogic(GameManager gameManager, [Autowired("MainScoreService")] ScoreService scoreService)
        {
            this.gameManager = gameManager;
            this.scoreService = scoreService;
        }
        
        public void ProcessGameLogic()
        {
            gameManager.StartGame();
            scoreService.AddScore(50);
        }
    }
    
### Bean Naming

Beans can be named in several ways:

    // Use the class name by default
    [GameObjectBean]
    public class Player : Node3D { }
    
    // Custom name
    [GameObjectBean("MainPlayer")]
    public class Player : Node3D { }
    
    // Use the node name
    [GameObjectBean(ENameType.GameObjectName)]
    public class Enemy : Node3D { }
    
    // Use the field value
    [GameObjectBean(ENameType.FieldValue)]
    public class ItemSpawner : Node3D
    {
        [BeanName]
        public string SpawnerID = "Level1Spawner";
    }
    
The `ENameType` enumeration provides the following options:

- `Custom`: Custom name, default value
- `ClassName`: Use the class name as the Bean name
- `GameObjectName`: Use the node name as the Bean name
- `FieldValue`: Use the field value marked with BeanName as the Bean name

### Cross-Scene Persistence

The `PersistAcrossScenes` attribute is used to mark Beans that should not be destroyed when switching scenes.

    // Persistent game manager
    [PersistAcrossScenes]
    [Component]
    public class GameProgress
    {
        public int Level { get; set; }
        public int Score { get; set; }
    }
    
    // Persistent audio manager
    [PersistAcrossScenes]
    [GameObjectBean]
    public class AudioManager : Node
    {
        public override void _Ready()
        {
            // Ensure it is not destroyed with the scene
            GetTree().Root.CallDeferred("add_child", this);
        }
        
        public void PlaySFX(string sfxPath)
        {
            // Play sound effect logic
        }
    }
    
### Using the Container API

The container provides the following main methods for manually managing Beans:

    // Get the IoC instance
    var ioc = GetNode("/root/CoreSystem").GetIoC();
    
    // Get the Bean
    var player = ioc.GetBean<Player>();
    var namedPlayer = ioc.GetBean<Player>("MainPlayer");
    
    // Create a node Bean
    var enemy = ioc.CreateNodeAsBean<Enemy>(enemyResource, "Boss", spawnPoint.Position, Quaternion.Identity);
    
    // Delete a node Bean
    ioc.DeleteNodeBean<Enemy>(enemy, "Boss", true);
    
    // Clear Beans
    ioc.ClearBeans(); // Clear Beans in the current scene
    ioc.ClearBeans("MainLevel"); // Clear Beans in the specified scene
    ioc.ClearBeans(true); // Clear all Beans, including persistent Beans

## Inheritance and Interfaces Based on the Liskov Substitution Principle

The container supports loosely coupled dependency injection through interfaces or base classes:

    // Define an interface
    public interface IWeapon
    {
        void Attack();
    }
    
    // Bean implementing the interface
    [GameObjectBean("Sword")]
    public class Sword : Node3D, IWeapon
    {
        public void Attack()
        {
            GD.Print("Sword attack!");
        }
    }
    
    // Another implementation
    [GameObjectBean("Bow")]
    public class Bow : Node3D, IWeapon
    {
        public void Attack()
        {
            GD.Print("Bow attack!");
        }
    }
    
    // Inject through the interface
    [GameObjectBean]
    public class Player : CharacterBody3D
    {
        [Autowired("Sword")]
        private IWeapon meleeWeapon;
        
        [Autowired("Bow")]
        private IWeapon rangedWeapon;
        
        public void AttackWithMelee()
        {
            meleeWeapon.Attack();
        }
        
        public void AttackWithRanged()
        {
            rangedWeapon.Attack();
        }
    }

When multiple classes implement the same interface, you need to use names to distinguish them.

## Contact Information

If you have any questions, suggestions, or contributions, please submit feedback via GitHub Issues.
