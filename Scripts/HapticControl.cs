using UnityEngine;
using System;
using System.Collections.Generic;

namespace RosSharp.RosBridgeClient
{
    public class HapticControl : UnityPublisher<MessageTypes.Geometry.Twist>
    {
        public HapticPlugin hapticPlugin; 
        public LaserScanReader laserScanReader;
        public GameObject goalManager;
        public RosConnector rosConnector;

        public double[] position3 = {0.0,0.0,0.0};
        public double[] velocity3 = {0.0,0.0,0.0};
        public double[] jointAngles = {0.0,0.0,0.0};
        public double[] gimbalAngles = {0.0,0.0,0.0};

        private MessageTypes.Geometry.Twist message;
        private bool activeState = false;  
        private bool userControlSignal = false;
        
        // private Dictionary<string, object> directions;     

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }
        

        private void Update()
        {
            hapticPlugin.UpdateButtonStatus();
            CheckUserInput();
            UpdateMessage();
        }

        private void InitializeMessage()
        {   
            HapticPlugin.setAnchorPosition("Default Device", new double[] {0,0,0});
            message = new MessageTypes.Geometry.Twist();
            message.linear = new MessageTypes.Geometry.Vector3();
            message.angular = new MessageTypes.Geometry.Vector3();
            initial_cmd_vel(message);
            Publish(message);
        }

        private void UpdateMessage()
        {
           if (hapticPlugin != null && activeState)
            {
                HapticPlugin.getPosition("Default Device", position3);
                HapticPlugin.getVelocity("Default Device", velocity3);
                HapticPlugin.getJointAngles("Default Device", jointAngles, gimbalAngles);
                //Debug.Log(jointAngles[2]);

                var result = laserScanReader.caculateMinDistance();
                bool[] directions = result.Item1;
                float distance = result.Item2;

                HandleDirection(directions[0], new double[] { 0, 0, 20 }, jointAngles, position3[2], position3[0], false, distance);  // 前
                HandleDirection(directions[1], new double[] { 0, 0, -20 }, jointAngles, position3[2], position3[0], true, distance); // 后
                //HandleDirection(directions[2], new double[] { 40, 0, 0 }, velocity3, position3[2], position3[0], false, 0.1);  // 左
                //HandleDirection(directions[3], new double[] { -40, 0, 0 }, velocity3, position3[2], position3[0],false, 0.1);  // 右

            }
            
        }

        private void HandleDirection(bool direction, double[] springValues, double[] jointAngles, double linearValue, double angularValue, bool isrear, double distance) {
           
            //ScreenFlash sf = GetComponent<ScreenFlash>();
            double force = CalculateForce(distance);
            
            // set spring force
            if (direction && DataManager.Instance.isWithCues) {
                HapticPlugin.setSpringValues("Default Device", springValues, force);
            }
            // 这里的阈值很关键，需要根据实际情况调整
            if (jointAngles[2] < 0.35 || jointAngles[2] > 0.5 || jointAngles[0] < -0.05 || jointAngles[0] > 0.05) {
                
                message.linear = GetGeometryVector3(linearValue);  // 映射到z轴或其他轴
                message.angular = GetGeometryRotate(angularValue); // 映射到x轴或其他轴
            }else{
                initial_cmd_vel(message);
            }
            
            Publish(message);
        }

        private double CalculateForce(double distance) {
            double minDistance = 0.1; // 距离的最小值
            double maxDistance = 6.0; // 距离的最大值
            double maxForce = 0.05;    // 力的最大值
            double minForce = 0.0;    // 力的最小值

            // 限制距离范围
            distance = Math.Clamp(distance, minDistance, maxDistance);

            // 使用指数函数计算力
            double force = maxForce * Math.Exp(-distance);

            // 限制最小值
            return Math.Clamp(force, minForce, maxForce);
        }

        private static void initial_cmd_vel(MessageTypes.Geometry.Twist message)
        {
            MessageTypes.Geometry.Vector3 init_vector3 = new MessageTypes.Geometry.Vector3();
            init_vector3.x = 0;
            init_vector3.y = 0;
            init_vector3.z = 0;
            message.linear = init_vector3;
            message.angular = init_vector3;
        }

        private static MessageTypes.Geometry.Vector3 GetGeometryVector3(double vec)
        {
            MessageTypes.Geometry.Vector3 geometryVector3 = new MessageTypes.Geometry.Vector3();
            geometryVector3.x = (-vec / 100);
            geometryVector3.y = 0;
            geometryVector3.z = 0;
            return geometryVector3;
        }

        private static MessageTypes.Geometry.Vector3 GetGeometryRotate(double vec)
        {
            MessageTypes.Geometry.Vector3 geometryVector3 = new MessageTypes.Geometry.Vector3();
            geometryVector3.x = 0;
            geometryVector3.y = 0;
            geometryVector3.z = vec / 50;
            return geometryVector3;
        }

        public MessageTypes.Geometry.Twist GetMessage()
        {
            return message;
        }

        public void SetVibrationForce(double force)
        {
            HapticPlugin.setVibrationValues("Default Device", new double[] {0,1,0}, force, 50, 0.5);
        }

        public void SetDeviceActive()
        {   
            activeState = true;
            GameObject image = GameObject.Find("WarnBroad");
            //GameObject goalManager = GameObject.Find("GoalManager");
            goalManager.SetActive(true);
            image.SetActive(false);
            
        }
        
        private void CheckUserInput()
        {
            if (DataManager.Instance.isWithCues)
            {
                bool limitX = position3[0] < -0.15 || position3[0] > 0.15;
                //bool limitY = jointAngles[2] < 0.0 || jointAngles[2] > 1.0;
                if (limitX)
                {
                    userControlSignal = true;
                }
                else
                {
                    userControlSignal = false;
                }
            
            
                bool isXinput = jointAngles[0] < -0.05 || jointAngles[0] > 0.05;
                bool isYinput = jointAngles[2] < 0.34 || jointAngles[2] > 0.48;
                if ((isXinput || isYinput ))
                {
                    rosConnector.GetComponent<IsTelePublisher>().teleOperation = true;
                    //rosConnector.GetComponent<IsAutoPublisher>().autoOperation = true;
                }
                // else if (userControlSignal)
                // {
                //     rosConnector.GetComponent<IsTelePublisher>().teleOperation = true;
                //     rosConnector.GetComponent<IsAutoPublisher>().autoOperation = false;
                // }
                else
                {
                    rosConnector.GetComponent<IsTelePublisher>().teleOperation = false;
                    //rosConnector.GetComponent<IsAutoPublisher>().autoOperation = true;
                }
            }
            
                
        }

    }
}
