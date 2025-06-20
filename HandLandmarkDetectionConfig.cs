// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe.Tasks.Vision.HandLandmarker;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    /// <summary>
    /// 手部特征点检测配置类
    /// 包含所有用于配置HandLandmarker任务的参数和设置
    /// </summary>
    public class HandLandmarkDetectionConfig
    {
        /// <summary>
        /// 硬件加速委托选项
        /// 决定模型运行在哪种硬件上(CPU/GPU)
        /// 在Windows和macOS上默认使用CPU，在其他平台(如移动设备)上默认使用GPU
        /// </summary>
        public Tasks.Core.BaseOptions.Delegate Delegate { get; set; } =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
          Tasks.Core.BaseOptions.Delegate.CPU;
#else
        Tasks.Core.BaseOptions.Delegate.GPU;
#endif

        /// <summary>
        /// 图像读取模式
        /// 确定如何从Unity纹理中读取图像数据
        /// CPUAsync: 异步从GPU读取到CPU (默认，性能较好)
        /// CPU: 同步从GPU读取到CPU
        /// GPU: 直接在GPU上处理
        /// </summary>
        public ImageReadMode ImageReadMode { get; set; } = ImageReadMode.CPUAsync;

        /// <summary>
        /// 运行模式
        /// IMAGE: 单张图像处理模式
        /// VIDEO: 视频处理模式(考虑帧之间的连续性)
        /// LIVE_STREAM: 实时流处理模式(异步处理，使用回调)
        /// 默认使用实时流模式，适合摄像头输入
        /// </summary>
        public Tasks.Vision.Core.RunningMode RunningMode { get; set; } = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

        /// <summary>
        /// 最大检测手数量
        /// 限制模型可以同时检测的手的数量，默认为1
        /// 增加此值可以检测多只手，但会增加计算量
        /// </summary>
        public int NumHands { get; set; } = 2;

        /// <summary>
        /// 手部检测最小置信度
        /// 手部检测器的最小置信度阈值，低于此值的检测结果将被过滤掉
        /// 取值范围0-1，默认0.5
        /// </summary>
        public float MinHandDetectionConfidence { get; set; } = 0.5f;

        /// <summary>
        /// 手部存在最小置信度
        /// 手部特征点检测器中对于手部存在的最小置信度阈值
        /// 取值范围0-1，默认0.5
        /// </summary>
        public float MinHandPresenceConfidence { get; set; } = 0.5f;

        /// <summary>
        /// 手部跟踪最小置信度
        /// 在视频/实时流模式下，用于决定是否继续跟踪先前检测到的手
        /// 取值范围0-1，默认0.5
        /// </summary>
        public float MinTrackingConfidence { get; set; } = 0.5f;

        /// <summary>
        /// 模型文件路径
        /// 指向手部特征点检测模型文件的路径
        /// </summary>
        public string ModelPath => "hand_landmarker.bytes";

        /// <summary>
        /// 创建并返回HandLandmarker选项
        /// 根据当前配置生成用于初始化HandLandmarker的选项对象
        /// </summary>
        /// <param name="resultCallback">结果回调函数，仅在LIVE_STREAM模式下使用</param>
        /// <returns>配置好的HandLandmarkerOptions对象</returns>
        public HandLandmarkerOptions GetHandLandmarkerOptions(HandLandmarkerOptions.ResultCallback resultCallback = null)
        {
            return new HandLandmarkerOptions(
              new Tasks.Core.BaseOptions(Delegate, modelAssetPath: ModelPath), // 基础选项，包含硬件加速设置和模型路径
              runningMode: RunningMode, // 运行模式
              numHands: NumHands, // 最大检测手数量
              minHandDetectionConfidence: MinHandDetectionConfidence, // 手部检测最小置信度
              minHandPresenceConfidence: MinHandPresenceConfidence, // 手部存在最小置信度
              minTrackingConfidence: MinTrackingConfidence, // 手部跟踪最小置信度
              resultCallback: resultCallback // 结果回调函数(实时流模式下需要)
            );
        }
    }
}
