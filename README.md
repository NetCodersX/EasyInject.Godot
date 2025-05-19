# Godot Easy Inject

[中文](README)|[English](README_en)

## 目录

- [介绍](#介绍)
- [为什么选择 Godot Easy Inject?](#为什么选择-godot-easy-inject)
- [安装与启用](#安装与启用)
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

## 介绍

Godot Easy Inject 是一个为 Godot 游戏引擎开发的依赖注入插件，帮助开发者更好地管理游戏组件间的依赖关系，使代码更加模块化、可测试和易于维护。

## 为什么选择 Godot Easy Inject?

在传统 Godot 开发中，获取节点引用通常需要使用 ``GetNode<T>(path)`` 或导出变量并在编辑器中手动拖拽。例如：

    // 传统方式获取节点引用
    public class Player : Node3D
    {
        // 需要在编辑器中手动拖拽或使用路径查找
        [Export]
        private InventorySystem inventory;
        
        private GameStateManager gameState;
        
        public override void _Ready()
        {
            // 硬编码路径获取节点
            gameState = GetNode<GameStateManager>("/root/GameStateManager");
        }
    }

这种方式在大型项目中会导致代码耦合度高、路径变更容易出错，且测试困难。
而使用 Godot Easy Inject，你只需添加几个特性标记，就能实现自动依赖注入：

    [GameObjectBean]
    public class Player : Node3D
    {
        [Autowired]
        private InventorySystem inventory;
        
        [Autowired]
        private GameStateManager gameState;
        
        public override void _Ready()
        {
            // 依赖已注入，直接使用
        }
    }

是否已经等不及想要尝试了呢？现在就开始吧！

## 安装与启用

### 安装插件

从 GitHub 下载插件

在 GitHub 仓库界面点击绿色的 Code 按钮，选择 Download ZIP，下载源码。

解压后将整个 EasyInject 文件夹复制到您的 Godot 项目的 addons 目录中。如果没有 addons 目录，请在项目根目录创建一个。

### 启用插件

在 Godot 编辑器中启用插件

打开 Godot 编辑器，进入项目设置（Project → Project Settings）。

选择 "插件" 选项卡（Plugins），找到 "core_system" 插件，将其状态改为 "启用"（Enable）。

#### 验证插件是否正常工作

插件启用后，只有在运行场景时才会自动初始化 IoC 容器。

要验证插件是否正常工作，您可以创建一个简单的测试脚本并运行场景。

## 使用方法

### CreateNode 节点自动创建

`CreateNode` 特性允许容器自动创建节点实例并注册为 Bean。

    // 自动创建节点并注册为Bean
    [CreateNode]
    public class DebugOverlay : Control
    {
        public override void _Ready()
        {
            // 节点创建逻辑
        }
    }

### GameObjectBean 游戏对象注册

`GameObjectBean `特性用于将场景中已存在的节点注册为 Bean。

    // 将节点注册为Bean
    [GameObjectBean]
    public class Player : CharacterBody3D
    {
        [Autowired]
        private GameManager gameManager;
    
        public override void _Ready()
        {
            // gameManager已注入，可直接使用
        }
    }

### Component 普通类对象

`Component` 特性用于注册普通 C# 类（非 `Node`）为 Bean。

    // 注册普通类为Bean
    [Component]
    public class GameManager
    {
        public void StartGame()
        {
            GD.Print("Game started!");
        }
    }
    
    // 使用自定义名称
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
    
### 依赖注入

`Autowired` 特性用于标记需要注入的依赖。

    // 字段注入
    [GameObjectBean]
    public class UIController : Control
    {
        // 基本注入
        [Autowired]
        private GameManager gameManager;
        
        // 属性注入
        [Autowired]
        public ScoreService ScoreService { get; set; }
        
        // 带名称的注入
        [Autowired("MainScoreService")]
        private ScoreService mainScoreService;
        
        public override void _Ready()
        {
            gameManager.StartGame();
            mainScoreService.AddScore(100);
        }
    }
    
    // 构造函数注入 (仅适用于普通类，不适用于Node)
    [Component]
    public class GameLogic
    {
        private readonly GameManager gameManager;
        private readonly ScoreService scoreService;
        
        // 构造函数注入
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
    
### Bean的命名

Bean 可以通过多种方式命名：

    // 默认使用类名
    [GameObjectBean]
    public class Player : Node3D { }
    
    // 自定义名称
    [GameObjectBean("MainPlayer")]
    public class Player : Node3D { }
    
    // 使用节点名称
    [GameObjectBean(ENameType.GameObjectName)]
    public class Enemy : Node3D { }
    
    // 使用字段值
    [GameObjectBean(ENameType.FieldValue)]
    public class ItemSpawner : Node3D
    {
        [BeanName]
        public string SpawnerID = "Level1Spawner";
    }
    
`ENameType` 枚举提供了以下选项：

- `Custom`：自定义名称，默认值
- `ClassName`：使用类名作为 Bean 名称
- `GameObjectName`：使用节点名称作为 Bean 名称
- `FieldValue`：使用标记了 BeanName 的字段值作为 Bean 名称

### 跨场景持久化

`PersistAcrossScenes` 特性用于标记在场景切换时不应被销毁的 Bean。

    // 持久化的游戏管理器
    [PersistAcrossScenes]
    [Component]
    public class GameProgress
    {
        public int Level { get; set; }
        public int Score { get; set; }
    }
    
    // 持久化的音频管理器
    [PersistAcrossScenes]
    [GameObjectBean]
    public class AudioManager : Node
    {
        public override void _Ready()
        {
            // 确保不随场景销毁
            GetTree().Root.CallDeferred("add_child", this);
        }
        
        public void PlaySFX(string sfxPath)
        {
            // 播放音效逻辑
        }
    }
    
### 使用容器 API

容器提供了以下主要方法，用于手动管理 Bean：

    // 获取IoC实例
    var ioc = GetNode("/root/CoreSystem").GetIoC();
    
    // 获取Bean
    var player = ioc.GetBean<Player>();
    var namedPlayer = ioc.GetBean<Player>("MainPlayer");
    
    // 创建节点Bean
    var enemy = ioc.CreateNodeAsBean<Enemy>(enemyResource, "Boss", spawnPoint.Position, Quaternion.Identity);
    
    // 删除节点Bean
    ioc.DeleteNodeBean<Enemy>(enemy, "Boss", true);
    
    // 清空Bean
    ioc.ClearBeans(); // 清空当前场景的Bean
    ioc.ClearBeans("MainLevel"); // 清空指定场景的Bean
    ioc.ClearBeans(true); // 清空所有Bean，包括持久化Bean

## 基于里氏替换原则的继承与接口

容器支持通过接口或基类实现松耦合依赖注入：

    // 定义接口
    public interface IWeapon
    {
        void Attack();
    }
    
    // 实现接口的Bean
    [GameObjectBean("Sword")]
    public class Sword : Node3D, IWeapon
    {
        public void Attack()
        {
            GD.Print("Sword attack!");
        }
    }
    
    // 另一个实现
    [GameObjectBean("Bow")]
    public class Bow : Node3D, IWeapon
    {
        public void Attack()
        {
            GD.Print("Bow attack!");
        }
    }
    
    // 通过接口注入
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

当多个类实现同一接口时，需要使用名称区分它们。

## 联系方式

如有任何问题、建议或贡献，请通过 GitHub Issues 提交反馈。
