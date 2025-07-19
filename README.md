🎱 3D Pool Game – Logic & Physics Engine

<img width="933" height="514" alt="Screenshot 2025-07-20 at 12 32 17 AM" src="https://github.com/user-attachments/assets/ebb1c5b7-7db4-4546-bfcd-7ac22b4c4b73" />
<img width="935" height="512" alt="Screenshot 2025-07-20 at 12 32 33 AM" src="https://github.com/user-attachments/assets/c51cc7c5-72cf-412e-9cac-4cb3ed4d0e40" />

A realistic 3D pool (billiards) game focused on gameplay logic, physics simulation, and AI shot evaluation. Designed and implemented from scratch using C# and Unity, with modular architecture and reusable logic components.



🚀 Features
🎯 Physics-Based Ball Movement

Custom ghost ball system for predicting ball trajectories

Dynamic collision and reflection logic using vector math

<img width="2436" height="1125" alt="IMG_5394" src="https://github.com/user-attachments/assets/8fd67804-bf2b-49f9-b310-2b304868a251" />

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

Extend to online multiplayer with Photon Fusion(in progress)

<img width="935" height="512" alt="Screenshot 2025-07-20 at 12 32 33 AM" src="https://github.com/user-attachments/assets/712f4fb3-875a-4932-9a09-798823649dad" />

<img width="968" height="541" alt="Screenshot 2025-07-20 at 12 32 58 AM" src="https://github.com/user-attachments/assets/c858762a-3f6f-4efb-89dc-c9f44942a1ea" />

📌 Why This Project Matters
This project demonstrates my ability to:

Architect real-time, logic-heavy systems from scratch

Solve mathematical and gameplay problems using clean code

Apply software engineering principles in game/simulation environments

Build production-quality systems beyond basic CRUD applications  
