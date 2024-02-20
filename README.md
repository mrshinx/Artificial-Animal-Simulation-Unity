# Artificial Animal Simulation with Unity
## About This Project
This is the project for my bachelor thesis at Rhein-Waal University of Applied Sciences. It revolves around building a 2D simulation with Unity in which artificial animals inhabit. 
## Current Features
Rabbits are trained using neural network technology to be able to perform basic action to survive such as eating, drinking and mating.

The rabbits are not hardcoded but instead are able to make decision on their own, to either seek food or water to fulfill their needs so that they can keep surviving. 
## Core Idea
Traditionally, to train similar behavior, the next objective is automatically given to the agent so that they know what to do or where to go next. This could prove to be a very efficient way of training.
However when it comes to animal, this is not "natural". The goal of this simulation is to create "believable" artifical animals that behave very similarly to their real-life counterpart. Thus, the agents are
only given information that their real-life counterpart also has access to, so that hopefully the decision-making process of the agent can be much more similar to real life.

The agents are given a "vision" in form of raycasts, some needs meters like hunger meter, thirst meter,... that decrease overtime. They have to find a way to fill these meter or be punished if not doing so.

## Result
The blue rabbits are male while the pink ones are female and the yellow ones are pregnant. Rabbits usually "hang out" near water source then occasionally go seek food then come back drinking when they are full.
![ezgif-7-1262922381](https://github.com/mrshinx/Artificial-Animal-Simulation-Unity/assets/45674057/e6fd6dd9-f51f-44d5-9893-96788ec65245)
