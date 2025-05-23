# Mobile-Robot-Project

This repository contains a Unity-based robot simulation integrated with a ROS backend. It demonstrates distributed control, sensor communication, and real-time interaction between Unity and ROS.

---

## 🚀 Getting Started

Follow the steps below to set up the project on your local machine.

### 📥 1. Clone the Repository

```bash
git clone https://github.com/imorange/Mobile-Robot-Project.git
cd Mobile-Robot-Project
```
### 🎮 2. Open the Unity Project
- Open Unity Hub

- Click Add → Add Existing Project

- Select the folder Mobile-Robot-Project/myunityproject

- Open it with Unity 2021.3+ (or your required version)

### 🤖 3. Set Up the ROS Package
```bash
# Copy ROS package into your catkin workspace
cp -r Ros_Package ~/catkin_ws/src/
cd ~/catkin_ws
catkin_make
source devel/setup.bash
```

