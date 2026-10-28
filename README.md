# Shipyard Surfers

Endless runner set in a port and shipyard, controlled with body gestures via webcam (or Xbox 360 Kinect). Built by [KNI Politechniki Morskiej w Szczecinie](https://knipm.edu.pl/project/shipyard-surfers/) for the II Piknik Naukowy Morskiego Centrum Nauki.

![Shipyard Surfers gameplay](Readme/uno.jpg)

## Features

- Steer a boat across 3 lanes, dodge port and pirate obstacles
- Coins and power-ups: coin magnet, super jump, double points
- Procedurally generated routes built from segments
- Real-time pose tracking with YOLO Pose (ONNX, Unity Inference Engine), auto GPU/CPU backend selection
- Per-player calibration
- Local web server with a live leaderboard and camera feed (WebSocket)

## Controls

| Gesture | Action |
|---|---|
| Wave hand sideways | Change lane |
| Raise both hands | Jump |
| Quickly lower both hands | Slide |

## Requirements

- **Unity 6000.3.17f1** (Unity 6.3), install this exact version via Unity Hub
- Universal Render Pipeline (URP 17.3.0, included via Package Manager)
- Webcam, or an Xbox 360 Kinect (on Windows 11, Memory Integrity (HVCI) must be turned off for the Kinect driver)

## Getting Started

```bash
git clone https://github.com/KNI-PM-Szczecin/ShipyardSurfers.git
```

1. Open Unity Hub → **Add** → select the cloned folder.
2. Open the project with Unity **6000.3.17f1**.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.

## Scenes

| Scene | Description |
|---|---|
| `MainMenu` | Main menu |
| `GameScene` | Main gameplay |
| `Calibration` | Motion-control calibration |
| `ObstacleShowcase` | Obstacle preview / testing |

## Tech Stack

Unity 6 (URP) · C# · YOLO Pose / ONNX / Unity Inference Engine · Python (model export, sprite generation in `Tools/`) · WebSockets

## Credits

- [KNI PM Szczecin](https://github.com/KNI-PM-Szczecin)
- 3D models: [Kenney](https://kenney.nl/)
