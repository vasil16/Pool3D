🎱 3D Pool Game – Logic & Physics Engine
A realistic 3D pool (billiards) game focused on gameplay logic, physics simulation, and AI shot evaluation. Designed and implemented from scratch using C# and Unity, with modular architecture and reusable logic components.

🚀 Features
🎯 Physics-Based Ball Movement

Custom ghost ball system for predicting ball trajectories

Dynamic collision and reflection logic using vector math

Realistic ball speed decay and table edge bounce behavior

🧠 AI Shot Evaluation System

Evaluates all valid object ball–pocket combinations

Scores and ranks shots based on angle, difficulty, and position

Enables CPU to take strategic, human-like shots

🔄 Turn Logic & Game Rules

Supports Player vs Player and Player vs CPU modes

Full rule engine: foul detection, ball-in-hand, win/loss conditions

Turn switching and valid shot enforcement

🎱 Spin & Cue Mechanics

Realistic cue direction and spin input logic

Spin influence alters cue ball post-shot trajectory

🧩 Architecture & Code Focus
🔹 Separation of Concerns
All core gameplay logic (physics, rules, AI) is fully decoupled from rendering and animations.

🔹 Modular Codebase
Clean, maintainable scripts for ball movement, collision detection, turn system, and AI decision-making.

🔹 Math & Vector-Based Simulation
Used raycasting, dot product, and ghost ball geometry for predictive logic and AI calculations.

📂 Technologies Used
Language: C#

Engine: Unity (Used only for scene setup and physics environment)

Math/Simulation: Custom vector-based calculations and trajectory prediction

Version Control: Github

🧠 What I Learned
Implemented advanced gameplay logic independently of visuals

Applied real-world physics and math for simulation-based problem-solving

Built a robust and testable codebase suitable for multiplayer and AI extension

Practiced clean code principles, modularity, and debugging in a real-time environment

🛠️ Future Improvements
Add UI/UX for player feedback (shot suggestions)

Improve AI strategy with simulation-based outcomes

Extend to online multiplayer with Photon Fusion(under progress)

📌 Why This Project Matters
This project demonstrates my ability to:

Architect real-time, logic-heavy systems from scratch

Solve mathematical and gameplay problems using clean code

Apply software engineering principles in game/simulation environments

Build production-quality systems beyond basic CRUD applications  
