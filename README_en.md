# Godot Easy Inject

## Table of contents

- [introduce](#introduce)
- [Why choose Godot Easy Inject?](#Why choose Godot Easy Inject?)
- [Install and enable](#Install and enable)
- [使用方法](#使用方法)
  - [CreateNode 节点自动创建](#createnode-节点自动创建)
  - [GameObjectBean 游戏对象注册](#gameobjectbean-游戏对象注册)
  - [Component 普通类对象](#component-普通类对象)
  - [依赖注入](#依赖注入)
  - [Bean的命名](#bean的命名)
  - [跨场景持久化](#跨场景持久化)
  - [使用容器 API](#使用容器-api)
- [基于里氏替换原则的继承与接口](#基于里氏替换原则的继承与接口)
- [联系方式](#联系方式)

## introduce

Godot Easy Inject is a dependency injection plug-in developed for Godot game engine, helping developers better manage dependencies between game components, making the code more modular, testable and easy to maintain.

## Why choose Godot Easy Inject?

In traditional Godot development, getting node references usually requires using ``GetNode<T>(path)`` or export the variable and drag it manually in the editor. For example:

// Traditional way to get node references
public class Player: Node3D
    {
// You need to manually drag and drop in the editor or use path search
[Export]
private InventorySystem inventory;
        
private GameStateManager gameState;
        
public override void _Ready()
        {
// Hard-coded path to obtain node
gameState = GetNode<GameStateManager>("/root/GameStateManager");
        }
    }

This method will lead to high code coupling, path changes are prone to errors, and difficult to test in large projects.
With Godot Easy Inject, you only need to add a few feature markers to achieve automatic dependency injection:

[GameObjectBean]
public class Player: Node3D
    {
[Autowired]
private InventorySystem inventory;
        
[Autowired]
private GameStateManager gameState;
        
public override void _Ready()
        {
// Dependency has been injected, use directly
        }
    }

Can't wait to try it? Let’s start now!

## Install and enable

### Install plug-ins

Download the plugin from GitHub

Click the green Code button in the GitHub repository interface, select Download ZIP, and download the source code.

After unzipping, copy the entire EasyInject folder to the addons directory of your Godot project. If there is no addons directory, create one in the project root directory.

### Enable plug-ins

Enable plug-ins in Godot Editor

Open Godot Editor and go to Project Settings (Project → Project Settings).

Select the Plugins tab (Plugins), locate the "core_system" plugin, and change its status to "Enable".

#### Verify that the plugin works properly

When the plug-in is enabled, the IoC container will be automatically initialized only when the scenario is running.

To verify that the plugin is working properly, you can create a simple test script and run the scenario.

## How to use

### CreateNode node automatically creates

`CreateNode`Features allow containers to automatically create node instances and register as beans.

// Automatically create nodes and register as beans
[CreateNode]
public class DebugOverlay : Control
    {
public override void _Ready()
        {
// Node creation logic
        }
    }

### GameObjectBean GameObject Registration

`GameObjectBean `Features are used to register existing nodes in the scene as beans.

// Register node as bean
[GameObjectBean]
public class Player: CharacterBody3D
    {
[Autowired]
private GameManager gameManager;
    
public override void _Ready()
        {
// gameManager has been injected and can be used directly
        }
    }

### Component Normal class object

`Component`Features are used to register ordinary C# classes (non-`Node`) is a bean.

// Register the normal class as Bean
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
    
### Dependency injection

`Autowired`Features are used to mark dependencies that need to be injected.

// Field injection
[GameObjectBean]
public class UIController : Control
    {
// Basic injection
[Autowired]
private GameManager gameManager;
        
// Attribute injection
[Autowired]
public ScoreService ScoreService { get; set; }
        
// Injection with name
[Autowired("MainScoreService")]
private ScoreService mainScoreService;
        
public override void _Ready()
        {
gameManager.StartGame();
mainScoreService.AddScore(100);
        }
    }
    
// Constructor injection (only for ordinary classes, not for Node)
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
    
### Naming of Bean

Beans can be named in a number of ways:

// Use class name by default
[GameObjectBean]
public class Player : Node3D { }
    
// Custom name
[GameObjectBean("MainPlayer")]
public class Player : Node3D { }
    
// Use node name
[GameObjectBean(ENameType.GameObjectName)]
public class Enemy : Node3D { }
    
// Use field values
[GameObjectBean(ENameType.FieldValue)]
public class ItemSpawner: Node3D
    {
[BeanName]
public string SpawnerID = "Level1Spawner";
    }
    
`ENameType`The enumeration provides the following options:

- `Custom`: Custom name, default value
- `ClassName`: Use the class name as the bean name
- `GameObjectName`: Use the node name as the bean name
- `FieldValue`: Use the field value marked with BeanName as the Bean name

### Cross-scene persistence

`PersistAcrossScenes`Features are used to mark beans that should not be destroyed during scene switching.

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
public class AudioManager: Node
    {
public override void _Ready()
        {
// Make sure not to be destroyed with the scene
GetTree().Root.CallDeferred("add_child", this);
        }
        
public void PlaySFX(string sfxPath)
        {
// Play sound effect logic
        }
    }
    
### Using the Container API

Containers provide the following main methods for manually managing beans:

// Get IoC instance
var ioc = GetNode("/root/CoreSystem").GetIoC();
    
// Get Bean
var player = ioc.GetBean<Player>();
var namedPlayer = ioc.GetBean<Player>("MainPlayer");
    
// Create a node bean
var enemy = ioc.CreateNodeAsBean<Enemy>(enemyResource, "Boss", spawnPoint.Position, Quaternion.Identity);
    
// Delete node beans
ioc.DeleteNodeBean<Enemy>(enemy, "Boss", true);
    
// Clear Bean
ioc.ClearBeans(); // Clear the Beans in the current scene
ioc.ClearBeans("MainLevel"); // Clear the beans in the specified scene
ioc.ClearBeans(true); // Clear all beans, including persistent beans

## Inheritance and Interface Based on the Reich replacement principle

Containers support loosely coupled dependency injection through interfaces or base classes:

// Define the interface
public interface IWeapon
    {
void Attack();
    }
    
// Implement the interface bean
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
    
// Inject through interface
[GameObjectBean]
public class Player: CharacterBody3D
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

When multiple classes implement the same interface, they need to be distinguished by names.

## Contact information

If you have any questions, suggestions or contributions, please submit feedback through GitHub Issues.
