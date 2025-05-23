#!/usr/bin/env python

import rospy
from geometry_msgs.msg import Twist
from std_msgs.msg import Float32
from std_msgs.msg import Bool
from sensor_msgs.msg import LaserScan
import numpy as np

FRONT_ANGLE = 0
LEFT_ANGLE = 90
RIGHT_ANGLE = 270
BACK_ANGLE = 180


class SharedControl:
    def __init__(self):
        # Initialize the node, subscribers, publisher, and rate
        rospy.init_node('shared_control', anonymous=True)
        
        self.navigation_cmd = Twist()
        self.haptic_cmd = Twist()

        self.lidar_data = LaserScan()
        self.isTeleop = Bool()
        self.isAuto = Bool()
        self.isFullControl = Bool()

        self.arbitration_value = 0
        
        self.navigation_sub = rospy.Subscriber('/nav_vel', Twist, self.navigation_callback)
        self.haptic_sub = rospy.Subscriber('/haptic_vel', Twist, self.haptic_callback)
        self.lidar_sub = rospy.Subscriber('/scan', LaserScan, self.lidar_callback)
        self.teleop_sub = rospy.Subscriber('/tele_code', Bool, self.teleop_callback)
        self.auto_sub = rospy.Subscriber('/auto_code', Bool, self.auto_callback)
        self.full_control_sub = rospy.Subscriber('/full_conrtol_code', Bool, self.full_control_callback)
        # self.switch_mode_sub = rospy.Subscriber('/switch_mode', Bool, self.switch_mode_callback)

        self.cmd_pub = rospy.Publisher('/cmd_vel', Twist, queue_size=10)
        self.arbitration_value_pub = rospy.Publisher('/arbitration_value', Float32, queue_size=10)
        self.input_cmd_vel_pub = rospy.Publisher('/input_value', Twist, queue_size=10)
        
        self.rate = rospy.Rate(10)
    
    
    def lidar_callback(self, msg):
        self.lidar_data = msg
    
        
    def lidar_data_processing(self, data):

        if len(data.ranges) != 360:
            rospy.logwarn("Expected 360 ranges, but got {}".format(len(data.ranges)))
            return 0.0  
        
        front_safety = self.areas_processing(data, FRONT_ANGLE)
        left_safety = self.areas_processing(data, LEFT_ANGLE)
        right_safety = self.areas_processing(data, RIGHT_ANGLE)
        back_safety = self.areas_processing(data, BACK_ANGLE)

        return front_safety

    def areas_processing(self, data, angle):
        # get the front 45 degrees of the lidar data
        # front_indices = list(range(315, 360)) + list(range(0, 46))  # [315, 359] + [0, 45]
        area_indices = list(range(angle-45, angle+45))
        
        # get the front ranges
        area_ranges = [data.ranges[i] for i in area_indices]
        
        # filter invalid values（inf or NaN）
        valid_area_ranges = [r for r in area_ranges if not (np.isinf(r) or np.isnan(r)) and r != 0]
        
        if len(valid_area_ranges) == 0:    
            safety = 1.0
        else:
            # find the minimum distance in the front
            min_area_distance = min(valid_area_ranges)
            # print("Min front distance: {}".format(min_front_distance))

            current_speed = self.navigation_cmd.linear.x

            # safe distance is proportional to the current speed
            safe_distance = max(0.2, 0.5 * current_speed) # minimum safe distance is 0.2 meters

            max_distance = data.range_max # maximum range of the lidar equals to 3.5 meters 
            
            if min_area_distance <= safe_distance:
                safety = 0.0
            else:
                # linearly interpolate the safety value
                safety = min(1.0, (min_area_distance - safe_distance) / (max_distance - safe_distance))
        print("Safety value: {}".format(safety))

        return safety

    def density_processing(self, data):
        pass

    def teleop_callback(self, msg):
        self.isTeleop = msg.data

    def auto_callback(self, msg):
        self.isAuto = msg.data
    
    def full_control_callback(self, msg):
        self.isFullControl = msg.data
        
    def navigation_callback(self, msg):
        self.navigation_cmd = msg

    def haptic_callback(self, msg):
        self.haptic_cmd = msg
        
    def run(self):
        while not rospy.is_shutdown():
            # print(self.isTeleop, self.isAuto, self.isFullControl)
            # Process the lidar data
            if(self.isFullControl == True and self.isTeleop == False and self.isAuto == False):
                alpha = 0
            elif(self.isAuto == True and self.isTeleop == False and self.isFullControl == False):
                alpha = 1
            else:
                alpha = self.lidar_data_processing(self.lidar_data)
                 
            safty = self.lidar_data_processing(self.lidar_data)

            combined_cmd = Twist()
            if(self.isFullControl == True and safty > 0.2):
                combined_cmd.linear.x = 0.6 * self.haptic_cmd.linear.x
                combined_cmd.angular.z = 0.6 * self.haptic_cmd.angular.z
            else:
                combined_cmd.linear.x = alpha * (self.navigation_cmd.linear.x) + (1 - alpha) * self.haptic_cmd.linear.x
                combined_cmd.angular.z = alpha * (self.navigation_cmd.angular.z) + (1 - alpha) * self.haptic_cmd.angular.z
            
            self.cmd_pub.publish(combined_cmd)
            self.arbitration_value_pub.publish(alpha)
            # self.input_cmd_vel_pub.publish(self.haptic_cmd)
            self.rate.sleep()


if __name__ == '__main__':
    try:
        shared_control = SharedControl()
        shared_control.run()
    except rospy.ROSInterruptException:
        pass