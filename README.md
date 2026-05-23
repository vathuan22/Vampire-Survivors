
# Class Survival

![Class Survival Logo](Assets/Image/MM.png)

**Class Survival** is a 2D survival arcade game built with Unity, evolved from a Vampire Survivors-style foundation into a class-based progression system. Players start with a basic character and a **Dagger**, then build power by selecting Classes, unlocking class-specific weapons, and promoting classes into stronger forms.

## Progression System

### How it works

- **Start**: Begin as a basic character with a **Dagger**.
- **First Level Up**: Choose your first **Class** (Warrior, Mage, or Ranger).
- **Subsequent Level Ups**: Select weapons belonging to your active class.
- **Promotion**: When all weapons for a class are maxed, you can **Promote** the class into a stronger tier.
- **Multi-Class**: There is a small chance (~15%) on each level-up to unlock a **2nd** or **3rd** class slot.
- **Class Limit**: Maximum of **3 classes** per run.

### Available Classes

| Class   | Weapons | Promotion |
|---------|---------|-----------|
| Warrior | Blade weapons (2-3) | Blade Master |
| Mage    | Magic spells (2-3) | Arch Mage |
| Ranger  | Ranged attacks (2-3) | Sniper Master |
tutututut

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Playing the Game](#playing-the-game)
  - [Controls](#controls)
  - [Gameplay Overview](#gameplay-overview)
- [Development](#development)
  - [Project Structure](#project-structure)
- [License](#license)
- [Contact](#contact)

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

What things you need to install the software and how to install them:

```bash
Unity Editor (2021.3 LTS or newer)
Git
```

### Installation

A step-by-step series of examples that tell you how to get a development environment running:

1. Clone the repository:
   ```bash
   git clone https://github.com/2312765-spec/Vampire-Survivors.git
   ```
2. Open Unity Hub and add the cloned repository folder.
3. Select the correct Unity version and open the project.
4. Open the **Main** scene and press Play.

## Setting Up Classes in the Editor

After opening the project in Unity:

1. Locate the **ClassManager** component in the scene (or add it to the GameManager object).
2. In the Inspector, assign the **ClassData** assets from `Assets/Classes/` to the **All Classes** list:
   - `Warrior.asset`
   - `Mage.asset`
   - `Ranger.asset`
3. For each **ClassData** asset, assign the **class weapons** (Weapon components from the scene).
4. Optionally assign **promotion class** references (e.g., Warrior → WarriorTier2).

## Playing the Game

### Controls

- **Arrow Keys / WASD:** Move the character
- **Mouse:** Choose your upgrade at each level up.
- **Escape:** Pause the game.

### Gameplay Overview

Survive as long as possible by defeating waves of enemies. On each level-up, choose:
- A **new class** (if you haven't chosen one yet, or by rare chance)
- A **class weapon** upgrade or unlock
- A **class promotion** (when all class weapons are maxed)

Collect experience to level up, coins to purchase stat upgrades, and stay alive!

![Gameplay Screenshot](Assets/Image/Game.png)

## Development

### Project Structure

The project follows this structure to ensure ease of navigation and development:

- **Assets/**
  - **Animations/**: Contains all in-game animations.
  - **Classes/**: ClassData ScriptableObject assets (Warrior, Mage, Ranger, etc.).
  - **Prefabs/**: Main enemy and pickup objects.
  - **Scenes/**: Unity scenes including the main game and menus.
  - **Scripts/**: Game logic scripts.
    - `ClassData.cs` — ScriptableObject defining a class (name, weapons, promotion).
    - `ClassManager.cs` — Runtime manager for the player's class progression.
    - `ExperienceLevelController.cs` — Level-up logic using the class system.
    - `LevelUpSellectionButton.cs` — UI button supporting class/weapon/promotion choices.

![Feature Addition Flowchart](Assets/Image/EMM.png)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details.

## Contact

- **Developers:** Antonis Kyriakou, George Constantinou and Dimitris Achilleos
- **Project Link:** https://github.com/2312765-spec/Vampire-Survivors
