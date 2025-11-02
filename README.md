# Planetfall: Bastion of Genesis
# (Documentation and Title will change in future)


## Overview

Planetfall: Bastion of Genesis is a real-time strategy (RTS) / city-builder game where the player is tasked with building and managing a futuristic colony on a new planet. The game is built in Unity and features a robust set of systems for player interaction, resource management, and strategic decision-making.

## Core Systems

### Player Controller

The `PlayerController` is the central point of interaction for the player. It handles:

*   **Building Placement:** The player can select a building from the UI and place it on the map. The controller provides a visual preview of the building and ensures it is placed on valid ground.
*   **Building Selection:** The player can select existing buildings to view their stats or perform actions.
*   **Input Management:** The game uses Unity's new Input System to handle all player input, including mouse clicks and camera controls.

### Building System

The `BuildingManager` is responsible for all building-related logic. It handles:

*   **Placement:** When the player places a building, the `BuildingManager` checks if there are enough resources and builders to construct it.
*   **Resource Consumption:** If the placement is valid, the `BuildingManager` deducts the resource cost and assigns the required builders.
*   **Building Data:** Each building is defined by a `BuildingData` asset, which contains information about its name, prefab, resource cost, and builders required.

### Resource System

The `ResourceManager` is a singleton that manages all player resources. It features:

*   **Resource Tracking:** The manager keeps track of the current amount and capacity of each resource.
*   **Resource Management:** It provides methods to add, remove, and check the amount of each resource.
*   **Event-Driven:** The `ResourceManager` fires an event whenever a resource amount changes, allowing the UI to update automatically.

### Worker System

The `WorkerManager` is responsible for managing all workers in the game. It handles:

*   **Worker Training:** The player can train new workers, which are added to the available worker pool.
*   **Worker Assignment:** When a building is constructed, the `WorkerManager` assigns the required workers, making them unavailable for other tasks.
*   **Worker Tracking:** The manager keeps track of the number of available workers of each type.

### UI System

The `UIManager` is a singleton that manages all UI elements in the game. It handles:

*   **Panel Management:** The manager controls the visibility of all UI panels, including the building selection panel and the building information panel.
*   **Data Display:** The UI displays real-time information about the player's resources, workers, and selected buildings.

### Camera System

The `CameraController` provides a flexible and intuitive camera system. It supports:

*   **Keyboard Movement:** The camera can be moved with the WASD keys.
*   **Edge Panning:** The camera will pan when the mouse is moved to the edge of the screen.
*   **Mouse Drag:** The camera can be dragged by holding down the middle mouse button.
*   **Zoom:** The camera can be zoomed in and out with the mouse wheel.

## How to Play

1.  **Select a Building:** Click on the building selection button to open the building menu.
2.  **Place the Building:** Choose a building and place it on the map. The required resources and builders will be consumed.
3.  **Manage Resources:** Keep an eye on your resources and build resource-generating structures to expand your colony.
4.  **Train Workers:** Train more workers to construct new buildings and expand your workforce.
5.  **Expand and Conquer:** Continue to expand your base and manage your resources to build a thriving colony.
