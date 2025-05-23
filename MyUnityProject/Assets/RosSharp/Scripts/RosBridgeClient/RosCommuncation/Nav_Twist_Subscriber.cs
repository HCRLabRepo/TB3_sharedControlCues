using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class Nav_Twist_Subscriber : UnitySubscriber<MessageTypes.Geometry.Twist>
    {
        //public RosConnector rosConnector;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        private bool isMessageReceived;
        
        private float angularSpeed = 0.0f;
        private float linearSpeed = 0.0f;
        private float xForce = 0.0f;
        private float zForce = 0.0f;
        public float smoothFactor = 0.2f;
        
        private double[] currentForce = {0.0,0.0,0.0};
        private double[] currentposition = {0.0,0.0,0.0};
        
        [SerializeField] float forceThreadholdUp = 1.5f;
        [SerializeField] float forceThreadholdLow = 1f; 
        [SerializeField] private float maxDeltaAngular = 0.1f;
        [SerializeField] private float maxDeltaLinear = 0.2f;
        [SerializeField] private float minForce = 0.5f;        
        [SerializeField] private float maxForce = 2f; 
        
        protected override void ReceiveMessage(MessageTypes.Geometry.Twist message)
        {
            linearVelocity = ToVector3(message.linear).Ros2Unity();
            angularVelocity = -ToVector3(message.angular).Ros2Unity();
            isMessageReceived = true;
        }
        
        private static Vector3 ToVector3(MessageTypes.Geometry.Vector3 geometryVector3)
        {
            return new Vector3((float)geometryVector3.x, (float)geometryVector3.y, (float)geometryVector3.z);
        }

        // Update is called once per frame
        void Update()
        {
            HapticPlugin.getPosition("Default Device", currentposition);
            //check if the message is received
            if (angularVelocity.y == 0.0f && linearVelocity.z == 0.0f)
            {
                DataManager.Instance.nav_vel_signal = false;
            }else
            {
                DataManager.Instance.nav_vel_signal = true;
            }
            
            float alpha = DataManager.Instance.alpha;
            float currentAngularSpeed = angularVelocity.y;
            float currentLinearSpeed = linearVelocity.z;
            
            float deltaLinear = currentLinearSpeed - linearSpeed;
            float deltaAngular = currentAngularSpeed - angularSpeed;
            //Debug.Log("deltaLinear: " + deltaLinear);
            
            
            //CheckUserInputByForce();
            OptimalizeForce(deltaLinear, deltaAngular);
            GetHumanInput();
            
            if(DataManager.Instance.isWithAssitanceCues && DataManager.Instance.isWithGuidanceForce) 
                SetGuidanceForce(xForce,zForce,alpha);
            
            angularSpeed = currentAngularSpeed;
            linearSpeed = currentLinearSpeed;
            
        }

        private void GetHumanInput()
        {
            Vector3 position = new Vector3((float)currentposition[0], (float)currentposition[1], (float)currentposition[2]);
            //Debug.Log(position);
        }
        
        
        private void OptimalizeForce(float deltaLinear, float deltaAngular) 
        {
            deltaAngular = Mathf.Clamp(deltaAngular, -maxDeltaAngular, maxDeltaAngular);
            deltaLinear = Mathf.Clamp(deltaLinear, -maxDeltaLinear, maxDeltaLinear);
            
            // linear mapping of x axis 
            xForce = Mathf.Lerp(
                minForce, 
                maxForce, 
                (deltaAngular + maxDeltaAngular) / (2 * maxDeltaAngular) // nomalize deltaAngular to [0, 1]
            );
            // linear mapping of z axis
            zForce = Mathf.Lerp(
                minForce, 
                maxForce, 
                (deltaLinear + maxDeltaLinear) / ( 2 * maxDeltaLinear) // nomalize deltaLinear to [0, 1]
            );
        }

        private void SetGuidanceForce(float xForce, float zForce, float alpha) 
        {
            if (!DataManager.Instance.isFullControl && DataManager.Instance.taskStart)
            {
                
                float forward_backward = - linearVelocity.z;
                float Left_Right = - angularVelocity.y;
                
                // set force direction by angular and linear velocity
                double[] forceDirection = new double[3] {Left_Right, 0.0, forward_backward};
            
                float force = new Vector3(xForce, 0, zForce).magnitude;
                if (force > maxForce)
                {
                    Debug.Log("突变的力：" + force);
                    force *= smoothFactor;
                }
                if (linearVelocity.z < 0.0f)
                {
                    force *= 0.5f;
                    HapticPlugin.setConstantForceValues("Default Device", forceDirection, force);
                }
                else if(Mathf.Abs(linearVelocity.z) > 0.15f)
                {
                    force *= 0.7f;
                    HapticPlugin.setConstantForceValues("Default Device", forceDirection, force);
                }
                else
                {
                    HapticPlugin.setConstantForceValues("Default Device", forceDirection, force);
                }
                
                
            }
            
        }
        
        
        
        // private void SetGuidanceForce(float xForce, float zForce, float alpha) 
        // {
        //     if (!DataManager.Instance.isFullControl && DataManager.Instance.taskStart)
        //     {
        //         
        //         float forward_backward = - linearVelocity.z;
        //         float Left_Right = - angularVelocity.y;
        //         
        //         // set force direction by angular and linear velocity
        //         double[] forceDirection = new double[3] {Left_Right, 0.0, forward_backward};
        //     
        //         float force = new Vector3(xForce * alpna, 0, zForce * alpha) + new Vector3(hxForce * (1-alpha), 0, hzForce* (1-alpha));
        //         if (force > maxForce)
        //         {
        //             Debug.Log("突变的力：" + force);
        //             force *= smoothFactor;
        //             
        //         }
        //             
        //         HapticPlugin.setConstantForceValues("Default Device", forceDirection, force);
        //     }
        // }
     
    }
}

